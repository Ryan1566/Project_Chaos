using RunMap;
using UnityEngine;

namespace Chaos.Gameplay.Interaction
{
    public static class RunContentWorldFactory
    {
        public static GameObject Create(NaturalMapPlan plan, MapPlacement placement, Transform parent)
        {
            var catalog = RunContentCatalog.Load();
            string id = placement.Kind == MapPlacementKind.Npc ? "traveller" : placement.Kind == MapPlacementKind.Interactive ? "chest"
                : placement.StableId.EndsWith("/0", System.StringComparison.Ordinal) ? "herbs" : "ore";
            var definition = catalog.Get(id);
            if (definition == null) throw new System.InvalidOperationException("局内交互内容未配置：" + id);
            var go = new GameObject(placement.StableId.Replace('/', '_')); go.transform.SetParent(parent, false);
            go.transform.localPosition = placement.Position + Vector2.up * (placement.Kind == MapPlacementKind.Npc ? .8f : .45f);
            var trigger = go.AddComponent<BoxCollider2D>(); trigger.isTrigger = true;
            trigger.size = placement.Kind == MapPlacementKind.Npc ? new Vector2(.7f, 1.6f) : new Vector2(.85f, .75f);
            var content = go.AddComponent<RunContentInteractable>(); content.ConfigureContent(placement.StableId, definition, plan);
            go.AddComponent<RunContentPresentation>().Initialize(content);
            return go;
        }
    }
}
