using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 世界观地图绘制工具（编辑器窗口）。
///
/// ══════════════════ 它做什么 ══════════════════
/// 读一张自定义底图 → 在图上摆**固定地点 / 兴趣点**、画**势力范围多边形** → 导出成两张配表的 `.xlsx`
/// （7 行表头格式，与 `TestTable` / `LoadingTable` 一致）。产出物与链路见
/// `Story/03_配置表设计/05_WorldMap_字段规格.md` / `06_WorldFaction_字段规格.md`、
/// `Story/02_系统策划/02_局外世界观地图系统.md` §7。
///
/// ⚠ 本窗口产出的 xlsx **不要手工改**：改了就和草稿不一致，下次重导会被覆盖。
///
/// ══════════════════ 三个设计上的硬点 ══════════════════
/// ① **`GUI.BeginClip` 【会】平移坐标原点** —— 这一条以前写反了，正是"所见≠所点"的根因。
///    **实测**（2022.3.57f1c2；探针 `WMClipProbe`，pixelsPerPoint=1.25）：
///      `clipRect=(120,80,300,200)` 时，剪辑内 `(0,0)` 的屏幕点 **等于** 剪辑外 `clipRect.position` 的屏幕点，
///      且 `delta = clipRect.position = (120,80)` → **与 `BeginGroup` 同源、确实平移原点**。
///    → `BeginClip` ~ `EndClip` 之间**一律用剪辑局部坐标**（见 `ToClipLocal` / `imageRectLocal`）；
///      而**鼠标命中判定继续用窗口坐标**（鼠标事件本身就是窗口坐标，不要跟着改）。
///      两者混用 = 用户报的"点击位置和实际不一致"。
/// ② **Y 轴翻转只在 `WorldMapToolGeometry` 里做**：本文件一律调它换算，**不自己写 `1 - y`**。
/// ③ ①有机器自检兜着：菜单 `Tools/局外地图/8. 校验坐标对齐（所见即所点）`。
/// </summary>
public class WorldMapToolWindow : EditorWindow
{
    private const string MenuPath = "Tools/局外地图/0. 打开世界观地图绘制工具";

    /// <summary>撤销栈上限（任务要求 ≥ 覆盖最近一次落点/顶点，上限 50）。</summary>
    private const int UndoLimit = 50;

    private enum ToolMode
    {
        Select,
        PlaceLocation,      //Kind = 1
        PlacePoi,           //Kind = 2
        DrawFactionPolygon,
        Erase,
    }

    [MenuItem(MenuPath, false, 100)]
    public static void Open()
    {
        var window = GetWindow<WorldMapToolWindow>("世界观地图绘制工具");
        window.minSize = new Vector2(1100f, 640f);
        window.Show();
    }

    /// <summary>
    /// **机器自检：所见 = 所点**（菜单 `Tools/局外地图/8.`）。
    ///
    /// 这里只做"标记 + 重绘"；真正的断言在 `RunAlignmentCheck` 里、
    /// **Repaint 且位于 `BeginClip` 之内**时才执行 —— 只有那一刻才能量到剪辑原点的真实屏幕位置。
    /// 以后改布局/缩放/绘制路径，跑一次这个菜单就知道有没有又错位。
    /// </summary>
    [MenuItem("Tools/局外地图/8. 校验坐标对齐（所见即所点）", false, 108)]
    private static void RequestAlignmentCheck()
    {
        var window = GetWindow<WorldMapToolWindow>("世界观地图绘制工具");
        if (window._draft == null)
        {
            Debug.LogError("[坐标对齐] 草稿未初始化，无法自检");
            return;
        }

        window._pendingAlignmentCheck = true;
        window.Repaint();
    }

    // ══════════════════ 状态 ══════════════════

    private WorldMapDraft _draft;
    private Texture2D _sourceTexture;
    private string _textureLoadedFor;
    private bool _initialized;

    private ToolMode _mode = ToolMode.Select;

    private float _zoom = 0.25f;
    private Vector2 _pan = new Vector2(20f, 20f);
    private bool _panInitialized;

    private Rect _canvasRect;

    /// <summary>菜单 8 请求的一次性坐标自检（在 Repaint + 剪辑内执行）。</summary>
    private bool _pendingAlignmentCheck;

    private bool _isPanning;
    private bool _spaceHeld;

    /// <summary>
    /// **笔尖**：下一个顶点插在它**之后**（用来表达"从任意点续画"）。索引指向 `_draft.factionVertices`。
    /// **-1 = 自动**（取当前 (时期,势力,区域) 的最后一个顶点）。
    /// </summary>
    private int _penHeadIndex = -1;

    /// <summary>当前选中的**顶点**索引（-1 = 没选）；「从选中顶点继续」用它。</summary>
    private int _selectedVertexIndex = -1;

    /// <summary>画布内鼠标位置（**窗口坐标**）。所有鼠标事件都更新；绘制前要过 `ToClipLocal`。</summary>
    private Vector2 _mouseCanvasPos;
    private bool _mouseInsideCanvas;

    /// <summary>是否画"收口边"的淡虚线（默认开）。**虚线 ≠ 生成顺序**，只表示导出时会自动闭合。</summary>
    private bool _showClosingDash = true;

    //「选择移动」模式的拖动状态：正在拖第几个点位/顶点
    private int _dragIndex = -1;
    private bool _dragIsPoint;
    private bool _dragUndoPushed;

    private int _selectedPoint = -1;
    private int _selectedRegion = -1;

    // 放置参数（Kind=1/2 共用）
    private uint _placeType = 1;
    private uint _placeRefId = 1001;
    private float _placeScale = 1f;
    private uint _placeLayer;

    // 势力参数
    private uint _stage = 1;
    private uint _factionId = 1;
    private uint _regionId = 1;
    private string _factionColor = "#2F3A4F";

    private readonly List<string> _undoStack = new List<string>();
    private readonly List<WorldMapToolIssue> _issues = new List<WorldMapToolIssue>();
    private string _statusMessage;

    private Vector2 _pointListScroll;
    private Vector2 _regionListScroll;
    private Vector2 _issueScroll;

    // ══════════════════ 生命周期 ══════════════════

    private void OnEnable()
    {
        if (_initialized && _draft != null) return;

        string message;
        _draft = WorldMapToolDataIO.Load(out message);
        if (_draft == null)
        {
            _draft = WorldMapToolDataIO.CreateDefault();
            _statusMessage = message + " → 已新建默认草稿（改完记得保存）";
        }
        else
        {
            _statusMessage = message;
        }

        _initialized = true;
        RefreshSourceTexture();
        ValidateNow();
    }

    private void OnDisable()
    {
        //注：不做自动保存 —— 草稿是"作者的东西"，静默写盘容易覆盖掉别人的意图。
        //未保存状态在工具条上以醒目颜色提示，关窗后下次打开会提示"有未保存改动"。
    }

    // ══════════════════ 延迟变更（IMGUI 硬要求） ══════════════════
    //
    // 在任何布局组【内部】直接改列表（或改元素数量），会让同一次 OnGUI 的 Layout 与 Repaint
    // 元素数不一致 → Unity 报 "Invalid GUILayout state" 并画错版面。
    // 所以所有"会改变元素数量"的变更都只【登记意图】，统一在 EventType.Layout 之前应用。
    // （实测：最初的实现用 GUIUtility.ExitGUI() + 直接 RemoveAt，开窗即连续报 4 条该错误。）

    private int _pendingDeletePoint = -1;
    private int _pendingDeleteRegion = -1;
    private bool _pendingDeleteSelection;
    private bool _pendingClearPoints;
    private bool _pendingClearVertices;
    private int _pendingMode = -1;
    private bool _hasPendingClick;
    private Vector2 _pendingClickPosition;

    /// <summary>把上一步登记的变更真正落实到草稿（只在 `EventType.Layout` 之前调用）。</summary>
    private void ApplyPendingState()
    {
        if (_pendingMode >= 0) { _mode = (ToolMode)_pendingMode; _pendingMode = -1; }

        if (_pendingDeletePoint >= 0)
        {
            if (_draft != null && _pendingDeletePoint < _draft.points.Count)
            {
                PushUndo("删除点位 #" + (_pendingDeletePoint + 1));
                _draft.points.RemoveAt(_pendingDeletePoint);
                _selectedPoint = -1;
                ValidateNow();
            }
            _pendingDeletePoint = -1;
        }

        if (_pendingDeleteRegion >= 0)
        {
            List<WorldMapFactionRegion> regions = WorldMapToolDataIO.GroupRegions(_draft);
            if (_pendingDeleteRegion < regions.Count)
            {
                PushUndo("删除区域");
                WorldMapFactionRegion region = regions[_pendingDeleteRegion];
                for (int i = region.sourceIndices.Count - 1; i >= 0; i--)
                    _draft.factionVertices.RemoveAt(region.sourceIndices[i]);
                _selectedRegion = -1;
                ResetPenHead(null);   //删除会移动下标 → 笔尖必须重置
                ValidateNow();
            }
            _pendingDeleteRegion = -1;
        }

        if (_pendingDeleteSelection) { _pendingDeleteSelection = false; DeleteSelection(); }

        if (_pendingClearPoints)
        {
            _pendingClearPoints = false;
            if (_draft != null && _draft.points.Count > 0)
            {
                PushUndo("清空点位");
                _draft.points.Clear();
                _selectedPoint = -1;
                ValidateNow();
            }
        }

        if (_pendingClearVertices)
        {
            _pendingClearVertices = false;
            if (_draft != null && _draft.factionVertices.Count > 0)
            {
                PushUndo("清空势力顶点");
                _draft.factionVertices.Clear();
                _selectedRegion = -1;
                ResetPenHead(null);
                ValidateNow();
            }
        }

        if (_hasPendingClick)
        {
            _hasPendingClick = false;
            OnCanvasLeftClick(_pendingClickPosition);
        }
    }

