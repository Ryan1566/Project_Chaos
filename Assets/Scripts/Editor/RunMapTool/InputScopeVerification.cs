#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Binding migrations and conflict rules, without opening a save or touching live settings.</summary>
public static class InputScopeVerification
{
    public static string Verify()
    {
        var lines = new List<string>(); int failed = 0;
        Action<bool, string> check = (ok, name) => { lines.Add((ok ? "PASS " : "FAIL ") + name); if (!ok) failed++; };
        var asset = UnityEngine.Object.Instantiate(Resources.Load<InputActionAsset>("Input/ChaosInputActions"));
        try
        {
            var map = asset.FindActionMap(InputManager.MapName);
            var ids = new HashSet<Guid>();
            foreach (var action in map.actions)
            {
                check(ids.Add(action.id), "unique action " + action.name);
                foreach (var binding in action.bindings) check(ids.Add(binding.id), "unique binding " + action.name + "/" + binding.name);
            }
            foreach (var pair in new[] { new[] {"Move", "VillageMove"}, new[] {"Jump", "VillageJump"},
                new[] {"Interact", "VillageInteract"}, new[] {"Map", "VillageMap"} })
            {
                var a = map.FindAction(pair[0]); var b = map.FindAction(pair[1]);
                check(a.bindings.Count == b.bindings.Count, "matching migration shape " + pair[0]);
                for (int i = 0; i < a.bindings.Count; i++)
                    check(a.bindings[i].path == b.bindings[i].path && a.bindings[i].groups == b.bindings[i].groups,
                        "matching defaults " + pair[0] + "/" + i);
                check(!InputManager.ScopesOverlap(pair[0], pair[1]), "disjoint gameplay " + pair[0]);
                check(InputManager.ScopesOverlap("Menu", pair[1]), "global overlap " + pair[1]);
            }
            foreach (string name in new[] { "Jump", "VillageJump" })
            {
                int count = 0;
                foreach (var binding in map.FindAction(name).bindings)
                    if (binding.groups == "Keyboard") { count++; check(binding.path == "<Keyboard>/space", name + " space default"); }
                check(count == 1, name + " single keyboard binding");
            }

            // An isolated manager object with an owned asset avoids constructor listeners and real settings.
            var manager = (InputManager)FormatterServices.GetUninitializedObject(typeof(InputManager));
            typeof(InputManager).GetField("_asset", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, asset);
            var run = map.FindAction("Map"); var village = map.FindAction("VillageMap");
            run.ApplyBindingOverride(0, "<Keyboard>/q");
            string legacy = asset.SaveBindingOverridesAsJson();
            manager.LoadOverridesJson(legacy);
            check(village.bindings[0].effectivePath == "<Keyboard>/q", "legacy map override carried outside");
            village.RemoveAllBindingOverrides();
            string scoped = manager.SaveOverridesJson();
            check(scoped.Contains("\"chaosScopeVersion\":1"), "scope version persisted");
            manager.LoadOverridesJson(scoped);
            check(run.bindings[0].effectivePath == "<Keyboard>/q", "run override roundtrip");
            check(village.bindings[0].effectivePath == "<Keyboard>/m", "outside defaults remain independent on reload");

            village.ApplyBindingOverride(0, "<Keyboard>/q");
            typeof(InputManager).GetMethod("ResolveConflict", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { run, 0, "<Keyboard>/m" });
            check(village.bindings[0].effectivePath == "<Keyboard>/q", "same button in disjoint scopes retained");
            var interact = map.FindAction("Interact");
            interact.ApplyBindingOverride(0, "<Keyboard>/q");
            typeof(InputManager).GetMethod("ResolveConflict", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { run, 0, "<Keyboard>/m" });
            check(interact.bindings[0].effectivePath == "<Keyboard>/m", "same scope conflict swaps old button");
            var menu = map.FindAction("Menu");
            menu.ApplyBindingOverride(0, "<Keyboard>/q");
            typeof(InputManager).GetMethod("ResolveConflict", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { run, 0, "<Keyboard>/m" });
            check(menu.bindings[0].effectivePath == "<Keyboard>/m", "global conflict resolved");
            manager.LoadOverridesJson("");
            check(run.bindings[0].effectivePath == "<Keyboard>/m" && village.bindings[0].effectivePath == "<Keyboard>/m", "reset both scopes");
        }
        finally { UnityEngine.Object.DestroyImmediate(asset); }
        lines.Add("FAILURES " + failed);
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "QA/RunMapUI"));
        File.WriteAllLines(Path.Combine(Application.dataPath, "QA/RunMapUI/input-scopes.txt"), lines);
        return string.Join("\n", lines);
    }
}
#endif
