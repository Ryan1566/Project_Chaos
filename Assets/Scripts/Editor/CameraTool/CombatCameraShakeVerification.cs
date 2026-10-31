#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cinemachine;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 战斗相机抖动的 Editor 侧可执行验收入口（不修改场景、Prefab 或资源）：
/// - 公开接口签名；
/// - 集中配置的默认值与运行时坏值防御；
/// - PlayerAttack / PlayerHealth 的真实触发点（IL 调用点）；
/// - 运行时装配现状与 Resources 配置资源是否存在。
///
/// 菜单：Tools/Project Chaos/验证/Combat Camera Shake
/// 批处理：-executeMethod CombatCameraShakeVerification.VerifyBatch
/// 报告：Library/CombatCameraShakeVerification.txt（生成物，不写入 Assets，避免污染工程）。
/// </summary>
public static class CombatCameraShakeVerification
{
    private static readonly List<string> Lines = new List<string>();
    private static int s_passed;
    private static int s_warnings;
    private static int s_failed;

    /// <summary>最近一次 Verify 是否没有 FAIL（WARN 不阻塞，但会写进报告）。</summary>
    public static bool LastPassed { get; private set; }

    [MenuItem("Tools/Project Chaos/验证/Combat Camera Shake")]
    public static void VerifyFromMenu()
    {
        string report = Verify();
        if (LastPassed) Debug.Log(report);
        else Debug.LogError(report);
    }

    /// <summary>批处理入口：有 FAIL 时返回非 0 退出码。</summary>
    public static void VerifyBatch()
    {
        Verify();
        if (Application.isBatchMode) EditorApplication.Exit(LastPassed ? 0 : 1);
    }

