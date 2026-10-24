using UnityEngine;

/// <summary>Shared actor geometry and terrain-only melee visibility tests.</summary>
public static class CombatQuery
{
    public static Collider2D Body(Transform actor)
    {
        if (actor == null) return null;
        Collider2D direct = actor.GetComponent<Collider2D>();
        if (direct != null && direct.enabled && !direct.isTrigger) return direct;
        Collider2D[] children = actor.GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < children.Length; i++)
            if (children[i].enabled && !children[i].isTrigger) return children[i];
        return null;
    }

    public static Vector2 Center(Transform actor)
    {
        Collider2D body = Body(actor);
        return body != null ? (Vector2)body.bounds.center : (Vector2)actor.position;
    }

    public static Vector2 Feet(Transform actor)
    {
        Collider2D body = Body(actor);
        return body != null ? new Vector2(body.bounds.center.x, body.bounds.min.y) : (Vector2)actor.position;
    }

    public static bool IsStandingTarget(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;
        AllyController ally = target.GetComponent<AllyController>();
        if (ally != null) return !ally.IsDowned && ally.Hp > 0;
        PlayerHealth player = target.GetComponent<PlayerHealth>();
        if (player != null) return !player.IsDowned;
        EnemyPlaceholder enemy = target.GetComponent<EnemyPlaceholder>();
        return enemy != null && !enemy.IsDead && enemy.Hp > 0;
    }

    public static bool HasLineOfSight(Transform source, Transform target)
    {
        if (source == null || target == null) return false;
        RaycastHit2D[] hits = Physics2D.LinecastAll(Center(source), Center(target));
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i].collider;
            if (!RunMap.RunMapPhysics.IsTerrain(hit)) continue;
            if (hit.transform == source || hit.transform.IsChildOf(source)
                || hit.transform == target || hit.transform.IsChildOf(target)) continue;
            // Other static damageable props remain obstacles; only this swing's own target is ignored.
            return false;
        }
        return true;
    }

    public static bool CanMelee(Transform source, Transform target, float range, float height)
    {
        if (source == null || !IsStandingTarget(target)) return false;
        Vector2 delta = Center(target) - Center(source);
        return delta.sqrMagnitude <= range * range
            && Mathf.Abs(Feet(target).y - Feet(source).y) <= height
            && HasLineOfSight(source, target);
    }
}
