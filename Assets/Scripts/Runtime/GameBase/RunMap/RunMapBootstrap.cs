using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using ChaosDebug;

namespace RunMap
{
    /// <summary>
    /// 局内地图的**运行时入口**：生成 → 装配 → 自检，并把"当前阶段"报给读条界面。
    ///
    /// 两条路径，二选一，**不会重复生成**：
    ///   ① **读条期生成（正式流程）**：`LoadingController` 在目标场景加载完、还没激活时
    ///      找到本组件并调 <see cref="BuildNow"/> —— 玩家看到的是读条 + 阶段文案，
    ///      而不是"进场景之后卡一下"；
    ///   ② **进场景即生成（单开场景调试用）**：直接 Play 目标场景时走 `Start()`。
    ///
    /// 阶段文案走 <see cref="RunMapLoadingText"/>（表驱动、按设置里的语言取列），
    /// 订阅 <see cref="StageChanged"/> 即可把文案打到读条界面上。
    /// </summary>
    public class RunMapBootstrap : MonoBehaviour
    {
        [Header("生成参数")]
        [Tooltip("本局种子（局外生成后带进来）")]
        public uint runSeed = 20261010u;

        [Tooltip("本局第几个场景（1~3）")]
        public int sceneIndex = 1;

        [Tooltip("进入场景就生成（读条期已经生成过则自动跳过，不会重复）")]
        public bool buildOnStart = true;

        [Tooltip("把拼好的地图挂到哪个父节点下（留空 = 本组件所在物体下）")]
        public Transform mapParent;

        [Tooltip("生成后把主相机对到入口房间（M8 / 相机系统接手前先用这个；只动运行时，不改场景文件）")]
        public bool frameCameraOnEntry = true;

        /// <summary>当前阶段文案（已按语言取好）。读条界面可以直接读它。</summary>
        public static string CurrentStageText { get; private set; }

        /// <summary>阶段变化事件（参数 = 已本地化的阶段文案）。</summary>
        public static event Action<string> StageChanged;

        /// <summary>最近一次生成出的布局（给战斗 / 小地图用）。</summary>
        public static MapLayout CurrentLayout { get; private set; }

        /// <summary>最近一次装配出的根物体。</summary>
        public static GameObject CurrentMapRoot { get; private set; }
        public static double LastPlanningStepMs { get; private set; }
        public static double LastAssemblyStepMs { get; private set; }
        public static double LastPreparationMs { get; private set; }

        /// <summary>本实例是否已生成过（单开场景调试走这条）。</summary>
        private bool _built;
        public bool IsBuilt { get { return _built; } }

        /// <summary>
        /// 读条期是否已替**下一个目标场景**生成过。
        /// 必须是静态：读条期生成时用的入口组件和目标场景里那个实例不是同一个对象，实例字段挡不住重复生成。
        /// </summary>
        private static bool _builtByLoader;

        /// <summary>待生成请求：谁开始一局 / 切场景，谁调 <see cref="RequestForScene"/> 登记。</summary>
        private static string _pendingScene;
        private static uint _pendingSeed;
        private static int _pendingSceneIndex;

        private void Start()
        {
            if (buildOnStart && !_built && !_builtByLoader) BuildNow();
        }

        /// <summary>
        /// 登记"下一个加载 <paramref name="sceneName"/> 时，替我生成第 <paramref name="sceneIndex"/> 个场景的地图"。
        /// **由派遣 / 读档流程调用**（当前测试流程也用它）。
        ///
        /// 为什么做成"显式请求 + 按场景名匹配"：读条期**不需要**去访问还没激活的目标场景
        /// （实测那它的 `Scene.isLoaded` / `GetRootGameObjects()` 都拿不到东西），
        /// 读条功能也就不会和本地化、场景对象探查这些东西耦合在一起。
        /// </summary>
        public static void RequestForScene(string sceneName, uint runSeed, int sceneIndex)
        {
            _pendingScene = sceneName;
            _pendingSeed = runSeed;
            _pendingSceneIndex = sceneIndex;
            _builtByLoader = false;
        }

