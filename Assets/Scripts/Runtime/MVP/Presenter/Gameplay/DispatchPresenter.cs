using System;
using Chaos.MVP.Model.Gameplay;
using Chaos.MVP.Presenter;
using LocalizationSystem;

namespace Chaos.MVP.Presenter.Gameplay
{
    /// <summary>派遣界面意图；查看、筛选与编队命令分开表达。</summary>
    public enum DispatchAction
    {
        Select,
        SetFilter,
        SetSearch,
        Add,
        Remove,
        SetControlled,
        Depart,
        Return,
        Close,
        Refresh,
        OpenRetainers
    }

    /// <summary>派遣界面的单向意图数据。</summary>
    public readonly struct DispatchIntent
    {
        public readonly DispatchAction Action;
        public readonly uint Id;
        public readonly DispatchFilter Filter;
        public readonly string Text;

        public DispatchIntent(DispatchAction action, uint id = 0,
            DispatchFilter filter = DispatchFilter.All, string text = null)
        {
            Action = action;
            Id = id;
            Filter = filter;
            Text = text;
        }
    }

    /// <summary>包含一次渲染所需快照的状态。</summary>
    public sealed class DispatchState
    {
        public DispatchSnapshot Snapshot;
    }

    /// <summary>派遣视图只暴露显示、反馈、关闭和意图入口。</summary>
    public interface IDispatchView
    {
        event Action<DispatchIntent> Intent;
        void Render(DispatchState state);
        void ShowMessage(string message, bool success);
        void ClosePanel();
    }

    /// <summary>
    /// 管理查看对象、筛选和搜索意图；订阅元数据变化、会话与语言刷新。
    /// 服务只负责命令和场景流程，成功出发/撤离后本 Presenter 不再刷新旧面板。
    /// </summary>
    public sealed class DispatchPresenter : PresenterBase<IDispatchView>
    {
        private readonly DispatchModel _model = new DispatchModel();
        private DispatchFilter _filter = DispatchFilter.All;
        private string _search = string.Empty;
        private uint _viewId;
        private LocalizationManager _language;
        private DispatchSnapshot _last;
        private bool _flowOwnsScene;
        private bool _suppressRefresh;

        protected override void OnAttach()
        {
            View.Intent += HandleIntent;
            MetaRuntimeDriver.Changed += Refresh;
            GameSession.Instance.ContextChanged += Refresh;
            _language = LocalizationManager.GetInstance();
            if (_language != null) _language.OnLanguageChanged.AddListener(LanguageChanged);
            Refresh();
        }

        protected override void OnDetach()
        {
            if (View != null) View.Intent -= HandleIntent;
            MetaRuntimeDriver.Changed -= Refresh;
            GameSession.Instance.ContextChanged -= Refresh;
            if (_language != null) _language.OnLanguageChanged.RemoveListener(LanguageChanged);
            _language = null;
        }

        private void LanguageChanged(LanguageType _)
        {
            Refresh();
        }

        /// <summary>重新读取只读快照；切场景成功后的旧面板不再刷新。</summary>
        public void Refresh()
        {
            if (IsDisposed || View == null || _flowOwnsScene || _suppressRefresh) return;
            DispatchSnapshot snapshot = _model.Read(_filter, _search, _viewId);
            _viewId = snapshot.ResolvedViewId;
            _last = snapshot;
            View.Render(new DispatchState { Snapshot = snapshot });
        }

        private void HandleIntent(DispatchIntent intent)
        {
            if (IsDisposed || _flowOwnsScene) return;

            switch (intent.Action)
            {
                case DispatchAction.Close:
                    View.ClosePanel();
                    return;
                case DispatchAction.OpenRetainers:
                    if (GameSession.Instance.IsPersistent && VillageSceneBootstrap.IsReady
                        && VillageSceneFlow.IsVillageScene && !DispatchService.Instance.IsInRun
                        && !RunSceneFlow.IsTransitioning)
                        UIManager.Instance.PushPanel(PanelType.RetainerPanel);
                    return;
                case DispatchAction.Refresh:
                    Refresh();
                    return;
                case DispatchAction.Select:
                    _viewId = intent.Id;
                    Refresh();
                    return;
                case DispatchAction.SetFilter:
                    _filter = intent.Filter;
                    Refresh();
                    return;
                case DispatchAction.SetSearch:
                    _search = intent.Text ?? string.Empty;
                    Refresh();
                    return;
                case DispatchAction.Add:
                    Add(intent.Id);
                    return;
                case DispatchAction.Remove:
                    Remove(intent.Id);
                    return;
                case DispatchAction.SetControlled:
                    SetControlled(intent.Id);
                    return;
                case DispatchAction.Depart:
                    Depart();
                    return;
                case DispatchAction.Return:
                    Return();
                    return;
            }
        }

