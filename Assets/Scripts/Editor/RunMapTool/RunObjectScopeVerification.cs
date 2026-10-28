using System;
using System.Collections.Generic;
using System.Reflection;
using RunMap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Ownership contracts only. No player saves, configs, map generation or existing scene edits.</summary>
public static class RunObjectScopeVerification
{
    [MenuItem("Tools/局内地图/验证出征对象归属")]
    public static void VerifyMenu() { Debug.Log(Verify()); }

    public static string Verify()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Run ownership contracts in Edit mode");
        if (RunMapBootstrap.CurrentMapRoot != null || NaturalMapRuntime.Current != null)
            throw new InvalidOperationException("Run contracts without an active map preview");
        var passed = new List<string>();
        Scene original = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        RunObjectScope first = null, second = null;
        FieldInfo containerField = typeof(PrototypeMapSpawner).GetField("_currentContainer", BindingFlags.Static | BindingFlags.NonPublic);
        FieldInfo mapField = typeof(RunMapBootstrap).GetField("<CurrentMapRoot>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
        object previousContainer = containerField.GetValue(null), previousMap = mapField.GetValue(null);
        try
        {
            SceneManager.SetActiveScene(scene);
            var foreign = new GameObject("PrototypePlayerRoot"); foreign.SetActive(false);
            foreign.AddComponent<PlayerLocomotion>();
            var foreignAlly = new GameObject("ForeignAlly"); foreignAlly.SetActive(false); foreignAlly.AddComponent<AllyController>();
            var foreignEnemy = new GameObject("ForeignEnemy"); foreignEnemy.SetActive(false); foreignEnemy.AddComponent<EnemyPlaceholder>();
            var foreignSpawn = new GameObject("PrototypeSpawnRoot"); foreignSpawn.SetActive(false); foreignSpawn.AddComponent<SpawnPoint>();
            var foreignDrop = new GameObject("PrototypeDropRoot");
            var foreignBoundary = new GameObject(RunBounds.RootName);

            first = new RunObjectScope(); second = new RunObjectScope();
            Check(first.Token != second.Token, "each stage has a distinct token", passed);
            GameObject firstRoot = first.GetOrCreateRoot(PlayerSpawner.PlayerRootName);
            GameObject detached = first.Register(new GameObject("DetachedOwnedPlayer"));
            var inactive = first.Register(new GameObject("InactiveOwnedActor")); inactive.SetActive(false);
            Check(!first.Owns(foreign) && !first.Owns(foreignAlly) && !first.Owns(foreignEnemy), "type and same name do not grant ownership", passed);
            bool rejected = false;
            try { second.Register(detached); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "an object cannot transfer into a newer stage", passed);

            GameObject nextRoot = second.GetOrCreateRoot(PlayerSpawner.PlayerRootName);
            nextRoot.transform.SetParent(firstRoot.transform, true);
            first.Dispose(); first.Dispose();
            Check(firstRoot == null && detached == null && inactive == null, "owned roots, detached and inactive actors are destroyed", passed);
            Check(foreign != null && foreignAlly != null && foreignEnemy != null && foreignSpawn != null
                && foreignDrop != null && foreignBoundary != null, "foreign shared controllers and same-name roots survive", passed);
            Check(nextRoot != null && nextRoot.transform.parent == null && second.Owns(nextRoot), "late old disposal preserves a nested newer scope", passed);

            GameObject replacement = second.GetOrCreateRoot("Replacement");
            second.DestroyOwned(nextRoot);
            Check(replacement != null && !second.Owns(nextRoot), "targeted owned-root removal leaves its siblings intact", passed);

            // Simulate the loader publishing B before a delayed clear of prototype container A.
            var oldMapScope = new RunObjectScope();
            try
            {
                GameObject oldContainer = oldMapScope.Register(new GameObject(PrototypeMapSpawner.ContainerName));
                GameObject newContainer = second.Register(new GameObject(PrototypeMapSpawner.ContainerName));
                GameObject newMap = second.Register(new GameObject("NewNaturalTerrain")); newMap.transform.SetParent(newContainer.transform);
                mapField.SetValue(null, newMap); containerField.SetValue(null, newContainer);
                var clear = typeof(PrototypeMapSpawner).GetMethod("ClearForScope", BindingFlags.Static | BindingFlags.NonPublic);
                clear.Invoke(null, new object[] { oldMapScope });
                Check(newContainer != null && RunMapBootstrap.CurrentMapRoot == newMap, "late old scope cannot clear a newer adopted container", passed);
                containerField.SetValue(null, oldContainer);
                clear.Invoke(null, new object[] { oldMapScope });
                Check(oldContainer == null && newMap != null && RunMapBootstrap.CurrentMapRoot == newMap,
                    "old container clears without resetting the loader's newer CurrentMapRoot", passed);
            }
            finally { oldMapScope.Dispose(); }
            return string.Join("\n", passed) + "\nPASS ownership contracts: " + passed.Count;
        }
        finally
        {
            containerField.SetValue(null, previousContainer); mapField.SetValue(null, previousMap);
            first?.Dispose(); second?.Dispose();
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void Check(bool valid, string text, List<string> passed)
    {
        if (!valid) throw new InvalidOperationException(text);
        passed.Add("PASS " + text);
    }
}
