using System.Collections.Generic;
using Chaos.Gameplay.RunTeam;
using LocalizationSystem;
using Spine.Unity;
using UnityEngine;

namespace Chaos.MVP.Model.Gameplay
{
    /// <summary>Adapts current-run growth, dispatched identities and scoped actors for the team view.</summary>
    public sealed class RunTeamModel
    {
        private uint _selected;
        private int _context, _runId;
        private string _message;
        private readonly Material _material = Resources.Load<Material>("UI/Spine/RunTeamSkeletonGraphic");
        private readonly SkeletonDataAsset _fallback;

        public RunTeamModel()
        {
            var prefab = Resources.Load<GameObject>("Player/Player");
            if (prefab != null) _fallback = prefab.GetComponentInChildren<SkeletonAnimation>(true)?.skeletonDataAsset;
            BeginVisit();
        }

        public void BeginVisit()
        {
            _context = GameSession.Instance.ContextId; _runId = RunTeamProgress.RunId;
            var ids = RunTeamProgress.MemberIds;
            _selected = ids.Count > 0 ? ids[0] : 0;
            _message = null;
        }

        private bool IsCurrent => _context == GameSession.Instance.ContextId && _runId == RunTeamProgress.RunId;

        public void Select(uint id)
        {
            if (IsCurrent && RunTeamProgress.TryRead(id, out _)) { _selected = id; _message = null; }
        }

        public void Allocate(RunTeamAttribute attribute)
        {
            if (!IsCurrent) { _message = "本局已经结束，请重新打开队伍页签"; return; }
            if (RunTeamProgress.TrySpend(_selected, attribute, out string reason))
                _message = "属性上限已提高（生命加点不治疗）";
            else _message = reason;
        }

        public RunTeamState Read()
        {
            var state = new RunTeamState { SelectedId = _selected, Message = _message, SpineMaterial = _material };
            if (!IsCurrent) { state.Message = "本局成长已清除"; return state; }
            var ids = RunTeamProgress.MemberIds;
            var members = new List<RunTeamMemberState>(ids.Count);
            for (int i = 0; i < ids.Count; i++)
            {
                uint id = ids[i];
                if (!RunTeamProgress.TryRead(id, out var growth)) continue;
                var actor = ActorFor(id, i == 0);
                var appearance = actor == null ? null : actor.GetComponent<RunCharacterPresentation>();
                var skeleton = actor == null ? null : actor.GetComponentInChildren<SkeletonAnimation>(true);
                string name = "门客 #" + id;
                var service = RetainerService.Instance;
                int fallbackHp = 100;
                if (service.TryGet(id, out var saved))
                {
                    var definition = service.GetDefinition(saved.protoId);
                    if (definition != null)
                    {
                        name = GameplayText.Text(definition.NameKey, name);
                        fallbackHp = (int)definition.Hp + service.BonusHp;
                    }
                }
                int hp = 0, max = fallbackHp + growth.HealthBonus;
                bool downed = true;
                if (actor != null && i == 0)
                {
                    var health = actor.GetComponent<PlayerHealth>();
                    if (health != null) { hp = health.Hp; max = health.maxHp; downed = health.IsDowned; }
                }
                else if (actor != null)
                {
                    var ally = actor.GetComponent<AllyController>();
                    if (ally != null) { hp = ally.Hp; max = ally.MaxHp; downed = ally.IsDowned; }
                }
                members.Add(new RunTeamMemberState {
                    Id = id, Name = name, Profession = appearance == null || string.IsNullOrWhiteSpace(appearance.profession)
                        ? "职业待配置" : appearance.profession,
                    Skin = appearance != null && !string.IsNullOrEmpty(appearance.previewSkin) ? appearance.previewSkin
                        : skeleton?.Skeleton?.Skin?.Name,
                    Portrait = appearance == null ? null : appearance.portrait,
                    SkeletonData = skeleton == null ? _fallback : skeleton.skeletonDataAsset,
                    Controlled = i == 0, Downed = downed, Level = growth.Level,
                    Experience = growth.Experience, ExperienceToNextLevel = growth.ExperienceToNextLevel,
                    Points = growth.UnspentPoints, Health = hp, MaxHealth = max,
                    MaxStamina = RunCharacterGrowthRules.BaseStamina + growth.StaminaBonus,
                    MaxMana = RunCharacterGrowthRules.BaseMana + growth.ManaBonus });
            }
            state.Members = members.ToArray();
            state.CanAllocate = DispatchService.Instance.IsInRun && RunEncounter.IsSceneReady
                && !RunSceneFlow.IsTransitioning && !RunEncounter.RunCleared && !RunEncounter.Defeated;
            return state;
        }

        private static GameObject ActorFor(uint id, bool controlled)
        {
            if (controlled)
            {
                var player = PlayerSpawner.CurrentPlayer;
                return player != null && RunEncounter.OwnsRuntimeObject(player) ? player : null;
            }
            foreach (var ally in AllyController.All)
                if (ally != null && ally.memberId == id && RunEncounter.OwnsRuntimeObject(ally.gameObject)) return ally.gameObject;
            return null;
        }
    }
}
