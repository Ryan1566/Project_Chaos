using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Chaos.MVP.Model.Gameplay
{
    public enum FamilyNodeStatus { Locked, Unaffordable, Available, Unlocked }
    public sealed class FamilyBoardState { public uint Id; public string Name, Description; public int Unlocked, Total; }
    public sealed class FamilyNodeState
    {
        public uint Id, Prerequisite; public float X, Y; public bool Milestone;
        public string Name, Description, Branch, Icon, Reason, Cost, PrerequisiteName, Effects;
        public FamilyNodeStatus Status;
    }
    public sealed class FamilyTreeSnapshot
    {
        public uint Board, Selected; public int Unlocked, Total;
        public string Resources, Summary, Error;
        public FamilyBoardState[] Boards = new FamilyBoardState[0];
        public FamilyNodeState[] Nodes = new FamilyNodeState[0];
    }
    /// <summary>将配置、事务状态和同源效果预览转换为界面快照。</summary>
    public sealed class FamilyTreeModel
    {
        public FamilyTreeSnapshot Read(uint boardId, uint selectedId)
        {
            var result = new FamilyTreeSnapshot();
            try
            {
                var service = FamilyTreeService.Instance; var catalog = service.Catalog;
                result.Boards = catalog.Boards.Where(x => x.Enable).OrderBy(x => x.Order).Select(x => new FamilyBoardState {
                    Id=x.Index, Name=catalog.Text(x.NameKey), Description=catalog.Text(x.DescriptionKey),
                    Total=catalog.Nodes.Count(n => n.Enable && n.Board==x.Index),
                    Unlocked=catalog.Nodes.Count(n => n.Enable && n.Board==x.Index && service.IsUnlocked(n.Index)) }).ToArray();
                result.Board = result.Boards.Any(x => x.Id==boardId) ? boardId : result.Boards.First().Id;
                var rows = new List<FamilyNodeState>();
                foreach (var node in catalog.Nodes.Where(x => x.Enable && x.Board==result.Board).OrderBy(x => x.OrderInBoard))
                {
                    bool unlocked=service.IsUnlocked(node.Index), available=service.CanUnlock(node.Index,out string reason);
                    var status=unlocked ? FamilyNodeStatus.Unlocked : available ? FamilyNodeStatus.Available
                        : node.PrereqNodeId!=0 && !service.IsUnlocked(node.PrereqNodeId) ? FamilyNodeStatus.Locked : FamilyNodeStatus.Unaffordable;
                    uint have=InventoryService.Instance.GetResource(VillageService.ResourceKindOf(node.CostType));
                    rows.Add(new FamilyNodeState { Id=node.Index,Prerequisite=node.PrereqNodeId,X=node.LayoutX,Y=node.LayoutY,
                        Milestone=node.NodeKind==1,Name=catalog.Text(node.NameKey),Description=catalog.Text(node.DescriptionKey),
                        Branch=catalog.Text(node.BranchKey),Icon=node.IconKey,Reason=reason ?? "可解锁",Status=status,
                        Cost=InventoryService.ResourceDisplayName(VillageService.ResourceKindOf(node.CostType))+" "+node.CostValue+"  /  持有 "+have,
                        PrerequisiteName=node.PrereqNodeId==0 ? "无前置" : catalog.Text(catalog.Node(node.PrereqNodeId).NameKey),
                        Effects=DescribeEffects(catalog,node.Index,unlocked) });
                }
                result.Nodes=rows.ToArray();
                result.Selected=rows.Any(x => x.Id==selectedId) ? selectedId : (rows.FirstOrDefault(x => x.Status==FamilyNodeStatus.Available) ?? rows.FirstOrDefault())?.Id ?? 0;
                result.Total=result.Boards.Sum(x=>x.Total); result.Unlocked=service.UnlockedCount;
                result.Resources=string.Join("    ",Enumerable.Range(1,4).Select(i => InventoryService.ResourceDisplayName(VillageService.ResourceKindOf((uint)i))+" "+InventoryService.Instance.GetResource(VillageService.ResourceKindOf((uint)i))));
                var snap=EffectAggregator.Instance.Snapshot;
                var contribution=EffectAggregator.Instance.PreviewNodes(catalog.Nodes.Where(x=>x.Enable && x.Board==result.Board && service.IsUnlocked(x.Index)).Select(x=>x.Index));
                result.Summary="本板块已获："+Contribution(contribution)+"\n家族总计：仓库 "+InventoryService.Instance.TotalSlots+" 格  ·  结算带出 "+MetaProgressService.Instance.DeployCap+" 件  ·  生命 +"+snap.HpBonus+"  攻击 +"+snap.AttackBonus+"  防御 +"+snap.DefenseBonus+"  移速 +"+snap.SpeedBonus;
            }
            catch(Exception error) { result.Error="家族树暂不可用："+error.Message; }
            return result;
        }
        public bool Unlock(uint id,out string reason) { return FamilyTreeService.Instance.Unlock(id,out reason); }
        private static string Contribution(MetaEffectSnapshot snapshot)
        {
            var parts=new List<string>();
            if(snapshot.TotalSlotsBonus!=0)parts.Add("仓库 +"+snapshot.TotalSlotsBonus+" 格");
            if(snapshot.DeployCapBonus!=0)parts.Add("带出 +"+snapshot.DeployCapBonus+" 件");
            if(snapshot.HpBonus!=0)parts.Add("生命 +"+snapshot.HpBonus);
            if(snapshot.AttackBonus!=0)parts.Add("攻击 +"+snapshot.AttackBonus);
            if(snapshot.DefenseBonus!=0)parts.Add("防御 +"+snapshot.DefenseBonus);
            if(snapshot.SpeedBonus!=0)parts.Add("移速 +"+snapshot.SpeedBonus);
            if(snapshot.CampRefillUnlocked)parts.Add("扎营补员");
            for(uint id=2001;id<=2004;id++)
            {
                if(snapshot.GetBuildSecondsDelta(id)!=0)parts.Add("营建 "+snapshot.GetBuildSecondsDelta(id)+" 秒");
                if(snapshot.GetOutputDelta(id)!=0)parts.Add("生产 +"+snapshot.GetOutputDelta(id));
            }
            return parts.Count==0?"尚未点亮":string.Join("  ·  ",parts);
        }
        private static string DescribeEffects(FamilyTreeCatalog catalog,uint id,bool unlocked)
        {
            var before=EffectAggregator.Instance.Snapshot; var after=EffectAggregator.Instance.PreviewAdditionalNode(id);
            var lines=new List<string>();var notes=new HashSet<string>();
            foreach(var link in catalog.EffectsOf(id))
            {
                var effect=catalog.Effects[link.EffectId];
                if(effect.EffectType==4 && effect.Target==1)notes.Add("生命上限加成不会治疗当前伤势。");
                if(effect.EffectType==4 && effect.Target==3)notes.Add("普通正伤害经过减伤后最低为1。");
                if(effect.EffectType==7)notes.Add("生产收益要求NPC实际进入建筑工作。");
                if(effect.EffectType==6)notes.Add("施工加成用于后续新建和升级，不修改已经开始的倒计时。");
                int value=Mathf.RoundToInt(effect.Value*link.Weight);
                string title="", current="", next="";
                switch(effect.EffectType)
                {
                    case 2: title="局外仓库格位"; current=Slots(before).ToString();next=Slots(after).ToString();break;
                    case 3: title="结算带出件数";current=(1+before.DeployCapBonus).ToString();next=(1+after.DeployCapBonus).ToString();break;
                    case 4:
                        string[] names={"","全队永久生命加成","全队普通攻击加成","全队普通防御减伤","全队水平移速加成"};
                        title=names[effect.Target];current=Attribute(before,effect.Target).ToString();next=Attribute(after,effect.Target).ToString();break;
                    case 5: title="扎营补充减员";current=before.CampRefillUnlocked?"已解锁":"未解锁";next=after.CampRefillUnlocked?"已解锁":"未解锁";break;
                    case 6:
                    case 7:
                        var building=catalog.Buildings[effect.Target]; string label=effect.Target==2001?"农田":effect.Target==2002?"材料生产建筑":effect.Target==2003?"图纸生产建筑":"钱财生产建筑";
                        if(effect.EffectType==6) { title=label+"一级施工秒数";current=Math.Max(1L,(long)building.BuildSeconds+before.GetBuildSecondsDelta(effect.Target)).ToString();next=Math.Max(1L,(long)building.BuildSeconds+after.GetBuildSecondsDelta(effect.Target)).ToString(); }
                        else { title=label+"一级每工作周期产量";current=Math.Max(0L,(long)building.OutputAmount+before.GetOutputDelta(effect.Target)).ToString();next=Math.Max(0L,(long)building.OutputAmount+after.GetOutputDelta(effect.Target)).ToString(); } break;
                }
                string signed=value>0?"+"+value:value.ToString();
                lines.Add(title+"  "+(unlocked ? "已生效（"+signed+"）" : current+" → "+next));
            }
            return string.Join("\n",lines)+(notes.Count==0?"":"\n\n"+string.Join("\n",notes));
        }
        private static int Slots(MetaEffectSnapshot s) { return Mathf.Clamp(InventoryService.BaseSlots+s.TotalSlotsBonus,0,InventoryService.HardMaxSlots); }
        private static int Attribute(MetaEffectSnapshot s,uint target)
        { return target==1?s.HpBonus:target==2?s.AttackBonus:target==3?s.DefenseBonus:s.SpeedBonus; }
    }
}
