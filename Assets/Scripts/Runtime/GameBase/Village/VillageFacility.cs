using TMPro;
using UnityEngine;

public enum VillageFacilityKind { None, Office, FamilyMansion, Warehouse }

/// <summary>Fixed non-production civic facility; its placeholder appearance is runtime-only and carries no collision or save state.</summary>
public sealed class VillageFacility : MonoBehaviour
{
    public string Id;
    public VillageFacilityKind Kind;
    public Vector2 Entrance => transform.position;
    public string DisplayName;

    private GameObject foundation, house;
    private TextMeshPro label;
    private SpriteRenderer facade, roof, door, leftWindow, rightWindow;
    private float nextTextRefresh;

    /// <summary>Applies the definition in place so repeated calls never duplicate the building or label.</summary>
    public void Initialize(VillageFacilityDefinition definition)
    {
        if (definition == null) throw new System.ArgumentNullException(nameof(definition));
        Id = definition.id ?? string.Empty;
        Kind = definition.kind;
        DisplayName = definition.name == null ? string.Empty : definition.name.Resolve();
        EnsureVisuals();
        ApplyVisuals(definition.width, definition.height, definition.color, DisplayName);
        nextTextRefresh = Time.unscaledTime + .5f;
    }

    private void EnsureVisuals()
    {
        if (foundation == null)
            foundation = VillageVisuals.Rectangle(transform, "FacilityFootprint", new Vector2(0, .1f), new Vector2(1, .2f), new Color(.8f, .65f, .3f), 1).gameObject;
        if (house == null)
        {
            house = new GameObject("FacilityBuilding"); house.transform.SetParent(transform, false);
            facade = VillageVisuals.Rectangle(house.transform, "Facade", new Vector2(0, 1.5f), new Vector2(4.8f, 2), Color.gray, -4);
            roof = VillageVisuals.Rectangle(house.transform, "Roof", new Vector2(0, 2.6f), new Vector2(6, .9f), new Color(.25f, .19f, .18f), -3);
            door = VillageVisuals.Rectangle(house.transform, "Door", new Vector2(0, .7f), new Vector2(1.1f, 1.2f), new Color(.13f, .14f, .17f), -2);
            leftWindow = VillageVisuals.Rectangle(house.transform, "WindowLeft", new Vector2(-1.5f, 1.6f), new Vector2(.7f, .7f), new Color(.85f, .75f, .45f), -2);
            rightWindow = VillageVisuals.Rectangle(house.transform, "WindowRight", new Vector2(1.5f, 1.6f), new Vector2(.7f, .7f), new Color(.85f, .75f, .45f), -2);
        }
        if (label == null)
        {
            label = VillageVisuals.Label(transform, "FacilityName", new Vector2(0, 3.6f), 2, .8f);
            label.enableWordWrapping = false; label.alignment = TextAlignmentOptions.Center;
        }
    }

    private void ApplyVisuals(float width, float height, Color color, string displayName)
    {
        width = Mathf.Max(0f, width); height = Mathf.Max(0f, height);
        float foundationHeight = Mathf.Min(.2f, height * .1f);
        foundation.transform.localPosition = new Vector2(0, foundationHeight * .5f);
        foundation.transform.localScale = new Vector2(width, foundationHeight);
        facade.transform.localPosition = new Vector2(0, height * .36f);
        facade.transform.localScale = new Vector2(Mathf.Max(0f, width - 1.2f), height * .52f); facade.color = color;
        roof.transform.localPosition = new Vector2(0, height * .66f);
        roof.transform.localScale = new Vector2(width, height * .22f);
        door.transform.localPosition = new Vector2(0, height * .18f);
        door.transform.localScale = new Vector2(Mathf.Min(1.1f, width * .2f), height * .3f);
        float windowWidth = Mathf.Min(.7f, width * .14f), windowHeight = Mathf.Min(.7f, height * .2f);
        leftWindow.transform.localPosition = new Vector2(width * -.25f, height * .4f); leftWindow.transform.localScale = new Vector2(windowWidth, windowHeight);
        rightWindow.transform.localPosition = new Vector2(width * .25f, height * .4f); rightWindow.transform.localScale = new Vector2(windowWidth, windowHeight);
        float labelHeight = height * .22f;
        label.transform.localPosition = new Vector2(0, height - labelHeight * .5f);
        label.rectTransform.sizeDelta = new Vector2(width, labelHeight);
        label.fontSize = Mathf.Clamp(height * .5f, 1.5f, 4f); label.text = displayName ?? string.Empty;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextTextRefresh) return;
        nextTextRefresh = Time.unscaledTime + .5f;
        RefreshLocalizedText();
    }

    private void OnEnable() { nextTextRefresh = 0f; }

    private void RefreshLocalizedText()
    {
        if (string.IsNullOrEmpty(Id)) return;
        VillageFacilityDefinition definition;
        try { definition = VillageFacilitySettings.Load().Get(Id); }
        catch (System.InvalidOperationException) { return; }
        if (definition == null || definition.name == null) return;
        string resolved = definition.name.Resolve();
        if (string.IsNullOrEmpty(resolved) || resolved == DisplayName) return;
        DisplayName = resolved;
        if (label != null) label.text = resolved;
    }
}