    // ══════════════════ 撤销栈 ══════════════════

    /// <summary>在**改动之前**压入快照（快照 = 整份草稿的 Json，稳且简单）。</summary>
    private void PushUndo(string label)
    {
        if (_draft == null) return;

        _undoStack.Add(JsonUtility.ToJson(_draft, false));
        while (_undoStack.Count > UndoLimit) _undoStack.RemoveAt(0);
        _draft.dirty = true;
        _statusMessage = "已记录撤销点：" + label + "（栈 " + _undoStack.Count + "/" + UndoLimit + "）";
    }

    private void PerformUndo()
    {
        if (_undoStack.Count == 0)
        {
            _statusMessage = "没有可撤销的操作";
            return;
        }

        int last = _undoStack.Count - 1;
        string snapshot = _undoStack[last];
        _undoStack.RemoveAt(last);

        _draft = JsonUtility.FromJson<WorldMapDraft>(snapshot);
        if (_draft != null)
        {
            _draft.dirty = true;
            if (_draft.points == null) _draft.points = new List<WorldMapPointEntry>();
            if (_draft.factionVertices == null) _draft.factionVertices = new List<WorldMapFactionVertex>();
        }
        _selectedPoint = -1;
        _selectedRegion = -1;
        ResetPenHead(null);   //撤销会整体回滚 → 顶点下标失效，笔尖必须重置
        _statusMessage = "已撤销（剩余 " + _undoStack.Count + " 步）";
        RefreshSourceTexture();
        ValidateNow();
    }

    // ══════════════════ 源图 ══════════════════

    private void RefreshSourceTexture()
    {
        if (_draft == null) return;
        if (_textureLoadedFor == _draft.sourceImagePath && _sourceTexture != null) return;

        _sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(_draft.sourceImagePath);
        _textureLoadedFor = _draft.sourceImagePath;

        if (_sourceTexture == null)
        {
            ChaosLogSafe("源图加载失败（路径不对或不是 Texture）：" + _draft.sourceImagePath);
        }
    }

    /// <summary>从 PNG 文件头刷新真实像素尺寸（**不要用 Texture2D.width**，导入设置会降采样）。</summary>
    private void RefreshImageSizeFromPngHeader()
    {
        if (_draft == null) return;

        int width, height;
        if (WorldMapToolDataIO.TryReadPngSize(_draft.sourceImagePath, out width, out height))
        {
            _draft.imageWidth = width;
            _draft.imageHeight = height;
            _draft.dirty = true;
            _statusMessage = "已从 PNG 文件头读到真实尺寸：" + width + " × " + height;
        }
        else
        {
            _statusMessage = "读 PNG 文件头失败（文件不存在或不是 PNG）：" + _draft.sourceImagePath;
        }
    }

    private static void ChaosLogSafe(string message)
    {
        Debug.LogWarning("[局外地图] " + message);
    }

    // ══════════════════ 布局与绘制 ══════════════════

    private void OnGUI()
    {
        if (_draft == null)
        {
            EditorGUILayout.HelpBox("草稿未初始化，请重新打开窗口。", MessageType.Error);
            return;
        }

        //⚠ OnGUI 里一旦抛异常，下面的 End* 就不会执行 → Unity 报
        //   "Invalid GUILayout state"（错误信息完全不指向真正的抛出点）。
        //   这里兜住并把异常原文打出来，免得以后再花时间猜。
        try
        {
            //⚠ 所有"会改变元素数量"的变更必须在 Layout 之前统一应用（详见 ApplyPendingState 注释）
            if (Event.current.type == EventType.Layout) ApplyPendingState();

            HandleGlobalKeys();

            DrawToolbar();

            EditorGUILayout.BeginHorizontal();
            DrawPointListPanel();
            DrawCanvasPanel();
            DrawRegionListPanel();
            EditorGUILayout.EndHorizontal();

            DrawBottomPanel();
        }
        catch (Exception e)
        {
            Debug.LogError("[局外地图] OnGUI 抛异常（会导致 GUILayout Begin/End 不配对）：" + e);
        }
    }

