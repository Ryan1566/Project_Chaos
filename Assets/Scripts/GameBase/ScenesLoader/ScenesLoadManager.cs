using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class ScenesLoadManager : SingletonBase<ScenesLoadManager>
{
    /// <summary>
    /// 同步加载场景
    /// </summary>
    /// <param name="name"></param>
    /// <param name="action"></param>
    public void LoadScene(string name,UnityAction action)
    {
        SceneManager.LoadScene(name);
        action();
    }

    /// <summary>
    /// 异步加载场景
    /// </summary>
    /// <param name="name"></param>
    /// <param name="action"></param>
    public void LoadSceneAsync(string name,UnityAction action)
    {
        MonoManager.Instance.StartCoroutine(LoadSceneAsyncFunc(name,action));
    }

    //异步加载协程
    private IEnumerator LoadSceneAsyncFunc(string name,UnityAction action)
    {
        AsyncOperation ao = SceneManager.LoadSceneAsync(name);
        while (!ao.isDone)
        {
            this.TriggerEvent(EventConstName.LoadProgress, new LoadingEventArgs
            {
                a_operation = ao
            });
            yield return ao.progress;//获取加载进度
        }

        action();
    }
}
