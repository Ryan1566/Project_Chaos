using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChaosDebug;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// 引了 System 之后 Object 会和 UnityEngine.Object 打架，而这里到处在用后者
using Object = UnityEngine.Object;

namespace SceneUIOverviewTool.Editor
{
    /// <summary>
    /// 在 Scene 视图里把 UI 与游戏场景分开显示：Canvas 从场景区摘掉，在视图左侧单独预览。
    ///
    /// 存在的意义：项目的 UI 是满屏尺寸的画布（1920x1080 @ scale 0.01，世界尺寸
    /// 20.53x11.55），主相机可见范围恰好也是这个尺寸。于是它在 Scene 视图里就是一堵和视锥
    /// 严丝合缝的墙，Grid / citizen 等场景内容全被盖住，根本没法编辑场景。
    ///
    /// 画布原本是 World Space，2026-09-24 改成 ScreenSpaceCamera + Main Camera。
    /// 起因是 World Space 下 transform 就是运行时位置，挪一下 Canvas 就会把运行时 UI 一起
    /// 挪出画面（提交 980dcfe 里那个 x=-27.9 就是这么来的，运行时一个 UI 都看不到）。
    /// 现在 RectTransform 由相机驱动，编辑期怎么拖都不影响运行时排版。
    ///
    /// 为什么用 SceneVisibilityManager 而不是 cullingMask：面板预制体（Resources/UIPanels）
    /// 全部在 Default 层而非 UI 层，UIManager 运行时 SetParent 也不改 layer。按层剔除对它们
    /// 完全无效。SceneVisibilityManager 按对象隐藏，不依赖 Layer。
    ///
    /// 为什么零污染：不改任何场景对象的位置 / 激活状态 / Layer。隐藏状态落在编辑器的
    /// Library/SceneVisibilityState.asset 里，不进版本库。Hierarchy 里会显示划掉的眼睛图标，
    /// 随时可以看出来是谁干的。
    ///
    /// 分离只作用于 Scene 视图，Game 视图与 Play 模式完全不受影响 —— SceneVisibilityManager
    /// 是纯编辑器状态，实测开关工具前后 Game 视图逐像素一致。但预览面板要临时改场景对象
    /// （激活停用面板 / 切 renderMode / 关渲染器），运行中做这些会真的干扰游戏，所以
    /// Play 期间只保留隐藏和轮廓框，不画预览面板。
    ///
    /// 已知副作用（无法消除）：
    /// 1. 预览前会临时激活停用的面板、渲染完立刻还原，这会给场景置一个 dirty 标记。
    ///    文件内容不受影响，存盘结果与原先一致。
    /// 2. 隐藏状态存在 Library/SceneVisibilityState.asset 里，它跨 Unity 重启存活，而记着
    ///    "这些是本工具隐藏的"的 SessionState 不存活。两者一错配，开着工具重启后就会留下一个
    ///    既没人认领、工具也管不着的隐藏 Canvas（实测踩到过）。现在靠 Library 下的隐藏账本自愈。
    /// </summary>
    /// 必须显式挂 InitializeOnLoad：静态构造函数是惰性的，不挂就得等别人来碰这个类。
    /// 唯一会主动碰它的是 Overlay 的 CreatePanelContent 和菜单校验，也就是说用户一旦把
    /// Overlay 从工具栏上摘掉，工具就会静默失效（SessionState 里还存着"已开启"，
    /// 但 duringSceneGui 根本没挂上，没人去执行隐藏）。
    [InitializeOnLoad]
    public static class SceneUIOverviewTool
    {
        private const string MenuPath = "Tool/UI/UI 与场景分离显示（Scene 视图）";
        private const string EnabledKey = "SceneUIOverviewTool.Enabled";
        private const string HiddenIdsKey = "SceneUIOverviewTool.HiddenCanvasIds";

        private const float Aspect = 16f / 9f;          // 与 Game 视图 1920x1080 一致
        private const float Margin = 8f;
        private const float ToolbarHeight = 24f;        // 避开 Scene 视图工具栏
        private const float MaxWidthRatio = 0.45f;      // 面板最多占视图宽度的 45%
        private const float BorderThickness = 2f;
        private const float SuppressCheckInterval = 0.5f;
        private const float CloseButtonSize = 16f;

