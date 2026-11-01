using System;
using Chaos.MVP.Model.Gameplay;

namespace Chaos.MVP.Presenter.Gameplay
{
    public enum VillageOfficeIntent {Tasks,Trade,Tax,Back}
    public interface IVillageOfficeView
    {
        event Action<VillageOfficeIntent> Intent;
        void Render(VillageOfficeState state);
        void CloseOffice();
    }
    public sealed class VillageOfficePresenter : PresenterBase<IVillageOfficeView>
    {
        readonly VillageOfficeModel model;
        public VillageOfficePresenter(VillageOfficeModel model){this.model=model;}
        protected override void OnAttach(){View.Intent+=Handle;Refresh();}
        protected override void OnDetach(){if(View!=null)View.Intent-=Handle;}
        public void Refresh()
        {
            if(IsDisposed||View==null)return;
            if(!model.IsCurrent||!model.CanOperate){View.CloseOffice();return;}
            View.Render(model.Read());
        }
        void Handle(VillageOfficeIntent intent)
        {
            if(IsDisposed||View==null)return;
            if(intent==VillageOfficeIntent.Back)
            {if(model.Back())Refresh();else View.CloseOffice();return;}
            if(!model.CanOperate){View.CloseOffice();return;}
            if(intent==VillageOfficeIntent.Tasks)
            {
                OfficeTasksPanel.Open(model.FacilityId);
            }
            else if(intent==VillageOfficeIntent.Trade)UIManager.Instance.PushPanel(PanelType.MarketPanel);
            else if(intent==VillageOfficeIntent.Tax){model.ShowTax();Refresh();}
        }
    }
}
