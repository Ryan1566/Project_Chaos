using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ChaosPlayerBuild
{
    [MenuItem("Chaos/构建/打包开发版", priority = 100)]
    private static void BuildDevelopment() => BuildFromMenu(true);

    [MenuItem("Chaos/构建/打包正式版", priority = 101)]
    private static void BuildRelease() => BuildFromMenu(false);

    private static void BuildFromMenu(bool development)
    {
        string channel = development ? "Dev" : "Release";
        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", channel, stamp, "Project_Chaos.exe"));
        BuildReport report = BuildPlayer(path, development);
        Debug.Log($"[Chaos 构建] {channel} {PlayerSettings.bundleVersion} · {report.summary.result}\n{path}");
    }

    public static BuildReport BuildPlayer(string outputPath, bool development)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || BuildPipeline.isBuildingPlayer)
            throw new InvalidOperationException("请在 Unity 空闲的编辑模式下打包。");

        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        if (scenes.Length == 0) throw new BuildFailedException("构建设置中没有启用的场景。");
        string path = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        return BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = path,
            target = BuildTarget.StandaloneWindows64,
            options = development ? BuildOptions.Development : BuildOptions.None
        });
    }
}

internal sealed class ChaosPlayerSceneProcessor : IProcessSceneWithReport
{
    public int callbackOrder => 0;

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        // 只改 Player 构建中的场景副本，编辑器 Play 保持控制台停用。
        if (report == null || !BuildPipeline.isBuildingPlayer) return;
        bool development = (report.summary.options & BuildOptions.Development) != 0;
        if (!development && (scene.name == "TestScene" || scene.name == "URP_TestScene"))
            throw new BuildFailedException($"正式构建包含测试场景 {scene.path}。请在构建设置中取消勾选。");

        var excluded = new HashSet<GameObject>();
        foreach (var root in scene.GetRootGameObjects())
        foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            // 控制台默认停用。开发包启动时启用，正式包移除整个画布及其日志订阅组件。
            if (component is IngameDebugConsole.DebugLogManager)
            {
                if (development) component.gameObject.SetActive(true);
                else excluded.Add(component.gameObject);
                continue;
            }

            if (development || (!(component is EnterGM) && !(component is GMPanel) && !(component is GM_UITestEnter))) continue;
            Transform parent = component.transform.parent;
            // 现有 GM 按钮与面板共用专用画布；移动到其他画布时只剔除各自的入口。
            GameObject target = parent != null && parent.name == "GM_Canvas" && parent.TryGetComponent<Canvas>(out _)
                ? parent.gameObject : component.gameObject;
            excluded.Add(target);
        }

        foreach (var target in excluded)
            if (target != null) UnityEngine.Object.DestroyImmediate(target);
    }
}
