using RunMap;
using UnityEngine;

namespace Chaos.Gameplay.Interaction
{
    /// <summary>预留出口锚点上的无阻挡门，交互范围和输入沿用局内交互服务。</summary>
    public sealed class RunExitDoor : RunInteractable
    {
        private int stage;

        public override string DisplayName => "通往下一场景的门";
        public override string ActionLabel => $"进入第 {stage + 1} 场景";
        public override Vector2 InteractionPoint => (Vector2)transform.position + Vector2.up * .8f;

        public static void Create(MapPlacement placement, int sceneIndex, Transform parent)
        {
            var root = new GameObject("RunExitDoor");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = placement.Position;
            var door = root.AddComponent<RunExitDoor>();
            door.stage = sceneIndex;
            door.Configure(placement.StableId);

            var frame = new Color(.76f, .53f, .23f);
            var inner = new Color(.10f, .25f, .22f);
            AddPiece(root.transform, "Doorway", new Vector2(0, .95f), new Vector2(1.10f, 1.9f), inner, 5);
            AddPiece(root.transform, "LeftPost", new Vector2(-.56f, .94f), new Vector2(.17f, 1.88f), frame, 6);
            AddPiece(root.transform, "RightPost", new Vector2(.56f, .94f), new Vector2(.17f, 1.88f), frame, 6);
            AddPiece(root.transform, "Lintel", new Vector2(0, 1.91f), new Vector2(1.38f, .19f), frame, 6);
            AddPiece(root.transform, "OpenPanel", new Vector2(.36f, .91f), new Vector2(.30f, 1.76f), new Color(.44f, .27f, .12f), 6);
        }

        private static void AddPiece(Transform parent, string name, Vector2 position, Vector2 size, Color color, int order)
        {
            var piece = new GameObject(name, typeof(SpriteRenderer));
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = piece.GetComponent<SpriteRenderer>();
            renderer.sprite = MapPopulationPlaceholder.WhiteSprite;
            renderer.color = color;
            renderer.sortingOrder = order;
        }

        public override bool CanInteract(GameObject actor, out string reason)
        {
            bool allowed = RunInteractionService.CanControl(actor) && RunEncounter.CanUseExit(stage);
            reason = allowed ? null : "出口暂不可用";
            return allowed;
        }

        protected override bool Perform(GameObject actor, out string result)
        {
            bool queued = RunEncounter.TryUseExit(stage);
            result = queued ? ActionLabel : "出口暂不可用";
            return queued;
        }
    }
}
