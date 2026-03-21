using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Json数据读取器
public class JsonDataConfigManager : SingletonBase<JsonDataConfigManager>
{
    public DataList<T> LoadData<T>()
    {
        //string json = Resources.Load<TextAsset>("Json/" + typeof(T).Name).text;
        string json = ResManager.Instance.Load<TextAsset>("Json/" + typeof(T).Name).text;
        return JsonUtility.FromJson<DataList<T>>(json);
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