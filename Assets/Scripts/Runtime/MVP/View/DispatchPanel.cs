using System.Collections.Generic;
using ChaosDebug;
using LocalizationSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 派遣编队面板（W2）：选 1~4 位门客 → 出发（就地生成局内地图，不切场景）→ 可撤离返回局外。
/// 不可出战的门客置灰并显示原因；**不显示任何单人收益/风险提示**（用户 U6）。
/// </summary>
public class DispatchPanel : BasePanel
{
    private static readonly Color NormalColor = new Color(0.92f, 0.93f, 0.96f, 1f);
    private static readonly Color DimColor = new Color(0.62f, 0.66f, 0.72f, 1f);
    private static readonly Color SelectedColor = new Color(0.30f, 0.85f, 0.40f, 1f);
    private static readonly Color WoundedColor = new Color(1.00f, 0.36f, 0.32f, 1f);
    private static readonly Color LowConfColor = new Color(1.00f, 0.82f, 0.25f, 1f);

    private TextMeshProUGUI _title;
    private TextMeshProUGUI _summary;
    private TextMeshProUGUI _hint;
    private RectTransform _content;
    private TMP_FontAsset _font;
    private bool _built;

    private void Awake() { BuildUi(); }
    public override void OnEnter() { BuildUi(); Refresh(); }

    private void BuildUi()
    {
        if (_built) return;
        _built = true;
        _title = FindChildText("Title");
        if (_title != null) _font = _title.font;
        _summary = CreateText("Summary", 64f, NormalColor, 22f);
        _content = CreateRect("Content", 110f);
        _hint = CreateText("Hint", 96f, DimColor, 20f);
        CreateBottomButtons();
    }

    private void Refresh()
    {
        if (_title != null) ApplyLocalized(_title, "ui_dispatch_title", "派遣");

        DispatchService svc = DispatchService.Instance;
        RetainerService rs = RetainerService.Instance;

        if (_summary != null)
            ApplyLocalized(_summary, "ui_dispatch_summary",
                "已选 " + svc.SelectedCount + "/" + DispatchService.MaxTeamSize
                + (svc.IsInRun ? "　　（局内）" : ""));

        if (_hint != null)
        {
            if (svc.IsInRun) ApplyLocalized(_hint, "ui_dispatch_inrun", "已进入局内（原型：就地生成）——点「撤离」返回局外");
            else if (svc.SelectedCount == 0) ApplyLocalized(_hint, "ui_dispatch_empty", "点门客名字选择（1~4 位）；不可出战的门客会置灰并显示原因");
            else ApplyLocalized(_hint, "ui_dispatch_hint", "");
        }

        ClearRows();

        List<RetainerSave> all = svc.GetCandidates();
        if (all.Count == 0)
        {
            AddRow(0, "暂无门客（用 GM 发放）", DimColor, 0u);
            return;
        }

        for (int i = 0; i < all.Count; i++)
        {
            RetainerSave retainer = all[i];
            if (retainer == null) continue;

            RetainerConfig def = rs.GetDefinition(retainer.protoId);
            string name = def == null ? ("门客#" + retainer.id) : def.NameKey;
            string unavailable = svc.GetUnavailableReason(retainer);
            bool selected = svc.SelectedIds.Contains(retainer.id);

            string line = (selected ? "【已选】" : "　　") + name
                + "　品质 " + retainer.quality + "　信心 " + retainer.confidence;
            if (!string.IsNullOrEmpty(unavailable)) line += "　（不可出战：" + unavailable + "）";

            Color color;
            if (!string.IsNullOrEmpty(unavailable))
                color = rs.GetState(retainer) == RetainerService.DeployState.Wounded ? WoundedColor : LowConfColor;
            else color = selected ? SelectedColor : NormalColor;

            AddRow(i, line, color, retainer.id);
        }
    }

