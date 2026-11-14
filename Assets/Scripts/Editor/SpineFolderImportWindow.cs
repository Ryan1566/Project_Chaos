using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

internal sealed class SpineFolderImportWindow : EditorWindow
{
    [Serializable]
    private sealed class ProjectOptions
    {
        public string destination = "Assets/Art/Spine";
        public bool keepSourceFolder = true;
    }

    private const string OptionsFile = "Packages/ChaosSpineImportSettings.json";
    private static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);
    private static string SourcePreference => "Chaos.SpineImport.Source." + Application.dataPath;

    private ProjectOptions options;
    private string source;
    private string status;
    private MessageType statusType;

    [MenuItem("Chaos/资源/Spine 导入与初始化")]
    private static void Open()
    {
        GetWindow<SpineFolderImportWindow>("Spine 导入").minSize = new Vector2(500, 210);
    }

    private void OnEnable()
    {
        source = EditorPrefs.GetString(SourcePreference, string.Empty);
        options = new ProjectOptions();
        string path = Path.Combine(ProjectRoot, OptionsFile);
        if (!File.Exists(path)) return;

        try
        {
            options = JsonUtility.FromJson<ProjectOptions>(File.ReadAllText(path)) ?? options;
        }
        catch (Exception exception)
        {
            SetStatus("读取项目选项失败：" + exception.Message, MessageType.Error);
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        source = EditorGUILayout.DelayedTextField("外部源目录", source);
        if (GUILayout.Button("选择…", GUILayout.Width(68)))
        {
            string selected = EditorUtility.OpenFolderPanel("选择 Spine 成品目录", source, "");
            if (!string.IsNullOrEmpty(selected))
            {
                source = selected;
                EditorPrefs.SetString(SourcePreference, source);
            }
        }
        if (EditorGUI.EndChangeCheck()) EditorPrefs.SetString(SourcePreference, source);
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.BeginHorizontal();
        options.destination = EditorGUILayout.DelayedTextField("项目目标目录", options.destination);
        if (GUILayout.Button("选择…", GUILayout.Width(68)))
        {
            string selected = EditorUtility.OpenFolderPanel("选择 Assets 内的目标目录", Application.dataPath, "");
            if (!string.IsNullOrEmpty(selected))
            {
                options.destination = selected;
                GUI.changed = true;
            }
        }
        EditorGUILayout.EndHorizontal();
        options.keepSourceFolder = EditorGUILayout.Toggle("保留源根目录名", options.keepSourceFolder);
        if (EditorGUI.EndChangeCheck()) SaveOptions();

        EditorGUILayout.HelpBox(
            "按相对路径新增或覆盖，保留目标侧的 .meta。图集请使用 .atlas.txt。\n" +
            "导入前将 Spine JSON 骨骼版本 3.8.75 写成 3.8，再由 Spine-Unity 初始化资产。",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode ||
                                           EditorApplication.isCompiling || string.IsNullOrWhiteSpace(source)))
        {
            if (GUILayout.Button("同步并初始化", GUILayout.Height(28))) Import();
        }
        if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, statusType);
    }

    private bool SaveOptions()
    {
        try
        {
            string destination = SpineFolderImport.GetDestination(ProjectRoot, options.destination);
            options.destination = Path.GetRelativePath(ProjectRoot, destination).Replace('\\', '/');
            string path = Path.Combine(ProjectRoot, OptionsFile);
            string json = JsonUtility.ToJson(options, true) + "\n";
            if (!File.Exists(path) || File.ReadAllText(path) != json) File.WriteAllText(path, json);
            SetStatus("已保存项目选项。", MessageType.Info);
            return true;
        }
        catch (Exception exception)
        {
            SetStatus("保存项目选项失败：" + exception.Message, MessageType.Error);
            return false;
        }
    }

    private void Import()
    {
        if (!SaveOptions()) return;
        try
        {
            EditorPrefs.SetString(SourcePreference, source);
            SetStatus(SpineFolderImport.Sync(ProjectRoot, source, options.destination, options.keepSourceFolder),
                MessageType.Info);
        }
        catch (Exception exception)
        {
            SetStatus("导入失败：" + exception.Message, MessageType.Error);
            Debug.LogException(exception);
        }
    }

    private void SetStatus(string message, MessageType type)
    {
        status = message;
        statusType = type;
    }
}

