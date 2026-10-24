using UnityEngine;

namespace RunMap
{
    /// <summary>Proves trigger lifecycle; interaction gameplay intentionally belongs to future content systems.</summary>
    public sealed class MapPopulationPlaceholder : MonoBehaviour
    {
        public string StableId;
        public MapPlacementKind Kind;
        public int EnterCount { get; private set; }
        private void Start()
        {
            if (Kind != MapPlacementKind.Npc && Kind != MapPlacementKind.Interactive) return;
            var inspectable = gameObject.AddComponent<Chaos.Gameplay.Interaction.MapInspectable>();
            inspectable.Kind = Kind; inspectable.Configure(StableId);
        }
        private static Sprite whiteSprite;
        public static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite == null) whiteSprite = Sprite.Create(Texture2D.whiteTexture,
                    new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(.5f, .5f), Texture2D.whiteTexture.width);
                return whiteSprite;
            }
        }
        private void OnTriggerEnter2D(Collider2D other)
        { if (other.GetComponentInParent<PlayerLocomotion>() != null) EnterCount++; }
    }
}
