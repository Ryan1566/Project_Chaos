using System;
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
        string json = ResManager.Instance.Load<TextAsset>("Json/" + typeof(T).Name).text;
        DataList<T> dataList = JsonUtility.FromJson<DataList<T>>(json);
        return dataList;
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
}