internal static class SpineFolderImport
{
    // 按 JSON 字符串和结构符号扫描，避免把其他字段或字符串正文当成骨骼版本。
    private static readonly Regex JsonTokens = new Regex(
        @"""skeleton""[ \t\r\n]*:[ \t\r\n]*(?<skeleton>\{)|""spine""[ \t\r\n]*:[ \t\r\n]*""(?<version>3\.8\.75)""|""(?:\\[\s\S]|[^""\\])*""|[{}\[\]]",
        RegexOptions.CultureInvariant);

    internal static string GetDestination(string projectRoot, string destination)
    {
        if (string.IsNullOrWhiteSpace(destination)) throw new ArgumentException("请选择 Assets 内的目标目录。");
        string assets = Path.Combine(projectRoot, "Assets");
        string full = FullPath(Path.Combine(projectRoot, destination.Trim()));
        if (!IsAtOrUnder(full, assets)) throw new ArgumentException("目标目录必须位于本项目的 Assets 内。");
        return full;
    }

    internal static string Sync(string projectRoot, string source, string destination, bool keepSourceFolder)
    {
        string sourceRoot = FullPath(source.Trim());
        if (!Directory.Exists(sourceRoot)) throw new DirectoryNotFoundException(sourceRoot);
        if (IsAtOrUnder(sourceRoot, projectRoot) || IsAtOrUnder(projectRoot, sourceRoot))
            throw new ArgumentException("请选择与 Unity 项目互不包含的外部源目录。");

        string targetRoot = GetDestination(projectRoot, destination);
        if (keepSourceFolder) targetRoot = Path.Combine(targetRoot, Path.GetFileName(sourceRoot));
        CheckAncestors(sourceRoot);
        CheckAncestors(targetRoot);

        var folders = new List<string>();
        var files = new List<(string Source, string Relative)>();
        Collect(sourceRoot, "", folders, files);

        foreach (string folder in folders) CheckTarget(Path.Combine(targetRoot, folder), true);
        foreach (var file in files) CheckTarget(Path.Combine(targetRoot, file.Relative), false);

        // 先暂存整批文件。正式替换时保留旧副本，以便同步失败后恢复。
        string stageRoot = Path.Combine(projectRoot, "Library", "ChaosSpineImport", Guid.NewGuid().ToString("N"));
        var applied = new List<(string Target, string Backup)>();
        int replaced = 0;
        int converted = 0;
        bool preserveBackups = false;
        try
        {
            Directory.CreateDirectory(stageRoot);
            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                string staged = Path.Combine(stageRoot, i + ".new");
                using (var input = File.OpenRead(file.Source))
                using (var output = File.Create(staged))
                    input.CopyTo(output);
                if (Path.GetExtension(file.Relative).Equals(".json", StringComparison.OrdinalIgnoreCase) &&
                    NormalizeVersion(staged)) converted++;
            }

            AssetDatabase.DisallowAutoRefresh();
            try
            {
                foreach (string folder in folders) Directory.CreateDirectory(Path.Combine(targetRoot, folder));
                for (int i = 0; i < files.Count; i++)
                {
                    string target = Path.Combine(targetRoot, files[i].Relative);
                    string staged = Path.Combine(stageRoot, i + ".new");
                    string backup = null;
                    if (File.Exists(target))
                    {
                        backup = Path.Combine(stageRoot, i + ".bak");
                        File.Replace(staged, target, backup);
                        replaced++;
                    }
                    else File.Move(staged, target);
                    applied.Add((target, backup));
                }
            }
            catch (Exception failure)
            {
                var rollbackErrors = new List<Exception>();
                for (int i = applied.Count - 1; i >= 0; i--)
                {
                    var file = applied[i];
                    try
                    {
                        if (file.Backup == null) File.Delete(file.Target);
                        else File.Replace(file.Backup, file.Target, null);
                    }
                    catch (Exception rollbackFailure) { rollbackErrors.Add(rollbackFailure); }
                }
                if (rollbackErrors.Count == 0) throw;
                preserveBackups = true;
                rollbackErrors.Insert(0, failure);
                throw new AggregateException("未能完整恢复文件，备份路径：" + stageRoot, rollbackErrors);
            }
            finally { AssetDatabase.AllowAutoRefresh(); }
        }
        finally
        {
            if (!preserveBackups && Directory.Exists(stageRoot))
            {
                try { UnityEditor.FileUtil.DeleteFileOrDirectory(stageRoot); }
                catch (IOException exception) { Debug.LogWarning(exception.Message); }
                catch (UnauthorizedAccessException exception) { Debug.LogWarning(exception.Message); }
            }
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        string assetFolder = Path.GetRelativePath(projectRoot, targetRoot).Replace('\\', '/');
        return $"已同步到 {assetFolder}\n新增 {files.Count - replaced}，覆盖 {replaced}，转换版本 {converted}。请在 Console 查看 Spine 初始化结果。";
    }

    private static void Collect(string sourceRoot, string relative, List<string> folders,
        List<(string Source, string Relative)> files)
    {
        folders.Add(relative);
        string folder = Path.Combine(sourceRoot, relative);
        foreach (string file in Directory.GetFiles(folder).OrderBy(path => path, StringComparer.Ordinal))
        {
            if (Path.GetExtension(file).Equals(".meta", StringComparison.OrdinalIgnoreCase)) continue;
            RejectLink(file);
            files.Add((file, Path.Combine(relative, Path.GetFileName(file))));
        }
        foreach (string child in Directory.GetDirectories(folder).OrderBy(path => path, StringComparer.Ordinal))
        {
            RejectLink(child);
            Collect(sourceRoot, Path.Combine(relative, Path.GetFileName(child)), folders, files);
        }
    }

    private static bool NormalizeVersion(string path)
    {
        using var reader = new StreamReader(path, new UTF8Encoding(false, true), true);
        string json = reader.ReadToEnd();
        Encoding encoding = reader.CurrentEncoding;
        reader.Close();
        int depth = 0;
        bool inSkeleton = false;
        foreach (Match token in JsonTokens.Matches(json))
        {
            if (token.Groups["skeleton"].Success)
            {
                if (depth == 1) inSkeleton = true;
                depth++;
                continue;
            }
            var version = token.Groups["version"];
            if (depth == 2 && inSkeleton && version.Success)
            {
                File.WriteAllText(path, json.Remove(version.Index, version.Length).Insert(version.Index, "3.8"), encoding);
                return true;
            }
            if (token.Value == "{" || token.Value == "[") depth++;
            if (token.Value == "}" || token.Value == "]")
            {
                if (depth == 2) inSkeleton = false;
                depth--;
            }
        }
        return false;
    }

    private static void CheckTarget(string path, bool directory)
    {
        if (File.Exists(path) || Directory.Exists(path)) RejectLink(path);
        if (directory ? File.Exists(path) : Directory.Exists(path))
            throw new IOException("目标路径的文件和目录冲突：" + path);
    }

    private static void CheckAncestors(string path)
    {
        for (string current = path; current != null; current = Path.GetDirectoryName(current))
            if (File.Exists(current) || Directory.Exists(current)) RejectLink(current);
    }

    private static void RejectLink(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("导入路径包含链接或目录联接：" + path);
    }

    private static string FullPath(string path)
    {
        string full = Path.GetFullPath(path);
        return full.Length == Path.GetPathRoot(full).Length ? full : full.TrimEnd('/', '\\');
    }

    private static bool IsAtOrUnder(string path, string root)
    {
        string prefix = root.TrimEnd('/', '\\') + Path.DirectorySeparatorChar;
        return path.Equals(root, StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}
