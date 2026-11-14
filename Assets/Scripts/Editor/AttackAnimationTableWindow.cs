using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Spine.Unity;
using Spine.Unity.AnimationTools;
using UnityEditor;
using UnityEngine;

/// <summary>导入外部战斗配置，验证并读取 Spine 资源，导出逐招对照表。</summary>
public sealed class AttackAnimationTableWindow : EditorWindow
{
    const string RuntimePath = "Assets/Resources/Combat/AttackAnimations.json";
    const string DefaultSkeleton = "Assets/Art/Spine/Citizen_001/citizen_001_SkeletonData.asset";
    SkeletonDataAsset _skeleton;
    string _message;
    MessageType _messageType;

    [MenuItem("Tools/战斗/Spine 动作配置")]
    static void Open()
    {
        var window = GetWindow<AttackAnimationTableWindow>("Spine 战斗动作");
        if (window._skeleton == null) window._skeleton = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(DefaultSkeleton);
    }

    void OnGUI()
    {
        _skeleton = (SkeletonDataAsset)EditorGUILayout.ObjectField("Spine 骨骼", _skeleton, typeof(SkeletonDataAsset), false);
        using (new EditorGUI.DisabledScope(_skeleton == null))
        {
            if (GUILayout.Button("导入 JSON 配置并校验 Spine"))
            {
                var file = EditorUtility.OpenFilePanel("选择战斗动作配置", "", "json");
                if (!string.IsNullOrEmpty(file)) Run(() =>
                {
                    var json = File.ReadAllText(file);
                    var table = AttackAnimationTable.Parse(json);
                    var names = Validate(table, _skeleton);
                    File.WriteAllText(RuntimePath, json, new UTF8Encoding(false));
                    AssetDatabase.ImportAsset(RuntimePath, ImportAssetOptions.ForceSynchronousImport);
                    return "已导入。已核对 " + names.Count + " 段动作。";
                });
            }
            if (GUILayout.Button("导出当前 JSON"))
            {
                var file = EditorUtility.SaveFilePanel("导出战斗动作配置", "", "AttackAnimations", "json");
                if (!string.IsNullOrEmpty(file)) Run(() =>
                {
                    File.WriteAllText(file, File.ReadAllText(RuntimePath), new UTF8Encoding(false));
                    return "已导出 JSON。";
                });
            }
            if (GUILayout.Button("扫描 Spine 并导出 CSV 对照"))
            {
                var file = EditorUtility.SaveFilePanel("导出 Spine 动作对照", "", "SpineAttackComparison", "csv");
                if (!string.IsNullOrEmpty(file)) Run(() =>
                {
                    var table = AttackAnimationTable.Parse(File.ReadAllText(RuntimePath));
                    File.WriteAllText(file, BuildComparison(table, _skeleton), new UTF8Encoding(true));
                    return "已导出 CSV，包含动画时长、位移骨、位移量、命中和连招窗口。";
                });
            }
            if (GUILayout.Button("从 Spine 补齐未配置动作并导出 JSON 草稿"))
            {
                var file = EditorUtility.SaveFilePanel("导出战斗动作草稿", "", "AttackAnimations_Draft", "json");
                if (!string.IsNullOrEmpty(file)) Run(() =>
                {
                    var table = AttackAnimationTable.Parse(File.ReadAllText(RuntimePath));
                    int count = AddUnconfiguredClips(table, _skeleton);
                    File.WriteAllText(file, JsonUtility.ToJson(table, true) + "\n", new UTF8Encoding(false));
                    return "已补齐 " + count + " 段。请核对命中和接招参数，再导入草稿。";
                });
            }
        }
        if (!string.IsNullOrEmpty(_message)) EditorGUILayout.HelpBox(_message, _messageType);
    }

    void Run(Func<string> action)
    {
        try { _message = action(); _messageType = MessageType.Info; }
        catch (Exception e) { _message = e.Message; _messageType = MessageType.Error; }
    }

    public static List<string> Validate(AttackAnimationTable table, SkeletonDataAsset skeleton)
    {
        var data = skeleton.GetSkeletonData(false);
        if (data == null) throw new FormatException("无法读取 Spine 骨骼。");
        var names = new List<string>();
        foreach (var move in table.moves)
        {
            // 同一份表可管理多种骨骼，本次只校验所选骨骼的动作体系。
            if (!MatchesSkeleton(move.animation, table, data)) continue;
            AttackAnimationTable.ValidateClip(move, data);
            names.Add(move.animation);
        }
        if (names.Count == 0) throw new FormatException("当前 Spine 骨骼未匹配到任何配置的攻击体系。");
        return names;
    }

    static bool MatchesSkeleton(string name, AttackAnimationTable table, Spine.SkeletonData data)
    {
        if (data.FindAnimation(name) != null) return true;
        foreach (var profile in table.profiles)
            if (data.FindAnimation(profile.light) != null
                && name.StartsWith("Attack_" + profile.id.Replace('/', '_') + "_", StringComparison.Ordinal)) return true;
        return false;
    }

    public static int AddUnconfiguredClips(AttackAnimationTable table, SkeletonDataAsset skeleton)
    {
        var data = skeleton.GetSkeletonData(false);
        if (data == null) throw new FormatException("无法读取 Spine 骨骼。");
        var moves = new List<AttackAnimationTable.Move>(table.moves);
        int previousCount = moves.Count;
        foreach (var clip in data.Animations)
        {
            if (!AttackAnimationTable.IsAttackName(clip.Name) || table.FindMove(clip.Name) != null) continue;
            var root = data.FindBone("root");
            moves.Add(new AttackAnimationTable.Move
            {
                animation = clip.Name,
                rootBone = root != null && clip.FindTranslateTimelineForBone(root.Index) != null ? root.Name : "",
                hitTime = Mathf.Round(clip.Duration * .5f * 10000f) / 10000f,
                hitWidth = 1.15f,
                hitHeight = .8f,
                hitOffset = .73f,
                damageMultiplier = 1f,
                speed = 1f
            });
        }
        table.moves = moves.ToArray();
        return moves.Count - previousCount;
    }

    public static string BuildComparison(AttackAnimationTable table, SkeletonDataAsset skeleton)
    {
        var data = skeleton.GetSkeletonData(false);
        var report = new StringBuilder("animation,configured,duration,rootBone,rootDeltaX,hitEvent,hitTime,hitWidth,hitHeight,hitOffset,damageMultiplier,speed,next,comboOpen,comboClose\r\n");
        foreach (var clip in data.Animations)
        {
            if (!AttackAnimationTable.IsAttackName(clip.Name)) continue;
            var move = table.FindMove(clip.Name);
            var bone = move?.rootBone;
            var timeline = string.IsNullOrEmpty(bone) ? null : clip.FindTranslateTimelineForBone(data.FindBoneIndex(bone));
            string Number(float? value) => value?.ToString("0.####", CultureInfo.InvariantCulture) ?? "";
            string Cell(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
            report.AppendLine(string.Join(",", Cell(clip.Name), move == null ? "no" : "yes", Number(clip.Duration), Cell(bone),
                Number(timeline == null ? (float?)null : timeline.Evaluate(clip.Duration).x - timeline.Evaluate(0f).x),
                Cell(move?.hitEvent), Number(move == null ? (float?)null : AttackAnimationTable.GetHitTime(move, clip)),
                Number(move?.hitWidth), Number(move?.hitHeight), Number(move?.hitOffset), Number(move?.damageMultiplier),
                Number(move?.speed), Cell(move?.next), Number(move?.comboOpen), Number(move?.comboClose)));
        }
        return report.ToString();
    }
}
