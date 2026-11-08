using System;
using System.Collections.Generic;
using System.Linq;
using Chaos.Gameplay.Interaction;
using Chaos.Gameplay.Items;
using Chaos.Gameplay.RunTeam;
using Chaos.MVP.Model.Gameplay;
using RunMap;
using UnityEngine;

public static partial class RunEncounter
{
    private static readonly HashSet<string> DefeatedEnemyIds = new HashSet<string>(StringComparer.Ordinal);

    internal static ActiveRunSave CaptureCheckpoint(uint departure)
    {
        var map = NaturalMapRuntime.Current;
        var player = PlayerSpawner.CurrentPlayer;
        if (map == null || player == null || !OwnsRuntimeObject(player) || Team.Count == 0) return null;
        var health = player.GetComponent<PlayerHealth>();
        var shape = player.GetComponent<Collider2D>();
        if (health == null || shape == null) return null;
        var save = new ActiveRunSave
        {
            format = ActiveRunSave.CurrentFormat, departure = departure, seed = _runSeed, stage = CurrentSceneIndex,
            era = FactionService.ResolveEra(), generatorVersion = map.Version, configFingerprint = map.ConfigFingerprint,
            layoutSignature = map.Signature(true, false), team = new List<uint>(Team), bag = RunInventory.CaptureSlots(),
            playerFeet = RunMapPhysics.Feet(shape), campUsed = CampService.CampUsed, timeMinutes = DayNightClock.RunMinutes,
            stagesPassed = StagesPassed, kills = RunKills, highestDamage = HighestRunDamage, playSeconds = RunPlaySeconds,
            killSequence = _killSeq, startingImportant = StartingImportantItems.OrderBy(id => id).ToList(),
            defeatedEnemies = DefeatedEnemyIds.OrderBy(id => id, StringComparer.Ordinal).ToList(),
            explored = RunMapExplorationService.Plan == map ? RunMapExplorationService.Fog?.CaptureExplored() : null,
            outcome = RunCleared ? 1 : Defeated ? 2 : 0
        };
        foreach (uint id in Team)
        {
            var member = new RunMemberSave { id = id };
            if (id == Team[0]) { member.hp = health.Hp; member.downed = health.IsDowned; }
            else
            {
                var ally = AllyController.All.FirstOrDefault(value => value != null && value.memberId == id && OwnsRuntimeObject(value.gameObject));
                if (ally == null) return null;
                member.hp = ally.Hp;
                member.downed = ally.IsDowned;
            }
            if (!RunTeamProgress.TryRead(id, out var growth)) return null;
            growth.Capture(member);
            save.members.Add(member);
        }
        // 同一帧内最后一名队员可能刚倒下，结算面板还没来得及打开。
        if (save.outcome == 0 && save.members.TrueForAll(member => member.downed)) save.outcome = 2;
        RunItemUseService.Capture(save);
        RunNpcQuestService.Capture(save.quests);
        foreach (var target in RunInteractable.Active)
            if (target is RunContentInteractable content && OwnsRuntimeObject(content.gameObject))
            {
                var claims = content.CaptureClaims();
                if (claims.claimed.Count > 0) save.claims.Add(claims);
            }
        if (_dropRoot != null)
            foreach (var drop in _dropRoot.GetComponentsInChildren<RunDropItem>(false))
                if (drop.count > 0) save.drops.Add(new RunGroundDropSave { id = drop.itemId, count = drop.count, position = drop.transform.position });
        if (save.outcome != 0) RunSettlementModel.Current?.CaptureChoices(save);
        return save;
    }

