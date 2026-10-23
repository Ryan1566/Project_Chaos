using System;
using ChaosDebug;
using UnityEngine;

/// <summary>
/// 在**当前场景**里就地生成一张 `RunMap` 局内地图（原型用）。
///
/// ⚠ 原型约定 T1/T2：**不切场景、不走 LoadingScene**，直接调 `RunMapBootstrap.BuildScene`，
///   把生成物挂到一个自动创建的容器 `PrototypeRunMap` 下。**不修改 RunMap 的任何代码**。
/// </summary>
public static class PrototypeMapSpawner
{
    /// <summary>容器节点名（固定；GM/自测都靠它找）。</summary>
    public const string ContainerName = "PrototypeRunMap";

    /// <summary>用后即弃的名字后缀：Play 模式下 Destroy 要到帧末才生效，先改名避免"新旧同名"。 </summary>
    private const string DestroyingSuffix = "_destroying";

    public static bool IsSpawned { get { return FindContainer() != null; } }

    /// <summary>接管读条期间已生成的地图容器，不重新生成。后续换场景/撤离沿用 Clear。</summary>
    public static GameObject AdoptPreparedMap()
    {
        GameObject root = RunMap.RunMapBootstrap.CurrentMapRoot;
        if (root == null) return null;
        Transform holder = root.transform.parent;
        if (holder != null && holder.name == "RunMap_Pending")
        {
            holder.name = ContainerName;
            return holder.gameObject;
        }
        return null;
    }

    /// <summary>
    /// 生成第 sceneIndex 场景（1~3）。不指定种子时自动生成；显式0是有效种子。
    /// 返回容器对象；失败返回 null（不抛异常）。
    /// </summary>
    public static GameObject Spawn(int sceneIndex, uint? seed = null)
    {
        if (sceneIndex < 1 || sceneIndex > 3)
        {
            ChaosLog.Warn(LogChannel.Config, "[原型地图] sceneIndex 必须在 1~3，收到 " + sceneIndex + "，忽略");
            return null;
        }

        Clear();

        if (!seed.HasValue)
        {
            seed = RunMap.NaturalMapRuntime.CreateRunSeed();
            ChaosLog.Info(LogChannel.Config, "[原型地图] 自动种子：" + seed);
        }

        GameObject container = new GameObject(ContainerName);
        GameObject root = null;

        try
        {
            root = RunMap.RunMapBootstrap.BuildScene(seed.Value, sceneIndex, container.transform, true);
        }
        catch (Exception e)
        {
            ChaosLog.Error(LogChannel.Config, "[原型地图] BuildScene 抛异常：" + e);
        }

        if (root == null)
        {
            ChaosLog.Warn(LogChannel.Config,
                "[原型地图] 未生成可用地图：" + RunMap.NaturalMapRuntime.LastError);
            if (Application.isPlaying) UnityEngine.Object.Destroy(container); else UnityEngine.Object.DestroyImmediate(container);
            return null;
        }
        else
        {
            ChaosLog.Success(LogChannel.Config,
                "[原型地图] 已生成 场景" + sceneIndex + " seed=" + seed
                + " 根对象=" + root.name + " 容器=" + ContainerName
                + " 阶段=" + RunMap.RunMapBootstrap.CurrentStageText
                + " 当前场景=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        return container;
    }

    /// <summary>销毁已生成的容器（Play 用 Destroy、编辑模式用 DestroyImmediate；可反复调用）。</summary>
    public static void Clear()
    {
        RunMap.RunMapBootstrap.ClearCurrent();
        GameObject container = FindContainer();
        while (container != null)
        {
            if (Application.isPlaying)
            {
                //Destroy 到帧末才生效 → 先改名，保证紧接着 Find 到的是"新容器"而不是这个待销毁的
                container.name = ContainerName + DestroyingSuffix;
                UnityEngine.Object.Destroy(container);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(container);
            }
            container = FindContainer();
        }
    }

    private static GameObject FindContainer()
    {
        return GameObject.Find(ContainerName);
    }
}
