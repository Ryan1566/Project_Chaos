#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Editor-only behavioral contracts. Requires Play and owns a new temporary save directory.</summary>
public static class RunBagVerification
{
    public static string Verify()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Run only in Play");
        string storage = Path.Combine(Path.GetTempPath(), "ChaosVillageIsolated-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(storage);
        SaveSlotService.UseEditorTestDirectory(storage);
        if (!string.Equals(Path.GetFullPath(SaveSlotService.SavesDirectory), Path.GetFullPath(storage), StringComparison.OrdinalIgnoreCase))
            throw new Exception("Isolation not bound");
        var report = new StringBuilder();
        Action<bool,string> check = (ok,name) => { if (!ok) throw new Exception("FAIL " + name); report.AppendLine("PASS " + name); };
        var defs = InventoryService.Instance.GetAllDefinitions();
        var added = new[] {
            new ItemConfig { Index=900001, RunCategory=2, StackLimit=9, Enable=true },
            new ItemConfig { Index=900002, RunCategory=1, IsUnique=true, StackLimit=99, Enable=true, BuyPrice=4, SellPrice=2 },
            new ItemConfig { Index=900003, RunCategory=3, StackLimit=8, Enable=true },
            new ItemConfig { Index=900004, RunCategory=0, StackLimit=7, Enable=true },
            new ItemConfig { Index=900005, RunCategory=1, IsUnique=false, StackLimit=99, Enable=true, BuyPrice=3, SellPrice=2 }
        };
        defs.AddRange(added);
        string reason;
        try
        {
            var legacy = JsonUtility.FromJson<SavePayload>("{\"version\":1,\"era\":1}");
            GameSession.Instance.BeginSandbox(legacy); RunInventory.BeginRun();
            check(RunInventory.GetCapacity(RunBagSection.Important)==0 && RunInventory.GetCapacity(RunBagSection.Engineering)==1
                && RunInventory.GetCapacity(RunBagSection.Recovery)==2 && RunInventory.GetCapacity(RunBagSection.Misc)==5, "old save defaults 0/1/2/5");
            check(RunInventory.GetMaximum(RunBagSection.Important)==5 && RunInventory.GetMaximum(RunBagSection.Engineering)==7
                && RunInventory.GetMaximum(RunBagSection.Recovery)==7 && RunInventory.GetMaximum(RunBagSection.Misc)==30, "hard limits 5/7/7/30");
            check(RunInventory.AddItem(900001,3,out reason) && RunInventory.AddItem(900003,2,out reason)
                && RunInventory.AddItem(900004,4,out reason), "incoming loot stored once");
            check(RunInventory.TryMove(RunBagSection.Misc,0,RunBagSection.Engineering,0,out reason), "engineering dragged to dedicated row");
            check(!RunInventory.TryMove(RunBagSection.Misc,1,RunBagSection.Engineering,0,out reason)
                && RunInventory.GetSlot(RunBagSection.Engineering,0).itemId==900001, "wrong category swap rejected atomically");
            check(RunInventory.TryMove(RunBagSection.Misc,1,RunBagSection.Recovery,0,out reason), "recovery dragged to dedicated row");
            check(RunInventory.AddItem(1,1,out reason) && RunInventory.TryMove(RunBagSection.Misc,0,RunBagSection.Recovery,0,out reason)
                && RunInventory.GetSlot(RunBagSection.Misc,0).itemId==900003 && RunInventory.GetSlot(RunBagSection.Recovery,0).itemId==1, "occupied slot swaps both stacks");
            check(RunInventory.GetCount(900001)==3 && RunInventory.GetCount(900003)==2 && RunInventory.GetCount(1)==1, "moves preserve counts without duplication");
            check(!RunInventory.AddItem(900001,7,out reason) && !RunInventory.AddItem(900001,uint.MaxValue,out reason)
                && RunInventory.GetCount(900001)==3, "stack cap and overflow reject without partial add");
            check(!RunInventory.AddItem(0,1,out reason) && !RunInventory.AddItem(1,0,out reason), "invalid definition and zero quantity rejected");
            check(!RunInventory.TryMove(RunBagSection.Misc,0,RunBagSection.Important,0,out reason), "locked dedicated slot rejects drop");
            var snapshot=RunInventory.Stacks; snapshot[0].count=500;
            check(RunInventory.GetCount(900001)==3, "read snapshots cannot mutate inventory");
            check(RunInventory.AddItem(900002,9,out reason) && RunInventory.GetCount(900002)==1 && RunBagProgression.Owns(900002), "important unique despite multi-count pickup");
            check(RunInventory.AddItem(900002,1,out reason) && RunInventory.GetCount(900002)==1, "repeat important pickup idempotent");
            check(!RunInventory.RemoveItem(900002,1,out reason) && !InventoryService.Instance.RemoveItem(900002,1,out reason), "important cannot consume/remove/sell");
            check(RunInventory.GrantSlots(RunBagSection.Important,2,"story-a",out reason)
                && RunInventory.GrantSlots(RunBagSection.Important,2,"story-a",out reason)
                && RunInventory.GetCapacity(RunBagSection.Important)==2, "grant source receipt prevents duplicate expansion");
            check(RunInventory.GrantSlots(RunBagSection.Misc,int.MaxValue,"event-cap",out reason)
                && RunInventory.GetCapacity(RunBagSection.Misc)==30, "large expansion clamps without overflow");
            check(!RunInventory.GrantSlots((RunBagSection)9,1,"bad",out reason)
                && !RunInventory.GrantSlots(RunBagSection.Misc,0,"bad",out reason), "invalid expansion rejected");
            bool lateGrant=true, lateOwnership=true;
            Action observer=()=>{
                lateGrant=RunInventory.GrantSlots(RunBagSection.Engineering,1,"late-notification",out string ignored);
                lateOwnership=InventoryService.Instance.AddItem(900005,1,out ignored);
            };
            MetaRuntimeDriver.Changed+=observer;
            try { check(MetaCommandRunner.Execute("QA.NotificationBoundary",(out string why)=>{why=null;return true;},out reason),"outer notification command commits"); }
            finally { MetaRuntimeDriver.Changed-=observer; }
            check(!lateGrant&&!lateOwnership&&RunInventory.GetCapacity(RunBagSection.Engineering)==1&&!RunBagProgression.Owns(900005),
                "post-save notification cannot join mutation or grant unsaved ownership");
            check(MetaCommandRunner.Execute("QA.EventReward",(out string why)=>{
                bool a=InventoryService.Instance.AddItem(900005,5,out why);
                bool b=RunInventory.GrantSlots(RunBagSection.Engineering,2,"event-nested",out why); return a&&b;
            },out reason) && RunBagProgression.Owns(900005) && RunInventory.GetCapacity(RunBagSection.Engineering)==3,
                "event/upbringing grants join outer command without nested save");
            var persisted=JsonUtility.FromJson<SavePayload>(JsonUtility.ToJson(GameSession.Instance.LoadedPayload));
            RunInventory.Clear(); RunInventory.BeginRun();
            check(RunInventory.GetCount(900002)==1 && RunInventory.GetCount(900005)==1 && RunInventory.GetCount(900001)==0, "new run restores permanent items only");
            GameSession.Instance.BeginSandbox(persisted); RunInventory.BeginRun();
            check(RunInventory.GetCapacity(RunBagSection.Important)==2 && RunInventory.GetCapacity(RunBagSection.Misc)==30
                && RunInventory.GetCount(900002)==1, "save roundtrip retains ownership and capacity receipts");
            GameSession.Instance.BeginSandbox(new SavePayload()); RunInventory.BeginRun();
            check(RunInventory.UsedSlots==0 && !RunBagProgression.Owns(900002), "new session does not leak previous bag or permanent ownership");
            GameSession.Instance.Clear();
            var payload=new SavePayload {resMoney=10};
            check(SaveSlotService.Instance.Save(3,payload), "owned isolated persistent fixture created");
            GameSession.Instance.Begin(3,payload,"MainScene"); RunInventory.BeginRun();
            string slot=SaveSlotService.Instance.GetSlotPath(3);
            var before=File.ReadAllBytes(slot);
            using(var file=new FileStream(slot,FileMode.Open,FileAccess.Read,FileShare.None))
            {
                check(!RunInventory.AddItem(900002,1,out reason) && !RunBagProgression.Owns(900002) && RunInventory.GetCount(900002)==0,
                    "EXPECTED_IO important save failure leaves ownership and physical bag unchanged");
                check(!RunInventory.GrantSlots(RunBagSection.Important,1,"disk-lock",out reason)
                    && RunInventory.GetCapacity(RunBagSection.Important)==0
                    && !GameSession.Instance.LoadedPayload.runSlotGrants.Contains("0:disk-lock"), "EXPECTED_IO failed expansion rolls back capacity and receipt");
            }
            check(before.SequenceEqual(File.ReadAllBytes(slot)), "failed saves leave isolated slot bytes unchanged");
            check(RunInventory.AddItem(900002,1,out reason) && RunInventory.GrantSlots(RunBagSection.Important,1,"disk-lock",out reason), "unlock and retry succeeds once");
            check(SaveSlotService.Instance.TryLoad(3,out SaveSlotFile saved) && saved.payload.ownedImportantItems.Contains(900002)
                && saved.payload.runImportantSlots==1, "persistent important ownership and slots read back from disk");
            check(!MarketService.Instance.TryBuy(900002,out reason) && payload.resMoney==10, "owned unique item cannot be charged twice by market");
            GameSession.Instance.BeginSandbox(new SavePayload()); RunInventory.BeginRun();
            // Fill all available physical slots, then permanently acquire an item without dropping it.
            GameSession.Instance.LoadedPayload.resMoney=10;
            check(MarketService.Instance.TryBuy(900005,out reason) && RunBagProgression.Owns(900005)
                && MarketService.Instance.GetCount(900005)==1 && GameSession.Instance.LoadedPayload.resMoney==7,
                "first market important purchase commits ownership and charges once");
            check(!MarketService.Instance.TryBuy(900005,out reason) && !MarketService.Instance.TrySell(900005,out reason)
                && GameSession.Instance.LoadedPayload.resMoney==7 && RunBagProgression.Owns(900005),
                "market prevents duplicate purchase and sale of important item");
            foreach(var d in defs.Where(d=>d.Enable&&d.RunCategory==0).Take(5)) check(RunInventory.AddItem(d.Index,1,out reason),"fill misc "+d.Index);
            check(RunInventory.AddItem(900002,1,out reason) && RunBagProgression.Owns(900002) && RunInventory.GetCount(900002)==0
                && RunInventory.GetUncarriedImportant().Contains(900002u), "full bag retains newly acquired important in permanent catalog");
            check(RunInventory.GrantSlots(RunBagSection.Important,1,"carry",out reason)
                && RunInventory.TryCarryImportant(900002,out reason) && RunInventory.GetCount(900002)==1, "catalog item can be carried after capacity is available");
            var asset=Resources.Load<InputActionAsset>("Input/ChaosInputActions");
            var player=asset.FindActionMap("Player");
            check(player.FindAction("Jump").bindings.Count(b=>b.groups=="Keyboard")==1
                && player.FindAction("Jump").bindings.First(b=>b.groups=="Keyboard").path=="<Keyboard>/space", "jump keyboard default is Space only");
            check(!player.bindings.Any(b=>b.groups=="Mouse" || b.groups=="KeyboardFixed"), "no hidden second keyboard/mouse bindings");
            check(player.FindAction("Functions").bindings.First(b=>b.groups=="Keyboard").path=="<Keyboard>/tab"
                && player.FindAction("Map").bindings.First(b=>b.groups=="Keyboard").path=="<Keyboard>/m", "Tab collection and M context-map defaults");
            report.AppendLine("DONE storage="+storage);
            return report.ToString();
        }
        finally
        {
            foreach(var d in added) defs.Remove(d);
            RunInventory.Clear(); GameSession.Instance.Clear();
            Directory.CreateDirectory(Application.dataPath+"/QA/RunBag");
            File.WriteAllText(Application.dataPath+"/QA/RunBag/contracts.txt",report.ToString());
        }
    }
}
#endif
