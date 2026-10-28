using System;
using ChaosDebug;
using UnityEngine;

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
    public enum SessionKind { None, Persistent, Sandbox }
    public SessionKind Kind { get; private set; }
    public int ContextId { get; private set; }
    public bool IsPersistent => Kind == SessionKind.Persistent;
    public bool IsSandbox => Kind == SessionKind.Sandbox;
    private string saveDirectory;
    public event Action ContextChanged;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetForPlay()
    {
        var state = Instance;
        state.Kind = SessionKind.None; state.HasSession = false; state.SelectedSlot = 0;
        state.LoadedPayload = null; state.TargetSceneName = null; state.saveDirectory = null;
        state.ContextId++; state.ContextChanged = null;
        MetaProgressService.Instance.ResetSessionRuntime();
    }
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
        if (MetaCommandRunner.IsExecuting) throw new InvalidOperationException("玩法命令执行中不能切换存档");
        if (!SaveSlotService.IsValidSlot(slot) || payload == null) throw new ArgumentException("正式会话需要有效槽位和已读取的数据");
        Kind = SessionKind.Persistent;
        saveDirectory = SaveSlotService.SavesDirectory;
        ContextId++;
        EventService.Instance.ResetRuntime();
        MetaProgressService.Instance.ResetSessionRuntime();
        DispatchService.Instance.ResetSelection();
        SelectedSlot = slot;
        LoadedPayload = payload ?? new SavePayload();
        TargetSceneName = targetSceneName;
        HasSession = true;
        ContextChanged?.Invoke();

        ChaosLog.Info(LogChannel.Save,
            "已进入存档会话：槽 " + slot + "，payload.version=" + LoadedPayload.version +
            "，目标场景=" + (string.IsNullOrEmpty(targetSceneName) ? "(未指定)" : targetSceneName));
    }

    /// <summary>Explicit prototype context. Its data never writes a player slot or aliases a real payload.</summary>
    public void BeginSandbox(SavePayload initial = null)
    {
        if (MetaCommandRunner.IsExecuting) throw new InvalidOperationException("玩法命令执行中不能切换会话");
        EventService.Instance.ResetRuntime(); MetaProgressService.Instance.ResetSessionRuntime(); DispatchService.Instance.ResetSelection();
        Kind = SessionKind.Sandbox; SelectedSlot = 0; saveDirectory = null; TargetSceneName = null;
        LoadedPayload = initial == null ? new SavePayload() : JsonUtility.FromJson<SavePayload>(JsonUtility.ToJson(initial));
        HasSession = true; ContextId++; ContextChanged?.Invoke();
    }

    public bool CanCommit(out string reason)
    {
        reason = null;
        if (!HasSession || LoadedPayload == null) { reason = "没有活动会话，请先读档或显式开始测试会话"; return false; }
        if (IsPersistent && (!SaveSlotService.IsValidSlot(SelectedSlot) || saveDirectory != SaveSlotService.SavesDirectory))
        { reason = "存档目录或槽位已改变，请重新读取存档"; return false; }
        return true;
    }

    public bool SaveCurrent(out string reason)
    {
        if (!CanCommit(out reason)) return false;
        if (IsSandbox) return true; // An in-memory commit, never an implicit slot 3 save.
        bool saved = SaveSlotService.Instance.Save(SelectedSlot, LoadedPayload);
        reason = saved ? null : "保存失败，请检查磁盘后重试（槽 " + SelectedSlot + "）";
        return saved;
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
        if (MetaCommandRunner.IsExecuting) throw new InvalidOperationException("玩法命令执行中不能退出会话");
        if (!HasSession) return;

        EventService.Instance.ResetRuntime();

        ChaosLog.Info(LogChannel.Save, "已退出存档会话（原槽位 " + SelectedSlot + "）");

        SelectedSlot = 0;
        LoadedPayload = null;
        TargetSceneName = null;
        HasSession = false;
        Kind = SessionKind.None; saveDirectory = null; ContextId++;
        MetaProgressService.Instance.ResetSessionRuntime();
        DispatchService.Instance.ResetSelection();
        ContextChanged?.Invoke();
    }
}
