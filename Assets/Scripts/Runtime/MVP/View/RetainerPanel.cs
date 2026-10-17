using System.Collections.Generic;
using ChaosDebug;
using LocalizationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 族人面板（W1a）：一行一个族人，**状态标签一眼可见**（可用 / 信心不足 / 重伤 Ns）。
/// 状态优先级：重伤 &gt; 信心不足 &gt; 可用（05 §8.2）。
///
/// 说明：UI 在 `Awake` 里**运行时构建**（只依赖 prefab 根上的 Title 文本当字体模板），
/// 所有界面文案挂 `LocalizedText`（key 先给字符串、中文兜底；等收集器统一补 key 表），
/// 与其它面板的既有用法一致。
/// </summary>
public class RetainerPanel : BasePanel
{
    private static readonly Color AvailableColor = new Color(0.30f, 0.85f, 0.40f, 1f);
    private static readonly Color WoundedColor = new Color(1.00f, 0.36f, 0.32f, 1f);
    private static readonly Color LowConfidenceColor = new Color(1.00f, 0.82f, 0.25f, 1f);
    private static readonly Color NormalColor = new Color(0.92f, 0.93f, 0.96f, 1f);
    private static readonly Color DimColor = new Color(0.72f, 0.76f, 0.82f, 1f);

    private TextMeshProUGUI _title;
    private TextMeshProUGUI _summary;
    private TextMeshProUGUI _hint;
    private RectTransform _content;
    private TMP_FontAsset _font;
    private bool _built;

    private void Awake()
    {
        BuildUi();
    }

    public override void OnEnter()
    {
        BuildUi();
        Refresh();
    }

    private void BuildUi()
    {
        if (_built) return;
        _built = true;

        _title = FindChildText("Title");
        if (_title != null)
        {
            _font = _title.font;
            ApplyLocalized(_title, "ui_retainer_panel_title", "族人");
        }

        _summary = CreateText("Summary", 64f, NormalColor, 22f);
        _content = CreateRect("Content");
        _hint = CreateText("Hint", 22f, DimColor, 22f);

        CreateCloseButton();
    }

    private void Refresh()
    {
        if (_title != null) ApplyLocalized(_title, "ui_retainer_panel_title", "族人");

        RetainerService svc = RetainerService.Instance;
        List<RetainerSave> all = svc.GetAll();

        int deployable = 0;
        for (int i = 0; i < all.Count; i++) if (svc.IsDeployable(all[i])) deployable++;

        if (_summary != null)
            ApplyLocalized(_summary, "ui_retainer_summary",
                "共 " + all.Count + "/" + RetainerService.MaxRetainers
                + " 位　　可出战 " + deployable + " 位　　（可出战 = 未重伤 且 信心 ≥ " + RetainerService.ConfidenceThreshold + "）");

        ClearRows();

        if (all.Count == 0)
        {
            if (_hint != null) ApplyLocalized(_hint, "ui_retainer_empty", "暂无族人（用 GM 发放）");
            return;
        }
        if (_hint != null) ApplyLocalized(_hint, "ui_retainer_hint", "");

        for (int i = 0; i < all.Count; i++)
        {
            RetainerSave retainer = all[i];
            if (retainer == null) continue;

            RetainerConfig def = svc.GetDefinition(retainer.protoId);
            string name = def == null ? ("族人#" + retainer.id) : def.NameKey;

            string line = name
                + "　　品质 " + retainer.quality
                + "　　信心 " + retainer.confidence
                + "　　" + StateLabel(svc, retainer);

            // 详情：生命 / 攻击 / 防御 / 速度（来自模板；属性不落盘，见 09 §5）
            if (def != null)
                line += "\n　　　生命 " + def.Hp + "　　攻击 " + def.Attack
                      + "　　防御 " + def.Defense + "　　速度 " + def.Speed;

            Color color;
            switch (svc.GetState(retainer))
            {
                case RetainerService.DeployState.Wounded: color = WoundedColor; break;
                case RetainerService.DeployState.LowConfidence: color = LowConfidenceColor; break;
                default: color = AvailableColor; break;
            }

            AddRow(i, line, color);
        }
    }

    /// <summary>状态标签（中文）：重伤 Ns / 信心不足 / 可用 —— 优先级 重伤 &gt; 信心不足 &gt; 可用。</summary>
    private static string StateLabel(RetainerService svc, RetainerSave retainer)
    {
        switch (svc.GetState(retainer))
        {
            case RetainerService.DeployState.Wounded:
                return "重伤 " + retainer.woundedRemainSeconds + "s";
            case RetainerService.DeployState.LowConfidence:
                return "信心不足";
            default:
                return "可用";
        }
    }

    private void ClearRows()
    {
        if (_content == null) return;
        for (int i = _content.childCount - 1; i >= 0; i--)
        {
            Transform child = _content.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }

    private void AddRow(int index, string text, Color color)
    {
        if (_content == null) return;

        GameObject go = new GameObject("Row" + index, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(_content, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -8f - index * 62f);
        rect.sizeDelta = new Vector2(-48f, 58f);

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = _font;
        label.fontSize = 22f;
        label.color = color;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.raycastTarget = false;
        label.text = text;
    }

    private TextMeshProUGUI FindChildText(string childName)
    {
        Transform child = transform.Find(childName);
        return child == null ? null : child.GetComponent<TextMeshProUGUI>();
    }

    private TextMeshProUGUI CreateText(string name, float y, Color color, float fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -y);
        rect.sizeDelta = new Vector2(-48f, 30f);

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = _font;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.raycastTarget = false;
        return label;
    }

    private RectTransform CreateRect(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(0f, 40f);
        rect.offsetMax = new Vector2(0f, -180f);
        return rect;
    }

    /// <summary>右下角「关闭」按钮。</summary>
    private void CreateCloseButton()
    {
        GameObject go = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-24f, 24f);
        rect.sizeDelta = new Vector2(160f, 48f);

        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0.18f, 0.21f, 0.26f, 1f);

        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        RectTransform textRect = (RectTransform)textGo.transform;
        textRect.SetParent(rect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = textGo.AddComponent<TextMeshProUGUI>();
        label.font = _font;
        label.fontSize = 22f;
        label.color = NormalColor;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        ApplyLocalized(label, "ui_common_close", "关闭");

        go.GetComponent<Button>().onClick.AddListener(() => Hide());
    }

    /// <summary>挂 LocalizedText（key 先给字符串，中文兜底；不自动刷新，避免把兜底中文冲掉）。</summary>
    private static void ApplyLocalized(TextMeshProUGUI label, string key, string fallback)
    {
        if (label == null) return;
        label.text = fallback;

        LocalizedText loc = label.GetComponent<LocalizedText>();
        if (loc == null) loc = label.gameObject.AddComponent<LocalizedText>();
        loc.localizationKey = key;
        loc.autoUpdateOnStart = false;
        loc.listenToLanguageChange = false;
    }
}