        /// <summary>读条期取请求：场景名匹配才返回 true。</summary>
        public static bool TryTakeRequest(string sceneName, out uint runSeed, out int sceneIndex)
        {
            runSeed = 0;
            sceneIndex = 1;
            if (string.IsNullOrEmpty(sceneName) || _pendingScene != sceneName) return false;
            runSeed = _pendingSeed;
            sceneIndex = _pendingSceneIndex;
            _pendingScene = null;
            return true;
        }

        /// <summary>
        /// 读条期生成：生成到 <paramref name="parent"/>（调用方给的 DontDestroyOnLoad 容器）。
        ///
        /// 为什么先放临时容器、等激活再搬：目标场景此刻**已加载但未激活**，往里建东西不可靠；
        /// 先建在一个跨场景容器里，目标场景激活时再 `MoveGameObjectToScene` 搬进去，稳。
        /// </summary>
        public static GameObject BuildIntoPending(Transform parent, uint runSeed, int sceneIndex)
        {
            GameObject root = BuildScene(runSeed, sceneIndex, parent, false);
            _builtByLoader = true;
            return root;
        }

        public static IEnumerable<string> BuildIntoPendingSteps(Transform parent, uint seed, int index)
        {
            foreach (var step in BuildSceneSteps(seed, index, parent, false)) yield return step;
            _builtByLoader = true;
        }

        /// <summary>
        /// 目标场景激活后调用：把读条期建好的地图**容器**搬进目标场景，显示出来并重新取景。
        ///
        /// ⚠ 必须搬容器、不能搬地图根：`MoveGameObjectToScene` 只接受**场景根物体**，
        /// 而地图根是容器的子物体（实测报 `Gameobject is not a root in a scene`）。
        /// ⚠ 容器在 DontDestroyOnLoad 期间是**隐藏**的：DDOL 物体照样渲染，
        /// 不隐藏就会出现在读条画面里（实测截图里就是它）。
        /// </summary>
        public static void FinalizePendingInto(GameObject container, Scene target)
        {
            if (container == null) return;
            container.SetActive(true);
            if (target.IsValid() && container.scene != target) SceneManager.MoveGameObjectToScene(container, target);
            foreach (var tiles in container.GetComponentsInChildren<TilemapCollider2D>()) tiles.ProcessTilemapChanges();
            foreach (var composite in container.GetComponentsInChildren<CompositeCollider2D>()) composite.GenerateGeometry();
            Physics2D.SyncTransforms();
            if (NaturalMapRuntime.Current != null && !NaturalMapAssembler.VerifyActive(NaturalMapRuntime.Current, CurrentMapRoot))
            {
                container.SetActive(false);
                GenerationFailed("激活后的地形碰撞校验失败");
                UnityEngine.Object.Destroy(container);
                return;
            }
            FrameCameraOnEntry(CurrentLayout, Camera.main);
        }

        /// <summary>
        /// 就地生成一次（读条期由 `LoadingController` 调用）。幂等：已生成过直接返回现有根物体。
        /// </summary>
        public GameObject BuildNow()
        {
            if (_built) return CurrentMapRoot;
            _built = true;

            Transform parent = mapParent != null ? mapParent : transform;
            GameObject root = BuildScene(runSeed, sceneIndex, parent, false);
            if (frameCameraOnEntry) FrameCameraOnEntry(CurrentLayout, null);
            return root;
        }


        /// <summary>生成并装配"本局第 sceneIndex 个场景"的地图。返回拼好的根物体（失败为 null）。</summary>
        public static GameObject BuildScene(uint runSeed, int sceneIndex, Transform parent, bool frameCamera = true)
        {
            foreach (var unused in BuildSceneSteps(runSeed, sceneIndex, parent, frameCamera)) { }
            return CurrentMapRoot;
        }

        public static void ClearCurrent()
        { CurrentMapRoot = null; CurrentLayout = null; NaturalMapRuntime.Clear(); }

