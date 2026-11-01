using System;

namespace Chaos.MVP.Model.Gameplay
{
    public sealed class VillageOfficeState
    {
        public string Title,Body,Tasks,Trade,Tax,Back;
        public bool TaxPage,TasksPage,Available;
    }
    /// <summary>Read-only office navigation state; tax rules and rewards are intentionally not invented here.</summary>
    public sealed class VillageOfficeModel
    {
        readonly string facilityId;
        readonly int context=GameSession.Instance.ContextId;
        readonly VillageWorldModel world=new VillageWorldModel();
        public bool TaxPage {get;private set;}
        public bool TasksPage {get;private set;}
        public VillageOfficeModel(string id){facilityId=id;}
        public bool IsCurrent=>GameSession.Instance.HasSession&&GameSession.Instance.ContextId==context
            &&VillageSceneFlow.IsVillageScene&&VillageSceneBootstrap.IsReady;
        public bool CanOperate=>IsCurrent&&world.CanUseFacility(facilityId);
        public void ShowTax(){if(CanOperate)TaxPage=true;}
        public void ShowEmptyTasks(){if(CanOperate)TasksPage=true;}
        public bool Back(){if(!TaxPage&&!TasksPage)return false;TaxPage=false;TasksPage=false;return true;}
        public VillageOfficeState Read()
        {
            var text=VillageFacilitySettings.Load();
            return new VillageOfficeState {Title=(TaxPage?text.taxTitle:TasksPage?text.tasksLabel:text.officeTitle).Resolve(),
                Body=(TaxPage?text.taxBody:TasksPage?text.tasksEmptyBody:text.officeBody).Resolve(),Tasks=text.tasksLabel.Resolve(),Trade=text.tradeLabel.Resolve(),
                Tax=text.taxLabel.Resolve(),Back=text.backLabel.Resolve(),TaxPage=TaxPage,TasksPage=TasksPage,Available=CanOperate};
        }
    }
}
