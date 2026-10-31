#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Chaos.Gameplay.RunTeam;
using Chaos.MVP.Model.Gameplay;
using RunMap;
using Spine.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

/// <summary>Formal four-member dispatch, menu, combat growth and three-stage lifecycle in isolated storage.</summary>
public static class RunTeamFlowVerification
{
    public static string Status { get; private set; } = "idle";
    public static bool Resume;
    private static string _storage, _overrides;
    private static Keyboard _keyboard;
    private static readonly StringBuilder Report = new StringBuilder();
    private static BasePanel Top => UIManager.Instance.panelStack.Count == 0 ? null : UIManager.Instance.panelStack.Peek();
    private static void Check(bool ok, string label)
    { if (!ok) throw new Exception(label); Report.AppendLine("PASS " + label); Flush(); }
    private static void Flush()
    { Directory.CreateDirectory(Application.dataPath + "/QA/RunTeam"); File.WriteAllText(Application.dataPath + "/QA/RunTeam/flow.txt", Report.ToString()); }
    private static void Isolation()
    { if (SaveSlotService.SavesDirectory != _storage) throw new Exception("save isolation lost"); }
    private static void Key(params Key[] keys) { InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys)); }
    private static void Click(string name)
    {
        var button = Top.GetComponentsInChildren<Button>(false).Single(b => b.name == name && b.interactable);
        ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    }
    private static RunTeamState State(RunTeamView view)
    { return (RunTeamState)typeof(RunTeamView).GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view); }
    public static void Start()
    {
        if (!Application.isPlaying || Status == "running" || Status == "visual") throw new Exception("requires idle Play");
        Report.Clear(); Status = "running"; Resume = false;
        _storage = Path.Combine(Path.GetTempPath(), "ChaosVillageIsolated-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_storage); SaveSlotService.UseEditorTestDirectory(_storage); Isolation();
        _overrides = InputManager.Instance.SaveOverridesJson(); InputManager.Instance.LoadOverridesJson("");
        _keyboard = InputSystem.AddDevice<Keyboard>("ChaosTeamVerificationKeyboard");
        GameSession.Instance.BeginSandbox(new SavePayload { resMoney = 1000, resGoods = 1000, resMaterial = 1000, resBlueprint = 1000 });
        for (int i = 0; i < 4; i++) RetainerService.Instance.RecruitRandom();
        var payload = JsonUtility.FromJson<SavePayload>(JsonUtility.ToJson(GameSession.Instance.LoadedPayload));
        GameSession.Instance.Clear(); Check(SaveSlotService.Instance.Save(3, payload), "isolated slot created");
        GameSession.Instance.Begin(3, payload, "MainScene"); VillageSceneFlow.EnterFromSave();
        UnityEngine.Object.FindObjectOfType<MonoController>().StartCoroutine(Guard(Run()));
    }
    private static IEnumerator Guard(IEnumerator source)
    {
        while (true)
        {
            object value;
            try
            {
                if (!source.MoveNext()) { Status = "passed"; Cleanup(); Report.AppendLine("DONE storage=" + _storage); Flush(); yield break; }
                value = source.Current;
            }
            catch (Exception e) { Status = "failed"; Report.AppendLine("FAIL " + e); Cleanup(); Flush(); yield break; }
            yield return value;
        }
    }
    private static void Cleanup()
    { InputManager.Instance.LoadOverridesJson(_overrides); if (_keyboard != null) InputSystem.RemoveDevice(_keyboard); }
    private static IEnumerator Run()
    {
        string reason;
        float deadline = Time.realtimeSinceStartup + 30;
        while ((!VillageSceneBootstrap.IsReady || !VillageSceneFlow.IsVillageScene) && Time.realtimeSinceStartup < deadline) yield return null;
        Check(VillageSceneBootstrap.IsReady, "formal village ready");
        Key(UnityEngine.InputSystem.Key.M); yield return new WaitForSecondsRealtime(.5f); Key(); yield return null;
        Check(Top is WorldMapPanel, "outside M world map"); Click("EnterRunBtn"); yield return new WaitForSecondsRealtime(.5f);
        Isolation(); foreach (var retainer in RetainerService.Instance.GetAll()) DispatchService.Instance.Toggle(retainer.id, out reason);
        Click("Btn_Depart"); deadline = Time.realtimeSinceStartup + 30;
        while ((!RunEncounter.IsSceneReady || RunSceneFlow.IsTransitioning) && Time.realtimeSinceStartup < deadline) yield return null;
        Check(RunEncounter.IsSceneReady && RunEncounter.CurrentSceneIndex == 1, "formal WorldScene stage1 ready");
        Key(UnityEngine.InputSystem.Key.Tab); yield return new WaitForSecondsRealtime(.4f); Key(); yield return null;
        Check(Top is RunMenuPanel && Time.timeScale == 0, "Tab collection pauses world"); Click("Tab0"); yield return null;
        var panel = (RunMenuPanel)Top;
        var view = panel.GetComponentInChildren<RunTeamView>();
        Check(view != null && State(view).Members.Length == 4 && State(view).SelectedId == State(view).Members[0].Id, "four dispatch members default controlled");
        var ids = State(view).Members.Select(m => m.Id).ToArray();
        Check(view.GetComponentsInChildren<Button>().Count(b => b.name.StartsWith("AvatarButton")) == 4, "exactly four avatar buttons");
        for (int i = 0; i < 4; i++) { Click("AvatarButton" + i); Check(State(view).SelectedId == ids[i], "portrait selection " + i); }
        Click("AvatarButton0");
        var graphics = view.GetComponentsInChildren<SkeletonGraphic>();
        Check(graphics.Length == 5 && graphics.All(g => g.unscaledTime && g.AnimationState.GetCurrent(0).Animation.Name == "Idle_Normal"), "five actual idle Spine previews");
        Check(view.GetComponentsInChildren<RunTeamSpinePreview>().All(p => p.GetComponent<RectMask2D>() != null), "all preview viewports clipped");
        float trackTime = graphics[0].AnimationState.GetCurrent(0).TrackTime;
        yield return new WaitForSecondsRealtime(.3f);
        Check(graphics[0].AnimationState.GetCurrent(0).TrackTime > trackTime && Time.timeScale == 0, "idle advances while paused");
        Check(graphics.All(g => !g.canvasRenderer.cull && g.canvasRenderers.All(r => !r.cull)), "all five preview meshes survive mask culling");
        for (int i = 0; i < 40; i++) view.Render(State(view));
        Check(view.GetComponentsInChildren<SkeletonGraphic>().Select(g => g.GetInstanceID()).SequenceEqual(graphics.Select(g => g.GetInstanceID())), "renders reuse five graphics");
        var probe = view.GetComponentsInChildren<RunTeamSpinePreview>().First();
        var original = State(view).Members[0].SkeletonData;
        var copy = UnityEngine.Object.Instantiate(original);
        var materialCopy = new Material(State(view).SpineMaterial);
        probe.Show(copy, null, materialCopy, false);
        var graphic = probe.GetComponentInChildren<SkeletonGraphic>();
        Check(graphic != null && graphic.skeletonDataAsset == copy && graphic.material == materialCopy, "cached preview switches asset and material");
        probe.Show(null, null, null, false); yield return null; yield return null;
        Check(probe.GetComponentInChildren<SkeletonGraphic>() == null, "cleared preview stays hidden through updates");
        probe.Show(original, State(view).Members[0].Skin, State(view).SpineMaterial, false);
        UnityEngine.Object.Destroy(copy); UnityEngine.Object.Destroy(materialCopy);
        Check(!view.GetComponentsInChildren<Button>().Single(b => b.name == "Allocate0").interactable, "no points disables allocation");
        var enemies = new List<EnemyPlaceholder>(); RunEncounter.CopyMapEnemies(enemies);
        Check(enemies.Count >= 6, "enough live normal enemies for reward checks");
        int previous = State(view).Members[0].Experience;
        enemies[0].TakeDamage(99999);
        var afterKill = State(view).Members[0];
        Check(afterKill.Experience == previous + 20 && State(view).Members.All(m => m.Experience == afterKill.Experience), "normal kill shares XP all dispatched");
        RunEncounter.NotifyEnemyKilled(enemies[0].roomId, enemies[0]);
        Check(State(view).Members[0].Experience == afterKill.Experience, "duplicate enemy callback gives no XP");
        enemies[1].Die("掉落死亡"); Check(State(view).Members[0].Experience == afterKill.Experience, "fall death gives no XP");
        for (int i = 2; State(view).Members[0].Level == 1 && i < enemies.Count; i++) enemies[i].TakeDamage(99999);
        Check(State(view).Members.All(m => m.Level >= 2 && m.Points >= 1), "level grants each member a point");
        var health = PlayerSpawner.CurrentPlayer.GetComponent<PlayerHealth>(); health.ApplyContactDamage(7);
        int hp = health.Hp, max = health.maxHp; Click("Allocate0");
        Check(health.maxHp == max + 10 && health.Hp == hp && State(view).Members[0].MaxHealth == max + 10, "controlled HP allocation changes actual max without healing");
        var bars = view.GetComponentsInChildren<Image>().Where(b => b.name == "Fill").ToArray();
        Check(bars.Length == 3 && bars.All(b => b.rectTransform.rect.width > 0)
            && Mathf.Abs(bars[0].rectTransform.rect.width / ((RectTransform)bars[0].transform.parent).rect.width - (float)hp / health.maxHp) < .001f,
            "actual colored bar geometry follows current over maximum");
        int playerMax = health.maxHp;
        Click("AvatarButton1"); var ally = AllyController.All.Single(a => a.memberId == ids[1] && RunEncounter.OwnsRuntimeObject(a.gameObject));
        ally.TakeDamageDirect(7); hp = ally.Hp; max = ally.MaxHp; Click("Allocate0");
        Check(ally.MaxHp == max + 10 && ally.Hp == hp, "AI HP allocation no healing");
        Click("AvatarButton2"); var downed = AllyController.All.Single(a => a.memberId == ids[2] && RunEncounter.OwnsRuntimeObject(a.gameObject));
        downed.ForceDown(); Click("Allocate0");
        Check(downed.IsDowned && downed.Hp == 0 && !downed.enabled, "allocation cannot revive downed AI");
        Click("AvatarButton3"); Click("Allocate1"); Check(State(view).Members[3].MaxStamina == 110, "stamina maximum allocation");
        Click("AvatarButton0");
        Check(((RectTransform)view.transform).pivot == Vector2.up && ((RectTransform)view.transform).anchoredPosition == new Vector2(0, -110), "team root preserves caller placement");
        Check(view.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Any(t => t.text.Contains("技能后续开放")), "skill placeholder present");
        Status = "visual"; Report.AppendLine("VISUAL ready"); Flush(); while (!Resume) yield return null; Status = "running";
        Click("Tab1"); Check(panel.GetComponentInChildren<RunTeamView>() == null, "bag switch hides team previews");
        Click("Tab2"); Check(panel.GetComponentInChildren<RunMapView>() != null, "map remains available");
        Click("Tab0"); Click("Settings"); yield return new WaitForSecondsRealtime(.5f);
        Check(Top is SettingPanel && Time.timeScale == 0, "settings covers collection with pause");
        UIManager.Instance.PopPanel(); yield return null;
        Check(Top == panel && State(view).SelectedId == ids[0], "return from settings restores controlled selection");
        Click("Continue"); Check(Time.timeScale == 1, "continue resumes world");
        for (int stage = 1; stage <= 3; stage++)
        {
            RunEncounter.KillAllEnemiesInScene();
            deadline = Time.realtimeSinceStartup + 30;
            if (stage < 3)
            {
                while ((RunEncounter.CurrentSceneIndex != stage + 1 || !RunEncounter.IsSceneReady || RunSceneFlow.IsTransitioning) && Time.realtimeSinceStartup < deadline) yield return null;
                Check(RunEncounter.CurrentSceneIndex == stage + 1 && RunEncounter.IsSceneReady, "stage " + (stage + 1) + " ready");
                var model = new RunTeamModel(); var state = model.Read();
                Check(state.Members.Length == 4 && state.Members[0].Level >= 2 && RunTeamProgress.HealthBonus(ids[0]) == 10
                    && PlayerSpawner.CurrentPlayer.GetComponent<PlayerHealth>().maxHp == playerMax, "growth and actual max retained stage " + (stage + 1));
                if (stage == 1)
                {
                    RunMenuPanel.Open(RunFunctionTab.Team); yield return null;
                    var v = ((RunMenuPanel)Top).GetComponentInChildren<RunTeamView>();
                    Click("AvatarButton3"); Click("Allocate2");
                    Check(State(v).Members[3].MaxMana == 110, "mana maximum allocation after stage change"); Click("Continue");
                }
            }
            else { while (!(Top is RunSettlementPanel) && Time.realtimeSinceStartup < deadline) yield return null; Check(Top is RunSettlementPanel, "three-map settlement reached"); }
        }
        Isolation(); Check(RunSettlementModel.Current.Confirm(true, out reason), "isolated settlement confirmed");
        Check(RunTeamProgress.MemberIds.Count == 0, "settlement clears growth");
        Check(RunSettlementModel.Current.ReturnToMeta(out reason), "formal return requested");
        deadline = Time.realtimeSinceStartup + 30; while ((!VillageSceneBootstrap.IsReady || !VillageSceneFlow.IsVillageScene) && Time.realtimeSinceStartup < deadline) yield return null;
        Check(VillageSceneBootstrap.IsReady && Time.timeScale == 1 && RunTeamProgress.MemberIds.Count == 0, "village return remains clean");
        RunTeamProgress.BeginRun(ids); GameSession.Instance.Clear();
        Check(RunTeamProgress.MemberIds.Count == 0, "session switch clears run state");
    }
}
#endif
