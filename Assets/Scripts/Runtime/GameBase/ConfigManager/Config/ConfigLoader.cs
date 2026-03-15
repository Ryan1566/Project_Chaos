using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 该脚本用于config读取
/// </summary>
public class ConfigLoader : SingletonBase<ConfigLoader>
{
    /// <summary>
    /// 加载config
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public DataList<T> LoadConfig<T>()
    {
        string json = Resources.Load<TextAsset>("Json/" + typeof(T).Name).text;
        DataList<T> dataList = JsonUtility.FromJson<DataList<T>>(json);
        return dataList;
    }
}
