#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Chaos.Gameplay.Items;
using Chaos.UI.RadialMenu;
using UnityEngine;

/// <summary>
/// 本次修复的最小隔离检查：地面法线/侧墙判定、运行时低摩擦材质的恢复与释放、
/// 轮盘二级稀疏槽的地址/快捷键及空分类文案。只使用内存沙盒，不读写存档。
/// 不自动启动 Unity；由编辑器测试入口或 Codex 显式调用 Run()。
/// </summary>
public static class GameplayPolishPhysicsVerification
{
    public static string Run()
    {
        var passed = new List<string>();
        VerifyGroundContract(passed);
        VerifySlideMaterialLifecycle(passed);
        VerifySparseRadialSlots(passed);
        return string.Join("\n", passed) + "\nPASS " + passed.Count;
    }

    private static void Check(bool valid, string label, List<string> passed)
    {
        if (!valid) throw new InvalidOperationException(label);
        passed.Add("PASS " + label);
    }

    private static void VerifyGroundContract(List<string> passed)
    {
        Check(PlayerSurfacePhysics.IsGroundNormal(Vector2.up), "flat floor is walkable", passed);
        Check(PlayerSurfacePhysics.IsGroundNormal(new Vector2(0.7071068f, 0.7071068f)), "45-degree slope is walkable", passed);
        Check(!PlayerSurfacePhysics.IsGroundNormal(new Vector2(1f, 0f)), "vertical wall is not ground", passed);
        Check(!PlayerSurfacePhysics.IsGroundNormal(new Vector2(0.9f, 0.1f)), "steep wall is not ground", passed);
        Check(PlayerSurfacePhysics.IsSideContactNormal(new Vector2(1f, 0f)), "vertical wall is a side contact", passed);
        Check(!PlayerSurfacePhysics.IsSideContactNormal(Vector2.up), "floor is not a side contact", passed);
        Check(!PlayerSurfacePhysics.IsSideContactNormal(new Vector2(0.7071068f, 0.7071068f)), "45-degree slope is not a side contact", passed);

        var root = new GameObject("GameplayPolishGroundVerification");
        try
        {
            var floor = new GameObject("Floor");
            floor.transform.SetParent(root.transform);
            floor.transform.position = new Vector3(0f, -0.1f, 0f);
            var floorCollider = floor.AddComponent<BoxCollider2D>();
            floorCollider.size = new Vector2(10f, 0.2f);

            var player = new GameObject("Probe");
            player.transform.SetParent(root.transform);
            var capsule = player.AddComponent<CapsuleCollider2D>();
            capsule.size = new Vector2(0.7f, 1.7f);
            capsule.offset = new Vector2(0f, 0.85f);
            Physics2D.SyncTransforms();

            Check(PlayerSurfacePhysics.ProbeGround(capsule, null), "floor below feet probes grounded", passed);
            player.transform.position = new Vector3(0f, 5f, 0f);
            Physics2D.SyncTransforms();
            Check(!PlayerSurfacePhysics.ProbeGround(capsule, null), "airborne probe stays ungrounded", passed);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void VerifySlideMaterialLifecycle(List<string> passed)
    {
        var original = new PhysicsMaterial2D("VerificationOriginal") { friction = 0.7f, bounciness = 0f, hideFlags = HideFlags.DontSave };
        var player = new GameObject("GameplayPolishMaterialVerification");
        try
        {
            var body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.simulated = false;
            var collider = player.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.7f, 1.7f);
            collider.offset = new Vector2(0f, 0.85f);
            collider.sharedMaterial = original;
            var surface = player.AddComponent<PlayerSurfacePhysics>();
            FieldInfo slideField = typeof(PlayerSurfacePhysics).GetField("_slideMaterial", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(slideField != null, "runtime slide material field is inspectable", passed);

            surface.SetSlideMaterial(true);
            Check(surface.IsSlideMaterialActive && collider.sharedMaterial != null
                && collider.sharedMaterial != original && collider.sharedMaterial.friction == 0f,
                "airborne side contact installs own zero-friction material", passed);
            Check(slideField.GetValue(surface) != null, "runtime material exists while active", passed);

            surface.SetSlideMaterial(false);
            Check(!surface.IsSlideMaterialActive && collider.sharedMaterial == original && body.sharedMaterial == null,
                "leaving the wall restores original collider/body materials", passed);
            Check(slideField.GetValue(surface) == null, "runtime material is released after restore", passed);

            surface.SetSlideMaterial(true);
            typeof(PlayerSurfacePhysics).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(surface, null);
            Check(!surface.IsSlideMaterialActive && collider.sharedMaterial == original,
                "disable restores original material", passed);
            Check(slideField.GetValue(surface) == null, "disable releases runtime material", passed);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(player);
            UnityEngine.Object.DestroyImmediate(original);
        }
    }

    private static void VerifySparseRadialSlots(List<string> passed)
    {
        if (GameSession.Instance.HasSession)
            throw new InvalidOperationException("Run the radial check without an active session; it uses a disposable sandbox.");
        GameSession.Instance.BeginSandbox(new SavePayload
        {
            runImportantSlots = 0,
            runEngineeringSlots = 2,
            runRecoverySlots = 5,
            runMiscSlots = 3
        });
        try
        {
            RunInventory.BeginRun();
            string reason;
            // 前三件先占满杂物行 0..2，第四件恢复道具才会真实落入恢复行 0 号物理槽。
            // 再把杂物行里的恢复道具拖到 4 号槽，制造 0/4 两个真实占用槽和中间空槽。
            Check(RunInventory.AddItem(1, 4, out reason), "stage recovery item 1 in misc slot 0", passed);
            Check(RunInventory.AddItem(5, 2, out reason), "stage recovery item 5 in misc slot 1", passed);
            Check(RunInventory.AddItem(10, 3, out reason), "stage engineering item 10 in misc slot 2", passed);
            Check(RunInventory.AddItem(6, 1, out reason), "spill recovery item 6 into recovery slot 0", passed);
            Check(RunInventory.GetSlot(RunBagSection.Misc, 0)?.itemId == 1u
                && RunInventory.GetSlot(RunBagSection.Misc, 1)?.itemId == 5u
                && RunInventory.GetSlot(RunBagSection.Misc, 2)?.itemId == 10u,
                "granted stacks occupy the staged misc slots", passed);
            Check(RunInventory.GetSlot(RunBagSection.Recovery, 0)?.itemId == 6u,
                "filling misc places the next recovery stack in physical slot 0", passed);
            Check(RunInventory.TryMove(RunBagSection.Misc, 0, RunBagSection.Recovery, 4, out reason),
                "move a recovery stack from misc into physical slot 4", passed);
            Check(RunInventory.GetSlot(RunBagSection.Recovery, 0)?.itemId == 6u
                && RunInventory.GetSlot(RunBagSection.Recovery, 4)?.itemId == 1u,
                "recovery row really owns physical slots 0 and 4", passed);

            var roots = new InventoryRadialProvider().BuildOptions();
            Check(roots.Count == 3, "only the three special rows are exposed", passed);
            Check(roots[0].Children == null || roots[0].Children.Count == 0, "unopened important row has no second level", passed);
            Check(!roots[0].Enabled && roots[0].DisabledReason == "未解锁", "unopened important row reports 未解锁", passed);
            Check(roots[1].Children == null || roots[1].Children.Count == 0, "empty engineering row has no second level", passed);
            Check(!roots[1].Enabled && roots[1].DisabledReason == "暂无道具", "empty engineering row reports 暂无道具", passed);
            Check(roots[2].Enabled, "recovery row with stacks is enabled", passed);
            Check(roots[2].Children != null && roots[2].Children.Count == 2, "recovery row hides empty slots", passed);
            Check(roots[2].Children[0].Id == "2:0" && roots[2].Children[0].ShortcutNumber == 1
                && roots[2].Children[0].ShortcutText == "1", "physical slot 0 keeps address and shortcut 1", passed);
            Check(roots[2].Children[1].Id == "2:4" && roots[2].Children[1].ShortcutNumber == 5
                && roots[2].Children[1].ShortcutText == "5", "physical slot 4 keeps address and shortcut 5", passed);
            Check(!roots[2].Children[0].Enabled && !string.IsNullOrEmpty(roots[2].Children[0].DisabledReason),
                "unavailable item remains visible with a disabled reason", passed);
            Check(roots[2].Children[0].CountText != "×0", "zero counts are never exposed", passed);
        }
        finally
        {
            RunInventory.Clear();
            GameSession.Instance.Clear();
        }
    }
}
#endif
