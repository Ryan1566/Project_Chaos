using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//资源加载管理器
public class ResManager : SingletonBase<ResManager>
{
    #region 同步加载资源
    /// <summary>
    /// 同步加载资源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <returns></returns>
    public T Load<T>(string name)where T : Object
    {
        T res = Resources.Load<T>(name);
        if (res is GameObject)
        {
            return GameObject.Instantiate(res);
            //return res;
        }
        else
        {
            return res;
        }
    }
    #endregion

    #region 异步加载资源
    /// <summary>
    /// 异步加载资源
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <param name="callback"></param>
    public void LoadAsync<T>(string name,UnityAction<T> callback) where T : Object
    {
        MonoManager.Instance.StartCoroutine(LoadAsyncFunc(name,callback));
    }

    /// <summary>
    /// 异步加载协程
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    private IEnumerator LoadAsyncFunc<T>(string name,UnityAction<T> callback) where T : Object
    {
        ResourceRequest res = Resources.LoadAsync<T>(name);
        yield return res;

        if(res.asset is GameObject)
        {
            callback(GameObject.Instantiate(res.asset) as T);
        }
        else
        {
            callback(res.asset as T);
        }
    }
    #endregion
}
