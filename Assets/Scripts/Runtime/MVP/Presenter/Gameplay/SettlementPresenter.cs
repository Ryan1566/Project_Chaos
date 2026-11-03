using System.Collections.Generic;
using Chaos.MVP.Model.Gameplay;
using LocalizationSystem;

namespace Chaos.MVP.Presenter.Gameplay
{
    public sealed class SettlementState
    {
        public string Title, Hint, Notice, Wounds, PageLabel, PrevLabel, NextLabel, ConfirmLabel;
        public bool Win;
        public bool CanPrev, CanNext, CanConfirm;
        public int Page;
        public IReadOnlyList<TextRow> Rows;
    }

    public sealed class SettlementPresenter : GameplayPresenter<SettlementState>
    {
        private static readonly string[] PageNames = { "队伍与战果", "战利品", "事件记录" };
        private readonly RunSettlementModel _model;
        private string _notice;

        public SettlementPresenter(RunSettlementModel model) { _model = model; }
        public static void Begin(bool win) { RunSettlementModel.Begin(win); }
        public static SettlementPresenter ForCurrentRun() { return new SettlementPresenter(RunSettlementModel.Current); }
        protected override void OnAttach() { RunControlModel.PauseWorld(true); base.OnAttach(); }

        protected override SettlementState CreateState()
        {
            int page = _model.Page;
            if (page < 0) page = 0;
            else if (page > 2) page = 2;

            var rows = new List<TextRow>();
            string hint, wounds = "";
            if (page == 0) BuildTeamPage(rows, out hint, out wounds);
            else if (page == 1) BuildLootPage(rows, out hint);
            else BuildEventPage(rows, out hint);

            return new SettlementState
            {
                Win = _model.Win,
                Page = page,
                Rows = rows,
                Title = _model.Win ? "局内通关" : "全员倒下 · 本局失败",
                Hint = hint,
                Notice = _notice,
                Wounds = wounds,
                PageLabel = "第 " + (page + 1) + " / 3 页 · " + PageNames[page],
                PrevLabel = "上一页",
                NextLabel = "下一页",
                ConfirmLabel = page == 2
                    ? (_model.Completed ? "重试返回" : _model.PendingSave ? "重试结算" : "确认结算并返回")
                    : "",
                CanPrev = page > 0 && !_model.Completed,
                CanNext = page < 2 && !_model.Completed,
                CanConfirm = page == 2
            };
        }

        private void BuildTeamPage(List<TextRow> rows, out string hint, out string wounds)
        {
            var report = _model.Report;
            var members = report == null ? null : report.Members;
            int total = members == null ? 0 : members.Count;
            int downed = 0;
            if (members != null)
                for (int i = 0; i < members.Count; i++)
                    if (members[i] != null && members[i].Downed) downed++;

            int stages = report == null ? 0 : report.StagesPassed;
            int kills = report == null ? 0 : report.Kills;
            int highest = report == null ? 0 : report.HighestDamage;
            float seconds = report == null ? 0f : report.PlaySeconds;
            rows.Add(new TextRow
            {
                Text = "本局统计　通过 " + stages + "/" + RunEncounter.MaxSceneIndex + " 场景　击杀 " + kills
                    + "　用时 " + FormatTime(seconds),
                Tone = TextTone.Normal
            });
            rows.Add(new TextRow
            {
                Text = "战斗统计　最高单次伤害 " + highest,
                Tone = TextTone.Normal
            });
            rows.Add(new TextRow
            {
                Text = "队伍状态　共 " + total + " 人　存活 " + (total - downed) + "　倒地 " + downed,
                Tone = downed > 0 ? TextTone.Warning : TextTone.Dim
            });

            if (members == null || total == 0)
            {
                rows.Add(new TextRow { Text = "暂无队伍成员快照。", Tone = TextTone.Dim });
            }
            else
            {
                for (int i = 0; i < members.Count; i++)
                {
                    var member = members[i];
                    if (member == null) continue;
                    string name = string.IsNullOrEmpty(member.Name) ? "门客 #" + member.Id : member.Name;
                    string health = member.MaxHealth > 0 ? member.Health + "/" + member.MaxHealth : "未知";
                    string profession = string.IsNullOrEmpty(member.Profession) ? "" : "　" + member.Profession;
                    rows.Add(new TextRow
                    {
                        Text = (member.Controlled ? "【操控】" : "【门客】") + name + "　Lv." + member.Level
                            + "　生命 " + health + "　" + (member.Downed ? "倒地" : "存活") + profession,
                        Tone = member.Downed ? TextTone.Warning : TextTone.Normal
                    });
                }
            }

            wounds = RunEncounter.LastWoundCount > 0
                ? RunEncounter.LastWoundCount + " 名门客重伤（30s）"
                : "";
            hint = "第 1 / 3 页：核对队伍姓名、等级、生命与倒地状态，然后前往“战利品”页。";
        }

