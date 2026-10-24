using UnityEngine;

namespace RunMap
{
    /// <summary>Shared physical queries: actors and triggers are never mistaken for terrain.</summary>
    public static class RunMapPhysics
    {
        public static bool IsTerrain(Collider2D collider)
        {
            return collider != null && collider.enabled && !collider.isTrigger
                && collider.GetComponentInParent<EnemyPlaceholder>() == null
                && collider.GetComponentInParent<EnemyDummy>() == null
                && collider.GetComponentInParent<PlayerHealth>() == null
                && collider.GetComponentInParent<AllyController>() == null
                && (collider.attachedRigidbody == null || collider.attachedRigidbody.bodyType == RigidbodyType2D.Static);
        }
        public static Vector2 Feet(Collider2D collider)
        { return new Vector2(collider.bounds.center.x, collider.bounds.min.y); }
        public static bool IsGrounded(Collider2D collider)
        {
            if (collider == null) return false;
            var b = collider.bounds;
            foreach (var hit in Physics2D.BoxCastAll(new Vector2(b.center.x, b.min.y + .06f),
                new Vector2(Mathf.Max(.1f, b.size.x - .12f), .04f), 0, Vector2.down, .13f))
                if (IsTerrain(hit.collider) && hit.collider != collider && hit.normal.y > .5f) return true;
            return false;
        }
        public static bool HasLineOfSight(Vector2 from, Vector2 to)
        {
            foreach (var hit in Physics2D.LinecastAll(from, to)) if (IsTerrain(hit.collider)) return false;
            return true;
        }
        public static bool CanOccupy(Vector2 feet, Vector2 size)
        {
            var center = feet + Vector2.up * (size.y * .5f + .06f);
            foreach (var c in Physics2D.OverlapBoxAll(center, size - new Vector2(.06f, .06f), 0))
                if (IsTerrain(c)) return false;
            foreach (var hit in Physics2D.RaycastAll(feet + Vector2.up * .12f, Vector2.down, .25f))
                if (IsTerrain(hit.collider) && hit.normal.y > .5f) return true;
            return false;
        }
    }
}