    private void HandleGlobalKeys()
    {
        Event e = Event.current;
        if (e.type != EventType.KeyDown) return;

        if (e.keyCode == KeyCode.Space) { _spaceHeld = true; e.Use(); return; }

        if (e.control && e.keyCode == KeyCode.Z)
        {
            PerformUndo();
            e.Use();
            Repaint();
        }
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // ── 第 1 行：源图 + MapId + 尺寸 ──
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("源图", GUILayout.Width(34f));
        EditorGUI.BeginChangeCheck();
        string path = EditorGUILayout.TextField(_draft.sourceImagePath);
        if (EditorGUI.EndChangeCheck())
        {
            _draft.sourceImagePath = path;
            _draft.dirty = true;
            RefreshSourceTexture();
        }
        if (GUILayout.Button("默认源图", GUILayout.Width(72f)))
        {
            _draft.sourceImagePath = WorldMapToolDataIO.DefaultSourceImagePath;
            _draft.dirty = true;
            RefreshSourceTexture();
            RefreshImageSizeFromPngHeader();
        }
        if (GUILayout.Button("读 PNG 尺寸", GUILayout.Width(90f))) RefreshImageSizeFromPngHeader();

        EditorGUILayout.LabelField("MapId", GUILayout.Width(42f));
        EditorGUI.BeginChangeCheck();
        int mapId = EditorGUILayout.IntField((int)_draft.mapId, GUILayout.Width(48f));
        if (EditorGUI.EndChangeCheck()) { _draft.mapId = (uint)Mathf.Max(1, mapId); _draft.dirty = true; }

        EditorGUILayout.LabelField("真实像素 " + _draft.imageWidth + "×" + _draft.imageHeight,
            GUILayout.Width(150f));
        EditorGUILayout.EndHorizontal();

        // ── 第 2 行：模式 ──
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("模式", GUILayout.Width(34f));
        int pickedMode = GUILayout.Toolbar((int)_mode, new[]
        {
            "选择/移动", "放地点(Kind=1)", "放兴趣点(Kind=2)", "画势力多边形", "擦除",
        }, GUILayout.Height(20f));
        //延后应用：模式切换会改变第 3 行的控件数量，直接改会让同帧 Layout/Repaint 元素数不一致
        if (pickedMode != (int)_mode) _pendingMode = pickedMode;
        EditorGUILayout.EndHorizontal();

        // ── 第 3 行：参数（随模式切换） ──
        EditorGUILayout.BeginHorizontal();
        if (_mode == ToolMode.PlaceLocation || _mode == ToolMode.PlacePoi)
        {
            EditorGUILayout.LabelField("Type", GUILayout.Width(34f));
            _placeType = (uint)Mathf.Max(0, EditorGUILayout.IntField((int)_placeType, GUILayout.Width(60f)));
            EditorGUILayout.LabelField("RefId", GUILayout.Width(38f));
            _placeRefId = (uint)Mathf.Max(0, EditorGUILayout.IntField((int)_placeRefId, GUILayout.Width(70f)));
            EditorGUILayout.LabelField("Scale", GUILayout.Width(38f));
            _placeScale = EditorGUILayout.FloatField(_placeScale, GUILayout.Width(50f));
            EditorGUILayout.LabelField("Layer", GUILayout.Width(38f));
            _placeLayer = (uint)Mathf.Max(0, EditorGUILayout.IntField((int)_placeLayer, GUILayout.Width(46f)));
        }
        else if (_mode == ToolMode.DrawFactionPolygon)
        {
            EditorGUILayout.LabelField("时期", GUILayout.Width(34f));
            uint prevStage = _stage;
            _stage = (uint)EditorGUILayout.IntPopup((int)_stage,
                new[] { "1 战国末期", "2 秦朝", "3 秦末起义初期", "4 楚汉争霸", "5 汉刘邦时期" },
                new[] { 1, 2, 3, 4, 5 }, GUILayout.Width(140f));

            EditorGUILayout.LabelField("FactionId", GUILayout.Width(64f));
            uint prevFaction = _factionId;
            _factionId = (uint)Mathf.Max(0, EditorGUILayout.IntField((int)_factionId, GUILayout.Width(50f)));

            //(时期,势力) 一变就自动带出该势力**已有区域**的颜色与推荐 RegionId ——
            //不做这一步最容易画出"同一势力两块不同色"（飞地在本土之外，颜色却不一样）
            if (_stage != prevStage || _factionId != prevFaction) OnFactionContextChanged();

            EditorGUILayout.LabelField("RegionId", GUILayout.Width(58f));
            _regionId = (uint)Mathf.Max(1, EditorGUILayout.IntField((int)_regionId, GUILayout.Width(50f)));

            if (GUILayout.Button("新建区域(同势力)", GUILayout.Width(130f))) CreateNewRegionForCurrentFaction();

            _showClosingDash = EditorGUILayout.ToggleLeft("显示闭合虚线", _showClosingDash, GUILayout.Width(104f));

            EditorGUILayout.LabelField("颜色", GUILayout.Width(34f));
            _factionColor = EditorGUILayout.TextField(_factionColor, GUILayout.Width(80f));
            if (GUILayout.Button("按势力取色", GUILayout.Width(84f)))
            {
                string suggested;
                if (WorldMapToolDataIO.SuggestedFactionColors.TryGetValue(_factionId, out suggested))
                    _factionColor = suggested;
            }
            //始终占位（元素数量恒定），只在非法时显示文字
            EditorGUILayout.LabelField(WorldMapToolValidator.IsValidColor(_factionColor) ? "" : "颜色须为 #RRGGBB",
                EditorStyles.miniLabel, GUILayout.Width(120f));
        }
        else
        {
            EditorGUILayout.LabelField("（选择/移动 或 擦除 模式无放置参数）", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndHorizontal();

        // ── 第 3.5 行：笔尖控制（仅"画势力"模式；模式切换已延后到 Layout，所以同帧元素数稳定） ──
        if (_mode == ToolMode.DrawFactionPolygon)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("笔尖", GUILayout.Width(34f));

            int headIndex = EffectivePenHeadIndex();
            string headText = headIndex < 0
                ? "（当前区域还没有顶点）"
                : ("第 " + (headIndex + 1) + " 个顶点" + (IsPenHeadExplicit() ? "（手动指定）" : "（自动=末点）"));
            EditorGUILayout.LabelField(headText, GUILayout.Width(200f));

            bool canUseSelected = _selectedVertexIndex >= 0
                && _selectedVertexIndex < _draft.factionVertices.Count
                && IsInCurrentRegion(_draft.factionVertices[_selectedVertexIndex]);
            EditorGUI.BeginDisabledGroup(!canUseSelected);
            if (GUILayout.Button("从选中顶点继续", GUILayout.Width(120f)))
                SetPenHead(_selectedVertexIndex, "从选中顶点继续");
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("回到末点", GUILayout.Width(80f)))
                ResetPenHead("笔尖已回到末点（自动 = 当前区域最后一个顶点）");

            EditorGUILayout.LabelField("虚线=导出时自动闭合，不是生成顺序", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        // ── 第 4 行：草稿 / 视图 / 撤销 ──
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("载入草稿", GUILayout.Width(80f))) LoadDraftWithConfirm();
        if (GUILayout.Button("保存草稿", GUILayout.Width(80f))) SaveDraft();
        if (GUILayout.Button("撤销 (Ctrl+Z)", GUILayout.Width(100f))) PerformUndo();
        if (GUILayout.Button("适应窗口", GUILayout.Width(80f))) FitToWindow();

        EditorGUILayout.LabelField("缩放", GUILayout.Width(34f));
        _zoom = EditorGUILayout.Slider(_zoom, 0.02f, 2f);

        EditorGUILayout.LabelField("撤销栈 " + _undoStack.Count + "/" + UndoLimit, GUILayout.Width(110f));

        //始终占位（元素数量恒定）
        Color savedDirtyColor = GUI.color;
        if (_draft.dirty) GUI.color = new Color(1f, 0.75f, 0.3f);
        EditorGUILayout.LabelField(_draft.dirty ? "● 有未保存改动" : "", EditorStyles.boldLabel, GUILayout.Width(110f));
        GUI.color = savedDirtyColor;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    // ══════════════════ 画布 ══════════════════

    private void DrawCanvasPanel()
    {
        EditorGUILayout.BeginVertical();
        _canvasRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none,
            GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        if (!_panInitialized && Event.current.type == EventType.Repaint && _canvasRect.width > 10f)
        {
            FitToWindow();
            _panInitialized = true;
        }

        HandleCanvasInput();

        //⚠ 只在 Repaint 事件里做实际绘制：在 Layout 事件里调 GUI.BeginClip、改 GUI.matrix、
        //   跑 GUI.DrawTexture，都会打乱 GUILayout 的内部状态 → Unity 报
        //   "Invalid GUILayout state in WorldMapToolWindow view"（实测，改前开窗即连续报错）。
        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(_canvasRect, new Color(0.16f, 0.16f, 0.18f, 1f));

            //自检要在剪辑外先量一次参考点（见 RunAlignmentCheck）
            bool doCheck = _pendingAlignmentCheck;
            Vector2 outZero = Vector2.zero, outClipPos = Vector2.zero;
            if (doCheck)
            {
                outZero = GUIUtility.GUIToScreenPoint(Vector2.zero);
                outClipPos = GUIUtility.GUIToScreenPoint(_canvasRect.position);
            }

            GUI.BeginClip(_canvasRect);

            //★ BeginClip 之后原点被平移到 _canvasRect.position（实测，见类注释①）
            //  → 剪辑内的一切绘制都必须用【剪辑局部坐标】；
            //    忘了减 _canvasRect.position 就是"所见≠所点"。
            Rect imageRectLocal = ToClipLocal(GetImageRect());

            if (_sourceTexture != null)
            {
                GUI.DrawTexture(imageRectLocal, _sourceTexture, ScaleMode.StretchToFill);
            }
            else
            {
                EditorGUI.DrawRect(imageRectLocal, new Color(0.25f, 0.12f, 0.12f, 1f));
            }

            DrawFactionPolygons(imageRectLocal);
            DrawPoints(imageRectLocal);

            if (doCheck)
            {
                _pendingAlignmentCheck = false;
                RunAlignmentCheck(outZero, outClipPos, imageRectLocal);
            }

            GUI.EndClip();

            //画布边框
            DrawRectOutline(_canvasRect, new Color(0.4f, 0.4f, 0.45f, 1f));
        }

        //提示文字用 GUI.Label（非 GUILayout）—— 不参与布局，Layout/Repaint 都能画
        GUI.Label(new Rect(_canvasRect.x + 6f, _canvasRect.y + 4f,
                _canvasRect.width - 12f, 20f),
            "滚轮缩放 / 中键或空格+左键拖拽平移 / 左键按当前模式操作　｜　实线=生成顺序，虚线=导出时自动闭合（不是生成顺序）",
            EditorStyles.miniLabel);

        //⚠ 必须与开头的 BeginVertical 配对！漏掉 EndVertical 会让【整个窗口】的 GUILayout 状态失衡，
        //   Unity 只在 HostView 层报 "Invalid GUILayout state in WorldMapToolWindow view"，
        //   错误信息完全不指向这里 —— 实测就是这一处漏配对（Begin/End 计数 17:16）。
        EditorGUILayout.EndVertical();
    }

    /// <summary>底图当前在**窗口坐标**下的矩形。命中判定用这个（鼠标事件是窗口坐标）。</summary>
    private Rect GetImageRect()
    {
        return new Rect(_pan.x, _pan.y, _draft.imageWidth * _zoom, _draft.imageHeight * _zoom);
    }

    /// <summary>
    /// 窗口坐标 → **剪辑局部坐标**（减掉 `_canvasRect.position`）。
    /// `BeginClip` 会把原点平移到 `_canvasRect.position`（实测，见类注释①），
    /// 所以剪辑内的绘制必须过这一层；**命中判定不要用**（鼠标事件是窗口坐标）。
    /// </summary>
    private Rect ToClipLocal(Rect windowRect)
    {
        return new Rect(windowRect.x - _canvasRect.x, windowRect.y - _canvasRect.y,
                        windowRect.width, windowRect.height);
    }

    /// <summary>窗口坐标 → 剪辑局部坐标（点）。橡皮筋的鼠标位置要用它（缓存的是窗口坐标）。</summary>
    private Vector2 ToClipLocal(Vector2 windowPoint)
    {
        return new Vector2(windowPoint.x - _canvasRect.x, windowPoint.y - _canvasRect.y);
    }

    // ══════════════════ 笔尖（"从任意点续画"） ══════════════════

    /// <summary>该顶点是否属于当前 (时期,势力,区域)。</summary>
    private bool IsInCurrentRegion(WorldMapFactionVertex v)
    {
        return v != null && v.stage == _stage && v.factionId == _factionId && v.regionId == _regionId;
    }

    /// <summary>当前区域内**最后一个顶点**在 draft 里的索引（该区域还没有顶点时返回 -1）。</summary>
    private int LastIndexOfCurrentRegion()
    {
        int last = -1;
        for (int i = 0; i < _draft.factionVertices.Count; i++)
            if (IsInCurrentRegion(_draft.factionVertices[i])) last = i;
        return last;
    }

    /// <summary>
    /// **实际生效的笔尖**：显式笔尖有效（在范围内且属于当前区域）就用它；
    /// 否则自动回退到"当前区域的最后一个顶点"（-1 = 该区域还没有顶点）。
    /// </summary>
    private int EffectivePenHeadIndex()
    {
        if (_penHeadIndex >= 0 && _penHeadIndex < _draft.factionVertices.Count
            && IsInCurrentRegion(_draft.factionVertices[_penHeadIndex]))
            return _penHeadIndex;

        return LastIndexOfCurrentRegion();
    }

    /// <summary>笔尖是否为"用户显式指定"（状态栏措辞用）。</summary>
    private bool IsPenHeadExplicit()
    {
        return _penHeadIndex >= 0 && _penHeadIndex < _draft.factionVertices.Count
            && IsInCurrentRegion(_draft.factionVertices[_penHeadIndex]);
    }

    /// <summary>笔尖顶点（没有则 null）。</summary>
    private WorldMapFactionVertex PenHeadVertex()
    {
        int i = EffectivePenHeadIndex();
        return (i >= 0 && i < _draft.factionVertices.Count) ? _draft.factionVertices[i] : null;
    }

    /// <summary>把某个顶点设为笔尖（会校验它属于当前区域）。</summary>
    private void SetPenHead(int vertexIndex, string why)
    {
        if (vertexIndex < 0 || vertexIndex >= _draft.factionVertices.Count) return;
        if (!IsInCurrentRegion(_draft.factionVertices[vertexIndex]))
        {
            _statusMessage = "该顶点不属于当前 时期/势力/区域，不能设为笔尖";
            return;
        }

        _penHeadIndex = vertexIndex;
        _selectedVertexIndex = vertexIndex;
        _statusMessage = "笔尖已移到第 " + (vertexIndex + 1) + " 个顶点（" + why + "），接下来从它连线";
    }

    /// <summary>清空笔尖 → 回到"自动 = 当前区域最后一个顶点"。</summary>
    private void ResetPenHead(string why)
    {
        _penHeadIndex = -1;
        _selectedVertexIndex = -1;
        if (!string.IsNullOrEmpty(why)) _statusMessage = why;
    }

    /// <summary>
    /// 新顶点要插入的位置 = **笔尖之后**。
    /// 笔尖无效/不属于当前区域时回退到"该区域在草稿里的最后一个顶点之后" ——
    /// **不要无脑 Add 到整个列表尾**：列表里混着其它区域，那样会把区域块打散（导出虽无害但难读）。
    /// </summary>
    private int InsertIndexAfterPenHead()
    {
        int head = EffectivePenHeadIndex();
        if (head >= 0 && head < _draft.factionVertices.Count) return head + 1;

        int last = LastIndexOfCurrentRegion();
        return last >= 0 ? last + 1 : _draft.factionVertices.Count;
    }

    /// <summary>
    /// 显示用"边计划"：**链边 = N-1**（生成顺序相邻的两点）；**收口边仅 N>=3 且开关打开**。
    /// 抽成纯函数是为了让绘制与自测**共用同一套判据**（验收 1 要断言边数）。
    /// </summary>
    private static void GetDisplayEdgePlan(int vertexCount, bool showClosing, out int chainCount, out bool hasClosing)
    {
        chainCount = vertexCount > 1 ? vertexCount - 1 : 0;
        hasClosing = showClosing && vertexCount >= 3;
    }

    /// <summary>画淡虚线：把一条线切成若干"实段/空段"，不引新依赖。</summary>
    private static void DrawDashedLine(Vector2 from, Vector2 to, Color color, float width, float seg, float gap)
    {
        Vector2 delta = to - from;
        float length = delta.magnitude;
        if (length < 0.5f) return;

        Vector2 dir = delta / length;
        float step = seg + gap;
        for (float done = 0f; done < length; done += step)
        {
            float end = Mathf.Min(done + seg, length);
            DrawLine(from + dir * done, from + dir * end, color, width);
        }
    }

    /// <summary>
    /// **所见 = 所点** 的机器自检（菜单 `Tools/局外地图/8.`）。必须在**剪辑内、Repaint** 里跑。
    ///
    /// 两条断言：
    ///   ① `|p - c| < 0.5`：命中路径（窗口坐标）的换算往返回到画布中心；
    ///   ② `|p2 - c| < 0.5`：**绘制路径**（剪辑局部坐标 + 剪辑原点）也落在同一个点上；
    ///   ③ 另外现场量一次剪辑原点：剪辑内 `(0,0)` 的屏幕点应等于剪辑外 `_canvasRect.position` 的屏幕点
    ///      —— 这条是根因证据，Unity 若改了 `BeginClip` 语义会先在这里炸出来。
    /// </summary>
    private void RunAlignmentCheck(Vector2 outZero, Vector2 outClipPos, Rect imageRectLocal)
    {
        Rect windowImageRect = GetImageRect();
        Vector2 c = _canvasRect.center;

        Vector2 n = WorldMapToolGeometry.CanvasToNormalized(c, windowImageRect);
        Vector2 p = WorldMapToolGeometry.NormalizedToCanvas(n, windowImageRect);                              //"所点"回算（窗口坐标）
        Vector2 p2 = _canvasRect.position + WorldMapToolGeometry.NormalizedToCanvas(n, imageRectLocal);      //"所见"回算（剪辑局部 + 剪辑原点）

        Vector2 insideZero = GUIUtility.GUIToScreenPoint(Vector2.zero);   //剪辑内 (0,0) 的屏幕点
        bool clipOffsets = (insideZero - outClipPos).magnitude < 0.5f;

        bool pass = (p - c).magnitude < 0.5f && (p2 - c).magnitude < 0.5f && clipOffsets;

        string msg = "[坐标对齐] c=" + c + " n=" + n + " p=" + p + " p2=" + p2
            + " | |p-c|=" + (p - c).magnitude.ToString("F3") + " |p2-c|=" + (p2 - c).magnitude.ToString("F3")
            + " | clipRect=" + _canvasRect + " imageRectLocal=" + imageRectLocal
            + " | insideZero=" + insideZero + " outClipPos=" + outClipPos
            + " | BEGINCLIP_OFFSETS_ORIGIN=" + clipOffsets
            + " || " + (pass ? "PASS" : "FAIL");

        if (pass) Debug.Log(msg); else Debug.LogError(msg);
        _statusMessage = msg;
    }

    private void FitToWindow()
    {
        if (_draft.imageWidth <= 0 || _draft.imageHeight <= 0) return;
        if (_canvasRect.width < 10f || _canvasRect.height < 10f) return;

        float scaleX = _canvasRect.width / _draft.imageWidth;
        float scaleY = _canvasRect.height / _draft.imageHeight;
        _zoom = Mathf.Max(0.02f, Mathf.Min(scaleX, scaleY) * 0.96f);

        _pan = new Vector2(
            _canvasRect.x + (_canvasRect.width - _draft.imageWidth * _zoom) * 0.5f,
            _canvasRect.y + (_canvasRect.height - _draft.imageHeight * _zoom) * 0.5f);
        Repaint();
    }

    private void HandleCanvasInput()
    {
        Event e = Event.current;

        //★ 鼠标位置**每个事件都缓存**（不能只在 Repaint 里取，否则橡皮筋会跟不上/不更新）
        _mouseCanvasPos = e.mousePosition;
        _mouseInsideCanvas = _canvasRect.Contains(e.mousePosition);

        if (e.type == EventType.KeyUp && e.keyCode == KeyCode.Space) { _spaceHeld = false; e.Use(); return; }

        bool inside = _mouseInsideCanvas;

        //滚轮缩放（以鼠标为锚点）
        if (e.type == EventType.ScrollWheel && inside)
        {
            float oldZoom = _zoom;
            _zoom = Mathf.Clamp(_zoom * (1f - e.delta.y * 0.04f), 0.02f, 2f);

            Vector2 anchor = (e.mousePosition - _pan) / Mathf.Max(0.0001f, oldZoom);
            _pan = e.mousePosition - anchor * _zoom;

            e.Use();
            Repaint();
            return;
        }

        //中键拖拽平移
        if (e.type == EventType.MouseDown && e.button == 2 && inside) { _isPanning = true; e.Use(); return; }
        if (_isPanning && e.type == EventType.MouseDrag) { _pan += e.delta; e.Use(); Repaint(); return; }
        if (_isPanning && e.type == EventType.MouseUp) { _isPanning = false; e.Use(); return; }

        //空格 + 左键拖拽平移
        if (_spaceHeld && e.type == EventType.MouseDrag && e.button == 0) { _pan += e.delta; e.Use(); Repaint(); return; }

        //「选择移动」：拖动已选中的点位/顶点。只改坐标、不改元素数量 → 在 MouseDrag 里改是安全的。
        if (e.type == EventType.MouseDrag && e.button == 0 && !_spaceHeld
            && _mode == ToolMode.Select && _dragIndex >= 0)
        {
            if (!_dragUndoPushed) { PushUndo("移动"); _dragUndoPushed = true; }

            Vector2 moved = WorldMapToolGeometry.ClampNormalized(
                WorldMapToolGeometry.CanvasToNormalized(e.mousePosition, GetImageRect()));

            if (_dragIsPoint && _dragIndex < _draft.points.Count)
                _draft.points[_dragIndex].Position = moved;
            else if (!_dragIsPoint && _dragIndex < _draft.factionVertices.Count)
                _draft.factionVertices[_dragIndex].Position = moved;

            _statusMessage = "移动中：" + (_dragIsPoint ? "点位" : "顶点") + " #" + (_dragIndex + 1)
                + "  X=" + moved.x.ToString("0.0000") + " Y=" + moved.y.ToString("0.0000");
            e.Use();
            Repaint();
            return;
        }

        //松开左键 → 结束拖动（下一次拖动会重新压一个新的撤销点）
        if (e.type == EventType.MouseUp)
        {
            _dragIndex = -1;
            _dragUndoPushed = false;
        }

        //左键：按模式操作
        if (e.type == EventType.MouseDown && e.button == 0 && inside && !_spaceHeld)
        {
            //延后到 Layout 之前处理：落点/画顶点会改变侧栏列表的元素数量，
            //而侧栏是在画布【之后】绘制的 → 直接改会让同帧元素数不一致。
            _hasPendingClick = true;
            _pendingClickPosition = e.mousePosition;
            e.Use();
            return;
        }

        //Delete：删除当前选中
        if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace))
        {
            _pendingDeleteSelection = true;
            e.Use();
            Repaint();
        }
    }

    private void OnCanvasLeftClick(Vector2 mousePosition)
    {
        Rect imageRect = GetImageRect();
        Vector2 normalized = WorldMapToolGeometry.ClampNormalized(
            WorldMapToolGeometry.CanvasToNormalized(mousePosition, imageRect));

        switch (_mode)
        {
            case ToolMode.Select:
                SelectNearest(normalized);
                break;

            case ToolMode.PlaceLocation:
                AddPoint(1, normalized);
                break;

            case ToolMode.PlacePoi:
                AddPoint(2, normalized);
                break;

            case ToolMode.DrawFactionPolygon:
                //先看是否点在**当前区域的已有顶点**附近：那是"把笔尖移过去"，**不新增顶点**
                if (!TryPickVertexAsPenHead(mousePosition)) AddFactionVertex(normalized);
                break;

            case ToolMode.Erase:
                EraseNearest(normalized);
                break;
        }

        ValidateNow();
        Repaint();
    }

    // ══════════════════ 编辑操作 ══════════════════

    private void AddPoint(uint kind, Vector2 normalized)
    {
        PushUndo("落点 Kind=" + kind);

        var point = new WorldMapPointEntry
        {
            kind = kind,
            type = _placeType,
            refId = _placeRefId,
            scale = _placeScale <= 0f ? 1f : _placeScale,
            layer = _placeLayer,
            enable = true,
        };
        point.Position = normalized;

        _draft.points.Add(point);
        _selectedPoint = _draft.points.Count - 1;
        _selectedRegion = -1;
        _statusMessage = "已落点 #" + _draft.points.Count + "（Kind=" + kind +
                         "，Type=" + _placeType + "，RefId=" + _placeRefId + "）";
    }

    /// <summary>
    /// 「画势力」模式下点在**当前区域的已有顶点**附近 → **不新增顶点**，改为把该顶点设为笔尖
    /// （= 「从任意点续画」）。阈值与选择模式一致（屏幕 ~14px 折算到归一化空间）。
    /// </summary>
    private bool TryPickVertexAsPenHead(Vector2 mousePosition)
    {
        Vector2 normalized = WorldMapToolGeometry.ClampNormalized(
            WorldMapToolGeometry.CanvasToNormalized(mousePosition, GetImageRect()));
        float threshold = PickThreshold();

        int best = -1;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < _draft.factionVertices.Count; i++)
        {
            WorldMapFactionVertex v = _draft.factionVertices[i];
            if (!IsInCurrentRegion(v)) continue;   //只认当前区域的顶点

            float d = Vector2.Distance(v.Position, normalized);
            if (d < bestDistance) { bestDistance = d; best = i; }
        }

        if (best >= 0 && bestDistance <= threshold)
        {
            SetPenHead(best, "点了已有顶点");
            return true;
        }
        return false;
    }

    /// <summary>鼠标命中阈值（归一化空间）：屏幕上 ~14px。</summary>
    private float PickThreshold()
    {
        return Mathf.Max(14f / Mathf.Max(1f, _draft.imageWidth * _zoom), 0.005f);
    }

    private void AddFactionVertex(Vector2 normalized)
    {
        if (!WorldMapToolValidator.IsValidColor(_factionColor))
        {
            _statusMessage = "颜色非法（须 #RRGGBB），已取消加顶点";
            return;
        }

        PushUndo("加顶点");

        var vertex = new WorldMapFactionVertex
        {
            stage = _stage,
            factionId = _factionId,
            regionId = _regionId,
            color = _factionColor,
            enable = true,
        };
        vertex.Position = normalized;

        //★ 插到【笔尖之后】：这样"从任意点续画"就能被表达出来。
        //  导出侧的 PointOrder 正是草稿里的**出现顺序**（WorldMapToolXlsxExporter.GetOrderedRegionVertices），
        //  所以"插入到笔尖之后"就等价于"PointOrder 从该点续上"，**不需要动表结构或运行时**。
        int insertAt = InsertIndexAfterPenHead();
        _draft.factionVertices.Insert(insertAt, vertex);

        //新顶点成为新笔尖 → 继续从它往下画
        _penHeadIndex = insertAt;
        _selectedVertexIndex = insertAt;

        _statusMessage = "已加顶点（插在第 " + (insertAt + 1) + " 位）：时期" + _stage + " / 势力" + _factionId +
                         " / 区域" + _regionId + "（该区域共 " + CountRegionVertices() + " 个顶点；笔尖=新顶点）";
    }

    private int CountRegionVertices()
    {
        int count = 0;
        for (int i = 0; i < _draft.factionVertices.Count; i++)
        {
            WorldMapFactionVertex v = _draft.factionVertices[i];
            if (v != null && v.stage == _stage && v.factionId == _factionId && v.regionId == _regionId) count++;
        }
        return count;
    }

    /// <summary>该 `(时期,势力)` 下**下一个可用**的区域号（已有 1、2 → 返回 3）。</summary>
    private uint NextRegionIdFor(uint stage, uint factionId)
    {
        uint max = 0;
        for (int i = 0; i < _draft.factionVertices.Count; i++)
        {
            WorldMapFactionVertex v = _draft.factionVertices[i];
            if (v == null || v.stage != stage || v.factionId != factionId) continue;
            if (v.regionId > max) max = v.regionId;
        }
        return max + 1;
    }

    /// <summary>
    /// `(时期,势力)` 变了之后：把**颜色**与**推荐 RegionId** 对齐到该势力已有的区域。
    /// 这是"飞地与本土同色"的第一道保障（第二道是导出时按势力统一取第一块的颜色，
    /// 第三道是校验里的"同势力不同色 = Error"）。
    /// </summary>
    private void OnFactionContextChanged()
    {
        List<WorldMapFactionRegion> regions = WorldMapToolDataIO.GroupRegions(_draft);
        for (int i = 0; i < regions.Count; i++)
        {
            WorldMapFactionRegion region = regions[i];
            if (region.stage != _stage || region.factionId != _factionId) continue;

            _factionColor = region.color;                       //继承该势力已有颜色
            _regionId = NextRegionIdFor(_stage, _factionId);    //推荐下一个区域号（新建飞地用）
            ResetPenHead(null);                                 //换了势力 → 笔尖重置
            _statusMessage = "已切到 时期" + _stage + "/势力" + _factionId + "：颜色继承 " + region.color
                + "，推荐 RegionId=" + _regionId + "（要画飞地就点【新建区域(同势力)】）";
            return;
        }

        //该势力还没有任何区域 → 用建议配色，区域号从 1 起
        string suggested;
        if (WorldMapToolDataIO.SuggestedFactionColors.TryGetValue(_factionId, out suggested)) _factionColor = suggested;
        _regionId = 1;
        ResetPenHead(null);
        _statusMessage = "时期" + _stage + "/势力" + _factionId + " 还没有区域：颜色用建议值 " + _factionColor + "，RegionId 从 1 起";
    }

    /// <summary>
    /// 「新建区域（同势力）」= 画**飞地**：`时期/势力` 不变，只把 `RegionId` 换成下一个可用号，
    /// 并**继承该势力已有区域的颜色**（保证同势力同色）。
    /// </summary>
    private void CreateNewRegionForCurrentFaction()
    {
        uint next = NextRegionIdFor(_stage, _factionId);

        List<WorldMapFactionRegion> regions = WorldMapToolDataIO.GroupRegions(_draft);
        for (int i = 0; i < regions.Count; i++)
        {
            if (regions[i].stage == _stage && regions[i].factionId == _factionId) { _factionColor = regions[i].color; break; }
        }

        _regionId = next;
        ResetPenHead(null);   //进了新区域 → 笔尖回到"自动=该区域末点"（该区域还没点，所以没有笔尖）
        _statusMessage = "已新建 时期" + _stage + "/势力" + _factionId + "/区域" + next + "（飞地）"
            + "　颜色 " + _factionColor + "　→ 接着在画布上点顶点即可";
        Debug.Log("[局外地图] " + _statusMessage);
    }

    /// <summary>
    /// 「选择移动」：找最近的点位（优先）或势力顶点并选中，**同时记住拖动目标** ——
    /// 之后按住左键拖动即可移动它（拖动分支见 HandleCanvasInput）。
    /// </summary>
    private void SelectNearest(Vector2 normalized)
    {
        //阈值按缩放折算：屏幕上 ~14px 以内算命中
        float threshold = Mathf.Max(14f / Mathf.Max(1f, _draft.imageWidth * _zoom), 0.01f);

        float bestDistance = float.MaxValue;
        int bestPoint = -1;
        for (int i = 0; i < _draft.points.Count; i++)
        {
            WorldMapPointEntry p = _draft.points[i];
            if (p == null) continue;
            float distance = Vector2.Distance(p.Position, normalized);
            if (distance < bestDistance) { bestDistance = distance; bestPoint = i; }
        }

        float vertexDistance = float.MaxValue;
        int vertexIndex = -1;
        for (int i = 0; i < _draft.factionVertices.Count; i++)
        {
            WorldMapFactionVertex v = _draft.factionVertices[i];
            if (v == null) continue;
            float distance = Vector2.Distance(v.Position, normalized);
            if (distance < vertexDistance) { vertexDistance = distance; vertexIndex = i; }
        }

        //点位优先（点位是主要编辑对象）
        if (bestPoint >= 0 && bestDistance <= threshold)
        {
            _selectedPoint = bestPoint;
            _selectedRegion = -1;
            _selectedVertexIndex = -1;
            _dragIndex = bestPoint;
            _dragIsPoint = true;
            _statusMessage = "已选中点位 #" + (bestPoint + 1) + "（按住左键可拖动）";
            return;
        }

        if (vertexIndex >= 0 && vertexDistance <= threshold)
        {
            WorldMapFactionVertex vertex = _draft.factionVertices[vertexIndex];
            _selectedPoint = -1;
            _dragIndex = vertexIndex;
            _dragIsPoint = false;

            //把绘制参数切到该顶点所属区域，方便继续加点；顺带在侧栏高亮对应区域
            _stage = vertex.stage;
            _factionId = vertex.factionId;
            _regionId = vertex.regionId;
            _factionColor = vertex.color;

            List<WorldMapFactionRegion> regions = WorldMapToolDataIO.GroupRegions(_draft);
            for (int r = 0; r < regions.Count; r++)
            {
                if (regions[r].stage == vertex.stage && regions[r].factionId == vertex.factionId
                    && regions[r].regionId == vertex.regionId) { _selectedRegion = r; break; }
            }

            //★ 需求 2-2：选择模式下点中顶点 → **同时设为笔尖**（便于发现"从任意点续画"）
            _selectedVertexIndex = vertexIndex;
            _penHeadIndex = vertexIndex;
            _statusMessage = "已选中势力顶点 #" + (vertexIndex + 1) +
                "（按住左键可拖动；**已设为笔尖**，切到「画势力」就能从它续画）";
            return;
        }

        _selectedPoint = -1;
        _selectedVertexIndex = -1;
        _dragIndex = -1;
        _statusMessage = "未命中任何点位/顶点";
    }

    private void EraseNearest(Vector2 normalized)
    {
        float bestDistance = float.MaxValue;
        int best = -1;
        bool bestIsPoint = true;

        for (int i = 0; i < _draft.points.Count; i++)
        {
            WorldMapPointEntry p = _draft.points[i];
            if (p == null) continue;
            float distance = Vector2.Distance(p.Position, normalized);
            if (distance < bestDistance) { bestDistance = distance; best = i; bestIsPoint = true; }
        }

        for (int i = 0; i < _draft.factionVertices.Count; i++)
        {
            WorldMapFactionVertex v = _draft.factionVertices[i];
            if (v == null) continue;
            float distance = Vector2.Distance(v.Position, normalized);
            if (distance < bestDistance) { bestDistance = distance; best = i; bestIsPoint = false; }
        }

        float threshold = 14f / Mathf.Max(1f, _draft.imageWidth * _zoom);
        if (best < 0 || bestDistance > Mathf.Max(threshold, 0.01f))
        {
            _statusMessage = "擦除：附近没有可删的点位/顶点";
            return;
        }

        PushUndo("擦除");
        if (bestIsPoint) { _draft.points.RemoveAt(best); _selectedPoint = -1; _statusMessage = "已擦除 1 个点位"; }
        else { _draft.factionVertices.RemoveAt(best); _statusMessage = "已擦除 1 个势力顶点"; }
    }

    private void DeleteSelection()
    {
        if (_selectedPoint >= 0 && _selectedPoint < _draft.points.Count)
        {
            PushUndo("删除点位");
            _draft.points.RemoveAt(_selectedPoint);
            _selectedPoint = -1;
            _statusMessage = "已删除点位";
            ValidateNow();
            return;
        }

        if (_selectedRegion >= 0)
        {
            List<WorldMapFactionRegion> regions = WorldMapToolDataIO.GroupRegions(_draft);
            if (_selectedRegion < regions.Count)
            {
                PushUndo("删除整个区域");
                WorldMapFactionRegion region = regions[_selectedRegion];

                //从后往前删，避免下标漂移
                for (int i = region.sourceIndices.Count - 1; i >= 0; i--)
                    _draft.factionVertices.RemoveAt(region.sourceIndices[i]);

                _selectedRegion = -1;
                _statusMessage = "已删除区域「" + region.Label + "」的全部顶点";
                ValidateNow();
            }
        }
    }

    // ══════════════════ 绘制：点位与多边形 ══════════════════

    private void DrawPoints(Rect imageRect)
    {
        for (int i = 0; i < _draft.points.Count; i++)
        {
            WorldMapPointEntry p = _draft.points[i];
            if (p == null) continue;

            Vector2 center = WorldMapToolGeometry.NormalizedToCanvas(p.Position, imageRect);

            Color color;
            if (p.kind == 1) color = new Color(1f, 0.85f, 0.2f, 1f);          //固定地点：黄
            else if (p.kind == 2) color = new Color(0.35f, 0.8f, 1f, 1f);      //兴趣点：青
            else color = new Color(0.7f, 0.7f, 0.7f, 1f);                      //预留类别：灰

            if (!p.enable) color = new Color(color.r, color.g, color.b, 0.35f);
            if (i == _selectedPoint) color = new Color(1f, 0.3f, 0.3f, 1f);    //选中：红

            float radius = 7f;
            DrawDisc(center, radius, color, i == _selectedPoint);
        }
    }

    private void DrawFactionPolygons(Rect imageRect)
    {
        List<WorldMapFactionRegion> regions = WorldMapToolDataIO.GroupRegions(_draft);
        int penHead = EffectivePenHeadIndex();   //-1 = 当前区域没有顶点

        for (int r = 0; r < regions.Count; r++)
        {
            WorldMapFactionRegion region = regions[r];
            if (region.vertices.Count == 0) continue;

            Color fill;
            if (!ColorUtility.TryParseHtmlString(region.color, out fill)) fill = Color.magenta;
            fill.a = 0.18f;

            //区域包围盒填充（轻量、不依赖三角化）
            Rect bounds = WorldMapToolGeometry.Bounds(region.vertices);
            Vector2 min = WorldMapToolGeometry.NormalizedToCanvas(new Vector2(bounds.xMin, bounds.yMax), imageRect);
            Vector2 max = WorldMapToolGeometry.NormalizedToCanvas(new Vector2(bounds.xMax, bounds.yMin), imageRect);
            EditorGUI.DrawRect(new Rect(min.x, min.y, max.x - min.x, max.y - min.y), fill);

            Color lineColor = r == _selectedRegion ? new Color(1f, 0.3f, 0.3f, 1f) : fill;
            lineColor.a = 1f;

            var canvasVertices = new List<Vector2>();
            for (int k = 0; k < region.vertices.Count; k++)
                canvasVertices.Add(WorldMapToolGeometry.NormalizedToCanvas(region.vertices[k], imageRect));

            int chainCount;
            bool hasClosing;
            GetDisplayEdgePlan(canvasVertices.Count, _showClosingDash, out chainCount, out hasClosing);

            //★ 链边：**只有生成顺序相邻的两点**之间才画实线（k → k+1）。
            //  以前是对每个顶点都画 (k+1)%N，于是"末点→首点"也被画成实线 ——
            //  用户看到的"连线总是从固定一个点出发连到最新的点"就是那条假边。
            for (int k = 0; k < chainCount; k++)
                DrawLine(canvasVertices[k], canvasVertices[k + 1], lineColor, 2f);

            //★ 收口边：仅 N>=3，且用**淡虚线**。虚线 ≠ 生成顺序，只表示"导出时会自动闭合"。
            if (hasClosing)
            {
                Color dashColor = lineColor;
                dashColor.a = 0.4f;
                DrawDashedLine(canvasVertices[canvasVertices.Count - 1], canvasVertices[0],
                    dashColor, 1f, 8f, 6f);
            }

            //顶点把手（笔尖那个跳过，下面单独用醒目样式画）
            for (int k = 0; k < canvasVertices.Count; k++)
            {
                if (k < region.sourceIndices.Count && region.sourceIndices[k] == penHead) continue;
                DrawDisc(canvasVertices[k], 4f, lineColor, false);
            }

            //选区标签
            if (canvasVertices.Count > 0)
            {
                GUI.Label(new Rect(canvasVertices[0].x + 6f, canvasVertices[0].y - 18f, 220f, 18f),
                    region.Label, EditorStyles.miniLabel);
            }

            //★ 笔尖：更大半径 + 双环 + 十字 + tiny label（只画当前区域的那一个）
            for (int k = 0; k < region.sourceIndices.Count && k < canvasVertices.Count; k++)
            {
                if (region.sourceIndices[k] != penHead) continue;

                Vector2 c = canvasVertices[k];
                Color headColor = new Color(1f, 0.55f, 0.10f, 1f);
                DrawDisc(c, 9f, headColor, true);                                  //外环
                DrawDisc(c, 4.5f, new Color(0.08f, 0.08f, 0.10f, 1f), false);       //内芯
                DrawLine(new Vector2(c.x - 12f, c.y), new Vector2(c.x + 12f, c.y), headColor, 1.5f);
                DrawLine(new Vector2(c.x, c.y - 12f), new Vector2(c.x, c.y + 12f), headColor, 1.5f);
                GUI.Label(new Rect(c.x + 13f, c.y + 7f, 260f, 18f),
                    "笔尖（下一个点从这里连）", EditorStyles.miniLabel);
            }
        }

        //★ 橡皮筋：当前笔尖 → 鼠标位置（只有"画势力"模式 + 鼠标在画布内 + 笔尖有效时才画）
        if (_mode == ToolMode.DrawFactionPolygon && _mouseInsideCanvas
            && penHead >= 0 && penHead < _draft.factionVertices.Count)
        {
            Vector2 headCanvas = WorldMapToolGeometry.NormalizedToCanvas(
                _draft.factionVertices[penHead].Position, imageRect);
            //⚠ 缓存的是**窗口坐标**，画布内要转成**剪辑局部坐标**
            Vector2 mouseLocal = ToClipLocal(_mouseCanvasPos);

            DrawLine(headCanvas, mouseLocal, new Color(1f, 0.75f, 0.20f, 0.9f), 1.5f);
            GUI.Label(new Rect(mouseLocal.x + 12f, mouseLocal.y + 6f, 220f, 18f),
                "橡皮筋 → 点这里落下一个顶点", EditorStyles.miniLabel);
        }
    }

    // ══════════════════ 侧栏：点位列表 ══════════════════

    private void DrawPointListPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(300f));
        EditorGUILayout.LabelField("点位（" + _draft.points.Count + "）", EditorStyles.boldLabel);

        _pointListScroll = EditorGUILayout.BeginScrollView(_pointListScroll, GUILayout.ExpandHeight(true));

        for (int i = 0; i < _draft.points.Count; i++)
        {
            WorldMapPointEntry p = _draft.points[i];
            if (p == null) continue;

            EditorGUILayout.BeginHorizontal();

            bool selected = i == _selectedPoint;
            if (selected) EditorGUILayout.LabelField("▶", GUILayout.Width(14f));
            else EditorGUILayout.LabelField("", GUILayout.Width(14f));

            if (GUILayout.Button("#" + (i + 1) + " K=" + p.kind, GUILayout.Width(78f)))
            {
                _selectedPoint = i;
                _selectedRegion = -1;
                Repaint();
            }

            EditorGUILayout.LabelField("T", GUILayout.Width(12f));
            EditorGUI.BeginChangeCheck();
            int type = EditorGUILayout.IntField((int)p.type, GUILayout.Width(46f));
            if (EditorGUI.EndChangeCheck()) { p.type = (uint)Mathf.Max(0, type); _draft.dirty = true; }

            EditorGUILayout.LabelField("R", GUILayout.Width(12f));
            EditorGUI.BeginChangeCheck();
            int refId = EditorGUILayout.IntField((int)p.refId, GUILayout.Width(58f));
            if (EditorGUI.EndChangeCheck()) { p.refId = (uint)Mathf.Max(0, refId); _draft.dirty = true; }

            EditorGUI.BeginChangeCheck();
            bool enable = EditorGUILayout.Toggle(p.enable, GUILayout.Width(18f));
            if (EditorGUI.EndChangeCheck()) { p.enable = enable; _draft.dirty = true; }

            if (GUILayout.Button("×", GUILayout.Width(22f)))
            {
                //⚠ 不要在这里直接 RemoveAt，也【不要】GUIUtility.ExitGUI()：
                //   ExitGUI 会抛出并把 Begin/End 配对跳过 → Unity 报 "Invalid GUILayout state"；
                //   在布局组内部改列表还会让同帧 Layout/Repaint 元素数不一致。
                //   统一登记意图，交给 ApplyPendingState 在 Layout 之前执行。
                _pendingDeletePoint = i;
                Repaint();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(
                "     X=" + p.x.ToString("0.0000", CultureInfo.InvariantCulture) +
                " Y=" + p.y.ToString("0.0000", CultureInfo.InvariantCulture) +
                " Scale=" + p.scale.ToString("0.##", CultureInfo.InvariantCulture) +
                " Layer=" + p.layer, EditorStyles.miniLabel);
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("清空全部点位") && _draft.points.Count > 0)
        {
            if (EditorUtility.DisplayDialog("确认", "清空全部 " + _draft.points.Count + " 个点位？", "清空", "取消"))
            {
                _pendingClearPoints = true;
                Repaint();
            }
        }

        EditorGUILayout.EndVertical();
    }

    // ══════════════════ 侧栏：势力区域列表 ══════════════════

    private void DrawRegionListPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(300f));
        EditorGUILayout.LabelField("势力区域（按 时期→势力→区域 分组）", EditorStyles.boldLabel);

        _regionListScroll = EditorGUILayout.BeginScrollView(_regionListScroll, GUILayout.ExpandHeight(true));

        List<WorldMapFactionRegion> regions = WorldMapToolDataIO.GroupRegions(_draft);

        //⚠ 平铺下标 r 必须保持为 regions 里的下标 —— 选中/删除/同步逻辑用的都是它
        int curStage = int.MinValue, curFaction = int.MinValue;
        for (int r = 0; r < regions.Count; r++)
        {
            WorldMapFactionRegion region = regions[r];

            // ── 新的一组 (时期,势力)：先打一行组汇总 ──
            if ((int)region.stage != curStage || (int)region.factionId != curFaction)
            {
                if (curStage != int.MinValue) EditorGUILayout.Space();
                curStage = (int)region.stage;
                curFaction = (int)region.factionId;

                int regionCount = 0, readyCount = 0;
                string detail = string.Empty;
                string groupColor = null;
                bool sameColor = true;
                for (int k = 0; k < regions.Count; k++)
                {
                    WorldMapFactionRegion g = regions[k];
                    if ((int)g.stage != curStage || (int)g.factionId != curFaction) continue;

                    regionCount++;
                    if (g.vertices.Count >= 3) readyCount++;
                    detail += (detail.Length > 0 ? "、" : "") + "R" + g.regionId + ":" + g.vertices.Count + "点";
                    if (groupColor == null) groupColor = g.color;
                    else if (!string.Equals(groupColor, g.color, System.StringComparison.OrdinalIgnoreCase)) sameColor = false;
                }

                EditorGUILayout.LabelField(
                    "时期" + curStage + "（" + WorldMapToolDataIO.StageName((uint)curStage) + "）/ 势力" + curFaction
                    + "　区域 " + regionCount + " 块，满3顶点 " + readyCount + "/" + regionCount
                    + ((regionCount > 1 && !sameColor) ? "　⚠同势力颜色不一致" : ""),
                    EditorStyles.boldLabel);
                EditorGUILayout.LabelField("　　" + detail + "　颜色 " + groupColor, EditorStyles.miniLabel);
            }

            EditorGUILayout.BeginHorizontal();
            if (r == _selectedRegion) EditorGUILayout.LabelField("▶", GUILayout.Width(14f));
            else EditorGUILayout.LabelField("", GUILayout.Width(14f));

            string shortLabel = "区域" + region.regionId + " · " + region.vertices.Count + "点"
                + (region.vertices.Count >= 3 ? "" : "（不足3点）");
            if (GUILayout.Button(shortLabel, EditorStyles.miniButton))
            {
                _selectedRegion = r;
                _selectedPoint = -1;
                //把绘制参数切到该区域，方便继续加点
                _stage = region.stage;
                _factionId = region.factionId;
                _regionId = region.regionId;
                _factionColor = region.color;
                ResetPenHead(null);   //换了区域 → 笔尖回到"自动=该区域末点"
                Repaint();
            }

            if (GUILayout.Button("×", GUILayout.Width(22f)))
            {
                _pendingDeleteRegion = r;   //同点位：延后到 Layout 之前执行
                Repaint();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("颜色", GUILayout.Width(34f));
            EditorGUI.BeginChangeCheck();
            string color = EditorGUILayout.TextField(region.color, GUILayout.Width(80f));
            if (EditorGUI.EndChangeCheck())
            {
                for (int k = 0; k < region.sourceIndices.Count; k++)
                {
                    WorldMapFactionVertex v = _draft.factionVertices[region.sourceIndices[k]];
                    if (v != null) v.color = color;
                }
                _draft.dirty = true;
            }
            EditorGUILayout.EndHorizontal();
        }

        if (regions.Count == 0) EditorGUILayout.LabelField("（还没有任何区域）", EditorStyles.miniLabel);

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("新建区域（同势力 · 飞地）")) CreateNewRegionForCurrentFaction();

        if (GUILayout.Button("清空全部势力顶点") && _draft.factionVertices.Count > 0)
        {
            if (EditorUtility.DisplayDialog("确认", "清空全部 " + _draft.factionVertices.Count + " 个顶点？", "清空", "取消"))
            {
                _pendingClearVertices = true;
                Repaint();
            }
        }

        EditorGUILayout.EndVertical();
    }

    // ══════════════════ 底部：错误列表 + 导出 ══════════════════

    private void DrawBottomPanel()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(170f));

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("校验（错误 = 阻断导出）", EditorStyles.boldLabel, GUILayout.Width(200f));
        if (GUILayout.Button("重新校验", GUILayout.Width(80f))) ValidateNow();

        if (GUILayout.Button("导出地图表（顶层）", GUILayout.Width(150f)))
        {
            WorldMapExportResult result = WorldMapToolXlsxExporter.ExportMapTable(_draft);
            ApplyExportResult(result);
        }

        if (GUILayout.Button("导出势力分表 ×5（子目录）", GUILayout.Width(180f)))
        {
            WorldMapExportResult result = WorldMapToolXlsxExporter.ExportFactionStageTables(_draft);
            ApplyExportResult(result);
        }

        if (GUILayout.Button("导出合并表（顶层）", GUILayout.Width(140f)))
        {
            WorldMapExportResult result = WorldMapToolXlsxExporter.ExportFactionMergedTable(_draft);
            ApplyExportResult(result);
        }

        if (GUILayout.Button("打印 (Kind,Type,RefId) 清单", GUILayout.Width(180f)))
            WorldMapToolXlsxExporter.PrintUsageList(_draft);

        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(_statusMessage))
            EditorGUILayout.LabelField(_statusMessage, EditorStyles.miniLabel);

        _issueScroll = EditorGUILayout.BeginScrollView(_issueScroll, GUILayout.ExpandHeight(true));
        if (_issues.Count == 0)
        {
            EditorGUILayout.LabelField("（无问题）", EditorStyles.miniLabel);
        }
        else
        {
            for (int i = 0; i < _issues.Count; i++)
            {
                WorldMapToolIssue issue = _issues[i];
                if (issue == null) continue;

                Color saved = GUI.color;
                if (issue.IsError) GUI.color = new Color(1f, 0.5f, 0.5f);
                else GUI.color = new Color(1f, 0.85f, 0.45f);
                EditorGUILayout.LabelField(issue.ToString());
                GUI.color = saved;
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private void ApplyExportResult(WorldMapExportResult result)
    {
        if (result == null) return;

        _statusMessage = result.Message;
        _issues.Clear();

        //先放导出本身的问题（含拒绝原因）
        _issues.AddRange(result.Issues);

        //再放一次当前草稿的常规校验，便于边改边看
        List<WorldMapToolIssue> mapIssues = WorldMapToolValidator.ValidateMapTable(_draft, false);
        List<WorldMapToolIssue> factionIssues = WorldMapToolValidator.ValidateFactionTables(_draft, false);
        _issues.AddRange(mapIssues);
        _issues.AddRange(factionIssues);

        if (result.Success)
        {
            Debug.Log("[局外地图] " + result.Message);
            for (int i = 0; i < result.WrittenFiles.Count; i++)
                Debug.Log("[局外地图]   已写出：" + result.WrittenFiles[i]);
        }
        else
        {
            Debug.LogError("[局外地图] " + result.Message + "\n" + WorldMapToolValidator.Describe(result.Issues));
        }
    }

    // ══════════════════ 草稿 / 校验 ══════════════════

    private void LoadDraftWithConfirm()
    {
        if (_draft.dirty && !EditorUtility.DisplayDialog("草稿未保存",
                "当前草稿有未保存改动，载入会丢弃它们。继续？", "丢弃并载入", "取消"))
            return;

        string message;
        WorldMapDraft loaded = WorldMapToolDataIO.Load(out message);
        if (loaded != null)
        {
            _draft = loaded;
            _undoStack.Clear();
            _selectedPoint = -1;
            _selectedRegion = -1;
            RefreshSourceTexture();
        }
        _statusMessage = message;
        ValidateNow();
    }

    private void SaveDraft()
    {
        string message;
        bool ok = WorldMapToolDataIO.Save(_draft, out message);
        _statusMessage = message;
        if (ok)
        {
            AssetDatabase.Refresh();
            Debug.Log("[局外地图] " + message);
        }
        else
        {
            Debug.LogError("[局外地图] " + message);
        }
    }

    private void ValidateNow()
    {
        _issues.Clear();
        if (_draft == null) return;

        //窗口里按"非顶层"口径给即时反馈（顶层才有的 0 行约束留到点导出时再阻断）
        _issues.AddRange(WorldMapToolValidator.ValidateMapTable(_draft, false));
        _issues.AddRange(WorldMapToolValidator.ValidateFactionTables(_draft, false));
    }

    // ══════════════════ 图元绘制（全部用 GUI.DrawTexture，保证受 BeginClip 裁剪） ══════════════════

    private static Texture2D _whiteTexture;

    private static Texture2D WhiteTexture
    {
        get
        {
            if (_whiteTexture == null)
            {
                _whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                _whiteTexture.SetPixel(0, 0, Color.white);
                _whiteTexture.Apply();
                _whiteTexture.hideFlags = HideFlags.HideAndDontSave;
            }
            return _whiteTexture;
        }
    }

    /// <summary>画一条线：用 1×1 白纹理 + `GUIUtility.RotateAroundPivot`（缩放/旋转矩阵的同一套机制）。</summary>
    private static void DrawLine(Vector2 from, Vector2 to, Color color, float width)
    {
        Vector2 delta = to - from;
        float length = delta.magnitude;
        if (length < 0.5f) return;

        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

        Matrix4x4 savedMatrix = GUI.matrix;
        Color savedColor = GUI.color;

        GUI.color = color;
        GUIUtility.RotateAroundPivot(angle, from);
        GUI.DrawTexture(new Rect(from.x, from.y - width * 0.5f, length, width), WhiteTexture);

        GUI.matrix = savedMatrix;
        GUI.color = savedColor;
    }

    /// <summary>画一个圆点（编辑器里没有圆图元，用一次性生成的圆纹理）。</summary>
    private static void DrawDisc(Vector2 center, float radius, Color color, bool highlight)
    {
        Texture2D texture = GetDiscTexture(highlight);
        Color savedColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), texture);
        GUI.color = savedColor;
    }

    private static readonly Dictionary<bool, Texture2D> DiscTextures = new Dictionary<bool, Texture2D>();

    private static Texture2D GetDiscTexture(bool highlight)
    {
        Texture2D cached;
        if (DiscTextures.TryGetValue(highlight, out cached) && cached != null) return cached;

        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size * 0.5f;
        var pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                Color pixel;
                if (distance > radius) pixel = new Color(0f, 0f, 0f, 0f);
                else if (distance > radius - (highlight ? 5f : 3f)) pixel = new Color(1f, 1f, 1f, 0.95f);
                else pixel = new Color(1f, 1f, 1f, 0.85f);

                pixels[y * size + x] = pixel;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;

        DiscTextures[highlight] = texture;
        return texture;
    }

    private static void DrawRectOutline(Rect rect, Color color)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
    }
}
