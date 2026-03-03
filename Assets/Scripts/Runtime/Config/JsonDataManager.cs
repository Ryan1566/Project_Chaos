using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//Json数据读取器
public class JsonDataManager : SingletonBase<JsonDataManager>
{
    /// <summary>
    /// 同步加载配置
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public JsonDataList<T> LoadData<T>()
    {
        string json = ResManager.Instance.Load<TextAsset>("Data/Json/" + typeof(T).Name).text;//同步加载方案
        Debug.Log("文件已同步解析完毕：" + typeof(T).Name);
        return JsonUtility.FromJson<JsonDataList<T>>(json);
    }

    /// <summary>
    /// 异步加载配置
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="OnLoadCompleted"></param>
    public void LoadDataAsync<T>(UnityAction<JsonDataList<T>> OnLoadCompleted)
    {
        ResManager.Instance.LoadAsync<TextAsset>("Data/Json/" + typeof(T).Name, (file) => {
            JsonDataList<T> result = null;
            if (file != null && file is TextAsset textAsset)//模式匹配 如果是TextAsset则自动转换并赋值为textAsset
            {
                result = JsonUtility.FromJson<JsonDataList<T>>(textAsset.text);
                Debug.Log("文件已异步解析完毕：" + typeof(T).Name);
            }
            else
            {
                Debug.Log("解析失败，文件丢失或类型异常：" + typeof(T).Name);
            }
            OnLoadCompleted(result);
        });

    }
}

/// <summary>
/// 配置数据的泛型基类
/// </summary>
/// <typeparam name="T">指定程序类</typeparam>
[Serializable]
public class JsonDataList<T>
{
    public List<T> datas = new List<T>();
}