    private void AddRow(int index, string text, Color color, uint id)
    {
        if (_content == null) return;

        GameObject go = new GameObject("Row" + index, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(_content, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -8f - index * 46f);
        rect.sizeDelta = new Vector2(-48f, 40f);

        Image bg = go.GetComponent<Image>();
        bg.color = new Color(0.14f, 0.16f, 0.20f, 1f);

        GameObject labelGo = new GameObject("Text", typeof(RectTransform));
        RectTransform labelRect = (RectTransform)labelGo.transform;
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10f, 0f); labelRect.offsetMax = new Vector2(-10f, 0f);

        TextMeshProUGUI label = labelGo.AddComponent<TextMeshProUGUI>();
        label.font = _font; label.fontSize = 22f; label.color = color;
        label.alignment = TextAlignmentOptions.Left; label.raycastTarget = false;
        label.text = text;

        if (id != 0u)
        {
            Button button = go.GetComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(() => DoToggle(id));
        }
        else
        {
            UnityEngine.Object.Destroy(go.GetComponent<Button>());
        }
    }

    private void DoToggle(uint id)
    {
        string reason;
        bool ok = DispatchService.Instance.Toggle(id, out reason);
        if (!ok && !string.IsNullOrEmpty(reason)) Debug.Log("[派遣] " + reason);
        Refresh();
    }

    private void DoDepart()
    {
        string reason;
        bool ok = DispatchService.Instance.Depart(out reason);

        if (ok && UIManager.Instance != null) UIManager.Instance.HideAllPanelsForGameplay();
        Debug.Log(ok ? "[派遣] 出发成功" : ("[派遣] 出发失败：" + reason));
        Refresh();
    }

    private void DoReturn()
    {
        string reason;
        bool ok = DispatchService.Instance.ReturnToMeta(out reason);

        if (ok && UIManager.Instance != null) UIManager.Instance.RestoreAfterGameplay();
        Debug.Log(ok ? "[派遣] 已撤离" : ("[派遣] 撤离失败：" + reason));
        Refresh();
    }

    private void CreateBottomButtons()
    {
        CreateButton("Btn_Depart", "出发", new Vector2(-380f, 24f), () => DoDepart());
        CreateButton("Btn_Return", "撤离 / 返回局外", new Vector2(-200f, 24f), () => DoReturn());
        CreateButton("Btn_Close", "关闭", new Vector2(-24f, 24f), () => Hide());
    }

    private void CreateButton(string name, string label, Vector2 offset, System.Action onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(1f, 0f); rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = offset;
        rect.sizeDelta = new Vector2(160f, 48f);
        go.GetComponent<Image>().color = new Color(0.20f, 0.42f, 0.72f, 1f);
        go.GetComponent<Button>().onClick.AddListener(() => onClick());

        GameObject t = new GameObject("Text", typeof(RectTransform));
        RectTransform tr = (RectTransform)t.transform;
        tr.SetParent(rect, false); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = t.AddComponent<TextMeshProUGUI>();
        tmp.font = _font; tmp.fontSize = 22f; tmp.color = NormalColor;
        tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;
        ApplyLocalized(tmp, "ui_dispatch_btn_" + name, label);
    }

    private TextMeshProUGUI FindChildText(string n) { Transform c = transform.Find(n); return c == null ? null : c.GetComponent<TextMeshProUGUI>(); }

    private TextMeshProUGUI CreateText(string name, float y, Color color, float size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -y);
        rect.sizeDelta = new Vector2(-48f, 28f);
        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = _font; label.fontSize = size; label.color = color;
        label.alignment = TextAlignmentOptions.TopLeft; label.raycastTarget = false;
        return label;
    }

    private RectTransform CreateRect(string name, float top)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = new Vector2(0f, 0f); rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(0f, 90f); rect.offsetMax = new Vector2(0f, -top);
        return rect;
    }

    private void ClearRows()
    {
        if (_content == null) return;
        for (int i = _content.childCount - 1; i >= 0; i--)
        { Transform c = _content.GetChild(i); c.SetParent(null, false); Destroy(c.gameObject); }
    }

    private static void ApplyLocalized(TextMeshProUGUI label, string key, string fallback)
    {
        if (label == null) return;
        label.text = fallback;
        LocalizedText loc = label.GetComponent<LocalizedText>();
        if (loc == null) loc = label.gameObject.AddComponent<LocalizedText>();
        loc.localizationKey = key; loc.autoUpdateOnStart = false; loc.listenToLanguageChange = false;
    }
}
