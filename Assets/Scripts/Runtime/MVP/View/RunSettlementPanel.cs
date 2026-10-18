using System.Collections.Generic;
using ChaosDebug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 局内结算面板（W3.2h 修整版）。
///
/// **本轮修的两个 UI 缺陷**：
/// 1. 按钮显示原始本地化 key（`ui_settlement_btn_Confirm`）→ 面板内**全部文案改纯中文**，
///    **不再挂 `LocalizedText`**（与族人/库存面板一致），杜绝 key 被回填；
/// 2. 布局散、标题被 GM 悬浮按钮压住 → 全部内容放进**居中的 800×600 内容区**：
///    标题贴顶、提示其下、列表中部、按钮贴底居中。
///
/// 结算推进逻辑与 W3.2e 完全一致（未改）：通关按 `带出 → MarkBossCleared → MarkSceneCleared(3)
/// → clears+1 → TryAdvanceDepth → maxClearedDepth → TryAdvanceEra → Save(3) → ReturnToMeta`；
/// 失败只写保留条目 + ReturnToMeta；`_settled` 幂等。**存档用槽 3，不动槽 1**。
/// </summary>
public class RunSettlementPanel : BasePanel
{
    private static bool _settled;
    private static bool _winMode;

    private const float PanelWidth = 800f;
    private const float PanelHeight = 600f;

    private TextMeshProUGUI _title;
    private TextMeshProUGUI _hint;
    private RectTransform _listArea;
    private TMP_FontAsset _font;
    private bool _built;

    private readonly List<uint> _allItemIds = new List<uint>();
    private readonly List<uint> _allCounts = new List<uint>();
    private readonly HashSet<uint> _selected = new HashSet<uint>();
    private readonly List<uint> _retained = new List<uint>();