        public static IEnumerable<string> BuildSceneSteps(uint seed, int index, Transform parent, bool frameCamera = true)
        {
            ClearCurrent();
            LastPlanningStepMs = LastAssemblyStepMs = LastPreparationMs = 0;
            NaturalMapBuild job = null;
            string error = null;
            var prepareClock = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var p = NaturalMapRuntime.LoadParameters(index);
                var actor = NaturalMapRuntime.CaptureActorProfile();
                var problems = p.Validate(actor);
                if (problems.Count > 0) throw new ArgumentException(string.Join("；", problems));
                job = new NaturalMapBuild(seed, index, p, NaturalMapRuntime.LoadStructures(p),
                    actor, NaturalMapRuntime.PopulationPasses.ToArray());
            }
            catch (Exception exception) { error = exception.Message; }
            LastPreparationMs = prepareClock.Elapsed.TotalMilliseconds;
            if (error != null) { GenerationFailed(error); yield break; }
            foreach (var step in SafeSteps(job.Steps().GetEnumerator(), value => error = value,
                ms => LastPlanningStepMs = Math.Max(LastPlanningStepMs, ms)))
            { if (step != null) TellNatural(step); yield return step; }
            if (error != null || job.Plan == null) { GenerationFailed(error ?? job.Error); yield break; }
            var assembler = new NaturalMapAssembler(job.Plan);
            foreach (var step in SafeSteps(assembler.Steps(parent).GetEnumerator(), value => error = value,
                ms => LastAssemblyStepMs = Math.Max(LastAssemblyStepMs, ms)))
            { if (step != null) TellNatural(step); yield return step; }
            if (error != null || assembler.Root == null)
            { assembler.Dispose(); GenerationFailed(error ?? assembler.Error); yield break; }
            CurrentMapRoot = assembler.Root;
            CurrentLayout = NaturalMapRuntime.LegacyMetadata(job.Plan);
            NaturalMapRuntime.Publish(job.Plan);
            if (frameCamera) FrameCameraOnEntry(CurrentLayout, null);
            TellNatural("局内地图就绪");
            ChaosLog.Info(LogChannel.General, "[RunMap] " + job.Plan.Report);
        }

        private static IEnumerable<string> SafeSteps(IEnumerator<string> steps, Action<string> failed, Action<double> measured)
        {
            using (steps)
            {
                while (true)
                {
                    bool next = false; string step = null, error = null;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    try { next = steps.MoveNext(); if (next) step = steps.Current; }
                    catch (Exception exception) { error = exception.ToString(); }
                    measured(watch.Elapsed.TotalMilliseconds);
                    if (error != null) { failed(error); yield break; }
                    if (!next) yield break;
                    yield return step;
                }
            }
        }
        private static void GenerationFailed(string error)
        {
            ClearCurrent(); NaturalMapRuntime.Fail(error ?? "地图生成失败");
            TellNatural("地图生成失败：" + NaturalMapRuntime.LastError);
            ChaosLog.Error(LogChannel.General, "[RunMap] " + NaturalMapRuntime.LastError);
        }
        private static readonly string[] NaturalStages = { "规划区域与边界", "生成连续地表", "雕刻地下与连接通路", "嵌入手工结构",
            "校验角色通行与生成锚点", "规划NPC、交互物与杂物", "地图计划校验完成", "创建地形瓦片", "落地手工结构", "建立地形碰撞",
            "创建内容占位物", "场景地图校验完成", "局内地图就绪" };
        private static void TellNatural(string message)
        {
            int index = Array.IndexOf(NaturalStages, message);
            if (index >= 0) message = RunMapLoadingText.ForStage((uint)(index + 9));
            CurrentStageText = message; if (StageChanged != null) StageChanged(message);
        }

