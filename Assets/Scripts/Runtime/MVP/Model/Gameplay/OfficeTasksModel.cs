using System;
using System.Linq;
using LocalizationSystem;

namespace Chaos.MVP.Model.Gameplay
{
    /// <summary>整理只读任务快照与界面意图，任务规则及持久化仍由领域服务负责。</summary>
    public sealed class OfficeTasksModel
    {
        readonly string facilityId;
        readonly uint mapLocationId;
        readonly int contextId = GameSession.Instance.ContextId;
        readonly VillageWorldModel world = new VillageWorldModel();
        public OfficeTaskCategory Category {get;private set;} = OfficeTaskCategory.County;
        public uint SelectedId {get;private set;}
        public string Message {get;private set;}
        public OfficeTasksModel(string id, uint locationId=0, uint selectedTask=0)
        { facilityId=id;mapLocationId=locationId;SelectedId=selectedTask;
            var selected=selectedTask==0?null:OfficeTaskService.Instance.GetTask(selectedTask);
            if(selected!=null)Category=selected.Category; }
        public bool IsCurrent => GameSession.Instance.HasSession && contextId==GameSession.Instance.ContextId
            && VillageSceneFlow.IsVillageScene && VillageSceneBootstrap.IsReady
            && (mapLocationId==0 ? world.CanUseFacility(facilityId)
                :WorldLocationService.Instance.GetLocation(GameSession.Instance.LoadedPayload.era,mapLocationId,WorldLocationIdentity.Official)?.CanOpenOffice==true);
        public void SelectCategory(OfficeTaskCategory value)
        { if(Enum.IsDefined(typeof(OfficeTaskCategory),value)){Category=value;SelectedId=0;Message=null;} }
        public void SelectTask(uint id) { SelectedId=id;Message=null; }
        public void Choose(uint id,uint cycle,uint option)
        {
            if(!IsCurrent){Message=OfficeTaskText.Get("stale");return;}
            var selected=OfficeTaskService.Instance.GetTask(id);
            if(mapLocationId!=0&&selected!=null&&selected.MapLocationId!=0&&selected.MapLocationId!=mapLocationId)
            {Message="该事务不属于当前地点。";return;}
            string reason;
            bool ok=option==0 ? OfficeTaskService.Instance.TryAccept(id,cycle,contextId,out reason)
                : OfficeTaskService.Instance.TryChoose(id,cycle,option,contextId,out reason);
            if(ok)Message=option==0?"事务已接取，可按M查看目标位置。":OfficeTaskText.Get("saved");
            else Message=reason;
        }
        public void NoReports() {Message=OfficeTaskText.Get("noReports");}
        public OfficeTasksDisplayState Read()
        {
            var tasks=OfficeTaskService.Instance.GetTasks(Category)
                .Where(t=>mapLocationId==0||t.MapLocationId==0||t.MapLocationId==mapLocationId).ToArray();
            if(!tasks.Any(t=>t.TaskId==SelectedId))SelectedId=tasks.Length==0?0:tasks[0].TaskId;
            var selected=tasks.FirstOrDefault(t=>t.TaskId==SelectedId);
            var payload=GameSession.Instance.LoadedPayload;
            return new OfficeTasksDisplayState {
                Title=OfficeTaskText.Get("title"),Summary=OfficeTaskText.Get("metrics",payload.reputation,payload.influence,payload.villageDevelopment),
                BackLabel=OfficeTaskText.Get("back"),LegacyLabel=OfficeTaskText.Get("reports",EventService.Instance.PendingCount),
                EmptyText=OfficeTaskText.Get("empty"),Message=Message,ShowLegacyButton=true,
                CategoryLabels=new[]{OfficeTaskText.Get("county"),OfficeTaskText.Get("admin"),OfficeTaskText.Get("preset"),OfficeTaskText.Get("emergency")},
                SelectedCategory=Category,SelectedTaskId=SelectedId,
                Rows=tasks.Select(t=>new OfficeTaskDisplayRow {TaskId=t.TaskId,Cycle=t.Cycle,Title=t.Title,
                    Status=OfficeTaskText.Status(t.Status),Time=t.Timed&&t.Status==OfficeTaskStatus.Active?OfficeTaskText.Time(t.RemainingSeconds):"",Selected=t.TaskId==SelectedId}).ToArray(),
                DetailTitle=selected?.Title??"",DetailBody=selected==null?"":selected.Body+"\n\n"+selected.Topic,
                DetailProgress=selected==null?"":Progress(selected),
                Options=selected==null?Array.Empty<OfficeTaskDisplayOption>():selected.Status==OfficeTaskStatus.Available
                    ?new[]{new OfficeTaskDisplayOption {TaskId=selected.TaskId,Cycle=selected.Cycle,OptionId=0,Text="接取事务",Enabled=true}}
                    :selected.Options.Select(o=>new OfficeTaskDisplayOption {
                    TaskId=selected.TaskId,Cycle=selected.Cycle,OptionId=o.Index,Text=Describe(o),
                    Enabled=selected.Status==OfficeTaskStatus.Active&&selected.SelectedOptionId==0
                        &&(selected.Required==0||selected.Progress>=selected.Required)}).ToArray()
            };
        }
        static string Progress(OfficeTaskSnapshot task)
        {
            if(task.Status==OfficeTaskStatus.Available)return task.Timed?"可接取 · 接取成功后开始倒计时":"可接取";
            if(task.Status==OfficeTaskStatus.AwaitingBattle)return OfficeTaskText.Get("awaitBattle");
            string result=OfficeTaskText.Status(task.Status);
            if(task.Required>0)result+="　"+OfficeTaskText.Get("progress",task.Progress,task.Required);
            if(task.Timed&&task.Status==OfficeTaskStatus.Active)result+="　"+OfficeTaskText.Time(task.RemainingSeconds);
            if(task.SelectedOptionId>0&&task.Status==OfficeTaskStatus.Active)result+="\n"+OfficeTaskText.Get("prepared");
            return result;
        }
        static string Describe(OfficeTaskTable_OfficeTaskOptionConfig option)
        {
            string text=option.OptionText;
            if(option.CostAmount>0)text+="　"+OfficeTaskText.Get("cost",InventoryService.ResourceDisplayName(VillageService.ResourceKindOf(option.CostResourceType)),option.CostAmount);
            if(option.RewardAmount>0)text+="　"+OfficeTaskText.Get("reward",InventoryService.ResourceDisplayName(VillageService.ResourceKindOf(option.RewardResourceType)),option.RewardAmount);
            if(option.ReputationDelta!=0||option.InfluenceDelta!=0||option.DevelopmentDelta!=0)
                text+="\n"+OfficeTaskText.Get("deltas",Signed(option.ReputationDelta),Signed(option.InfluenceDelta),Signed(option.DevelopmentDelta));
            return text;
        }
        static string Signed(int number) {return number>0?"+"+number:number.ToString();}
    }
}