        private static readonly Color BackgroundColor = new Color(0.13f, 0.13f, 0.13f, 1f);
        private static readonly Color BorderColor = new Color(0.35f, 0.72f, 1f, 1f);
        private static readonly Color BoundsColor = new Color(1f, 0.62f, 0.18f, 1f);
        private static readonly Color CloseButtonColor = new Color(0.85f, 0.35f, 0.35f, 1f);

        // 隐藏账本。放 Library 下是刻意的：它和 SceneVisibilityManager 的隐藏状态
        // （Library/SceneVisibilityState.asset）寿命一致，Library 被删则两者一起失效，
        // 不会出现"账本还在、隐藏状态却已经没了"的错配。
        private static string LedgerPath
        {
            get { return Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/SceneUIOverviewTool.state")); }
        }

        // 不用 PreviewRenderUtility：它的相机 cameraType 是 Preview 且绑定在自建的
        // "Preview Scene" 上，渲染不到当前游戏场景里的 Canvas（实测恒为 0 像素，
        // 把它 camera.scene 指回当前场景也无济于事）。普通相机 + RenderTexture 实测正常。
        private static Camera _previewCamera;
        private static RenderTexture _previewRT;

        // 本工具隐藏的 Canvas，以及其中「用户本来就隐藏了」的节点（还原时不能一起放出来）
        private static readonly List<Canvas> _hiddenCanvases = new List<Canvas>();
        private static readonly List<GameObject> _preHidden = new List<GameObject>();

        private static readonly List<GameObject> _activated = new List<GameObject>();
        private static readonly List<Renderer> _suppressed = new List<Renderer>();

        // 渲染期间被临时改了渲染目标的画布，见 SwitchOverlayCanvases。
        // 两个列表配对使用：_recameraedOriginals[i] 是 _recameraedCanvases[i] 原来的相机。
        private static readonly List<Canvas> _flippedCanvases = new List<Canvas>();
        private static readonly List<Canvas> _recameraedCanvases = new List<Canvas>();
        private static readonly List<Camera> _recameraedOriginals = new List<Camera>();

        // GetWorldCorners 的复用缓冲，避免每次 repaint 都分配
        private static readonly Vector3[] _corners = new Vector3[4];

        // 「需要抑制非 UI 渲染器」的判定要遍历场景，缓存起来定期刷新
        private static bool _needSuppress;
        private static double _nextSuppressCheck;

        static SceneUIOverviewTool()
        {
            SceneView.duringSceneGui += OnSceneGui;
            AssemblyReloadEvents.beforeAssemblyReload += CleanupPreview;
            EditorApplication.quitting += OnEditorQuitting;
            EditorSceneManager.sceneOpened += OnSceneOpened;

            // 域重载后 SessionState 还在但内存里的跟踪列表没了，重建一次
            if (Enabled) AdoptHiddenCanvases();

            // 上次没善终（开着工具重启、或编辑器被强杀）的话，Library 里还留着隐藏状态，
            // 而工具已经是关的了。延后一帧，等场景加载完再按账本还原。
            EditorApplication.delayCall += HealLedger;
        }

        // 正常退出时收尾。被强杀时这里不会触发，那种情况留给 HealLedger。
        private static void OnEditorQuitting()
        {
            if (Enabled) Enabled = false;
            CleanupPreview();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            HealLedger();
        }

        // Overlay 上的开关靠它把勾同步回来。菜单、面板上的 ×、别的 Scene 视图都能改这个开关，
        // 不回灌的话 Overlay 会一直显示创建时的旧值：用菜单开了，浮窗上还是没勾，
        // 点一下反而"没反应"（赋的值等于当前值，setter 直接返回），得点两下才关得掉。
        public static event Action<bool> EnabledChanged;

        public static bool Enabled
        {
            get { return SessionState.GetBool(EnabledKey, false); }
            set
            {
                if (SessionState.GetBool(EnabledKey, false) == value) return;

                SessionState.SetBool(EnabledKey, value);

                if (value)
                {
                    ApplyHide();
                    ChaosLog.Info("[UI分离显示] 已开启：Canvas 已从 Scene 视图摘出，左侧为 UI 预览");
                }
                else
                {
                    RestoreHide();
                    CleanupPreview();
                    ChaosLog.Info("[UI分离显示] 已关闭");
                }

                NotifyEnabledChanged();
                SceneView.RepaintAll();
            }
        }

        private static void NotifyEnabledChanged()
        {
            if (EnabledChanged != null) EnabledChanged(Enabled);
        }

        [MenuItem(MenuPath, false, 200)]
        private static void ToggleEnabled()
        {
            Enabled = !Enabled;
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleEnabledValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;// Play 期间也能开关：隐藏是纯 Scene 视图状态，碰不到游戏
        }

        // ── 场景视图回调 ──────────────────────────────────────────────

        private static void OnSceneGui(SceneView sv)
        {
            if (sv == null || sv.camera == null) return;

            var e = Event.current;
            if (e == null) return;

            // 关闭按钮的命中必须在非 Repaint 帧做。GUI.Button 只在鼠标事件那一帧返回 true，
            // 而整块面板的渲染开销（Camera.Render）只该在 Repaint 掏一次，两者凑不到一块，
            // 所以不用 GUI.Button，改成自己在同一个矩形上做命中。
            if (e.type != EventType.Repaint)
            {
                if (Enabled) HandleCloseClick(sv, e);
                return;
            }

            // 轮廓框在两种状态下都画。它标的是 UI 的世界覆盖范围，跟有没有分离无关；
            // 未分离时反而更用得着 —— 那时 UI 就是糊在场景上的一堵墙，全靠这个框才知道边界在哪。
            var roots = GetRootCanvases();
            bool hasRoots = roots.Count > 0;
            if (hasRoots)
            {
                // 世界空间的虚框要在 BeginGUI 之前画，否则会落到 GUI 坐标系里
                DrawWorldBounds(roots);
            }

            if (!Enabled) return;

            ApplyHide();
            if (!hasRoots) return;

            // Play 期间不画预览：预览要临时激活停用面板、切 renderMode、关渲染器，
            // 运行时做这些会真的动到游戏对象（触发 OnEnable/OnDisable、改 Game 视图成像）。
            // 而隐藏走的是 SceneVisibilityManager —— 纯 Scene 视图状态，运行中照常安全。
            if (EditorApplication.isPlaying) return;

            DrawPreviewPanel(sv, roots);
        }

        private static void HandleCloseClick(SceneView sv, Event e)
        {
            if (e.type != EventType.MouseDown || e.button != 0) return;

            Rect panel;
            if (!TryGetPanelRect(sv, out panel)) return;
            if (!GetCloseRect(panel).Contains(e.mousePosition)) return;

            Enabled = false;
            e.Use();
        }

        private static List<Canvas> GetRootCanvases()
        {
            var result = new List<Canvas>();
            var all = Object.FindObjectsOfType<Canvas>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].isRootCanvas) result.Add(all[i]);
            return result;
        }

