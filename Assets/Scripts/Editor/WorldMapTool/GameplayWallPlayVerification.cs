#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEngine;

/// <summary>Actual fixed-step movement checks in a remote, temporary fixture; never changes the map.</summary>
public static class GameplayWallPlayVerification
{
    public static IEnumerator Run(Action<bool, string> check)
    {
        var root = new GameObject("ChaosPolishTemporaryPhysics");
        var original = new PhysicsMaterial2D("ChaosPolishOriginal") { friction = .8f };
        Vector3 offset = new Vector3(10000, 10000);
        try
        {
            var wall = new GameObject("Wall"); wall.transform.SetParent(root.transform);
            wall.transform.position = offset + new Vector3(1.2f, 5);
            var wc = wall.AddComponent<BoxCollider2D>(); wc.size = new Vector2(1, 20); wc.sharedMaterial = original;
            var floor = new GameObject("Floor"); floor.transform.SetParent(root.transform);
            floor.transform.position = offset + new Vector3(0, -.1f);
            var fc = floor.AddComponent<BoxCollider2D>(); fc.size = new Vector2(10, .2f); fc.sharedMaterial = original;
            var actor = new GameObject("ControlledProbe"); actor.transform.SetParent(root.transform);
            actor.transform.position = offset + new Vector3(.34f, 7);
            var body = actor.AddComponent<Rigidbody2D>(); body.gravityScale = 3; body.freezeRotation = true;
            var col = actor.AddComponent<CapsuleCollider2D>(); col.size = new Vector2(.7f, 1.7f); col.offset = Vector2.up * .85f; col.sharedMaterial = original;
            var locomotion = actor.AddComponent<PlayerLocomotion>();
            float startY = body.position.y; bool sliding = false;
            for (int i = 0; i < 30; i++)
            {
                locomotion.InjectInput(1, false, 1); yield return new WaitForFixedUpdate();
                sliding |= actor.GetComponent<PlayerSurfacePhysics>().IsSlideMaterialActive;
            }
            check(sliding && body.position.y < startY - 2 && body.velocity.y < -4, "holding into high-friction wall falls instead of hanging");
            check(!locomotion.IsGrounded, "side wall is not grounded");
            float beforeJump = body.velocity.y; locomotion.InjectInput(1, true, .5f); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            check(body.velocity.y < beforeJump, "pressing jump on wall cannot wall-jump");

            body.position = (Vector2)offset + new Vector2(-2, .02f); body.velocity = Vector2.zero;
            locomotion.InjectInput(0, false, 2); Physics2D.SyncTransforms();
            for (int i = 0; i < 10; i++) yield return new WaitForFixedUpdate();
            check(locomotion.IsGrounded && col.sharedMaterial == original, "landing restores original material and ground detection");
            locomotion.InjectInput(0, true, 1); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            check(body.velocity.y > 8 && !actor.GetComponent<PlayerSurfacePhysics>().IsSlideMaterialActive, "flat-ground jump retains original impulse");

            var slope = new GameObject("Slope"); slope.transform.SetParent(root.transform);
            slope.transform.position = offset + new Vector3(-15, 0); slope.transform.rotation = Quaternion.Euler(0, 0, 30);
            var sc = slope.AddComponent<BoxCollider2D>(); sc.size = new Vector2(8, .4f); sc.sharedMaterial = original;
            body.position = (Vector2)offset + new Vector2(-15, .25f); body.velocity = Vector2.zero;
            locomotion.InjectInput(0, false, 2); Physics2D.SyncTransforms();
            for (int i = 0; i < 15; i++) yield return new WaitForFixedUpdate();
            check(locomotion.IsGrounded, "normal 30-degree slope remains grounded");
            locomotion.InjectInput(0, true, 1); yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            check(body.velocity.y > 8, "normal slope jump retains original impulse");
            var surface = actor.GetComponent<PlayerSurfacePhysics>(); surface.SetSlideMaterial(true);
            locomotion.enabled = false;
            check(col.sharedMaterial == original && !surface.IsSlideMaterialActive, "disabling movement restores owned materials");
        }
        finally { UnityEngine.Object.Destroy(root); UnityEngine.Object.Destroy(original); }
    }
}
#endif
