using System.Collections.Generic;
using ChaosDebug;
using LocalizationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Chaos.MVP.Model.WorldMap;
using Chaos.MVP.Presenter.WorldMap;

/// <summary>
/// 村庄中的世界地图子界面；地图上的正式派遣按钮连接出征流程。
///
/// ══════════════════ 层级契约（**冻结**，task-26 会照这些名字搭 prefab，改名字会直接失效） ══════════════════
/// WorldMapPanel
/// ├─ BaseMap        (Image)                底图；同时也是 IconLayer 的"坐标系"
/// ├─ FactionMask    (WorldFactionMask)     势力范围蒙版（组件继承 RawImage，一个 quad 画完）
/// ├─ IconLayer      (RectTransform)        点位图标容器（**必须与 BaseMap 完全重合**，见下）
/// ├─ InfoBar        (LocalizedText + TextMeshProUGUI)  信息条
/// └─ BottomBar
///    ├─ ReturnBtn         返回（PopPanel）
///    ├─ EnterRunBtn       选择出战门客
///    ├─ StageDebugPrev    时期预览 -1
///    └─ StageDebugNext    时期预览 +1
///
/// ⚠ **IconLayer 必须与 BaseMap 同矩形**：点位用"归一化坐标当 anchorMin/anchorMax"定位，
///   归一化 → 屏幕的映射完全由 IconLayer 的矩形决定。两者不重合 → 点位整体偏移。
///
/// ⚠ **InfoBar 的 TextMeshProUGUI 必须挂本工程的 `MSYH SDF` 字体**（与其它面板一致）：
///   本面板的信息条是**中文**（"时期 1（战国末期）…"）。若字体留空 → TMP 回退到
///   `LiberationSans SDF`（不含 CJK）→ 每个汉字都变成 □（实测会刷一屏 warning）。
///
/// ══════════════════ 生命周期约定（R3） ══════════════════
/// `UIManager` **会缓存面板实例**、`OnEnter` 每次打开都触发 →
/// **找节点与绑按钮只做一次（`Awake`）**，`OnEnter` 只刷新数据。
/// 否则每次打开都会重复 `AddListener`，点一下触发 N 次。
/// </summary>
public class WorldMapPanel : BasePanel, IWorldMapView
{
    // ── 冻结的层级名 ──
    private const string NodeBaseMap = "BaseMap";
    private const string NodeFactionMask = "FactionMask";
    private const string NodeIconLayer = "IconLayer";
    private const string NodeInfoBar = "InfoBar";
    private const string NodeReturnBtn = "BottomBar/ReturnBtn";
    private const string NodeEnterRunBtn = "BottomBar/EnterRunBtn";
    private const string NodeStagePrev = "BottomBar/StageDebugPrev";
    private const string NodeStageNext = "BottomBar/StageDebugNext";

    /// <summary>当前只有 1 = 天下中原（多图预留）。</summary>
    private const uint CurrentMapId = 1;

    [Header("点位图标（可留空；留空则运行时生成白色圆点，按 Kind 上色）")]
    [SerializeField] private Sprite[] kindIcons;

    [Header("点位图标基础尺寸（像素）")]
    [SerializeField] private float iconBaseSize = 28f;

    private Image _baseMap;
    private WorldFactionMask _factionMask;
    private RectTransform _iconLayer;
    private TextMeshProUGUI _infoText;
    private LocalizedText _infoLocalized;
    private Button _returnBtn;
    private Button _enterRunBtn;
    private Button _stagePrevBtn;
    private Button _stageNextBtn;
    private TextMeshProUGUI _retainerEntryLabel;
    private TextMeshProUGUI _inventoryEntryLabel;
    private TextMeshProUGUI _marketEntryLabel;
    private TextMeshProUGUI _villageEntryLabel, _familyEntryLabel, _eraEntryLabel, _saveEntryLabel, _settingsEntryLabel;
    private TextMeshProUGUI _metaStatus, _metaHint, _returnPrompt;
    private GameObject _returnConfirmation;

    private readonly List<GameObject> _iconObjects = new List<GameObject>();
    private readonly Dictionary<uint, Sprite> _fallbackIcons = new Dictionary<uint, Sprite>();

    private WorldMapModel _model;
    private WorldMapPresenter _presenter;
    public event System.Action<WorldMapIntent> Intent;
    private WorldMapPoint _selected;

    // ══════════════════ 生命周期 ══════════════════

