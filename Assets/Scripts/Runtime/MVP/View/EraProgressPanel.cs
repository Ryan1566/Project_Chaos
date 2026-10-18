using ChaosDebug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 时代进度面板（W4b）：居中 800×600 内容区、**纯中文（不挂 LocalizedText）**。
///
/// 8 行信息 + 「推进时代」二次确认（第一次点变「确认推进到 时代N（不可回退）」+「取消」）。
/// 门槛①读取用**反射兜底**（`BossClearedThisRun` / `_bossClearedThisRun`），拿不到按 false ——
/// 待 `MetaProgressService` 暴露公开属性后替换。
/// </summary>
public class EraProgressPanel : BasePanel
{
    private const float PanelWidth = 800f;
    private const float PanelHeight = 600f;
    private const int MaxEra = 5;

    private TextMeshProUGUI _title;
    private TextMeshProUGUI _lines;      //8 行合并成一个多行文本（左对齐）
    private TextMeshProUGUI _hint;
    private Button _advanceButton;
    private Button _cancelButton;
    private TMP_FontAsset _font;
    private bool _built;
    private bool _confirming;

    public static void Open()
    {
        if (UIManager.Instance == null) return;
        UIManager.Instance.RestoreAfterGameplay();
        UIManager.Instance.PushPanel(PanelType.EraProgressPanel);

        EraProgressPanel panel = Object.FindFirstObjectByType<EraProgressPanel>(FindObjectsInactive.Include);
        if (panel != null)
        {
            if (!panel.gameObject.activeSelf) panel.gameObject.SetActive(true);
            Transform parent = panel.transform.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf) parent.gameObject.SetActive(true);
                parent = parent.parent;
            }
            panel.BuildUi();
            panel._confirming = false;
            panel.Refresh();
        }
    }

    private void Awake() { BuildUi(); }
    public override void OnEnter() { BuildUi(); _confirming = false; Refresh(); }

    private void BuildUi()
    {
        if (_built) return;
        _built = true;

        Transform oldTitle = transform.Find("Title");
        if (oldTitle != null)
        {
            var oldTmp = oldTitle.GetComponent<TMPro.TextMeshProUGUI>();
            if (oldTmp != null) _font = oldTmp.font;
            oldTitle.gameObject.SetActive(false);
        }
        if (_font == null) _font = TMP_Settings.defaultFontAsset;

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

        _title = MakeText(boxRect, "Title", "时代进度", 34f, new Color(0.96f, 0.97f, 1f, 1f));
        RectTransform tr = (RectTransform)_title.transform;
        tr.anchorMin = new Vector2(0f, 1f); tr.anchorMax = new Vector2(1f, 1f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.offsetMin = new Vector2(0f, -70f); tr.offsetMax = new Vector2(0f, -16f);

        //8 行信息（一个多行文本，左对齐）
        _lines = MakeText(boxRect, "Lines", "", 22f, new Color(0.92f, 0.93f, 0.96f, 1f));
        RectTransform lr = (RectTransform)_lines.transform;
        lr.anchorMin = new Vector2(0f, 1f); lr.anchorMax = new Vector2(1f, 1f);
        lr.pivot = new Vector2(0.5f, 1f);
        lr.offsetMin = new Vector2(60f, -420f); lr.offsetMax = new Vector2(-60f, -90f);
        _lines.alignment = TextAlignmentOptions.TopLeft;
        _lines.enableWordWrapping = true;

        _hint = MakeText(boxRect, "Hint", "", 20f, new Color(1f, 0.85f, 0.3f, 1f));
        RectTransform hr = (RectTransform)_hint.transform;
        hr.anchorMin = new Vector2(0f, 0f); hr.anchorMax = new Vector2(1f, 0f);
        hr.pivot = new Vector2(0.5f, 0f);
        hr.offsetMin = new Vector2(40f, 120f); hr.offsetMax = new Vector2(-40f, 190f);

        _advanceButton = MakeButton(boxRect, "Btn_Advance", "推进时代", new Vector2(-150f, 36f));
        _advanceButton.onClick.AddListener(OnAdvance);
        _cancelButton = MakeButton(boxRect, "Btn_Cancel", "关闭", new Vector2(150f, 36f));
        _cancelButton.onClick.AddListener(() =>
        {
            if (_confirming) { _confirming = false; Refresh(); }
            else Hide();
        });
    }

    private void Refresh()
    {
        MetaProgressService meta = MetaProgressService.Instance;
        uint era = meta.Era;
        uint depth = meta.Depth;
        uint cap = meta.DepthCap;
        uint clears = meta.Payload.clears;
        int maxDepth = (int)meta.Payload.maxClearedDepth;

        uint milestoneType = EraConfigService.Instance.GetMilestoneType(era);
        uint milestoneValue = EraConfigService.Instance.GetMilestoneValue(era);
        bool bossOk = BossClearedThisRun();
        bool clearOk = milestoneValue == 0u || clears >= milestoneValue;

        string milestoneText = milestoneValue == 0u ? "无需里程碑" : (clears + "/" + milestoneValue);

        var sb = new System.Text.StringBuilder();
        sb.Append("时代　").Append(era).Append("/").Append(MaxEra).Append("　（").Append(EraConfigService.Instance.GetNameKey((uint)era)).Append("）\n");
        sb.Append("深度　").Append(depth).Append("/").Append(cap).Append(depth >= cap ? "　已满" : "").Append("\n");
        sb.Append("累计通关　").Append(clears).Append(" 次\n");
        sb.Append("里程碑　").Append(milestoneText).Append("（类型 ").Append(milestoneType).Append("）\n");
        sb.Append("门槛① 本次通关已满足　").Append(bossOk ? "✔" : "✘").Append("\n");
        sb.Append("门槛② 累计通关 ≥ ").Append(milestoneValue == 0u ? 0 : milestoneValue)
          .Append("（当前 ").Append(clears).Append("）　").Append(clearOk ? "✔" : "✘").Append("\n");
        sb.Append("历史最深　").Append(maxDepth).Append(" 层\n");
        if (_lines != null) _lines.text = sb.ToString();

        //按序第一个失败原因
        string reason = null;
        if (era >= (uint)MaxEra) reason = "已是最后时代";
        else if (!bossOk) reason = "需先击败当前时代 boss（本次通关未满足）";
        else if (!clearOk) reason = "需累计通关 " + milestoneValue + " 次（当前 " + clears + "）";

        bool canAdvance = reason == null;
        SetButton(_advanceButton, canAdvance);
        if (_cancelButton != null)
        {
            var tmp = _cancelButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmp != null) tmp.text = _confirming ? "取消" : "关闭";
        }

        if (_advanceButton != null)
        {
            var btnTmp = _advanceButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (btnTmp != null)
                btnTmp.text = _confirming ? ("确认推进到 时代" + (era + 1u) + "（不可回退）") : "推进时代";
        }

        if (_hint != null)
        {
            if (_confirming) _hint.text = "再次点击确认 → 时代 " + era + " → " + (era + 1) + "（不可回退）";
            else if (!canAdvance) _hint.text = reason;
            else _hint.text = "条件已满足，可推进时代（难度 " + EraConfigService.Instance.GetBaseDifficulty(era)
                + " → " + EraConfigService.Instance.GetBaseDifficulty(era + 1u) + "）";
        }

        ChaosLog.Info(LogChannel.Config, "[时代面板] era=" + era + " depth=" + depth + "/" + cap
            + " clears=" + clears + " milestone=" + milestoneText + " bossOk=" + bossOk + " clearOk=" + clearOk
            + " 可推进=" + canAdvance + (reason == null ? "" : (" 原因=" + reason)));
    }

    private void OnAdvance()
    {
        MetaProgressService meta = MetaProgressService.Instance;
        uint era = meta.Era;

        //按序校验（不满足直接返回，提示已在 Refresh 里给出）
        if (era >= (uint)MaxEra) { Refresh(); return; }
        if (!BossClearedThisRun()) { _confirming = false; Refresh(); return; }
        uint milestoneValue = EraConfigService.Instance.GetMilestoneValue(era);
        if (milestoneValue != 0u && meta.Payload.clears < milestoneValue) { _confirming = false; Refresh(); return; }

        if (!_confirming) { _confirming = true; Refresh(); return; }

        //二次确认 → 真正推进
        uint beforeEra = era;
        uint beforeDifficulty = EraConfigService.Instance.GetBaseDifficulty(era);
        bool ok = meta.TryAdvanceEra();
        uint afterEra = meta.Era;
        uint afterDifficulty = EraConfigService.Instance.GetBaseDifficulty(meta.Era);
        L(ok, beforeEra, afterEra, beforeDifficulty, afterDifficulty);
        _confirming = false;
        Refresh();
    }

    private void L(bool ok, uint beforeEra, uint afterEra, uint beforeDifficulty, uint afterDifficulty)
    {
        if (ok)
            ChaosLog.Success(LogChannel.Config, "[时代面板] 推进成功：时代 " + beforeEra + " → " + afterEra
                + "，难度 " + beforeDifficulty + " → " + afterDifficulty + "，深度上限 " + MetaProgressService.Instance.DepthCap);
        else
            ChaosLog.Warn(LogChannel.Config, "[时代面板] 推进失败：条件不满足或已到最后时代（era=" + MetaProgressService.Instance.Era + "）");
    }

    /// <summary>门槛①：本次通关是否已打 boss（W4c：改成正名访问，不再反射）。</summary>
    private static bool BossClearedThisRun()
    {
        MetaProgressService meta = MetaProgressService.Instance;
        return meta != null && meta.BossClearedThisRun;
    }

    private static void SetButton(Button button, bool interactable)
    {
        if (button == null) return;
        button.interactable = interactable;
        button.GetComponent<Image>().color = interactable
            ? new Color(0.2f, 0.42f, 0.72f, 1f)
            : new Color(0.22f, 0.24f, 0.28f, 1f);
    }

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

    private Button MakeButton(Transform parent, string name, string label, Vector2 offset)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = offset;
        rect.sizeDelta = new Vector2(300f, 54f);
        go.GetComponent<Image>().color = new Color(0.2f, 0.42f, 0.72f, 1f);

        TextMeshProUGUI tmp = MakeText(rect, "Text", label, 22f, Color.white);
        RectTransform tr = (RectTransform)tmp.transform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        tmp.alignment = TextAlignmentOptions.Center;
        return go.GetComponent<Button>();
    }
}
