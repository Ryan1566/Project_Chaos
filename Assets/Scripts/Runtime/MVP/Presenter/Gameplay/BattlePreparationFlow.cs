using UnityEngine;

/// <summary>跨界面保留当前战备草稿，直到入局成功或关闭整个派遣流程。</summary>
public static class BattlePreparationFlow
{
    private static BattleLoadout draft;
    public static bool IsStarting { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    internal static void Clear() { draft = null; IsStarting = false; }

    internal static void DiscardWhenDispatchClosed()
    {
        if (!IsStarting && !UIManager.Instance.HasPanelInStack(PanelType.DispatchPanel)) Clear();
    }

    internal static BattleLoadout GetDraft()
    {
        if (draft == null || !draft.IsCurrent) draft = new BattleLoadout();
        return draft;
    }

    internal static bool Open()
    {
        GetDraft();
        return UIManager.Instance.PushPanel(PanelType.BattlePreparationPanel) != null;
    }

    internal static bool Start(BattleLoadout loadout, out string reason)
    {
        if (IsStarting) { reason = "正在准备入局"; return false; }
        IsStarting = true;
        if (DispatchService.Instance.DepartPrepared(loadout, out reason)) return true;
        IsStarting = false;
        return false;
    }

    internal static void ReopenAfterFailure(string reason)
    {
        IsStarting = false;
        UIManager.Instance.PushPanel(PanelType.DispatchPanel);
        if (Open() && UIManager.Instance.panelStack.Peek() is BattlePreparationPanel battle)
            battle.ShowMessage(reason, false);
    }
}