    private static bool ApplyCheckpoint(ActiveRunSave save, NaturalMapPlan map)
    {
        if (map == null || map.Version != save.generatorVersion || map.ConfigFingerprint != save.configFingerprint
            || map.Signature(true, false) != save.layoutSignature) return false;
        var dead = new HashSet<string>(save.defeatedEnemies, StringComparer.Ordinal);
        if (dead.Count != save.defeatedEnemies.Count) return false;
        for (int index = AllEnemies.Count - 1; index >= 0; index--)
        {
            var go = AllEnemies[index];
            if (!dead.Remove(go.name)) continue;
            var enemy = go.GetComponent<EnemyPlaceholder>();
            AliveByRoom[enemy.roomId].Remove(enemy);
            DefeatedEnemyIds.Add(go.name);
            AllEnemies.RemoveAt(index);
            go.SetActive(false);
            _objectScope.DestroyOwned(go);
        }
        if (dead.Count != 0) return false;
        var targets = RunInteractable.Active.OfType<RunContentInteractable>()
            .Where(target => OwnsRuntimeObject(target.gameObject)).ToDictionary(target => target.StableId, StringComparer.Ordinal);
        foreach (var claim in save.claims)
            if (claim == null || string.IsNullOrEmpty(claim.id) || !targets.TryGetValue(claim.id, out var target)
                || !target.RestoreClaims(claim)) return false;
        foreach (var drop in save.drops)
        {
            if (drop == null || drop.count == 0 || BattleLoadout.Definition(drop.id) == null
                || !RunCheckpoint.IsFinite(drop.position.x) || !RunCheckpoint.IsFinite(drop.position.y)) return false;
            SpawnGroundDrop(drop.id, drop.count, drop.position, null, true);
        }
        if (!RunNpcQuestService.Restore(save.quests)) return false;
        CampService.RestoreRunState(save.campUsed);
        DayNightClock.RestoreRunTime(save.timeMinutes);
        StagesPassed = save.stagesPassed;
        RunKills = save.kills;
        HighestRunDamage = save.highestDamage;
        RunPlaySeconds = save.playSeconds;
        _killSeq = save.killSequence;
        StartingImportantItems.Clear();
        StartingImportantItems.UnionWith(save.startingImportant);
        RunItemUseService.Restore(save);
        var player = PlayerSpawner.CurrentPlayer.GetComponent<PlayerHealth>();
        player.ApplyCarriedVitals(save.members[0].hp, save.members[0].downed);
        RestoreCheckpointPosition(player.GetComponent<Collider2D>(), save.playerFeet, map);
        foreach (var ally in AllyController.All)
        {
            if (ally == null || !OwnsRuntimeObject(ally.gameObject)) continue;
            var member = save.members.Find(value => value.id == ally.memberId);
            if (member == null) return false;
            ally.ApplyCarriedVitals(member.hp, member.downed);
            var shape = ally.GetComponent<Collider2D>();
            if (shape != null)
            {
                var playerFeet = RunMapPhysics.Feet(player.GetComponent<Collider2D>());
                int index = Team.IndexOf(member.id);
                RestoreCheckpointPosition(shape, playerFeet + Vector2.right * (index * 1.3f), map);
            }
        }
        if (save.members[0].downed && save.outcome == 0) EnterSpectator(CountAliveAllies());
        Physics2D.SyncTransforms();
        RunMapExplorationService.Tick(true);
        return RunMapExplorationService.Fog?.RestoreExplored(save.explored) == true;
    }

    private static void RestoreCheckpointPosition(Collider2D shape, Vector2 desired, NaturalMapPlan map)
    {
        if (shape == null || !RunCheckpoint.IsFinite(desired.x) || !RunCheckpoint.IsFinite(desired.y)) return;
        var size = shape.bounds.size;
        Vector2 best = default;
        float distance = 36f;
        bool found = false;
        foreach (var cell in map.StandingCells)
        {
            var candidate = new Vector2(cell.x + .5f, cell.y + .04f);
            float score = (candidate - desired).sqrMagnitude;
            if (score >= distance || !RunMapPhysics.CanOccupy(candidate, size)) continue;
            distance = score;
            best = candidate;
            found = true;
        }
        // 附近没有安全落脚点时，保留地图给出的出生锚点。
        if (!found) return;
        Vector2 delta = best - RunMapPhysics.Feet(shape);
        if (shape.attachedRigidbody != null)
        {
            shape.attachedRigidbody.position += delta;
            shape.attachedRigidbody.velocity = Vector2.zero;
        }
        else shape.transform.position += (Vector3)delta;
        Physics2D.SyncTransforms();
    }

    internal static void RestoreSettlement(ActiveRunSave save)
    {
        if (save.outcome == 0) return;
        RunCleared = save.outcome == 1;
        Defeated = save.outcome == 2;
        _clearedCurrentScene = RunCleared;
        _sceneReady = !Defeated;
        RunSettlementPanel.Open(RunCleared);
        RunSettlementModel.Current?.RestoreChoices(save);
        UnityEngine.Object.FindObjectOfType<RunSettlementPanel>()?.RefreshFromOpen();
        RunTeamHud.SetVisible(false);
    }
}
