using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;

/// <summary>
/// 常驻对象标记（挂在 `EnterScene` 的三个根上：`UIRoot` / `CameraRoot` / `Managers`）。
///
/// ══════════════════ 为什么需要幂等守卫（task-24 修） ══════════════════
/// `EnterScene` 是**会被反复加载**的（局内 → 读条 → 回 EnterScene），而 `DontDestroyOnLoad`
/// 的对象在切场景时**不会消失** —— 于是第二次加载 `EnterScene` 时，场景里那份新拷贝会和
/// 上一份常驻对象**同时存在**，后果都是实打实的：
///   · 双 `UIRoot` → 双 `Canvas` → `UIManager.FindCanvas()`（按名字 `GameObject.Find("Canvas")`）
///     可能拿到错的那一个，之后所有面板都挂到废弃画布上；
///   · 双 `CameraRoot` → 双 `AudioListener` → Unity 每帧刷 "There are 2 audio listeners"；
///   · 双 `Managers` → 双 `LocalizationManager`/`AudioManager`，事件订阅翻倍。
/// 所以：**已存在同 key 的活实例时，后来这一份自我销毁**。
///
/// ⚠ `persistKey` 用 `[SerializeField]` 暴露，而不是在代码里写死 `UIRoot/Managers` 名字列表：
///   常驻对象的"身份"应当由场景配置决定，代码不该硬编码节点名。
///   留空 = 用 `gameObject.name`（当前三个都没填，按名字区分已足够）。
/// </summary>
public class DontDestroy : MonoBehaviour
{
    /// <summary>常驻身份 key。留空 = 用 <see cref="GameObject.name"/>。</summary>
    [SerializeField] private string persistKey;

    /// <summary>
    /// 当前存活的常驻实例（key → 实例）。
    /// **静态** → 跨场景存活，正是判重所需的语义。
    /// </summary>
    private static readonly Dictionary<string, DontDestroy> Alive = new Dictionary<string, DontDestroy>();

    private string Key
    {
        get { return string.IsNullOrEmpty(persistKey) ? gameObject.name : persistKey; }
    }

    private void Awake()
    {
        string key = Key;

        DontDestroy existing;
        //existing != null 用的是 Unity 重载的 ==：已销毁的旧实例会等于 null，
        //所以"上一份已经没了"的情况会自然走到下面接管，不会把常驻能力锁死。
        if (Alive.TryGetValue(key, out existing) && existing != null && existing != this)
        {
            ChaosLog.Warn(LogChannel.UI,
                "[DontDestroy] 已存在同 key 的常驻实例「" + key + "」（" + existing.gameObject.name +
                "），销毁重复的 " + gameObject.name + "，避免双份 UI / 相机 / Manager");
            Destroy(gameObject);
            return;
        }

        Alive[key] = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        //只有"当前登记的那一个"才允许把自己从表里摘掉：
        //被销毁的重复实例不能误删原实例的登记，否则下一次加载就判不出重复了。
        DontDestroy existing;
        if (Alive.TryGetValue(Key, out existing) && existing == this) Alive.Remove(Key);
    }
}
