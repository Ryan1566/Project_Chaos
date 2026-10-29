using System;
using System.Collections.Generic;

/// <summary>Persistent unlocks and unique ownership; gameplay content supplies stable grant source IDs.</summary>
public static class RunBagProgression
{
    public static int GetCapacity(RunBagSection section)
    {
        var p = MetaProgressService.Instance.Payload;
        switch (section)
        {
            case RunBagSection.Important: return UnityEngine.Mathf.Clamp(p.runImportantSlots, 0, 5);
            case RunBagSection.Engineering: return UnityEngine.Mathf.Clamp(p.runEngineeringSlots, 0, 7);
            case RunBagSection.Recovery: return UnityEngine.Mathf.Clamp(p.runRecoverySlots, 0, 7);
            case RunBagSection.Misc: return UnityEngine.Mathf.Clamp(p.runMiscSlots, 0, 30);
            default: return 0;
        }
    }
    public static bool Owns(uint id)
    { return MetaProgressService.Instance.Payload.ownedImportantItems?.Contains(id) == true; }
    public static IReadOnlyList<uint> OwnedItems()
    {
        var result = new List<uint>(new HashSet<uint>(MetaProgressService.Instance.Payload.ownedImportantItems ?? new List<uint>()));
        result.Sort(); return result;
    }
    public static bool AcquireImportant(uint id, out string reason)
    {
        reason = null;
        if (!GameSession.Instance.HasSession) { reason = "没有有效存档会话"; return false; }
        var def = InventoryService.Instance.GetDefinition(id);
        if (def == null || !def.Enable || def.RunCategory != 1) { reason = "不是有效的重要道具"; return false; }
        if (Owns(id)) return true;
        if (MetaCommandRunner.IsExecuting)
        {
            reason = "请通过外层玩法命令授予永久道具";
            return false;
        }
        return MetaCommandRunner.Execute("RunBag.AcquireImportant", (out string why) => {
            RegisterImportantInCommand(id); why = null; return true;
        }, out reason);
    }
    // Outside event/reward commands already own persistence; never nest a second save transaction.
    public static void RegisterImportantInCommand(uint id)
    {
        if (!MetaCommandRunner.IsMutating) throw new InvalidOperationException("Permanent ownership requires a gameplay mutation");
        var p = MetaProgressService.Instance.Payload;
        if (p.ownedImportantItems == null) p.ownedImportantItems = new List<uint>();
        if (!p.ownedImportantItems.Contains(id)) p.ownedImportantItems.Add(id);
    }
    public static bool GrantSlots(RunBagSection section, int amount, string sourceId, out string reason)
    {
        reason = null;
        if (!GameSession.Instance.HasSession) { reason = "没有有效存档会话"; return false; }
        if ((uint)section > 3 || amount <= 0 || string.IsNullOrWhiteSpace(sourceId))
        { reason = "扩格区域、数量或来源无效"; return false; }
        if (MetaCommandRunner.IsExecuting && !MetaCommandRunner.IsMutating)
        { reason = "提交或通知期间不能授予格位"; return false; }
        string receipt = ((int)section) + ":" + sourceId;
        var p = MetaProgressService.Instance.Payload;
        if (p.runSlotGrants != null && p.runSlotGrants.Contains(receipt)) return true;
        int current = GetCapacity(section), max = RunInventory.GetMaximum(section);
        if (current >= max) { reason = "该区域已达到格位上限"; return false; }
        int target = (int)Math.Min((long)current + amount, max);
        if (MetaCommandRunner.IsExecuting)
        {
            ApplyGrant(section, target, receipt);
            return true;
        }
        return MetaCommandRunner.Execute("RunBag.GrantSlots", (out string why) => {
            ApplyGrant(section, target, receipt);
            why = null; return true;
        }, out reason);
    }
    private static void ApplyGrant(RunBagSection section, int target, string receipt)
    {
            var payload = MetaProgressService.Instance.Payload;
            switch (section)
            {
                case RunBagSection.Important: payload.runImportantSlots = target; break;
                case RunBagSection.Engineering: payload.runEngineeringSlots = target; break;
                case RunBagSection.Recovery: payload.runRecoverySlots = target; break;
                case RunBagSection.Misc: payload.runMiscSlots = target; break;
            }
            if (payload.runSlotGrants == null) payload.runSlotGrants = new List<string>();
            payload.runSlotGrants.Add(receipt);
    }
}
