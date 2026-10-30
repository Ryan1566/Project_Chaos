using TMPro;
using UnityEngine;

public sealed class VillagePlot : MonoBehaviour
{
    public uint BuildingId;
    public Vector2 Entrance => transform.position;
    private GameObject house, foundation;
    private TextMeshPro label;
    private float nextRefresh;
    private void Start()
    {
        foundation = VillageVisuals.Rectangle(transform, "ConstructionFootprint", new Vector2(0, .1f), new Vector2(9, .2f), new Color(.8f, .65f, .3f), 1).gameObject;
        house = new GameObject("CompletedBuilding"); house.transform.SetParent(transform, false);
        Color color = BuildingId == 2001 ? new Color(.6f, .65f, .35f) : BuildingId == 2002 ? new Color(.55f, .48f, .4f)
            : BuildingId == 2003 ? new Color(.65f, .58f, .42f) : new Color(.62f, .43f, .35f);
        VillageVisuals.Rectangle(house.transform, "Facade", new Vector2(0, 2.3f), new Vector2(8, 4.6f), color, -4);
        VillageVisuals.Rectangle(house.transform, "Roof", new Vector2(0, 4.9f), new Vector2(9, .8f), new Color(.25f, .19f, .18f), -3);
        VillageVisuals.Rectangle(house.transform, "Door", new Vector2(0, 1.05f), new Vector2(1.3f, 2.1f), new Color(.13f, .14f, .17f), -2);
        for (int i = -1; i <= 1; i += 2) VillageVisuals.Rectangle(house.transform, "Window", new Vector2(i * 2.5f, 2.7f), new Vector2(1, 1), new Color(.85f, .75f, .45f), -2);
        label = VillageVisuals.Label(transform, "BuildingStatus", new Vector2(0, 6.1f), 14, 2);
        Refresh();
    }
    private void Update() { if (Time.unscaledTime >= nextRefresh) { nextRefresh = Time.unscaledTime + .25f; Refresh(); } }
    private void Refresh()
    {
        if (house == null) return;
        var save = VillageService.Instance.Find(BuildingId); bool built = save != null && save.completed;
        house.SetActive(built); foundation.SetActive(!built);
        string state = save == null ? VillageText.Get("PressToBuild", VillageSceneBootstrap.BindingText(InputManager.ActionVillageInteract)) : !built ? VillageText.Get("Queue", save.remainSeconds)
            : VillageText.Get("Level", save.level) + " · " + (VillageWorkforce.ActiveWorkers(BuildingId) > 0 ? VillageText.Get("Working") : VillageText.Get("Idle"))
                + "\n" + VillageText.Get("Production", save.pendingOutput);
        label.text = VillageText.Get("Building" + BuildingId) + "\n" + state;
    }
    private void OnDrawGizmos()
    { Gizmos.color = Color.yellow; Gizmos.DrawWireCube(transform.position + Vector3.up * 2.5f, new Vector3(8, 5, .1f)); }
}