        // ── 摘掉 UI ───────────────────────────────────────────────────

        private static void ApplyHide()
        {
            var svm = SceneVisibilityManager.instance;
            if (svm == null) return;

            // 这个方法每个 Repaint 都会走一遍，而落盘是有代价的（两次写文件）。
            // 只有隐藏集合真的变了才写 —— 否则 Play 期间每帧都在刷 Library。
            bool changed = false;

            var roots = GetRootCanvases();
            for (int i = 0; i < roots.Count; i++)
            {
                var canvas = roots[i];

                // 已经藏着的一律不动：用户自己藏的不能接管，我们藏的也不必重复操作
                if (svm.IsHidden(canvas.gameObject, false)) continue;

                // 走到这里说明它当前可见，两种情况都得藏：第一次接管，以及我们藏过、却被外部
                // 放了出来 —— Unity 退出 Play 模式时会回滚 Scene 可见性状态，实测踩到过。
                // 不能拿 _hiddenCanvases 里有没有当"已经藏好了"的证据，否则列表说藏着、画面露着，
                // 这一条 continue 掉之后就再也藏不上了。
                if (!_hiddenCanvases.Contains(canvas))
                {
                    // 记下子树里原本就被隐藏的节点：还原时 Show(root, true) 会把它们一起放出来
                    foreach (var t in canvas.GetComponentsInChildren<Transform>(true))
                        if (svm.IsHidden(t.gameObject, false)) _preHidden.Add(t.gameObject);

                    _hiddenCanvases.Add(canvas);
                }

                svm.Hide(canvas.gameObject, true);
                changed = true;
            }

            for (int i = _hiddenCanvases.Count - 1; i >= 0; i--)
                if (_hiddenCanvases[i] == null) { _hiddenCanvases.RemoveAt(i); changed = true; }

            if (!changed) return;

            SaveHiddenIds();
            SaveLedger();
        }

