using ChaosDebug;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 配置表运行时读取入口。同步与异步都先通过 ConfigTableAssets.Resolve 解析类型对应的资源路径，
/// 不直接把物理文件名写死到业务层。
/// </summary>
public class ConfigLoader : SingletonBase<ConfigLoader>
{
    /// <summary>
    /// 同步加载配置。解析路径、资源缺失和 Json 解析失败都会抛出带类型与路径的明确异常，不会对空资源解引用。
    /// </summary>
    /// <typeparam name="T">配置数据类</typeparam>
    /// <returns>配置列表</returns>
    public DataList<T> LoadData<T>()
    {
        string typeName = typeof(T).Name;
        string path = ConfigTableAssets.Resolve(typeName);
        TextAsset asset;
        try
        {
            asset = ResManager.Instance.Load<TextAsset>(path);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"配置表加载失败：类型「{typeName}」读取资源时异常，资源路径「{path}」", ex);
        }
        if (asset == null)
        {
            throw new InvalidOperationException($"配置表加载失败：类型「{typeName}」对应的资源不存在，资源路径「{path}」");
        }

        DataList<T> result;
        try
        {
            result = JsonUtility.FromJson<DataList<T>>(asset.text);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"配置表解析失败：类型「{typeName}」，资源路径「{path}」", ex);
        }

        if (result == null || result.datas == null)
        {
            throw new InvalidOperationException($"配置表解析结果为空：类型「{typeName}」，资源路径「{path}」");
        }

        ChaosLog.Info(LogChannel.Config, "文件已同步解析完毕：" + typeName + "，资源路径：" + path);
        return result;
    }

    /// <summary>
    /// 异步加载配置。与同步加载共用同一个 ConfigTableAssets.Resolve 结果；资源或解析失败时回调 null，
    /// 并记录明确诊断，不在加载器内部产生空引用。
    /// </summary>
    /// <typeparam name="T">配置数据类</typeparam>
    /// <param name="OnLoadCompleted">加载完成回调，失败时参数为 null</param>
    public void LoadDataAsync<T>(UnityAction<DataList<T>> OnLoadCompleted)
    {
        string typeName = typeof(T).Name;
        string path;
        try
        {
            path = ConfigTableAssets.Resolve(typeName);
        }
        catch (Exception ex)
        {
            ChaosLog.Error(LogChannel.Config, $"配置表解析失败：类型「{typeName}」；{ex.Message}");
            if (OnLoadCompleted != null)
            {
                OnLoadCompleted(null);
            }
            return;
        }

        try
        {
            ResManager.Instance.LoadAsync<TextAsset>(path, (file) =>
            {
                HandleAsyncLoaded(file, typeName, path, OnLoadCompleted);
            });
        }
        catch (Exception ex)
        {
            ChaosLog.Error(LogChannel.Config, $"配置表加载失败：类型「{typeName}」启动异步加载时异常，资源路径「{path}」；{ex.Message}");
            if (OnLoadCompleted != null)
            {
                OnLoadCompleted(null);
            }
        }
    }

    /// <summary>
    /// 统一处理异步加载结果：解析成功回调数据，资源或解析失败回调 null，并记录中文诊断。
    /// </summary>
    private static void HandleAsyncLoaded<T>(
        TextAsset file,
        string typeName,
        string path,
        UnityAction<DataList<T>> onLoadCompleted)
    {
        DataList<T> result = null;
        if (file != null)
        {
            try
            {
                result = JsonUtility.FromJson<DataList<T>>(file.text);
                if (result == null || result.datas == null)
                {
                    ChaosLog.Error(LogChannel.Config, $"配置表解析结果为空：类型「{typeName}」，资源路径「{path}」");
                    result = null;
                }
                else
                {
                    ChaosLog.Info(LogChannel.Config, "文件已异步解析完毕：" + typeName + "，资源路径：" + path);
                }
            }
            catch (Exception ex)
            {
                ChaosLog.Error(LogChannel.Config, $"配置表解析失败：类型「{typeName}」，资源路径「{path}」；{ex.Message}");
                result = null;
            }
        }
        else
        {
            ChaosLog.Warn(LogChannel.Config, $"配置表加载失败：类型「{typeName}」对应的资源缺失或类型异常，资源路径「{path}」");
        }

        if (onLoadCompleted != null)
        {
            onLoadCompleted(result);
        }
    }
}

/// <summary>
/// 泛型基类 各种数据类的列表
/// </summary>
/// <typeparam name="T">Data类</typeparam>
[Serializable]
public class DataList<T>
{
    public List<T> datas = new List<T>();
}
