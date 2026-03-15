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

//泛型基类
[Serializable]
public class DataList<T>
{
    public List<T> datas = new List<T>();
}