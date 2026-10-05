using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using ChaosDebug;

//场景加载管理器
public class ScenesLoadManager : SingletonBase<ScenesLoadManager>
{
    #region 同步加载场景
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
    #endregion

    #region 异步加载资源
    /// <summary>
    /// 异步加载场景
    /// </summary>
    /// <param name="name"></param>
    /// <param name="action"></param>
    public void LoadSceneAsync(string name,UnityAction action)
    {
        MonoManager.Instance.StartCoroutine(LoadSceneAsyncFunc(name,action));
    }

    /// <summary>
    /// 异步加载协程
    /// </summary>
    /// <param name="name"></param>
    /// <param name="action"></param>
    /// <returns></returns>
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
    #endregion

    #region 可持控的异步加载（读条界面用）
    /// <summary>
    /// 开始异步加载场景，并把 AsyncOperation 交给调用方【自己持控进度与激活时机】。
    ///
    /// ══════════════ 与上面两个入口的区别 ══════════════
    ///   · LoadScene      = 同步，切完再回调；
    ///   · LoadSceneAsync = 协程里一口气加载完并激活，调用方拿不到进度；
    ///   · 本方法         = 返回 AsyncOperation，且默认 allowSceneActivation=false
    ///                      → 场景资源加载完但【不激活】，调用方可以等"进度条满 + 轮播图就绪"
    ///                        再自己把 allowSceneActivation 设为 true。
    ///
    /// 【注意】Unity 的进度语义（调用方必须知道）：allowSceneActivation=false 时 progress 最大只到 0.9；
    ///   一旦把 allowSceneActivation 设为 true，progress 会【直接变成 1.0】。
    ///   所以调用方做 「progress / 0.9」归一化时【必须 Clamp01】，否则进度会溢出到 1.11。
    ///
    /// 【注意】本方法不碰任何"读条界面"的事，也不触发事件：保持 SceneManager 调用集中在这一个类里，
    ///   读条逻辑归 LoadingController。EventConstName.LoadProgress 保持现状（0 订阅，不接）。
    /// </summary>
    /// <param name="name">场景名（必须在 Build Settings 的 Scenes In Build 里）</param>
    /// <param name="activateOnComplete">true = 加载完自动激活（等价于旧的异步行为）；false = 等调用方自己激活</param>
    /// <returns>AsyncOperation；场景名为空或不在 Build Settings 时返回 null（并报 Error），不抛异常</returns>
    public AsyncOperation BeginLoadSceneAsync(string name, bool activateOnComplete = false)
    {
        if (string.IsNullOrEmpty(name))
        {
            ChaosLog.Error(LogChannel.Scene, "BeginLoadSceneAsync：场景名为空，已中止加载");
            return null;
        }

        //预检（软）：按工程约定 Assets/Scenes/<名>.unity 反查 Build Settings 索引；查不到只 Warn 不拦。
        //【注意】不要用 Application.CanStreamedLevelBeLoaded 做硬门禁 —— 本机 Unity 2022.3.57f1c2 实测：
        //   它对【已经在 Build Settings 里的场景】在编辑器下也返回 false（TestScene 实测 False），
        //   拿它当守卫会把合法场景直接挡掉，比不检查更糟。
        int buildIndex = SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + name + ".unity");
        if (buildIndex < 0)
        {
            ChaosLog.Warn(LogChannel.Scene,
                "BeginLoadSceneAsync：未能按约定路径 Assets/Scenes/" + name + ".unity 反查到 Build Settings 条目" +
                "（场景可能在别的目录）；仍会尝试加载");
        }

        AsyncOperation ao;
        try
        {
            ao = SceneManager.LoadSceneAsync(name);
        }
        catch (System.Exception e)
        {
            //场景不在 Build Settings 时 LoadSceneAsync 会抛 ArgumentException —— 这里兜住，
            //让读条界面能"停在原地并提示"，而不是把异常抛到 UI 层
            ChaosLog.Error(LogChannel.Scene,
                "BeginLoadSceneAsync：加载场景 '" + name + "' 失败（是否已加入 Build Settings 的 Scenes In Build？）：" + e.Message);
            return null;
        }

        if (ao == null)
        {
            ChaosLog.Error(LogChannel.Scene,
                "BeginLoadSceneAsync：SceneManager.LoadSceneAsync('" + name + "') 返回 null");
            return null;
        }

        if (!activateOnComplete) ao.allowSceneActivation = false;

        ChaosLog.Info(LogChannel.Scene,
            "已开始异步加载场景 '" + name + "'（activateOnComplete=" + activateOnComplete + "）");
        return ao;
    }
    #endregion
}
