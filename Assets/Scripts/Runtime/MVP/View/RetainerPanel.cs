using System.Collections.Generic;
using ChaosDebug;
using TMPro;
using UnityEngine;

/// <summary>
/// 族人面板（W1a）：一行一个族人，**状态标签一眼可见**（可用/重伤 Ns/信心不足）。
/// 状态优先级：重伤 &gt; 信心不足 &gt; 可用（05 §8.2）。
///
/// 说明：UI 在 `Awake` 里**运行时构建**（只依赖 prefab 根上的 Title 文本当字体模板），
/// 这样 prefab 只需根 + 标题，避免手搓大量子节点；样式/层级仍沿用既有面板约定。
/// </summary>
public class RetainerPanel : BasePanel
{
    private static readonly Color AvailableColor = new Color(0.30f, 0.85f, 0.40f, 1f);
    private static readonly Color WoundedColor = new Color(1.00f, 0.36f, 0.32f, 1f);
    private static readonly Color LowConfidenceColor = new Color(1.00f, 0.82f, 0.25f, 1f);
    private static readonly Color NormalColor = new Color(0.92f, 0.93f, 0.96f, 1f);

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
        if (_title != null) _font = _title.font;

        _summary = CreateText("Summary", 28f, new Color(0.80f, 0.84f, 0.90f, 1f), 22f);
        _content = CreateRect("Content");
        _hint = CreateText("Hint", 22f, new Color(0.75f, 0.78f, 0.84f, 1f), 22f);
    }

    private void Refresh()
    {
        if (_title != null) _title.text = "RETAINERS";

        RetainerService svc = RetainerService.Instance;
        List<RetainerSave> all = svc.GetAll();

        int deployable = 0;
        for (int i = 0; i < all.Count; i++) if (svc.IsDeployable(all[i])) deployable++;

        if (_summary != null)
            _summary.text = "TOTAL " + all.Count + "/" + RetainerService.MaxRetainers
                + "    READY " + deployable
                + "    (READY = !WOUNDED && CONF >= " + RetainerService.ConfidenceThreshold + ")";

        //清空旧行（Destroy 到帧末生效 → 先 detach，避免同帧重复）
        if (_content != null)
        {
            for (int i = _content.childCount - 1; i >= 0; i--)
            {
                Transform child = _content.GetChild(i);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }

        if (all.Count == 0)
        {
            if (_hint != null) _hint.text = "(no retainers yet - GM > System > Retainers > Recruit)";
            return;
        }
        if (_hint != null) _hint.text = "";

        for (int i = 0; i < all.Count; i++)
        {
            RetainerSave retainer = all[i];
            if (retainer == null) continue;

            RetainerConfig def = svc.GetDefinition(retainer.protoId);
            string name = def == null ? ("族人#" + retainer.id + "（模板缺失）") : def.NameKey;

            string line = name
                + "    Q" + retainer.quality
                + "    CONF " + retainer.confidence
                + "    " + AsciiState(svc, retainer);

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

    /// <summary>状态标签的 ASCII 版（本项目 MSYH SDF 源字体缺「信心/未/足」等字形，中文会显示成方块）。</summary>
    private static string AsciiState(RetainerService svc, RetainerSave retainer)
    {
        switch (svc.GetState(retainer))
        {
            case RetainerService.DeployState.Wounded: return "WOUNDED " + retainer.woundedRemainSeconds + "s";
            case RetainerService.DeployState.LowConfidence: return "LOW CONF";
            default: return "READY";
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
        rect.anchoredPosition = new Vector2(24f, -8f - index * 40f);
        rect.sizeDelta = new Vector2(-48f, 36f);

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = _font;
        label.fontSize = 24f;
        label.color = color;
        label.alignment = TextAlignmentOptions.Left;
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
        rect.offsetMin = new Vector2(0f, 120f);
        rect.offsetMax = new Vector2(0f, -140f);
        return rect;
    }
}
