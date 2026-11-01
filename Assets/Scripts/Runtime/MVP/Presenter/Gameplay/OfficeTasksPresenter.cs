using System;
using Chaos.MVP.Model.Gameplay;

namespace Chaos.MVP.Presenter.Gameplay
{
    public interface IOfficeTasksView
    {
        event Action<OfficeTaskCategory> CategorySelected;
        event Action<uint> TaskSelected;
        event Action<uint,uint,uint> OptionSelected;
        event Action BackRequested;
        event Action LegacyEventsRequested;
        void Render(OfficeTasksDisplayState state);
        void CloseTasks();
    }
    /// <summary>负责面板导航和意图转发，界面刷新不驱动任务触发或计时。</summary>
    public sealed class OfficeTasksPresenter : PresenterBase<IOfficeTasksView>
    {
        readonly OfficeTasksModel model;
        public OfficeTasksPresenter(OfficeTasksModel value) {model=value;}
        protected override void OnAttach()
        {
            View.CategorySelected+=Category;View.TaskSelected+=Task;View.OptionSelected+=Choose;
            View.BackRequested+=Back;View.LegacyEventsRequested+=Legacy;Refresh();
        }
        protected override void OnDetach()
        {
            if(View==null)return;
            View.CategorySelected-=Category;View.TaskSelected-=Task;View.OptionSelected-=Choose;
            View.BackRequested-=Back;View.LegacyEventsRequested-=Legacy;
        }
        public void Refresh()
        {
            if(IsDisposed||View==null)return;
            if(!model.IsCurrent){View.CloseTasks();return;}
            View.Render(model.Read());
        }
        void Category(OfficeTaskCategory category){if(model.IsCurrent){model.SelectCategory(category);Refresh();}}
        void Task(uint id){if(model.IsCurrent){model.SelectTask(id);Refresh();}}
        void Choose(uint id,uint cycle,uint option){if(model.IsCurrent){model.Choose(id,cycle,option);Refresh();}else View?.CloseTasks();}
        void Back(){View?.CloseTasks();}
        void Legacy()
        {
            if(!model.IsCurrent){View?.CloseTasks();return;}
            if(EventService.Instance.HasPending)UIManager.Instance.PushPanel(PanelType.EventPanel);
            else {model.NoReports();Refresh();}
        }
    }
}