        private static void RestoreHide()
        {
            var svm = SceneVisibilityManager.instance;
            if (svm != null)
            {
                for (int i = 0; i < _hiddenCanvases.Count; i++)
                    if (_hiddenCanvases[i] != null) svm.Show(_hiddenCanvases[i].gameObject, true);

                for (int i = 0; i < _preHidden.Count; i++)
                    if (_preHidden[i] != null) svm.Hide(_preHidden[i], false);
            }

            _hiddenCanvases.Clear();
            _preHidden.Clear();
            SessionState.SetString(HiddenIdsKey, string.Empty);
            ClearLedger();
        }

        // SessionState 跨域重载存活，用它把跟踪列表接回来，避免隐藏状态变成孤儿
        private static void SaveHiddenIds()
        {
            // Play 里场景对象是临时实例，InstanceID 出了 Play 就失效。存进去只会把编辑期
            // 那套有效 id 覆盖掉，下次重载就再也认不回来 —— 而这正是这个 key 存在的意义。
            if (EditorApplication.isPlaying) return;

            var sb = new StringBuilder();
            for (int i = 0; i < _hiddenCanvases.Count; i++)
            {
                if (_hiddenCanvases[i] == null) continue;
                if (sb.Length > 0) sb.Append(',');
                sb.Append(_hiddenCanvases[i].gameObject.GetInstanceID());
            }
            SessionState.SetString(HiddenIdsKey, sb.ToString());
        }