    public static void Open(bool win)
    {
        _winMode = win;
        _settled = false;
        if (UIManager.Instance == null) return;

        //缺陷B 修复：局内 Canvas 被隐藏 → 先恢复再加面板
        UIManager.Instance.RestoreAfterGameplay();
        RunDepthHud.SetVisible(false);   //W4e：结算时隐藏 HUD
        UIManager.Instance.PushPanel(PanelType.RunSettlementPanel);

        RunSettlementPanel panel = Object.FindFirstObjectByType<RunSettlementPanel>(FindObjectsInactive.Include);
        if (panel != null)
        {
            if (!panel.gameObject.activeSelf) panel.gameObject.SetActive(true);
            Transform parent = panel.transform.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf) parent.gameObject.SetActive(true);
                parent = parent.parent;
            }
            panel.RefreshFromOpen();
        }
    }

    /// <summary>缺陷A 修复：PushPanel 后显式刷新（面板已缓存时 OnEnter 不重跑）。</summary>
    public void RefreshFromOpen()
    {
        BuildUi();
        Refresh();
    }

    private void Awake() { BuildUi(); }
    public override void OnEnter() { BuildUi(); Refresh(); }

    // ══════════════════ UI 构建（居中 800×600）══════════════════
    private void BuildUi()
    {
        if (_built) return;
        _built = true;
        _title = FindChildText("Title");
        if (_title != null)
        {
            _font = _title.font;
            //prefab 自带的 Title 在左上角（会被 GM 悬浮按钮压住）→ 隐藏，改用内容区里的居中标题
            _title.gameObject.SetActive(false);
        }
        if (_font == null) _font = TMP_Settings.defaultFontAsset;

        //居中内容区
        GameObject box = new GameObject("ContentBox", typeof(RectTransform), typeof(Image));
        RectTransform boxRect = (RectTransform)box.transform;
        boxRect.SetParent(transform, false);
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.anchoredPosition = Vector2.zero;
        boxRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        Image bg = box.GetComponent<Image>();
        bg.color = new Color(0.10f, 0.11f, 0.14f, 0.98f);
        bg.raycastTarget = true;

        //标题（顶部居中，纯中文，不挂 LocalizedText）
        _title = MakeText(boxRect, "Title", "局内结算", 34f, new Color(0.96f, 0.97f, 1f, 1f));
        RectTransform titleRect = (RectTransform)_title.transform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(0f, -70f);
        titleRect.offsetMax = new Vector2(0f, -16f);

        //提示（标题下方）
        _hint = MakeText(boxRect, "Hint", "", 20f, new Color(1f, 0.85f, 0.3f, 1f));
        RectTransform hintRect = (RectTransform)_hint.transform;
        hintRect.anchorMin = new Vector2(0f, 1f);
        hintRect.anchorMax = new Vector2(1f, 1f);
        hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.offsetMin = new Vector2(24f, -160f);
        hintRect.offsetMax = new Vector2(-24f, -80f);

        //列表区（中部，带遮罩裁剪）
        GameObject listGo = new GameObject("ListArea", typeof(RectTransform));
        _listArea = (RectTransform)listGo.transform;
        _listArea.SetParent(boxRect, false);
        _listArea.anchorMin = new Vector2(0f, 0f);
        _listArea.anchorMax = new Vector2(1f, 1f);
        _listArea.offsetMin = new Vector2(24f, 96f);
        _listArea.offsetMax = new Vector2(-24f, -168f);
        listGo.AddComponent<RectMask2D>();

        //底部按钮（居中并排，纯中文）
        MakeButton(boxRect, "Btn_Confirm", "确认", new Vector2(-100f, 30f), Confirm);
        MakeButton(boxRect, "Btn_Discard", "放弃全部", new Vector2(100f, 30f), DiscardAll);
    }

    private void Refresh()
    {
        PauseWorld(true);
        if (_title != null) _title.text = _winMode ? "局内通关" : "局内失败";

        CollectItems();
        if (_hint != null)
        {
            if (_winMode)
                _hint.text = "可带出 " + MetaProgressService.Instance.DeployCap + " 件（已选 " + _selected.Count + "）";
            else
                _hint.text = "失败：保留 50% 掉落（" + _allItemIds.Count + " 条 → 保留 " + _retained.Count + " 条："
                    + RetainedNames() + "）";
        }

        ClearList();
        float y = -4f;
        for (int i = 0; i < _allItemIds.Count; i++)
        {
            uint id = _allItemIds[i];
            uint count = _allCounts[i];
            bool selected = _selected.Contains(id);
            bool retained = _retained.Contains(id);

            string line = (selected ? "【已选】" : (retained ? "【保留】" : "　　")) + "物品 " + id + " ×" + count;
            Color color = selected || retained ? new Color(0.35f, 0.88f, 0.45f, 1f) : new Color(0.92f, 0.93f, 0.96f, 1f);
            MakeRow(line, color, y);
            if (_winMode) MakeSelectButton(id, selected, y);
            y -= 42f;
        }
    }

    private void CollectItems()
    {
        _allItemIds.Clear(); _allCounts.Clear(); _retained.Clear();
        List<RunInventory.Stack> stacks = RunInventory.Stacks;
        for (int i = 0; i < stacks.Count; i++)
        {
            _allItemIds.Add(stacks[i].itemId);
            _allCounts.Add(stacks[i].count);
        }

        if (!_winMode && _allItemIds.Count > 0)
        {
            int keep = _allItemIds.Count / 2;
            var indices = new List<int>();
            for (int i = 0; i < _allItemIds.Count; i++) indices.Add(i);
            var random = new System.Random(20261018);
            for (int i = 0; i < keep; i++)
            {
                int pick = random.Next(0, indices.Count);
                _retained.Add(_allItemIds[indices[pick]]);
                indices.RemoveAt(pick);
            }
        }

        var alive = new HashSet<uint>(_allItemIds);
        _selected.RemoveWhere(x => !alive.Contains(x));
    }

    private string RetainedNames()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < _retained.Count; i++) sb.Append(i == 0 ? "" : "/").Append(_retained[i]);
        return sb.Length == 0 ? "无" : sb.ToString();
    }

    private void MakeRow(string text, Color color, float y)
    {
        if (_listArea == null) return;
        TextMeshProUGUI label = MakeText(_listArea, "Row", text, 22f, color);
        RectTransform rect = (RectTransform)label.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.offsetMin = new Vector2(8f, y - 34f);
        rect.offsetMax = new Vector2(-150f, y);
        label.alignment = TextAlignmentOptions.Left;
    }

    private void MakeSelectButton(uint itemId, bool selected, float y)
    {
        if (_listArea == null) return;
        GameObject go = new GameObject("Sel_" + itemId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(_listArea, false);
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(-136f, y - 34f);
        rect.offsetMax = new Vector2(-8f, y);
        go.GetComponent<Image>().color = selected ? new Color(0.25f, 0.55f, 0.35f, 1f) : new Color(0.2f, 0.42f, 0.72f, 1f);
        TextMeshProUGUI tmp = MakeText(rect, "Text", selected ? "取消" : "选中", 20f, Color.white);
        RectTransform tr = (RectTransform)tmp.transform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        tmp.alignment = TextAlignmentOptions.Center;
        uint captured = itemId;
        go.GetComponent<Button>().onClick.AddListener(() => ToggleSelect(captured));
    }

    private void ToggleSelect(uint itemId)
    {
        if (_selected.Contains(itemId)) { _selected.Remove(itemId); Refresh(); return; }

        int cap = MetaProgressService.Instance.DeployCap;
        if (_selected.Count >= cap)
        {
            if (_hint != null) _hint.text = "最多带出 " + cap + " 件";
            ChaosLog.Warn(LogChannel.Config, "[结算] 超上限：最多带出 " + cap + " 件，拒绝勾选物品 " + itemId);
            return;
        }
        _selected.Add(itemId);
        Refresh();
    }

    // ══════════════════ 结算推进（与 W3.2e 一致，未改）══════════════════
    private void Confirm()
    {
        if (_settled)
        {
            ChaosLog.Warn(LogChannel.Config, "[结算] 已结算过（幂等），忽略重复确认");
            return;
        }
        _settled = true;

        MetaProgressService meta = MetaProgressService.Instance;
        SavePayload payload = meta.Payload;

        if (!_winMode)
        {
            int added = 0;
            for (int i = 0; i < _allItemIds.Count; i++)
            {
                if (!_retained.Contains(_allItemIds[i])) continue;
                string reason;
                if (InventoryService.Instance.AddItem(_allItemIds[i], _allCounts[i], out reason)) added++;
                else ChaosLog.Warn(LogChannel.Config, "[结算] 失败态保留条目入库失败：" + reason);
            }
            ChaosLog.Success(LogChannel.Config, "[结算] 失败结算：保留 " + _retained.Count + " 条，实际入库 " + added
                + " 条；进度不变（clears=" + payload.clears + " depth=" + meta.Depth + " era=" + meta.Era + "）");
            Finish();
            return;
        }

        int exported = 0;
        foreach (uint id in _selected)
        {
            int index = _allItemIds.IndexOf(id);
            if (index < 0) continue;
            string reason;
            if (InventoryService.Instance.AddItem(id, _allCounts[index], out reason)) exported++;
            else ChaosLog.Warn(LogChannel.Config, "[结算] 带出入库失败：" + reason);
        }

        meta.MarkBossCleared();
        meta.MarkSceneCleared(3);
        payload.clears++;
        meta.TryAdvanceDepth();
        if (payload.depth > payload.maxClearedDepth) payload.maxClearedDepth = payload.depth;
        meta.TryAdvanceEra();

        bool saved = SaveSlotService.Instance.Save(3, payload);
        ChaosLog.Success(LogChannel.Config, "[结算] 通关结算：带出 " + exported + "/" + _selected.Count + " 件；clears="
            + payload.clears + " depth=" + payload.depth + " maxClearedDepth=" + payload.maxClearedDepth
            + " era=" + payload.era + " Save(3)=" + saved);
        Finish();
    }

    private void DiscardAll()
    {
        if (_settled) { ChaosLog.Warn(LogChannel.Config, "[结算] 已结算过（幂等），忽略"); return; }
        _selected.Clear();
        Confirm();
    }

    private void Finish()
    {
        PauseWorld(false);
        RunInventory.Clear();
        string reason;
        DispatchService.Instance.ReturnToMeta(out reason);
        Hide();
    }

    private static void PauseWorld(bool paused)
    {
        foreach (PlayerLocomotion loco in Object.FindObjectsOfType<PlayerLocomotion>()) loco.enabled = !paused;
        foreach (PlayerAttack attack in Object.FindObjectsOfType<PlayerAttack>()) attack.enabled = !paused;
        foreach (AllyController ally in Object.FindObjectsOfType<AllyController>()) ally.enabled = !paused;
        foreach (EnemyPlaceholder enemy in Object.FindObjectsOfType<EnemyPlaceholder>()) enemy.enabled = !paused;
        ChaosLog.Info(LogChannel.UI, "[结算] 世界 " + (paused ? "已暂停" : "已恢复"));
    }

    // ── 通用小工具（面板内文案一律纯中文，不挂 LocalizedText）──
    private TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = _font;
        label.fontSize = size;
        label.color = color;
        label.text = text;
        label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.Center;
        return label;
    }

    private void MakeButton(Transform parent, string name, string label, Vector2 offset, System.Action onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = offset;
        rect.sizeDelta = new Vector2(180f, 52f);
        go.GetComponent<Image>().color = new Color(0.2f, 0.42f, 0.72f, 1f);
        go.GetComponent<Button>().onClick.AddListener(() => onClick());

        TextMeshProUGUI tmp = MakeText(rect, "Text", label, 22f, Color.white);
        RectTransform tr = (RectTransform)tmp.transform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private void ClearList()
    {
        if (_listArea == null) return;
        for (int i = _listArea.childCount - 1; i >= 0; i--)
        {
            Transform c = _listArea.GetChild(i);
            c.SetParent(null, false);
            Destroy(c.gameObject);
        }
    }

    private TextMeshProUGUI FindChildText(string n)
    {
        Transform c = transform.Find(n);
        return c == null ? null : c.GetComponent<TextMeshProUGUI>();
    }
}