        private void BuildLootPage(List<TextRow> rows, out string hint)
        {
            var items = _model.GetItems();
            if (items == null || items.Count == 0)
            {
                rows.Add(new TextRow { Text = "本局没有可结算的战利品。", Tone = TextTone.Dim });
            }
            else
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    string name = GameplayText.ItemName(item.Id);
                    if (string.IsNullOrEmpty(name)) name = "物品 #" + item.Id;
                    string label;
                    TextTone tone;
                    bool actionable = false;
                    string actionLabel = null;
                    if (item.Important)
                    {
                        label = "【重要】" + name + " ×" + item.Count + "（自动保留，只读）";
                        tone = TextTone.Warning;
                        actionLabel = "只读";
                    }
                    else if (_model.Win)
                    {
                        label = "【带回】" + name + " ×" + item.Count + "（成功结算自动携带）";
                        tone = TextTone.Success;
                        actionLabel = "自动带回";
                    }
                    else
                    {
                        actionable = !_model.Completed;
                        label = (item.Selected ? "【已选】" : "【未选】") + name + " ×" + item.Count;
                        tone = item.Selected ? TextTone.Success : TextTone.Normal;
                        actionLabel = item.Selected ? "取消" : "选中";
                    }
                    rows.Add(new TextRow
                    {
                        Id = item.Id,
                        Selected = item.Selected,
                        Enabled = actionable,
                        Text = label,
                        Tone = tone,
                        Action = actionable ? PanelAction.Toggle : PanelAction.Refresh,
                        ActionLabel = actionLabel
                    });
                }
            }

            string carry = _model.Win
                ? "成功：全部战利品自动带回。"
                : "失败：默认空选，按失败额度最多带出 " + _model.FailureCarryCap
                    + " 种（已选 " + _model.SelectedCount + "）。";
            hint = "第 2 / 3 页：战利品。" + carry + "重要道具只读并自动保留。";
        }

        private void BuildEventPage(List<TextRow> rows, out string hint)
        {
            var report = _model.Report;
            var events = report == null ? null : report.Events;
            int count = events == null ? 0 : events.Count;
            rows.Add(new TextRow { Text = "事件记录（只读）", Tone = TextTone.Dim });
            if (count == 0)
            {
                rows.Add(new TextRow { Text = "本局暂无事件记录。", Tone = TextTone.Dim });
            }
            else
            {
                for (int i = 0; i < events.Count; i++)
                {
                    if (string.IsNullOrEmpty(events[i])) continue;
                    rows.Add(new TextRow { Text = "· " + events[i], Tone = TextTone.Normal });
                }
            }

            if (_model.Completed)
                hint = "结算已保存。点击“重试返回”，不会重复发放奖励。";
            else if (_model.PendingSave)
                hint = "保存待重试：点击“重试结算”不会重复结算。";
            else
                hint = "第 3 / 3 页：事件记录。确认后才会提交结算并返回；前两页不会提交。";
        }

        private static string FormatTime(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int total = (int)seconds;
            return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }

        protected override void Execute(PanelIntent intent)
        {
            if (intent.Action == PanelAction.Toggle)
            {
                if (_model.Page != 1) return;
                bool ok = _model.Toggle(intent.Id, out string reason);
                if (ok) _notice = null;
                else if (!string.IsNullOrEmpty(reason)) _notice = reason;
                Refresh();
                return;
            }
            if (intent.Action == PanelAction.Advance)
            {
                Navigate(1);
                return;
            }
            if (intent.Action == PanelAction.Return)
            {
                Navigate(-1);
                return;
            }
            if (intent.Action != PanelAction.Confirm) return;

            // 结算只允许在第三页提交；前两页的确认意图只提示，不触碰存档。
            if (_model.Page != 2)
            {
                ShowNotice("请在第三页确认结算。");
                return;
            }
            if (!_model.Completed && !_model.Confirm(false, out string saveReason))
            {
                ShowNotice((string.IsNullOrEmpty(saveReason) ? "结算未完成" : saveReason)
                    + "；点击确认重试，不会重复结算。");
                return;
            }
            ReturnToMeta();
        }

        private void Navigate(int delta)
        {
            int before = _model.Page;
            _model.MovePage(delta);
            if (before != _model.Page) Refresh();
        }

        private void ShowNotice(string message)
        {
            _notice = message;
            Refresh();
        }

        private void ReturnToMeta()
        {
            bool formalRun = RunSceneFlow.IsFormalRun;
            // 正式场景由返回流程移除旧界面；失败时保留本页供重试。
            // 原型入口先关闭面板，因为返回可能同步压入新事件。
            if (!formalRun) View.ClosePanel();
            if (_model.ReturnToMeta(out string returnReason)) return;
            if (!formalRun)
            {
                var reopened = UIManager.Instance.PushPanel(PanelType.RunSettlementPanel) as IGameplayView<SettlementState>;
                reopened?.ShowMessage(returnReason + "；结算已保存，点击确认重试返回。", false);
                return;
            }
            ShowNotice(returnReason + "；结算已保存，点击确认重试返回。");
        }
    }
}
