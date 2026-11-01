using System;
using Chaos.MVP.Model.Gameplay;
using LocalizationSystem;

namespace Chaos.MVP.Presenter.Gameplay
{
    public enum FamilyTreeAction { SelectBoard, SelectNode, Unlock, ResetView, Close }
    public readonly struct FamilyTreeIntent
    {
        public readonly FamilyTreeAction Action; public readonly uint Id;
        public FamilyTreeIntent(FamilyTreeAction action,uint id=0) { Action=action;Id=id; }
    }
    public sealed class FamilyTreeState { public FamilyTreeSnapshot Tree; }
    public interface IFamilyTreeView
    {
        event Action<FamilyTreeIntent> Intent;
        void Render(FamilyTreeState state);
        void ShowMessage(string message,bool success);
        void ClosePanel();
        void ResetTreeView();
    }
    /// <summary>管理选择与面板订阅，解锁通过领域模型执行保存事务。</summary>
    public sealed class FamilyTreePresenter : PresenterBase<IFamilyTreeView>
    {
        private readonly FamilyTreeModel _model=new FamilyTreeModel();
        private uint _board,_selected; private LocalizationManager _language;
        protected override void OnAttach()
        {
            View.Intent+=Handle;
            MetaRuntimeDriver.Changed+=Refresh;
            _language=LocalizationManager.GetInstance();
            if(_language!=null) _language.OnLanguageChanged.AddListener(LanguageChanged);
            Refresh();
        }
        protected override void OnDetach()
        {
            if(View!=null) View.Intent-=Handle;
            MetaRuntimeDriver.Changed-=Refresh;
            if(_language!=null) _language.OnLanguageChanged.RemoveListener(LanguageChanged);
        }
        private void LanguageChanged(LanguageType _) { Refresh(); }
        public void Refresh()
        {
            if(IsDisposed || View==null) return;
            var tree=_model.Read(_board,_selected); _board=tree.Board;_selected=tree.Selected;
            View.Render(new FamilyTreeState { Tree=tree });
        }
        private void Handle(FamilyTreeIntent intent)
        {
            switch(intent.Action)
            {
                case FamilyTreeAction.Close:View.ClosePanel();return;
                case FamilyTreeAction.ResetView:View.ResetTreeView();return;
                case FamilyTreeAction.SelectBoard:_board=intent.Id;_selected=0;break;
                case FamilyTreeAction.SelectNode:_selected=intent.Id;break;
                case FamilyTreeAction.Unlock:
                    bool ok=_model.Unlock(_selected,out string reason);
                    Refresh();View.ShowMessage(ok?"已解锁，永久成长已保存":reason,ok);return;
            }
            Refresh();
        }
    }
}