    private void Awake()
    {
        FindNodes();
        BindButtons();   //⚠ 只在这里绑：OnEnter 每次打开都会跑（见类注释 R3）
        BuildMetaEntries();
    }

    public override void OnEnter()
    {
        _presenter?.Dispose();
        _model?.Dispose();
        //节点可能在别的时机被替换/缺失，这里再找一次是廉价的保险（找到就覆盖，找不到保留旧引用）
        FindNodes();

        _selected = null;
        RefreshMetaEntryLabels();
        _model = new WorldMapModel();
        _presenter = new WorldMapPresenter(_model);
        _presenter.Attach(this);
    }

    public override void OnExit()
    {
        //绘制资源由 View 缓存；导航与数据订阅随本次显示释放。
        _selected = null;
        _presenter?.Dispose();
        _presenter = null;
        _model?.Dispose();
        _model = null;
    }

    // ══════════════════ 节点与按钮 ══════════════════

    private void FindNodes()
    {
        _baseMap = FindComponent<Image>(NodeBaseMap);
        _factionMask = FindComponent<WorldFactionMask>(NodeFactionMask);
        _iconLayer = FindRect(NodeIconLayer);
        _infoText = FindComponent<TextMeshProUGUI>(NodeInfoBar);
        _infoLocalized = FindComponent<LocalizedText>(NodeInfoBar);
        if (_infoText != null)
        {
            // Keep the development GM toggle clear of the normal map information.
            RectTransform infoRect = _infoText.rectTransform;
            infoRect.anchorMin = new Vector2(0, 1);
            infoRect.anchorMax = new Vector2(1, 1);
            infoRect.pivot = new Vector2(.5f, 1);
            infoRect.offsetMin = new Vector2(150, -64);
            infoRect.offsetMax = new Vector2(-24, -18);
            _infoText.fontSize = 23;
        }

        if (_iconLayer == null) ChaosLog.Error(LogChannel.UI, "[局外地图] 找不到 " + NodeIconLayer + "，点位无法显示");
        if (_baseMap == null) ChaosLog.Warn(LogChannel.UI, "[局外地图] 找不到 " + NodeBaseMap + "，底图不可用（其余功能仍可用）");
        if (_factionMask == null) ChaosLog.Warn(LogChannel.UI, "[局外地图] 找不到 " + NodeFactionMask + "，势力范围不可见");
        if (_infoText == null) ChaosLog.Warn(LogChannel.UI, "[局外地图] 找不到 " + NodeInfoBar + " 上的 TextMeshProUGUI，信息条不可用");
    }

    private void BindButtons()
    {
        _returnBtn = FindComponent<Button>(NodeReturnBtn);
        _enterRunBtn = FindComponent<Button>(NodeEnterRunBtn);
        _stagePrevBtn = FindComponent<Button>(NodeStagePrev);
        _stageNextBtn = FindComponent<Button>(NodeStageNext);

        if (_returnBtn != null) _returnBtn.onClick.AddListener(OnReturnClicked);
        if (_enterRunBtn != null) _enterRunBtn.onClick.AddListener(OnEnterRunClicked);
        if (_stagePrevBtn != null) _stagePrevBtn.onClick.AddListener(OnStagePrevClicked);
        if (_stageNextBtn != null) _stageNextBtn.onClick.AddListener(OnStageNextClicked);

        if (_returnBtn == null) ChaosLog.Warn(LogChannel.UI, "[局外地图] 缺少 " + NodeReturnBtn);
        if (_enterRunBtn == null) ChaosLog.Warn(LogChannel.UI, "[局外地图] 缺少 " + NodeEnterRunBtn);
    }

    private T FindComponent<T>(string path) where T : Component
    {
        Transform node = transform.Find(path);
        if (node == null) return null;

        T component = node.GetComponent<T>();
        if (component == null) ChaosLog.Warn(LogChannel.UI, "[局外地图] " + path + " 上没有 " + typeof(T).Name);
        return component;
    }

    private RectTransform FindRect(string path)
    {
        Transform node = transform.Find(path);
        return node as RectTransform;
    }

