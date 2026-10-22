using System.Collections.Generic;
using Chaos.MVP.Model.Gameplay;
using LocalizationSystem;

namespace Chaos.MVP.Presenter.Gameplay
{
    public sealed class SettlementState
    {
        public string Title, Hint, Wounds;
        public bool Win;
        public IReadOnlyList<TextRow> Rows;
    }
    public sealed class SettlementPresenter : GameplayPresenter<SettlementState>
    {
        private readonly RunSettlementModel _model;
        public SettlementPresenter(RunSettlementModel model) { _model = model; }
        public static void Begin(bool win) { RunSettlementModel.Begin(win); }
        public static SettlementPresenter ForCurrentRun() { return new SettlementPresenter(RunSettlementModel.Current); }
        protected override void OnAttach() { RunControlModel.PauseWorld(true); base.OnAttach(); }
        protected override SettlementState CreateState()
        {
            var rows = new List<TextRow>();
            var retainedNames = new List<string>();
            foreach (var item in _model.GetItems())
            {
                string name = GameplayText.ItemName(item.Id);
                if (item.Retained) retainedNames.Add(name);
                rows.Add(new TextRow { Id = item.Id, Selected = item.Selected,
                    Text = (item.Selected ? "【已选】" : item.Retained ? "【保留】" : "　　") + name + " ×" + item.Count,
                    Tone = item.Selected || item.Retained ? TextTone.Success : TextTone.Normal });
            }
            string hint = _model.Win ? "可带出 " + MetaProgressService.Instance.DeployCap + " 件（已选 " + _model.SelectedCount + "）"
                : "失败：保留 50% 掉落（" + rows.Count + " 条 → 保留 " + _model.RetainedCount + " 条：" + (retainedNames.Count == 0 ? "无" : string.Join("/", retainedNames)) + "）";
            if (_model.PendingSave) hint += "；保存待重试，点击确认不会重复结算";
            return new SettlementState { Win = _model.Win, Rows = rows.ToArray(), Title = _model.Win ? "局内通关" : "全员倒下 · 本局失败", Hint = hint,
                Wounds = RunEncounter.LastWoundCount > 0 ? RunEncounter.LastWoundCount + " 名门客重伤（30s）" : "" };
        }
        protected override void Execute(PanelIntent intent)
        {
            if (intent.Action == PanelAction.Toggle)
            {
                bool ok = _model.Toggle(intent.Id, out string reason); Refresh(); if (!ok && reason != null) Message(reason); return;
            }
            if (intent.Action != PanelAction.Confirm && intent.Action != PanelAction.Discard) return;
            if (!_model.Confirm(intent.Action == PanelAction.Discard, out string saveReason))
            {
                if (saveReason != null) Message(saveReason + "；点击确认重试，不会重复结算");
                return;
            }
            // Remove the old settlement before a return can push a story event on top of it.
            View.ClosePanel();
            _model.ReturnToMeta(out string returnReason);
        }
    }
}
