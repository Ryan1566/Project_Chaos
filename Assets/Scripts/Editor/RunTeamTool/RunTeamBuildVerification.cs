#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Explicit editor-only build check. Outputs stay in an owned temporary directory.</summary>
public static class RunTeamBuildVerification
{
    public static string Build()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || BuildPipeline.isBuildingPlayer)
            throw new InvalidOperationException("Requires idle Edit mode");
        string result = Application.dataPath + "/QA/RunTeam/build.txt";
        string output = Path.Combine(Path.GetTempPath(), "ChaosRunTeamBuild-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"), "Project_Chaos.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        File.WriteAllText(result, "running output=" + output);
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
        return Record(report);
    }

    public static string Record(BuildReport report)
    {
        if (report == null) throw new InvalidOperationException("No build report");
        string summary = "result=" + report.summary.result + "\nerrors=" + report.summary.totalErrors
            + "\nwarnings=" + report.summary.totalWarnings + "\nstarted=" + report.summary.buildStartedAt.ToString("O")
            + "\noutput=" + report.summary.outputPath;
        File.WriteAllText(Application.dataPath + "/QA/RunTeam/build.txt", summary); return summary;
    }
}
#endif
