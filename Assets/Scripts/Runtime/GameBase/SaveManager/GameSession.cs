using ChaosDebug;

/// <summary>
/// 当前存档会话：「玩家选了哪个档位、这份存档里有什么」，供**切场景之后**的玩法场景读取。
///
/// ══════════════ 为什么必须是【不继承 MonoBehaviour】的静态单例 ══════════════
/// 它要活过场景切换：玩家在 `SLPanel` 里点「开始游戏」，而真正消费这份数据的玩法场景
/// 是**另一个场景**。`SingletonMono` / `SingletonAutoMono` 都是 MonoBehaviour
/// （要么靠 Awake 赋值、要么 new GameObject + DontDestroyOnLoad），而本类没有任何
/// Unity 对象语义（没有 Transform、不需要 Update），所以用 `SingletonBase`（普通 C# 静态单例）
/// 最直接，也最不容易出错。见 `04_架构/05_存档槽位系统落地方案.md` §决策 9 ④。
///
/// ⚠ 注意它**不负责落盘**：写文件是 `SaveSlotService` 的事。本类只是"本次运行内的会话状态"，
/// 进程退出即消失 —— 这是有意的，别把它当成第二份存档。
/// </summary>
public class GameSession : SingletonBase<GameSession>
{
    /// <summary>当前选中的槽位（1..3）；未开始会话时为 0。</summary>
    public int SelectedSlot { get; private set; }

    /// <summary>当前槽位读出来的存档内容；未开始会话时为 null。</summary>
    public SavePayload LoadedPayload { get; private set; }

    /// <summary>是否已经进入过一次会话（`SelectedSlot != 0` 语义更明确的写法）。</summary>
    public bool HasSession { get; private set; }

    /// <summary>
    /// **读条场景的目标**：这次要切到哪个场景。
    ///
    /// ══════════════ 为什么放在这里 ══════════════
    /// 玩法入口现在是「存档面板 → `LoadingScene` → 目标场景」三段（`06_加载读条界面落地方案.md` §4）：
    /// 面板只负责把目标场景名**写进这个跨场景静态单例**，然后切到 `LoadingScene`；
    /// `LoadingController` 再读它决定要流式加载哪个场景。
    /// 这样 `LoadingScene` 对**任意**目标场景都可复用，不必为每个目标改一遍读条场景。
    ///
    /// 为空 = 没有读过存档就进了读条场景（异常路径）→ `LoadingController` 报 Error 并停在原地。
    /// </summary>
    public string TargetSceneName { get; private set; }

    /// <summary>
    /// 开始一次会话：记下槽位、读到的 payload，以及**要切到的目标场景**。
    /// 由面板在**写盘成功之后、切场景之前**调用。
    /// </summary>
    /// <param name="slot">槽位（1..3）</param>
    /// <param name="payload">该槽位的存档内容</param>
    /// <param name="targetSceneName">读条结束后要进入的场景名（如 "WorldScene"）</param>
    public void Begin(int slot, SavePayload payload, string targetSceneName)
    {
        SelectedSlot = slot;
        LoadedPayload = payload ?? new SavePayload();
        TargetSceneName = targetSceneName;
        HasSession = true;

        ChaosLog.Info(LogChannel.Save,
            "已进入存档会话：槽 " + slot + "，payload.version=" + LoadedPayload.version +
            "，目标场景=" + (string.IsNullOrEmpty(targetSceneName) ? "(未指定)" : targetSceneName));
    }

    /// <summary>
    /// 只设置目标场景（不开始会话）。用于「已经进过会话、只是想换个目标场景」的场合。
    /// 传空等同于清除。
    /// </summary>
    public void SetTargetScene(string targetSceneName)
    {
        TargetSceneName = string.IsNullOrEmpty(targetSceneName) ? null : targetSceneName;
    }

    /// <summary>清掉会话（例如将来「返回主菜单」时）。没有会话时调用是空操作。</summary>
    public void Clear()
    {
        if (!HasSession) return;

        ChaosLog.Info(LogChannel.Save, "已退出存档会话（原槽位 " + SelectedSlot + "）");

        SelectedSlot = 0;
        LoadedPayload = null;
        TargetSceneName = null;
        HasSession = false;
    }
}