        private static void AdoptHiddenCanvases()
        {
            _hiddenCanvases.Clear();
            string raw = SessionState.GetString(HiddenIdsKey, string.Empty);
            if (string.IsNullOrEmpty(raw)) return;

            string[] parts = raw.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                int id;
                if (!int.TryParse(parts[i], out id)) continue;
                var go = EditorUtility.InstanceIDToObject(id) as GameObject;
                if (go == null) continue;
                var canvas = go.GetComponent<Canvas>();
                if (canvas != null) _hiddenCanvases.Add(canvas);
            }
        }

        // ── 场景里的覆盖范围框 ────────────────────────────────────────

        private static void DrawWorldBounds(List<Canvas> roots)
        {
            Handles.color = BoundsColor;
            var corners = new Vector3[4];

            for (int i = 0; i < roots.Count; i++)
            {
                var rt = roots[i].transform as RectTransform;
                if (rt == null) continue;

                rt.GetWorldCorners(corners);
                for (int e = 0; e < 4; e++)
                    Handles.DrawDottedLine(corners[e], corners[(e + 1) % 4], 4f);

                Handles.Label(corners[1], string.Format("  UI 覆盖范围 {0:F1} x {1:F1}",
                    Vector3.Distance(corners[0], corners[3]),
                    Vector3.Distance(corners[0], corners[1])));
            }
        }

        // ── 左侧预览面板 ──────────────────────────────────────────────

        // 面板矩形算一份给两处用：Repaint 时画，鼠标事件时做命中。
        // 抽出来是为了让"画的"和"点的"永远是同一个矩形。
        private static bool TryGetPanelRect(SceneView sv, out Rect rect)
        {
            rect = default(Rect);

            var viewSize = sv.position;
            float maxW = viewSize.width * MaxWidthRatio;
            float maxH = viewSize.height - ToolbarHeight - Margin * 2f;
            if (maxW < 48f || maxH < 48f) return false;// 视图太小，画了也看不清

            float w = Mathf.Min(maxW, maxH * Aspect);
            rect = new Rect(Margin, ToolbarHeight + Margin, w, w / Aspect);
            return true;
        }

        // 摆在标题行右端，不压住预览画面
        private static Rect GetCloseRect(Rect panel)
        {
            return new Rect(panel.xMax - CloseButtonSize, panel.y - CloseButtonSize - 2f,
                CloseButtonSize, CloseButtonSize);
        }

        private static void DrawPreviewPanel(SceneView sv, List<Canvas> roots)
        {
            Rect rect;
            if (!TryGetPanelRect(sv, out rect)) return;

            float w = rect.width, h = rect.height;

            int uiMask = BuildUiLayerMask(roots);
            if (uiMask == 0) return;

            EnsurePreview(Mathf.RoundToInt(w), Mathf.RoundToInt(h));
            RefreshSuppressFlag(roots, uiMask);

            Handles.BeginGUI();
            try
            {
                // Overlay 画布不经过相机渲染路径，Camera.Render() 对它完全无效。
                // 渲染期间临时切成 WorldSpace，渲染完立刻切回（与临时激活面板同一类操作）。
                SwitchOverlayCanvases(roots);
                try
                {
                    ConfigurePreviewCamera(roots[0], uiMask);

                    bool suppress = _needSuppress;
                    if (suppress) SuppressNonUi(roots, uiMask);

                    ActivateInactivePanels(roots);
                    try
                    {
                        _previewCamera.Render();
                    }
                    finally
                    {
                        RestoreActivated();
                        if (suppress) RestoreSuppressed();
                    }
                }
                finally
                {
                    RestoreOverlayCanvases();
                }

                if (_previewRT != null) GUI.DrawTexture(rect, _previewRT, ScaleMode.StretchToFill, false);
                DrawPanelBorder(rect);

                GUI.Label(new Rect(rect.x, rect.y - 18f, rect.width, 18f),
                    string.Format("UI 预览  {0:F0} x {1:F0}      × 关闭", w, h));

                var close = GetCloseRect(rect);
                EditorGUI.DrawRect(close, CloseButtonColor);
                GUI.Label(close, "×");
            }
            finally
            {
                Handles.EndGUI();
            }
        }

        private static void DrawPanelBorder(Rect r)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, BorderThickness), BorderColor);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - BorderThickness, r.width, BorderThickness), BorderColor);
            EditorGUI.DrawRect(new Rect(r.x, r.y, BorderThickness, r.height), BorderColor);
            EditorGUI.DrawRect(new Rect(r.xMax - BorderThickness, r.y, BorderThickness, r.height), BorderColor);
        }

        private static void ConfigurePreviewCamera(Canvas rootCanvas, int uiMask)
        {
            var cam = _previewCamera;

            cam.cullingMask = uiMask;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BackgroundColor;
            cam.orthographic = false;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
            cam.fieldOfView = 60f;

            // 首选复刻主相机，预览成像才和 Game 视图一致。
            // 前提是主相机确实看得见这块 UI：实测 TestScene 的 Canvas 被摆在主相机视锥外面
            // （四角视口 x 全为负），照抄主相机只会得到一块全空的面板。
            // 另外 Overlay 画布按定义就是贴在屏幕上的，跟主相机在哪无关，一律直接框住。
            var mainCam = Camera.main;
            if (mainCam != null
                && !_flippedCanvases.Contains(rootCanvas)
                && IsCanvasInView(mainCam, rootCanvas))
            {
                cam.transform.SetPositionAndRotation(mainCam.transform.position, mainCam.transform.rotation);
                cam.fieldOfView = mainCam.fieldOfView;
                cam.nearClipPlane = mainCam.nearClipPlane;
                cam.farClipPlane = mainCam.farClipPlane;
                cam.orthographic = mainCam.orthographic;
                if (mainCam.orthographic) cam.orthographicSize = mainCam.orthographicSize;
                return;
            }

            FrameCanvas(cam, rootCanvas);
        }

        // 主相机能不能看见这块 UI。判据是"投影矩形与视口有没有交集"，而不是"有没有角落
        // 落在视口内"：这块 Canvas 是按相机可见范围量身定做的，四角恰好压在视口边界上，
        // 逐角比较会被浮点误差甩到外面，于是明明看得见却误走了兜底。
        // 只露出一部分时同样返回 true —— 那正是游戏里的真实样子，照抄主相机才对。
        private static bool IsCanvasInView(Camera probe, Canvas canvas)
        {
            var rt = canvas == null ? null : canvas.transform as RectTransform;
            if (rt == null) return false;

            rt.GetWorldCorners(_corners);
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < 4; i++)
            {
                var v = probe.WorldToViewportPoint(_corners[i]);
                if (v.z <= 0f) return false;// 有角落在相机背后，谈不上看得见
                if (v.x < minX) minX = v.x;
                if (v.x > maxX) maxX = v.x;
                if (v.y < minY) minY = v.y;
                if (v.y > maxY) maxY = v.y;
            }
            return maxX >= 0f && minX <= 1f && maxY >= 0f && minY <= 1f;
        }

        // 退路：正对 Canvas 架一台正交相机把它框满。
        // 用正交而不是透视，是因为相机一定沿 Canvas 法线正对，平面正对时两者成像一致；
        // 而且正交免掉了量纲问题 —— Overlay 画布的世界尺寸是 1920x1080 这个量级，
        // 透视相机得放到近千单位外才框得住，far clip 会被撑得很勉强。
        private static void FrameCanvas(Camera cam, Canvas rootCanvas)
        {
            var rt = rootCanvas == null ? null : rootCanvas.transform as RectTransform;
            if (rt == null) return;

            rt.GetWorldCorners(_corners);
            Vector3 center = (_corners[0] + _corners[2]) * 0.5f;
            Vector3 forward = rt.forward, up = rt.up;
            float w = Vector3.Distance(_corners[0], _corners[3]);
            float h = Vector3.Distance(_corners[0], _corners[1]);
            if (w <= 0f || h <= 0f) return;

            float depth = Mathf.Max(w, h) + 1f;// 正交下这个距离不影响成像，只要别是 0

            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(h * 0.5f, w * 0.5f / Aspect) * 1.02f;// 留一点边距
            cam.nearClipPlane = depth * 0.01f;
            cam.farClipPlane = depth * 3f;
            cam.transform.SetPositionAndRotation(center - forward * depth, Quaternion.LookRotation(forward, up));
        }

        // ── 画布渲染目标：渲染期间临时改掉，让预览相机渲染得到它 ──────────
        //
        // 两种屏幕空间画布预览相机都渲染不到，原因各不相同：
        // · ScreenSpaceOverlay 根本不走相机渲染路径，只能先切成 WorldSpace 当一块世界平面画；
        // · ScreenSpaceCamera 只认自己的 worldCamera，得把 worldCamera 临时换成预览相机。
        //   后者是实测出来的：不换的话 _previewRT 恒为纯清屏色（0 个非背景像素）。

        private static void SwitchOverlayCanvases(List<Canvas> roots)
        {
            _flippedCanvases.Clear();
            _recameraedCanvases.Clear();
            _recameraedOriginals.Clear();

            for (int i = 0; i < roots.Count; i++)
            {
                var c = roots[i];
                if (c == null) continue;

                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    c.renderMode = RenderMode.WorldSpace;
                    _flippedCanvases.Add(c);
                }
                else if (c.renderMode == RenderMode.ScreenSpaceCamera && c.worldCamera != _previewCamera)
                {
                    // 它是按相机视野排版的，换了相机就等于按预览视野排版 ——
                    // 成像比切成 WorldSpace 更忠实，因为排版规则本身没变。
                    _recameraedCanvases.Add(c);
                    _recameraedOriginals.Add(c.worldCamera);
                    c.worldCamera = _previewCamera;
                }
            }
        }

        private static void RestoreOverlayCanvases()
        {
            for (int i = 0; i < _flippedCanvases.Count; i++)
                if (_flippedCanvases[i] != null) _flippedCanvases[i].renderMode = RenderMode.ScreenSpaceOverlay;
            _flippedCanvases.Clear();

            for (int i = 0; i < _recameraedCanvases.Count; i++)
                if (_recameraedCanvases[i] != null) _recameraedCanvases[i].worldCamera = _recameraedOriginals[i];
            _recameraedCanvases.Clear();
            _recameraedOriginals.Clear();
        }

        // ── 同层污染处理 ──────────────────────────────────────────────

        // 面板预制体在 Default 层，若该层也有场景物体，预览相机会把它们一起渲染进来。
        // 这里只在确实存在冲突时才做抑制，正常情况下零开销。
        private static int BuildUiLayerMask(List<Canvas> roots)
        {
            int mask = 0;
            for (int i = 0; i < roots.Count; i++)
                foreach (var t in roots[i].GetComponentsInChildren<Transform>(true))
                    mask |= 1 << t.gameObject.layer;
            return mask;
        }

        private static bool IsUnderAnyRoot(Transform t, List<Canvas> roots)
        {
            for (int i = 0; i < roots.Count; i++)
                if (roots[i] != null && t.IsChildOf(roots[i].transform)) return true;
            return false;
        }

        private static void RefreshSuppressFlag(List<Canvas> roots, int uiMask)
        {
            if (EditorApplication.timeSinceStartup < _nextSuppressCheck) return;
            _nextSuppressCheck = EditorApplication.timeSinceStartup + SuppressCheckInterval;

            _needSuppress = false;
            var renderers = Object.FindObjectsOfType<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if ((uiMask & (1 << renderers[i].gameObject.layer)) == 0) continue;
                if (IsUnderAnyRoot(renderers[i].transform, roots)) continue;
                _needSuppress = true;
                return;
            }
        }

        private static void SuppressNonUi(List<Canvas> roots, int uiMask)
        {
            _suppressed.Clear();
            var renderers = Object.FindObjectsOfType<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if ((uiMask & (1 << r.gameObject.layer)) == 0) continue;
                if (IsUnderAnyRoot(r.transform, roots)) continue;
                if (r.forceRenderingOff) continue;// 本来就关着的，别接管
                r.forceRenderingOff = true;
                _suppressed.Add(r);
            }
        }

        private static void RestoreSuppressed()
        {
            for (int i = 0; i < _suppressed.Count; i++)
                if (_suppressed[i] != null) _suppressed[i].forceRenderingOff = false;
            _suppressed.Clear();
        }

        // ── 临时激活停用面板 ──────────────────────────────────────────

        // 只在渲染前后同步做，中间不会有机会存盘，所以文件内容不受影响
        private static void ActivateInactivePanels(List<Canvas> roots)
        {
            _activated.Clear();
            for (int i = 0; i < roots.Count; i++)
            {
                var t = roots[i].transform;
                for (int c = 0; c < t.childCount; c++)
                {
                    var child = t.GetChild(c).gameObject;
                    if (child.activeSelf) continue;
                    child.SetActive(true);
                    _activated.Add(child);
                }
            }
        }

        private static void RestoreActivated()
        {
            for (int i = 0; i < _activated.Count; i++)
                if (_activated[i] != null) _activated[i].SetActive(false);
            _activated.Clear();
        }

        // ── 隐藏账本 ──────────────────────────────────────────────────
        //
        // SceneVisibilityManager 的隐藏状态存在 Library/SceneVisibilityState.asset 里，跨 Unity
        // 重启存活；而记着"这些是本工具隐藏的"的 SessionState 不存活。两者一错配，就会出现
        // 一个既没人认领、工具也管不着、在 Hierarchy 里孤零零划着眼睛的 Canvas。
        // 账本把这份记忆落到 Library 下，寿命与隐藏状态一致。

        private static void SaveLedger()
        {
            // 同 SaveHiddenIds：账本记的必须是编辑期场景里的东西
            if (EditorApplication.isPlaying) return;

            try
            {
                var sb = new StringBuilder();
                for (int i = 0; i < _hiddenCanvases.Count; i++)
                {
                    var canvas = _hiddenCanvases[i];
                    if (canvas == null) continue;
                    sb.Append(canvas.gameObject.scene.path)
                      .Append('\t')
                      .Append(GetHierarchyPath(canvas.transform))
                      .Append('\n');
                }

                if (sb.Length == 0) ClearLedger();
                else File.WriteAllText(LedgerPath, sb.ToString());
            }
            catch (Exception e)
            {
                // 账本写不进去不该影响工具本身，最坏情况也只是下次重启留下孤儿
                ChaosLog.Warn("[UI分离显示] 隐藏账本写入失败：" + e.Message);
            }
        }

        private static void ClearLedger()
        {
            try
            {
                if (File.Exists(LedgerPath)) File.Delete(LedgerPath);
            }
            catch (Exception e)
            {
                ChaosLog.Warn("[UI分离显示] 隐藏账本清理失败：" + e.Message);
            }
        }

        // 工具是关的、账本却非空 —— 说明上次是开着工具被强杀的，按账本把该还原的还原掉。
        // 场景还没打开的先留在账本里，等它打开时（sceneOpened）再处理。
        private static void HealLedger()
        {
            if (Enabled) return;

            string path = LedgerPath;
            string[] lines;
            try
            {
                if (!File.Exists(path)) return;
                lines = File.ReadAllLines(path);
            }
            catch (Exception e)
            {
                ChaosLog.Warn("[UI分离显示] 隐藏账本读取失败：" + e.Message);
                return;
            }

            var svm = SceneVisibilityManager.instance;
            if (svm == null) return;

            var leftover = new StringBuilder();
            int restored = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i])) continue;

                string[] parts = lines[i].Split('\t');
                if (parts.Length != 2) continue;

                var scene = FindLoadedScene(parts[0]);
                if (!scene.IsValid()) { leftover.Append(lines[i]).Append('\n'); continue; }// 场景没开，下次再说

                var t = FindByPath(scene, parts[1]);
                if (t == null) continue;// 物体已经没了，这条作废

                svm.Show(t.gameObject, true);
                restored++;
            }

            try
            {
                if (leftover.Length == 0) { if (File.Exists(path)) File.Delete(path); }
                else File.WriteAllText(path, leftover.ToString());
            }
            catch (Exception e)
            {
                ChaosLog.Warn("[UI分离显示] 隐藏账本回写失败：" + e.Message);
            }

            if (restored > 0)
            {
                ChaosLog.Warn("[UI分离显示] 上次退出时工具是开着的，已还原 " + restored + " 个被隐藏的 Canvas");
                SceneView.RepaintAll();
            }
        }

        private static Scene FindLoadedScene(string scenePath)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.isLoaded && s.path == scenePath) return s;
            }
            return default(Scene);
        }

        private static string GetHierarchyPath(Transform t)
        {
            var sb = new StringBuilder(t.name);
            while (t.parent != null)
            {
                t = t.parent;
                sb.Insert(0, '/').Insert(0, t.name);
            }
            return sb.ToString();
        }

        private static Transform FindByPath(Scene scene, string path)
        {
            string[] parts = path.Split('/');
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name != parts[0]) continue;

                var cur = roots[i].transform;
                for (int p = 1; p < parts.Length && cur != null; p++) cur = cur.Find(parts[p]);
                if (cur != null) return cur;
            }
            return null;
        }

        // ── 预览渲染器生命周期 ────────────────────────────────────────

        private static void EnsurePreview(int width, int height)
        {
            if (_previewCamera == null)
            {
                var go = new GameObject("__SceneUIOverviewPreviewCam") { hideFlags = HideFlags.HideAndDontSave };
                _previewCamera = go.AddComponent<Camera>();
                // 只手动 Render，绝不能让它自己往屏幕上画
                _previewCamera.enabled = false;
                _previewCamera.hideFlags = HideFlags.HideAndDontSave;
            }

            width = Mathf.Clamp(width, 16, 4096);
            height = Mathf.Clamp(height, 16, 4096);
            if (_previewRT != null && _previewRT.width == width && _previewRT.height == height) return;

            ReleasePreviewRT();
            _previewRT = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            _previewRT.hideFlags = HideFlags.HideAndDontSave;
            _previewRT.Create();
            _previewCamera.targetTexture = _previewRT;
        }

        private static void ReleasePreviewRT()
        {
            if (_previewCamera != null) _previewCamera.targetTexture = null;
            if (_previewRT == null) return;
            _previewRT.Release();// 不释放会泄漏 RenderTexture
            Object.DestroyImmediate(_previewRT);
            _previewRT = null;
        }

        private static void CleanupPreview()
        {
            ReleasePreviewRT();
            if (_previewCamera == null) return;
            Object.DestroyImmediate(_previewCamera.gameObject);
            _previewCamera = null;
        }
    }

    /// <summary>
    /// Scene 视图工具栏上的开关按钮。overlay 由 Unity 的 Overlay 系统管理，
    /// 位置和显隐跟着布局走，不需要自己算工具栏坐标。
    /// </summary>
    [Overlay(typeof(SceneView), OverlayId, "UI 分离显示", true)]
    public class SceneUIOverviewOverlay : Overlay
    {
        public const string OverlayId = "scene-ui-overview-tool";

        public override VisualElement CreatePanelContent()
        {
            var toggle = new Toggle("UI 分离显示") { value = SceneUIOverviewTool.Enabled };
            toggle.tooltip = "把 Canvas 从 Scene 视图里摘出来，单独在左侧预览";

            toggle.RegisterValueChangedCallback(evt => SceneUIOverviewTool.Enabled = evt.newValue);

            // 菜单、面板上的 ×、别的 Scene 视图都能改这个开关，不回灌的话这里会一直
            // 停在创建时的旧值上，点一下反而像"没反应"。
            Action<bool> sync = v => { if (toggle.value != v) toggle.SetValueWithoutNotify(v); };
            SceneUIOverviewTool.EnabledChanged += sync;
            toggle.RegisterCallback<DetachFromPanelEvent>(evt => SceneUIOverviewTool.EnabledChanged -= sync);

            return toggle;
        }
    }
}
