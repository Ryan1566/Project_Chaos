using Spine.Unity;
using TMPro;
using UnityEngine;

public static class VillageVisuals
{
    public static SpriteRenderer Rectangle(Transform parent, string name, Vector2 position, Vector2 size, Color color, int order = 0)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = size;
        var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = RunMap.MapPopulationPlaceholder.WhiteSprite;
        renderer.color = color; renderer.sortingOrder = order; return renderer;
    }
    public static TextMeshPro Label(Transform parent, string name, Vector2 position, float width, float height)
    {
        // TMP's world renderer must run Awake before its font setter (actors are assembled inactive).
        var go = new GameObject(name);
        var label = go.AddComponent<TextMeshPro>(); label.font = RunTeamHud.ResolveFont(); label.fontSize = 3;
        label.color = new Color(.12f, .18f, .24f);
        go.transform.SetParent(parent, false); go.transform.localPosition = position;
        label.alignment = TextAlignmentOptions.Center; label.rectTransform.sizeDelta = new Vector2(width, height);
        label.GetComponent<MeshRenderer>().sortingOrder = 30; return label;
    }
    public static GameObject CreateActor(Transform parent, string name, Vector2 feet, out GameObject visual)
    {
        var prefab = Resources.Load<GameObject>("Player/Ally");
        var source = prefab == null ? null : prefab.GetComponentInChildren<SkeletonAnimation>(true);
        if (source == null) throw new System.InvalidOperationException("Missing Player/Ally appearance");
        var actor = new GameObject(name); actor.SetActive(false); actor.transform.SetParent(parent, false); actor.transform.position = feet;
        var body = actor.AddComponent<Rigidbody2D>(); body.gravityScale = 3; body.constraints = RigidbodyConstraints2D.FreezeRotation;
        var shape = actor.AddComponent<CapsuleCollider2D>(); shape.size = new Vector2(.7f, 1.7f); shape.offset = Vector2.up * .85f;
        visual = Object.Instantiate(source.gameObject, actor.transform, false);
        visual.transform.localPosition = source.transform.localPosition;
        return actor;
    }
}
