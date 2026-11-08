using System;
using System.Collections.Generic;
using System.Linq;

public readonly struct BattleWarehouseRow
{
    public readonly uint Id, Stock, Selected;
    public BattleWarehouseRow(uint id, uint stock, uint selected) { Id = id; Stock = stock; Selected = selected; }
}

public interface IBattlePreparationView
{
    void Render(IReadOnlyList<BattleWarehouseRow> warehouse, IReadOnlyList<BattleSlotSave> slots);
    void ShowMessage(string message, bool success);
    void ShowQuantity(uint id, uint current, uint maximum, Action<uint> confirm);
    void ShowPresets(IReadOnlyList<BattlePresetSave> presets, int selected);
    void HidePresets();
    void Confirm(string message, Action confirm);
}

/// <summary>处理配装意图和手势时序，视图不接触仓库写入。</summary>
public sealed class BattlePreparationPresenter
{
    private const double TimingTolerance = 0.000000001;
    private readonly BattleLoadout draft;
    private readonly IBattlePreparationView view;
    private uint pressedId, lastItem;
    private int preset = -1, lastPreset = -1;
    private double nextRepeat, lastItemAt, lastPresetAt;
    private bool repeating;

    public BattlePreparationPresenter(BattleLoadout draft, IBattlePreparationView view)
    { this.draft = draft; this.view = view; }

    public void Refresh()
    {
        if (!draft.IsCurrent) return;
        var ids = new HashSet<uint>(InventoryService.Instance.GetStacks().Where(stack => stack != null && stack.count > 0)
            .Select(stack => stack.itemId));
        ids.UnionWith(RunBagProgression.OwnedItems());
        var warehouse = ids.OrderBy(id => id).Where(id => BattleLoadout.Definition(id) != null)
            .Select(id => new BattleWarehouseRow(id, BattleLoadout.Available(id), draft.Count(id)))
            .Where(row => row.Stock > 0).ToList();
        view.Render(warehouse, draft.Snapshot());
    }

    public void BeginPress(uint id, double now, bool ctrl = false)
    {
        pressedId = id;
        nextRepeat = ctrl ? double.PositiveInfinity : now + .4;
        repeating = false;
        if (ctrl) lastItem = 0;
    }

    public void Repeat(double now)
    {
        if (pressedId == 0 || now + TimingTolerance < nextRepeat) return;
        repeating = true;
        lastItem = 0;
        do
        {
            nextRepeat += .1;
            if (!Add(pressedId, false, quietAtLimit: true)) nextRepeat = double.PositiveInfinity;
        }
        while (pressedId != 0 && now + TimingTolerance >= nextRepeat);
    }

    public void Release(uint id, bool all, double now)
    {
        if (pressedId != id) return;
        bool wasRepeat = repeating;
        pressedId = 0;
        repeating = false;
        if (wasRepeat) return;
        if (all) { lastItem = 0; Add(id, true); return; }
        double elapsed = now - lastItemAt;
        if (id == lastItem && elapsed <= .4 + TimingTolerance)
        {
            if (elapsed + TimingTolerance < .1) return;
            lastItem = 0;
            view.ShowQuantity(id, draft.Count(id), draft.Maximum(id), value => Set(id, value));
            return;
        }
        lastItem = id;
        lastItemAt = now;
        Add(id, false);
    }

    public void CancelPress() { pressedId = 0; repeating = false; }
    public void ResetGestures() { CancelPress(); lastItem = 0; lastPreset = -1; }
    public bool CanDrag => !repeating;

    public bool Add(uint id, bool all, RunBagSection? section = null, int index = -1, bool quietAtLimit = false)
    {
        uint maximum = draft.Maximum(id);
        uint current = draft.Count(id);
        if (current >= maximum)
        {
            if (!quietAtLimit) view.ShowMessage("库存不足或已达到叠加上限", false);
            return false;
        }
        return Set(id, all ? maximum : current + 1, section, index);
    }

    public void Remove(uint id, bool entire)
    {
        uint current = draft.Count(id);
        if (current != 0) Set(id, entire ? 0 : current - 1);
    }

    private bool Set(uint id, uint count, RunBagSection? section = null, int index = -1)
    {
        if (!draft.SetQuantity(id, count, out string reason, section, index))
        { view.ShowMessage(reason, false); return false; }
        Refresh();
        return true;
    }

    public void Move(RunBagSection from, int fromIndex, RunBagSection to, int toIndex)
    {
        if (!draft.Move(from, fromIndex, to, toIndex, out string reason)) view.ShowMessage(reason, false);
        else Refresh();
    }

    public void OpenPresets()
    { ResetGestures(); preset = -1; RefreshPresets(); }

    private void RefreshPresets() => view.ShowPresets(draft.Presets(), preset);

    public void SelectPreset(int index, double now)
    {
        var options = draft.Presets();
        if (index < 0 || index >= options.Count) return;
        double elapsed = now - lastPresetAt;
        if (lastPreset == index && elapsed <= .5 + TimingTolerance)
        {
            if (elapsed + TimingTolerance < .1) return;
            lastPreset = -1;
            if (options[index].saved) LoadPreset();
            return;
        }
        preset = lastPreset = index;
        lastPresetAt = now;
        RefreshPresets();
    }

    public void SavePreset()
    {
        var options = draft.Presets();
        if (preset < 0 || preset >= options.Count) return;
        int index = preset;
        lastPreset = -1;
        if (options[index].saved) view.Confirm("覆盖这个档位保存的组合？", () => ChangePreset(index, false, false));
        else ChangePreset(index, false, false);
    }

    public void RemovePreset()
    {
        if (preset < 0 || draft.Presets().Count <= BattleLoadout.MinPresets) return;
        int index = preset;
        lastPreset = -1;
        view.Confirm("删除选中的组合档位？", () => ChangePreset(index, false, true));
    }

    public void AddPreset() => ChangePreset(-1, true, false);

    private void ChangePreset(int index, bool add, bool remove)
    {
        lastPreset = -1;
        if (!draft.ChangePreset(index, add, remove, out string reason)) view.ShowMessage(reason, false);
        else
        {
            int count = draft.Presets().Count;
            preset = add ? count - 1 : Math.Min(index, count - 1);
            view.ShowMessage(remove ? "已删除档位" : add ? "已增加档位" : "已保存当前组合", true);
        }
        RefreshPresets();
    }

    public void LoadPreset()
    {
        lastPreset = -1;
        if (!draft.LoadPreset(preset, out string message)) { view.ShowMessage(message, false); return; }
        view.HidePresets();
        Refresh();
        view.ShowMessage(message, true);
    }

    public void EnterRun()
    {
        ResetGestures();
        if (!BattlePreparationFlow.Start(draft, out string reason)) view.ShowMessage(reason, false);
    }
}