    /// <summary>Formal outside navigation. The map hierarchy remains available to the map tools.</summary>
    private void BuildMetaEntries()
    {
        RectTransform toolbar = GameplayUi.Rect(transform, "MetaEntries", Vector2.one, Vector2.one,
            new Vector2(-24, -100), new Vector2(280, 430));
        toolbar.gameObject.AddComponent<Image>().color = new Color(.07f, .10f, .14f, .96f);
        RectTransform status = GameplayUi.Rect(toolbar, "StatusArea", new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(8, -8), new Vector2(264, 98));
        _metaStatus = GameplayUi.Text(status, "Status", "", 20);
        _metaStatus.alignment = TextAlignmentOptions.Left;
        _retainerEntryLabel = Entry(toolbar, "Retainers", "门客", 0, () => OpenMetaPanel(PanelType.RetainerPanel));
        _inventoryEntryLabel = Entry(toolbar, "Inventory", "库存", 1, () => OpenMetaPanel(PanelType.InventoryPanel));
        _villageEntryLabel = Entry(toolbar, "Village", "村庄", 2, () => OpenMetaPanel(PanelType.VillagePanel));
        _familyEntryLabel = Entry(toolbar, "FamilyTree", "家族树", 3, () => OpenMetaPanel(PanelType.FamilyTreePanel));
        _marketEntryLabel = Entry(toolbar, "Market", "市场", 4, () => OpenMetaPanel(PanelType.MarketPanel));
        _eraEntryLabel = Entry(toolbar, "EraProgress", "时代进度", 5, () => OpenMetaPanel(PanelType.EraProgressPanel));
        _saveEntryLabel = Entry(toolbar, "Save", "保存进度", 6, () => Intent?.Invoke(new WorldMapIntent(WorldMapAction.Save)));
        _settingsEntryLabel = Entry(toolbar, "Settings", "设置", 7, () => OpenMetaPanel(PanelType.SettingPanel));
        RectTransform hint = GameplayUi.Rect(toolbar, "HintArea", new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(8, -342), new Vector2(264, 80));
        _metaHint = GameplayUi.Text(hint, "Hint", "", 19);
        _metaHint.color = new Color(1, .85f, .5f);

        // Preview controls stay out of the normal player flow; GM retains progress setup tools.
        if (_stagePrevBtn != null) _stagePrevBtn.gameObject.SetActive(false);
        if (_stageNextBtn != null) _stageNextBtn.gameObject.SetActive(false);
        _returnConfirmation = new GameObject("ReturnConfirmation", typeof(RectTransform), typeof(Image));
        var overlay = (RectTransform)_returnConfirmation.transform;
        overlay.SetParent(transform, false);
        overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0, 0, 0, .8f);
        var box = GameplayUi.Rect(overlay, "Box", new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(620, 230));
        box.gameObject.AddComponent<Image>().color = new Color(.09f, .13f, .18f, 1);
        var prompt = GameplayUi.Rect(box, "Prompt", new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -12), new Vector2(580, 100));
        _returnPrompt = GameplayUi.Text(prompt, "Text", "保存当前进度并返回主菜单？", 26);
        GameplayUi.Button(box, "Confirm", "保存并返回", new Vector2(36, -140), new Vector2(250, 56),
            () => Intent?.Invoke(new WorldMapIntent(WorldMapAction.ConfirmReturn)));
        GameplayUi.Button(box, "Cancel", "继续游戏", new Vector2(334, -140), new Vector2(250, 56),
            () => Intent?.Invoke(new WorldMapIntent(WorldMapAction.CancelReturn)));
        _returnConfirmation.SetActive(false);
        RefreshMetaEntryLabels();
    }

    private TextMeshProUGUI Entry(RectTransform parent, string name, string label, int index, UnityEngine.Events.UnityAction action)
    {
        var button = GameplayUi.Button(parent, name, label, new Vector2(10 + index % 2 * 134, -116 - index / 2 * 54), new Vector2(126, 46), action);
        return button.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void RefreshMetaEntryLabels()
    {
        GameplayText.Bind(_retainerEntryLabel, "ui_world_retainer", "门客");
        GameplayText.Bind(_inventoryEntryLabel, "ui_world_inventory", "库存");
        GameplayText.Bind(_marketEntryLabel, "ui_world_market", "市场");
        GameplayText.Bind(_villageEntryLabel, "ui_village_title", "村庄");
        GameplayText.Bind(_familyEntryLabel, "ui_familytree_title", "家族树");
        GameplayText.Bind(_eraEntryLabel, "ui_era_progress_title", "时代进度");
        GameplayText.Bind(_saveEntryLabel, "ui_world_save", "保存进度");
        GameplayText.Bind(_settingsEntryLabel, "ui_world_settings", "设置");
    }

    private void OpenMetaPanel(PanelType type)
    {
        Intent?.Invoke(new WorldMapIntent(WorldMapAction.OpenPanel, panel: type));
    }

    // ══════════════════ 数据 → 表现 ══════════════════

    /// <summary>时期取存档里的 `era`；取不到 / 越界 → 兜底 1（只 Warn，绝不让面板打不开）。</summary>
    public void Render(WorldMapSnapshot state)
    {
        _selected = state.Selected;
        ApplyFactionMask(state.Areas);
        RebuildIcons(state.Points);
        UpdateSelectionScales();
        SetInfoText(state.Info);
        if (_metaStatus != null) _metaStatus.text = state.Status;
        if (_metaHint != null) _metaHint.text = state.Hint;
        if (_returnConfirmation != null) _returnConfirmation.SetActive(state.ConfirmReturn);
        if (_returnPrompt != null) _returnPrompt.text = state.Hint;
        if (_returnBtn != null)
        {
            var label = _returnBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) GameplayText.Bind(label, "ui_return", "返回村庄");
        }
    }
    private void ApplyFactionMask(List<WorldFactionArea> areas)
    {
        if (_factionMask == null) return;
        _factionMask.SetAreas(areas, ResolveBaseMapAspect());
    }

    /// <summary>底图宽高比 = 高 / 宽。拿不到 sprite 时按 1（蒙版会略变形，但不影响正确性）。</summary>
    private float ResolveBaseMapAspect()
    {
        if (_baseMap == null || _baseMap.sprite == null) return 1f;

        Rect rect = _baseMap.sprite.rect;
        if (rect.width <= 0f) return 1f;
        return rect.height / rect.width;
    }

    /// <summary>销毁全部图标对象（先失活再 Destroy —— Destroy 帧末才生效，同帧内仍会渲染/吃射线）。</summary>
    private void ClearIcons()
    {
        for (int i = 0; i < _iconObjects.Count; i++)
        {
            if (_iconObjects[i] == null) continue;
            _iconObjects[i].SetActive(false);
            Destroy(_iconObjects[i]);
        }
        _iconObjects.Clear();
    }

    /// <summary>
    /// 按点位重建/更新图标。
    ///
    /// ⚠ **必须「够用就复用」，不能「全清再重建」**：`UIManager` 有可能在同一帧内多次 Show/Hide
    ///   （每次都会跑 `OnEnter`），而 `Destroy` 要到**帧末**才生效 ——
    ///   「全清 + 立刻 new」会在同一帧里堆出 N 倍图标（实测 5 轮 Show/Hide 后
    ///   `IconLayer` 子物体从 5 涨到 **25**）。这里按数量收敛：多了失活销毁、少了补建、正好只更新。
    /// </summary>
    private void RebuildIcons(List<WorldMapPoint> points)
    {
        if (_iconLayer == null) return;

        int want = points != null ? points.Count : 0;

        //多了：从尾部失活并销毁
        for (int i = _iconObjects.Count - 1; i >= want; i--)
        {
            GameObject extra = _iconObjects[i];
            _iconObjects.RemoveAt(i);
            if (extra == null) continue;
            extra.SetActive(false);
            Destroy(extra);
        }

        if (want == 0)
        {
            ChaosLog.Warn(LogChannel.UI, "[局外地图] 底图 " + CurrentMapId + " 上没有点位（表为空或全被 Enable=false 过滤）");
            return;
        }

        //少了：补齐（顺序追加，下标与 points 一一对应）
        while (_iconObjects.Count < want) _iconObjects.Add(CreateIconObject());

        for (int i = 0; i < want; i++) ApplyIcon(_iconObjects[i], points[i]);

        ChaosLog.Info(LogChannel.UI, "[局外地图] 点位图标已就绪：" + want + " 个");
    }

    /// <summary>建一个空的图标对象（位置与精灵由 <see cref="ApplyIcon"/> 填）。</summary>
    private GameObject CreateIconObject()
    {
        var go = new GameObject("Point",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(WorldMapPointIcon));

        var rect = (RectTransform)go.transform;
        rect.SetParent(_iconLayer, false);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(iconBaseSize, iconBaseSize);

        Image image = go.GetComponent<Image>();
        image.raycastTarget = true;   //点位要能点

        return go;
    }

    /// <summary>把一个图标对象对齐到点位上（复用路径也走这里，所以位置/颜色每次都刷新）。</summary>
    private void ApplyIcon(GameObject go, WorldMapPoint point)
    {
        if (go == null || point == null) return;

        go.name = "Point_" + point.index;
        go.SetActive(true);

        var rect = (RectTransform)go.transform;
        //⚠ 直接用归一化坐标当锚点：映射完全由 IconLayer 的矩形决定，所以它必须与 BaseMap 重合
        rect.anchorMin = rect.anchorMax = new Vector2(point.position.x, point.position.y);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(iconBaseSize, iconBaseSize);
        rect.localScale = Vector3.one;   //复用时要清掉上一轮残留的选中/悬停缩放

        Image image = go.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = ResolveIcon(point.kind);
            image.color = KindColor(point.kind);
        }

        WorldMapPointIcon icon = go.GetComponent<WorldMapPointIcon>();
        if (icon != null) icon.Bind(point, OnPointClicked);
    }

    /// <summary>按 Kind 取图标：优先用 Inspector 上配的 `kindIcons`，否则用运行时生成的圆点。</summary>
    private Sprite ResolveIcon(uint kind)
    {
        int index = (int)kind - 1;
        if (kindIcons != null && index >= 0 && index < kindIcons.Length && kindIcons[index] != null)
        {
            return kindIcons[index];
        }

        Sprite cached;
        if (_fallbackIcons.TryGetValue(kind, out cached) && cached != null) return cached;

        Sprite generated = CreateCircleSprite();
        _fallbackIcons[kind] = generated;
        return generated;
    }

    /// <summary>
    /// 运行时生成一个白色圆点 Sprite（一次生成、按 Kind 缓存，颜色由 `Image.color` 上）。
    /// 加 `HideFlags.HideAndDontSave` 防止被当成资产，`OnDestroy` 里显式释放。
    /// </summary>
    private static Sprite CreateCircleSprite()
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;

        float radius = size * 0.5f;
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                //边缘留 1.5px 过渡，避免锯齿
                float alpha = Mathf.Clamp01((radius - distance) / 1.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    /// <summary>Kind 配色：1 = 固定地点（暖黄）、2 = 兴趣点（青）、3+ = 预留（灰）。</summary>
    private static Color KindColor(uint kind)
    {
        if (kind == 1u) return new Color(1f, 0.85f, 0.25f, 1f);
        if (kind == 2u) return new Color(0.35f, 0.80f, 1f, 1f);
        return new Color(0.75f, 0.75f, 0.78f, 1f);
    }

    private void OnPointClicked(WorldMapPoint point)
    {
        if (point != null) Intent?.Invoke(new WorldMapIntent(WorldMapAction.Select, point: point.index));
    }
    private void UpdateSelectionScales()
    {
        for (int i = 0; i < _iconObjects.Count; i++)
        {
            GameObject go = _iconObjects[i];
            if (go == null) continue;

            WorldMapPointIcon icon = go.GetComponent<WorldMapPointIcon>();
            if (icon == null) continue;

            //点位是纯 C# 对象 → 引用相等就是"同一个点"（比按名字比对可靠）
            icon.SetSelected(_selected != null && ReferenceEquals(icon.Point, _selected));
        }
    }

    /// <summary>
    /// 写信息条。⚠ `InfoBar` 上同时挂着 `LocalizedText`：它在语言切换时会**覆盖** TMP 文本，
    /// 所以显示"运行时拼接的三元组"时必须把它禁用，否则切一次语言内容就被冲掉。
    /// </summary>
    private void SetInfoText(string text)
    {
        if (_infoLocalized != null) _infoLocalized.enabled = false;
        if (_infoText != null) _infoText.text = text;
    }

    private void OnReturnClicked() { Intent?.Invoke(new WorldMapIntent(WorldMapAction.Return)); }
    private void OnEnterRunClicked() { OpenMetaPanel(PanelType.DispatchPanel); }
    private void OnStagePrevClicked() { ChangeStageForDebug(-1); }
    private void OnStageNextClicked() { ChangeStageForDebug(1); }
    private void ChangeStageForDebug(int delta) { Intent?.Invoke(new WorldMapIntent(WorldMapAction.Preview, delta: delta)); }
    private void OnDestroy()
    {
        OnExit();
        ClearIcons();

        //运行时生成的圆点 Sprite/Texture 不是资产，必须自己释放（HideAndDontSave 也不会自动回收）
        foreach (KeyValuePair<uint, Sprite> pair in _fallbackIcons)
        {
            if (pair.Value == null) continue;
            if (pair.Value.texture != null) Destroy(pair.Value.texture);
            Destroy(pair.Value);
        }
        _fallbackIcons.Clear();
    }
}
