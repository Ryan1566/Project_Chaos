using ChaosDebug;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 该脚本用于config读取
/// </summary>
public class ConfigLoader : SingletonBase<ConfigLoader>
{
    /// <summary>
    /// 同步加载配置
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public DataList<T> LoadData<T>()
    {
        string json = ResManager.Instance.Load<TextAsset>(GlobalPath.data_JsonPathToRead + typeof(T).Name).text;//同步加载方案
        ChaosLog.Info(LogChannel.Config, "文件已同步解析完毕：" + typeof(T).Name);
        return JsonUtility.FromJson<DataList<T>>(json);
    }

    /// <summary>
    /// 异步加载配置
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="OnLoadCompleted"></param>
    public void LoadDataAsync<T>(UnityAction<DataList<T>> OnLoadCompleted)
    {
        ResManager.Instance.LoadAsync<TextAsset>(GlobalPath.data_JsonPathToRead + typeof(T).Name, (file) => {
            DataList<T> result = null;
            if (file != null && file is TextAsset textAsset)//模式匹配 如果是TextAsset则自动转换并赋值为textAsset
            {
                result = JsonUtility.FromJson<DataList<T>>(textAsset.text);
                ChaosLog.Info(LogChannel.Config, "文件已异步解析完毕：" + typeof(T).Name);
            }
            else
            {
                ChaosLog.Warn(LogChannel.Config, "解析失败，文件丢失或类型异常：" + typeof(T).Name);
            }
            OnLoadCompleted(result);
        });
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