    public static string Verify()
    {
        Lines.Clear();
        s_passed = 0;
        s_warnings = 0;
        s_failed = 0;

        Lines.Add("CombatCameraShake verification @ " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        Lines.Add("Unity " + Application.unityVersion + " | batch=" + Application.isBatchMode);

        VerifyApi();
        VerifySettings();
        VerifyTriggerSites();
        VerifyIntegration();
        VerifySettingsAsset();

        string summary = "PASS " + s_passed + " | WARN " + s_warnings + " | FAIL " + s_failed;
        LastPassed = s_failed == 0;
        Lines.Add(string.Empty);
        Lines.Add(summary);
        Lines.Add(LastPassed
            ? (s_warnings > 0 ? "RESULT PASS (WITH WARNINGS)" : "RESULT PASS")
            : "RESULT FAIL");

        string report = string.Join("\n", Lines);
        WriteReport(report);
        return report;
    }

    private static void VerifyApi()
    {
        MethodInfo attach = typeof(CombatCameraShake).GetMethod(
            "Attach", BindingFlags.Public | BindingFlags.Static);
        if (attach == null)
        {
            Fail("CombatCameraShake.Attach 不存在");
        }
        else
        {
            ParameterInfo[] parameters = attach.GetParameters();
            PassIf(parameters.Length == 3
                && parameters[0].ParameterType == typeof(Camera)
                && parameters[1].ParameterType == typeof(CinemachineVirtualCamera)
                && parameters[2].ParameterType == typeof(Transform),
                "Attach(Camera, CinemachineVirtualCamera, Transform)");
            PassIf(attach.ReturnType == typeof(CombatCameraShake), "Attach 返回 CombatCameraShake");
        }

        MethodInfo detach = typeof(CombatCameraShake).GetMethod(
            "Detach", BindingFlags.Public | BindingFlags.Instance);
        if (detach == null)
        {
            Fail("CombatCameraShake.Detach 不存在");
        }
        else
        {
            ParameterInfo[] parameters = detach.GetParameters();
            PassIf(!detach.IsStatic && parameters.Length == 1 && parameters[0].ParameterType == typeof(Transform),
                "Detach(Transform) 为实例方法");
        }

        MethodInfo notifyHit = typeof(CombatCameraShake).GetMethod(
            "NotifyHit", BindingFlags.Public | BindingFlags.Static);
        if (notifyHit == null)
        {
            Fail("CombatCameraShake.NotifyHit 不存在");
        }
        else
        {
            ParameterInfo[] parameters = notifyHit.GetParameters();
            bool optionalHeavy = parameters.Length == 3
                && parameters[2].IsOptional
                && parameters[2].DefaultValue is bool
                && !(bool)parameters[2].DefaultValue;
            PassIf(parameters.Length == 3
                && parameters[0].ParameterType == typeof(Transform)
                && parameters[1].ParameterType == typeof(Vector3)
                && parameters[2].ParameterType == typeof(bool)
                && optionalHeavy,
                "NotifyHit(Transform, Vector3, bool heavy = false)");
        }

        MethodInfo notifyHurt = typeof(CombatCameraShake).GetMethod(
            "NotifyHurt", BindingFlags.Public | BindingFlags.Static);
        if (notifyHurt == null)
        {
            Fail("CombatCameraShake.NotifyHurt 不存在");
        }
        else
        {
            ParameterInfo[] parameters = notifyHurt.GetParameters();
            PassIf(parameters.Length == 2
                && parameters[0].ParameterType == typeof(Transform)
                && parameters[1].ParameterType == typeof(Vector3),
                "NotifyHurt(Transform, Vector3)");
        }
    }

    private static void VerifySettings()
    {
        CombatCameraShakeSettings settings = ScriptableObject.CreateInstance<CombatCameraShakeSettings>();
        try
        {
            PassIf(Mathf.Approximately(settings.NormalHitAmplitude, 0.05f), "普通命中幅度默认 0.05");
            PassIf(Mathf.Approximately(settings.NormalHitDuration, 0.10f), "普通命中时长默认 0.10");
            PassIf(Mathf.Approximately(settings.HurtAmplitude, 0.12f), "受击幅度默认 0.12");
            PassIf(Mathf.Approximately(settings.HurtDuration, 0.14f), "受击时长默认 0.14");
            PassIf(Mathf.Approximately(settings.MaxAmplitude, 0.28f), "叠加上限默认 0.28");
            PassIf(Mathf.Approximately(settings.MinTriggerInterval, 0.06f), "最小间隔默认 0.06");
            PassIf(Mathf.Approximately(settings.IntensityScale, 1f), "强度倍率默认 1");

            PassIf(SetFloat(settings, "normalHitAmplitude", -5f) && settings.NormalHitAmplitude == 0f,
                "负幅度被钳制为 0");
            PassIf(SetFloat(settings, "normalHitDuration", 0f) && settings.NormalHitDuration == 0.01f,
                "零时长被钳制为 0.01");
            PassIf(SetFloat(settings, "intensityScale", -1f) && settings.IntensityScale == 0f,
                "负倍率被钳制为 0");
            PassIf(SetFloat(settings, "maxAmplitude", 99f) && settings.MaxAmplitude == 2f,
                "幅度上限被钳制为 2");
            PassIf(SetFloat(settings, "normalHitAmplitude", float.NaN)
                && Mathf.Approximately(settings.NormalHitAmplitude, 0.05f),
                "NaN 幅度回退默认值");
            PassIf(SetFloat(settings, "hurtAmplitude", float.PositiveInfinity)
                && Mathf.Approximately(settings.HurtAmplitude, 0.12f),
                "Infinity 幅度回退默认值");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(settings);
        }
    }

    private static void VerifyTriggerSites()
    {
        PassIf(CountCalls(typeof(PlayerAttack), "DoAttack", "NotifyHit") == 1,
            "PlayerAttack.DoAttack 仅实际命中后调用 NotifyHit 一次");
        PassIf(CountCalls(typeof(PlayerHealth), "ApplyContactDamage", "NotifyHurt") == 1,
            "PlayerHealth.ApplyContactDamage 真实扣血时调用 NotifyHurt 一次");
        PassIf(CountCalls(typeof(PlayerHealth), "ForceDown", "NotifyHurt") == 0,
            "GM 强制倒地不触发受击抖动");
        PassIf(CountCalls(typeof(PlayerHealth), "RefillFullHp", "NotifyHurt") == 0,
            "治疗/扎营不触发受击抖动");
    }

    private static void VerifyIntegration()
    {
        List<string> callers = FindAttachCallers();
        if (callers.Count == 0)
        {
            Warn("未发现 CombatCameraShake.Attach 调用方：运行时不会绑定任何相机。"
                + "本批可写范围不含 PlayerSpawner / PlayerCameraFollow / 场景引导，需后续接入。");
            return;
        }

        int shown = Mathf.Min(callers.Count, 5);
        string detail = string.Join(", ", callers.GetRange(0, shown).ToArray());
        if (callers.Count > shown) detail += " ...";
        PassIf(true, "发现 Attach 调用方: " + detail);
    }

    private static void VerifySettingsAsset()
    {
        CombatCameraShakeSettings asset =
            Resources.Load<CombatCameraShakeSettings>(CombatCameraShakeSettings.ResourcesPath);
        if (asset == null)
        {
            Warn("Resources/" + CombatCameraShakeSettings.ResourcesPath
                + " 不存在：运行时会使用内置默认值，功能可用但无法在 Inspector 调参。");
            return;
        }
        PassIf(true, "配置资源存在: " + AssetDatabase.GetAssetPath(asset));
    }

    private static List<string> FindAttachCallers()
    {
        List<string> callers = new List<string>();
        Type[] types;
        try
        {
            types = typeof(CombatCameraShake).Assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            types = exception.Types;
            Warn("运行时类型仅部分加载，装配扫描可能不完整: " + exception.Message);
        }

        for (int i = 0; i < types.Length; i++)
        {
            Type type = types[i];
            if (type == null || type == typeof(CombatCameraShake)) continue;

            MethodBase[] methods;
            try
            {
                List<MethodBase> combined = new List<MethodBase>();
                combined.AddRange(type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly));
                combined.AddRange(type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly));
                methods = combined.ToArray();
            }
            catch (Exception)
            {
                continue;
            }

            for (int j = 0; j < methods.Length; j++)
            {
                MethodBase method = methods[j];
                if (method == null || method.IsAbstract) continue;
                if (!HasCall(method, typeof(CombatCameraShake), "Attach")) continue;

                callers.Add(type.FullName + "." + method.Name);
                break;
            }
        }
        return callers;
    }

    private static int CountCalls(Type owner, string methodName, string calleeName)
    {
        MethodInfo method = owner.GetMethod(methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        if (method == null) return -1;
        return CountCalls(method, typeof(CombatCameraShake), calleeName);
    }

    private static int CountCalls(MethodBase method, Type calleeType, string calleeName)
    {
        MethodBody body = GetBody(method);
        if (body == null) return 0;
        byte[] il = body.GetILAsByteArray();
        if (il == null) return 0;

        int count = 0;
        for (int i = 0; i + 4 < il.Length; i++)
        {
            if (il[i] != 0x28 && il[i] != 0x6F) continue; // call / callvirt
            if (IsTargetCall(method, il, i, calleeType, calleeName)) count++;
        }
        return count;
    }

    private static bool HasCall(MethodBase method, Type calleeType, string calleeName)
    {
        MethodBody body = GetBody(method);
        if (body == null) return false;
        byte[] il = body.GetILAsByteArray();
        if (il == null) return false;

        for (int i = 0; i + 4 < il.Length; i++)
        {
            if (il[i] != 0x28 && il[i] != 0x6F) continue; // call / callvirt
            if (IsTargetCall(method, il, i, calleeType, calleeName)) return true;
        }
        return false;
    }

    private static bool IsTargetCall(MethodBase caller, byte[] il, int opcodeIndex, Type calleeType, string calleeName)
    {
        try
        {
            int token = BitConverter.ToInt32(il, opcodeIndex + 1);
            Type[] typeArguments = caller.DeclaringType == null
                ? Type.EmptyTypes
                : caller.DeclaringType.GetGenericArguments();
            MethodBase callee = caller.Module.ResolveMethod(token, typeArguments, caller.GetGenericArguments());
            return callee != null && callee.DeclaringType == calleeType && callee.Name == calleeName;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static MethodBody GetBody(MethodBase method)
    {
        try
        {
            return method.GetMethodBody();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool SetFloat(object target, string fieldName, float value)
    {
        FieldInfo field = target.GetType().GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null || field.FieldType != typeof(float)) return false;
        field.SetValue(target, value);
        return true;
    }

    private static void PassIf(bool condition, string name)
    {
        if (condition)
        {
            s_passed++;
            Lines.Add("PASS " + name);
        }
        else
        {
            Fail(name);
        }
    }

    private static void Fail(string name)
    {
        s_failed++;
        Lines.Add("FAIL " + name);
    }

    private static void Warn(string name)
    {
        s_warnings++;
        Lines.Add("WARN " + name);
    }

    private static void WriteReport(string report)
    {
        try
        {
            string library = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library"));
            Directory.CreateDirectory(library);
            File.WriteAllText(Path.Combine(library, "CombatCameraShakeVerification.txt"), report);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[CombatCameraShakeVerification] 报告写入失败: " + exception.Message);
        }
    }
}
#endif