        private void Add(uint id)
        {
            DispatchService service = DispatchService.Instance;
            if (service.IsInRun)
            {
                Refresh();
                Message(GameplayText.Text("ui_dispatch_msg_inrun_edit", "局内只读，返回局外后再调整编队。"), false);
                return;
            }
            if (id == 0)
            {
                Refresh();
                Message(GameplayText.Text("ui_dispatch_msg_pick", "请先在左侧查看一位门客。"), false);
                return;
            }
            if (service.SelectedIds.Contains(id))
            {
                Refresh();
                Message(GameplayText.Text("ui_dispatch_msg_already", "该门客已在编队中。"), false);
                return;
            }
            if (service.SelectedIds.Count >= DispatchService.MaxTeamSize)
            {
                Refresh();
                Message(GameplayText.Text("ui_dispatch_msg_full", "队伍已满（最多 4 位）。"), false);
                return;
            }
            if (!service.Toggle(id, out string reason))
            {
                Refresh();
                Message(reason, false);
                return;
            }
            Refresh();
            Message(GameplayText.Text("ui_dispatch_msg_added", "已加入队伍：{0}", _model.GetRoleName(id)), true);
        }

        private void Remove(uint id)
        {
            DispatchService service = DispatchService.Instance;
            if (service.IsInRun)
            {
                Refresh();
                Message(GameplayText.Text("ui_dispatch_msg_inrun_edit", "局内只读，返回局外后再调整编队。"), false);
                return;
            }
            if (!service.SelectedIds.Contains(id))
            {
                Refresh();
                Message(GameplayText.Text("ui_dispatch_msg_not_member", "该门客不在编队中。"), false);
                return;
            }
            if (!service.Toggle(id, out string reason))
            {
                Refresh();
                Message(reason, false);
                return;
            }
            Refresh();
            Message(GameplayText.Text("ui_dispatch_msg_removed", "已移出队伍：{0}", _model.GetRoleName(id)), true);
        }

        private void SetControlled(uint id)
        {
            DispatchService service = DispatchService.Instance;
            if (!service.SetControlled(id, out string reason))
            {
                Refresh();
                Message(reason, false);
                return;
            }
            Refresh();
            Message(GameplayText.Text("ui_dispatch_msg_controlled", "已设为玩家操控：{0}", _model.GetRoleName(id)), true);
        }

        private void Depart()
        {
            DispatchService service = DispatchService.Instance;
            if (!service.CanDepartWithReason(out string reason))
            {
                Refresh();
                Message(reason, false);
                return;
            }

            if (GameSession.Instance.IsPersistent)
            {
                if (!BattlePreparationFlow.Open()) { Refresh(); Message("战备界面未能打开", false); }
                return;
            }

            // 出发期间屏蔽同步事件触发的重绘；成功后场景流程所有者接管旧面板。
            _suppressRefresh = true;
            if (!service.Depart(out reason))
            {
                _suppressRefresh = false;
                Refresh();
                Message(reason, false);
                return;
            }
            _flowOwnsScene = true;
        }

        private void Return()
        {
            DispatchService service = DispatchService.Instance;
            _suppressRefresh = true;
            if (!service.ReturnToMeta(out string reason))
            {
                _suppressRefresh = false;
                Refresh();
                Message(reason, false);
                return;
            }
            _flowOwnsScene = true;
        }

        private void Message(string text, bool success)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (!IsDisposed && View != null) View.ShowMessage(text, success);
        }
    }
}
