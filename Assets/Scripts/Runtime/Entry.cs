using System.Collections;
using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 启动入口（挂在 `EnterScene` 的 `Managers/GameManager` 上）。
///
/// ══════════════════ 两种进入方式（task-24 起） ══════════════════
/// ① **首次启动**：做一次性的框架初始化（UIManager / InputManager / SettingsManager），并推主菜单；
/// ② **二次进入**：从局内读完条回到 `EnterScene` —— 初始化**不能再来一遍**
///    （`SettingsManager.Init` 会重复订阅、`InputManager.Init` 会重复加载动作表），
///    只按 `GameSession.HasSession` 路由（有会话 → MainScene 村庄；没有 → 主菜单）。
///
/// ══════════════════ ⚠ 为什么二次路由不写在 Start 里（实测层级决定的） ══════════════════
/// `EnterScene` 的实际层级是：
///     CameraRoot(DontDestroy) / Directional Light / UIRoot(DontDestroy) / Managers(DontDestroy)
///     Managers 的子物体：LocalizationManager / **GameManager（Entry 就挂在这里）** / AudioManager
/// 也就是说 **Entry 所在的 GameManager 是常驻根 `Managers` 的子物体**。
/// 二次加载 `EnterScene` 时，新来的那份 `Managers` 会被 `DontDestroy` 的幂等守卫销毁，
/// **它的子物体 GameManager 跟着一起销毁 → 新 Entry 的 `Start` 根本不会执行**。
/// 所以二次路由挂在**静态的 `SceneManager.sceneLoaded`** 回调上：静态委托不依赖场景里任何对象存活，
/// `EnterScene` 一加载完就能跑。
/// （`Start` 里保留同一条路由作为兜底：若将来 GameManager 被移出 Managers 子树，那条路径同样正确。）
/// </summary>
public class Entry : MonoBehaviour
{
    /// <summary>回局外时要路由的场景名。</summary>
    private const string EnterSceneName = "EnterScene";

    /// <summary>
    /// 是否已经做过一次启动初始化。
    /// **静态** → 跨场景存活，正是"只初始化一次"所需的语义。
    /// </summary>
    private static bool _booted;

    private void Start()
    {
        if (!_booted)
        {
            _booted = true;

            //只注册一次：静态回调 + 静态字段，不依赖任何场景对象存活（见类注释）
            SceneManager.sceneLoaded += OnSceneLoaded;

            UIManager.Instance.OnInit();

            // 顺序有讲究：输入动作表要先加载好，SettingsManager 才能把存档里的按键绑定覆盖
            // 喂给它（ApplyToRuntime 里的 ApplyInput 会调 InputManager.LoadOverridesJson）。
            InputManager.Instance.Init();
            SettingsManager.Instance.Init();
            if (GetComponent<MetaRuntimeDriver>() == null) gameObject.AddComponent<MetaRuntimeDriver>();

            UIManager.Instance.PushPanel(PanelType.MainMenuPanel);
            return;
        }

        //兜底路径：二次进入且本对象仍存活时（正常情况它已被常驻守卫连带销毁，见类注释）
        if (gameObject.scene.name == EnterSceneName) RouteBySession("Entry.Start 兜底");
    }

    /// <summary>场景加载完成的**静态**回调：只关心"回到 EnterScene"这一件事。</summary>
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_booted) return;                      //首次启动走 Start，不走这里
        if (scene.name != EnterSceneName) return;  //只在回局外时路由

        RouteBySession("sceneLoaded(" + scene.name + ")");
    }

    /// <summary>按会话状态选择村庄场景或主菜单；地图只由玩家主动打开。</summary>
    private static void RouteBySession(string reason)
    {
        if (UIManager.Instance == null)
        {
            ChaosLog.Error(LogChannel.UI, "[Entry] UIManager 不存在，无法路由面板（" + reason + "）");
            return;
        }

        if (GameSession.Instance.IsSandbox) GameSession.Instance.Clear();
        bool hasSession = GameSession.Instance.IsPersistent;
        if (VillageSceneFlow.RecoveringToSaveSlots) { VillageSceneFlow.ShowEntryFailure(); return; }
        if (hasSession)
        {
            if (VillageSceneFlow.IsLoading) return;
            try { VillageSceneFlow.EnterFromSave(); }
            catch (System.Exception exception)
            {
                ChaosLog.Error(LogChannel.Scene, "[Entry] 返回村庄失败，已保留当前存档会话，可重新选择槽位重试：" + exception.Message);
                UIManager.Instance.SetRootPanel(PanelType.SLPanel);
            }
            return;
        }
        ChaosLog.Info(LogChannel.UI, "[Entry] 无存档会话 → 主菜单（" + reason + "）");
        UIManager.Instance.SetRootPanel(PanelType.MainMenuPanel);
    }
}