        /// <summary>
        /// 把相机对到入口房间中心。
        /// ⚠ 这是"M8 / 相机系统接手前"的临时措施：相机归属最终该由战斗系统决定，
        /// 这里只保证"进场景能看见刚生成的地图"。**只动运行时，不写回场景文件。**
        /// </summary>
        public static void FrameCameraOnEntry(MapLayout layout, Camera cam)
        {
            if (NaturalMapRuntime.Current != null)
            {
                if (cam == null) cam = Camera.main;
                var anchor = NaturalMapRuntime.Current.FindPlacement(MapPlacementKind.Player);
                if (cam != null && anchor != null)
                {
                    if (cam.orthographic) cam.orthographicSize = 12f;
                    cam.transform.position = new Vector3(anchor.Position.x, anchor.Position.y + 3f, cam.transform.position.z);
                }
                return;
            }
            if (layout == null) return;
            RoomNode entry = layout.Find(layout.EntryId);
            if (entry == null) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            float cx = (entry.CellX + 0.5f) * RunMapConst.ChunkW;
            float cy = (entry.CellY + 0.5f) * RunMapConst.ChunkH;

            // 让整间房（40×24 格）真的落在画面里：
            // 正交相机调 size；透视相机按 FOV 退到足够远 —— 否则只能看到房间中间一片背景层。
            Vector3 p = cam.transform.position;
            if (cam.orthographic)
            {
                cam.orthographicSize = RunMapConst.ChunkH * 0.5f;
                cam.transform.position = new Vector3(cx, cy, p.z);
            }
            else
            {
                float dist = (RunMapConst.ChunkH * 0.5f) / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                cam.transform.position = new Vector3(cx, cy, -(Mathf.Abs(dist) + 1f));
            }
            ChaosLog.Info(LogChannel.General, "[RunMap] 相机已对到入口房 (" + cx + ", " + cy + ") ortho=" + cam.orthographic);
        }


        /// <summary>
        /// 装配后的自检：**竖井通道不能被地形堵住**（跨层与支路都走这条竖直通道）。
        /// 这是把"写进文档的规则"变成"能跑的断言"——之前就是靠同类检查抓到平台横切通道的缺陷。
        /// </summary>
        public static string VerifyAssembled(GameObject root, MapLayout layout, RunMapCatalog cat)
        {
            if (root == null) return "没有装配结果，跳过自检";

            int blocked = 0, rooms = 0;
            foreach (Transform room in root.transform)
            {
                rooms++;
                Transform g = room.Find("Tilemap_Ground");
                if (g == null) continue;
                Tilemap tm = g.GetComponent<Tilemap>();
                if (tm == null) continue;

                // 层间连接块本身就是竖井（通道就在它身体里），跳过
                ChunkDef def = FindByRoomName(room.name, layout, cat);
                if (def != null && def.Kind == ChunkKind.LayerLink) continue;

                for (int x = RunMapConst.ShaftColumn - 1; x <= RunMapConst.ShaftColumn + 1; x++)
                    for (int y = 3; y <= RunMapConst.ChunkH - 3; y++)
                        if (tm.GetTile(new Vector3Int(x, y, 0)) != null) blocked++;
            }

            if (blocked > 0)
                ChaosLog.WarnTrace("[RunMap] 竖井通道被占 " + blocked + " 格（跨层 / 支路可能走不通）");
            return "房间 " + rooms + " 个，竖井通道被占 " + blocked + " 格";
        }

        private static ChunkDef FindByRoomName(string goName, MapLayout layout, RunMapCatalog cat)
        {
            // 名字形如 R12_Combat_L2
            int us = goName.IndexOf('_');
            if (us <= 1) return null;
            ushort id;
            if (!ushort.TryParse(goName.Substring(1, us - 1), out id)) return null;
            RoomNode n = layout.Find(id);
            return n == null ? null : cat.Get(n.ChunkId);
        }

        private static void Tell(uint stage)
        {
            string text = RunMapLoadingText.ForStage(stage);
            CurrentStageText = text;
            if (StageChanged != null) StageChanged(text);
            ChaosLog.Info(LogChannel.General, "[RunMap] 阶段 " + stage + "：" + text);
        }
    }
}
