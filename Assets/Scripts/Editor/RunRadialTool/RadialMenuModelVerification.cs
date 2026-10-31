#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Chaos.Gameplay.Items;
using Chaos.UI.RadialMenu;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Explicit editor checks: geometry, state transitions, health composition and authored bindings.</summary>
public static class RadialMenuModelVerification
{
    public static string Run()
    {
        int checks = 0;
        Action<bool, string> check = (ok, label) => { if (!ok) throw new Exception(label); checks++; };
        var model = new RadialMenuModel(32, 250, .2f);
        for (int count = 1; count <= 12; count++)
        {
            var options = Enumerable.Range(0, count).Select(i => new RadialMenuOption { Id = i.ToString(), ShortcutNumber = i + 1 }).ToArray();
            for (int i = 0; i < count; i++)
            {
                model.Open(options); check(model.SelectedIndex == -1, "default neutral");
                double angle = i * 2 * Math.PI / count;
                model.MovePointer((float)Math.Sin(angle) * 150, (float)Math.Cos(angle) * 150);
                check(model.SelectedIndex == i, "clockwise up sector " + count + "/" + i);
            }
        }
        var leaf = new RadialMenuOption { Id = "use", ShortcutNumber = 2 };
        var roots = new[] { new RadialMenuOption { Id = "parent", Children = new[] { leaf } }, new RadialMenuOption { Id = "disabled", Enabled = false, DisabledReason = "blocked" } };
        model.Open(roots); roots[0].Name = "changed after snapshot";
        check(model.RootOptions[0].Name == null, "source snapshots isolated");
        model.MovePointer(0, 31); check(model.SelectedIndex == -1, "deadzone");
        model.MovePointer(0, 100); check(!model.Tick(.1f) && model.Tick(.1f) && model.Level == 2 && model.SelectedIndex == -1, "hover expands and resets");
        check(model.SelectShortcut(2), "slot shortcut"); model.MovePointer(0, 0);
        check(model.SelectedIndex == 0 && model.IsOpen, "shortcut selects without executing");
        check(model.TryConfirm(out var confirmed, out _) && confirmed.Id == "use" && !model.IsOpen, "left-confirm yields leaf once");
        check(!model.TryConfirm(out _, out _), "one-shot confirmation");
        model.Open(roots); model.MovePointer(0, -150);
        check(!model.TryConfirm(out _, out string reason) && reason == "blocked" && model.IsOpen, "disabled refuses use");
        model.MovePointer(float.NaN, 2); check(!float.IsNaN(model.PointerX), "nonfinite device input ignored");
        model.MovePointer(float.MaxValue, float.MaxValue);
        check(model.PointerX * model.PointerX + model.PointerY * model.PointerY <= 62501, "extreme delta clamped");
        model.Open(roots); model.MovePointer(0, 100); model.TryConfirm(out _, out _); model.Back();
        check(model.Level == 1 && model.SelectedIndex == -1, "back resets parent"); model.Close();
        bool invalid = false; try { new RadialMenuModel(100, 50, .2f); } catch (ArgumentOutOfRangeException) { invalid = true; }
        check(invalid, "invalid geometry rejected");

        var obj = new GameObject("RadialVerification", typeof(RectTransform));
        try
        {
            var wedge = obj.AddComponent<RadialWedgeGraphic>(); check(wedge.GetComponent<CanvasRenderer>() != null, "custom wedge owns CanvasRenderer"); wedge.Configure(80, 180, 0, 117, Color.white);
            using (var helper = new VertexHelper())
            {
                typeof(RadialWedgeGraphic).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Invoke(wedge, new object[] { helper });
                var verts = new List<UIVertex>(); helper.GetUIVertexStream(verts);
                check(verts.Count > 0 && verts.All(v => v.position.magnitude >= 79.99f), "wedge has no center vertices");
                for (int i = 0; i < verts.Count; i += 3)
                    check(((verts[i].position + verts[i + 1].position + verts[i + 2].position) / 3).magnitude > 79, "triangles preserve center hole");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(obj); }
        obj = new GameObject("HealthVerification");
        try
        {
            var health = obj.AddComponent<PlayerHealth>();
            typeof(PlayerHealth).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(health, null);
            health.SetRunHealthBonus(10); health.SetItemHealthBonus(20);
            check(health.maxHp == 130 && health.Hp == 100, "growth and item bonus compose without healing");
            check(health.RestoreHealth(15) == 15 && health.Hp == 115, "partial heal");
            health.SetItemHealthBonus(0); check(health.maxHp == 110 && health.Hp == 110, "buff expiry clamps but preserves growth");
            check(health.RestoreHealth(10) == 0, "full hp no healing");
        }
        finally { UnityEngine.Object.DestroyImmediate(obj); }
        var data = SettingsData.CreateDefault(); check(data.itemWheelMode == 1, "default hold");
        data.itemWheelMode = 0; check(data.Clone().itemWheelMode == 0, "settings serialize selection");
        data.ResetSection(SettingCategory.Gameplay); check(data.itemWheelMode == 1, "gameplay reset");
        var asset = Resources.Load<InputActionAsset>("Input/ChaosInputActions");
        foreach (var pair in new[] { (InputManager.ActionInventory, "<Keyboard>/i"), (InputManager.ActionTeam, "<Keyboard>/t"), (InputManager.ActionItemWheel, "<Keyboard>/leftAlt") })
        {
            var a = asset.FindAction("Player/" + pair.Item1);
            check(a.bindings.Count(b => b.groups == "Keyboard") == 1 && a.bindings.First(b => b.groups == "Keyboard").path == pair.Item2, "single binding " + pair.Item1);
            check(InputManager.ScopeOf(pair.Item1) == InputManager.BindingScope.InRun, "run scope " + pair.Item1);
        }
        var prefab = Resources.Load<GameObject>("UIPanels/SettingPanel");
        var row = prefab.GetComponentsInChildren<SettingRow_Selector>(true).Single(r => r.settingId == SettingIds.ItemWheelMode);
        check(row.transform.Find("Options").childCount == 2, "authored exactly two trigger modes");
        check(ItemBase.Create(10).ParentCategory == ItemParentCategory.Engineering && ItemBase.Create(11).ParentCategory == ItemParentCategory.Important, "legacy run category mapping preserved");
        check(!ItemBase.Create(11).Consumes, "important cannot consume");
        return "PASS " + checks + " independent checks";
    }
}
#endif
