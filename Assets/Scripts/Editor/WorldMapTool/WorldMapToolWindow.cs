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
/// ══════════════════ 两个设计上的硬点 ══════════════════
/// ① **画布用 `GUI.BeginClip`，且坐标是「窗口坐标」**：`BeginClip` 只做裁剪、**不平移坐标原点**
///    （平移是 `BeginGroup` 的行为）。所以底图矩形、点位、多边形的绘制坐标都带 `_pan` 偏移，
///    不要以为进了 clip 就变成 (0,0) 起算 —— 这一条写错会让整张图偏移。
/// ② **Y 轴翻转只在 `WorldMapToolGeometry` 里做**：本文件一律调它换算，**不自己写 `1 - y`**。
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
    private bool _isPanning;
    private bool _spaceHeld;

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
            _stage = (uint)EditorGUILayout.IntPopup((int)_stage,
                new[] { "1 战国末期", "2 秦朝", "3 秦末起义初期", "4 楚汉争霸", "5 汉刘邦时期" },
                new[] { 1, 2, 3, 4, 5 }, GUILayout.Width(140f));

            EditorGUILayout.LabelField("FactionId", GUILayout.Width(64f));
            _factionId = (uint)Mathf.Max(0, EditorGUILayout.IntField((int)_factionId, GUILayout.Width(50f)));

            EditorGUILayout.LabelField("RegionId", GUILayout.Width(58f));
            _regionId = (uint)Mathf.Max(1, EditorGUILayout.IntField((int)_regionId, GUILayout.Width(50f)));

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

            //BeginClip 只裁剪、不平移原点 → 下面用的都是**窗口坐标**
            GUI.BeginClip(_canvasRect);
            Rect imageRect = GetImageRect();

            if (_sourceTexture != null)
            {
                GUI.DrawTexture(imageRect, _sourceTexture, ScaleMode.StretchToFill);
            }
            else
            {
                EditorGUI.DrawRect(imageRect, new Color(0.25f, 0.12f, 0.12f, 1f));
            }

            DrawFactionPolygons(imageRect);
            DrawPoints(imageRect);

            GUI.EndClip();

            //画布边框
            DrawRectOutline(_canvasRect, new Color(0.4f, 0.4f, 0.45f, 1f));
        }

        //提示文字用 GUI.Label（非 GUILayout）—— 不参与布局，Layout/Repaint 都能画
        GUI.Label(new Rect(_canvasRect.x + 6f, _canvasRect.y + 4f,
                _canvasRect.width - 12f, 20f),
            "滚轮缩放 / 中键或空格+左键拖拽平移 / 左键按当前模式操作", EditorStyles.miniLabel);

        //⚠ 必须与开头的 BeginVertical 配对！漏掉 EndVertical 会让【整个窗口】的 GUILayout 状态失衡，
        //   Unity 只在 HostView 层报 "Invalid GUILayout state in WorldMapToolWindow view"，
        //   错误信息完全不指向这里 —— 实测就是这一处漏配对（Begin/End 计数 17:16）。
        EditorGUILayout.EndVertical();
    }

    /// <summary>底图当前在**窗口坐标**下的矩形。</summary>
    private Rect GetImageRect()
    {
        return new Rect(_pan.x, _pan.y, _draft.imageWidth * _zoom, _draft.imageHeight * _zoom);
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

        if (e.type == EventType.KeyUp && e.keyCode == KeyCode.Space) { _spaceHeld = false; e.Use(); return; }

        bool inside = _canvasRect.Contains(e.mousePosition);

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
                AddFactionVertex(normalized);
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

        _draft.factionVertices.Add(vertex);
        _statusMessage = "已加顶点：时期" + _stage + " / 势力" + _factionId +
                         " / 区域" + _regionId + "（该区域共 " + CountRegionVertices() + " 个顶点）";
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

            _statusMessage = "已选中势力顶点 #" + (vertexIndex + 1) + "（按住左键可拖动）";
            return;
        }

        _selectedPoint = -1;
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

            //边线（含闭合边 last -> first：本工具不重复首点，闭合是"画出来"的）
            Color lineColor = r == _selectedRegion ? new Color(1f, 0.3f, 0.3f, 1f) : fill;
            lineColor.a = 1f;

            var canvasVertices = new List<Vector2>();
            for (int k = 0; k < region.vertices.Count; k++)
                canvasVertices.Add(WorldMapToolGeometry.NormalizedToCanvas(region.vertices[k], imageRect));

            for (int k = 0; k < canvasVertices.Count; k++)
            {
                Vector2 a = canvasVertices[k];
                Vector2 b = canvasVertices[(k + 1) % canvasVertices.Count];
                DrawLine(a, b, lineColor, 2f);
            }

            //顶点把手
            for (int k = 0; k < canvasVertices.Count; k++)
                DrawDisc(canvasVertices[k], 4f, lineColor, false);

            //选区标签
            if (canvasVertices.Count > 0)
            {
                GUI.Label(new Rect(canvasVertices[0].x + 6f, canvasVertices[0].y - 18f, 220f, 18f),
                    region.Label, EditorStyles.miniLabel);
            }
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
        EditorGUILayout.LabelField("势力区域", EditorStyles.boldLabel);

        _regionListScroll = EditorGUILayout.BeginScrollView(_regionListScroll, GUILayout.ExpandHeight(true));

        List<WorldMapFactionRegion> regions = WorldMapToolDataIO.GroupRegions(_draft);

        for (int r = 0; r < regions.Count; r++)
        {
            WorldMapFactionRegion region = regions[r];

            EditorGUILayout.BeginHorizontal();
            if (r == _selectedRegion) EditorGUILayout.LabelField("▶", GUILayout.Width(14f));
            else EditorGUILayout.LabelField("", GUILayout.Width(14f));

            if (GUILayout.Button(region.Label, EditorStyles.miniButton))
            {
                _selectedRegion = r;
                _selectedPoint = -1;
                //把绘制参数切到该区域，方便继续加点
                _stage = region.stage;
                _factionId = region.factionId;
                _regionId = region.regionId;
                _factionColor = region.color;
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

        EditorGUILayout.EndScrollView();

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
