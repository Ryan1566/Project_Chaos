using Chaos.MVP.Model.Gameplay;
using UnityEngine;

namespace Chaos.MVP.Presenter.Gameplay
{
    public sealed class CampState
    {
        public string State, Hint;
        public bool CanHeal, CanRefill;
        public bool InRun;

        // 时间轮盘：轮盘开关、24 个整点预览、当前时辰与安全区原因都由 Presenter 统一给出。
        public bool WheelOpen;
        public bool CanSetTime;
        public string TimeReason;
        public int CurrentHour;
        public int PreviewHour;
        public string CurrentTimeText;   // 当前整点，例如 "08:00"
        public string PreviewTimeText;   // 预览整点，例如 "12:00"
        public string CurrentPhaseText;  // DayNightClock 给出的当前阶段，例如 "白昼"
        public string PreviewPhaseText;  // 预览阶段
        public string TimeSummary;
    }
    public sealed class CampPresenter : GameplayPresenter<CampState>
    {
        // 轮盘只在确认时调用服务切换时间；点击整点仅改变这里的预览值。
        private bool _wheelOpen;
        private int _previewHour;

        protected override void OnAttach() { RunControlModel.SetPlayerInput(false); base.OnAttach(); }
        protected override void CloseView()
        {
            // 轮盘打开时，关闭请求先退回扎营菜单；不释放操作锁，避免面板仍在却恢复玩家输入。
            if (_wheelOpen)
            {
                _wheelOpen = false;
                _previewHour = CurrentHour;
                Refresh();
                return;
            }
            RunControlModel.SetPlayerInput(true);
            base.CloseView();
        }
        public void ReleaseControl() { RunControlModel.SetPlayerInput(true); }

        /// <summary>局内时钟当前整点（0~23）。子时包含 23 点与 0 点两个整点格。</summary>
        private static int CurrentHour
        {
            get
            {
                // RunMinutes 为局内全局分钟数；统一折算到 24 小时制整点。
                int hour = Mathf.FloorToInt(DayNightClock.RunMinutes / 60f) % 24;
                return NormalizeHour(hour);
            }
        }

        private static int NormalizeHour(int hour)
        {
            hour %= 24;
            return hour < 0 ? hour + 24 : hour;
        }

        /// <summary>把 0~23 整点格式化为 HH:00；阶段文本由 DayNightClock 独立给出。</summary>
        private static string FormatHour(int hour)
        {
            return NormalizeHour(hour).ToString("00") + ":00";
        }

        protected override CampState CreateState()
        {
            bool heal = CampService.CanHeal(out string healReason), refill = CampService.CanRefill(out string refillReason);
            int currentHour = CurrentHour;
            // 轮盘未打开时预览值跟随当前时间；打开后保留玩家点选的整点，直到确认或取消。
            if (!_wheelOpen) _previewHour = currentHour;
            bool canSetTime = CampService.CanSetTime(out string setTimeReason);
            float currentMinute = DayNightClock.RunMinutes;
            string currentTimeText = currentHour.ToString("00") + ":" + (Mathf.FloorToInt(currentMinute) % 60).ToString("00");
            string previewTimeText = FormatHour(_previewHour);
            string currentPhaseText = DayNightClock.DescribeRunMinute(currentMinute);
            string previewPhaseText = DayNightClock.DescribeRunHour(_previewHour);

            return new CampState
            {
                CanHeal = heal, CanRefill = refill, InRun = DispatchService.Instance.IsInRun,
                State = "药品：" + CampService.MedicineCount + "　　" + (CampService.IsInSafeRoom ? "安全房间（可扎营）" : "战斗中无法扎营")
                    + (CampService.CampUsed ? "　　本局已扎营" : ""),
                Hint = heal || refill ? "选择一项修整（扎营每局 1 次）" : !CampService.IsInSafeRoom ? "战斗中无法扎营" : healReason ?? refillReason,
                WheelOpen = _wheelOpen,
                CanSetTime = canSetTime,
                TimeReason = setTimeReason,
                CurrentHour = currentHour,
                PreviewHour = _previewHour,
                CurrentTimeText = currentTimeText,
                PreviewTimeText = previewTimeText,
                CurrentPhaseText = currentPhaseText,
                PreviewPhaseText = previewPhaseText,
                TimeSummary = "当前时间：" + currentTimeText + " · " + currentPhaseText
            };
        }

        protected override void Execute(PanelIntent intent)
        {
            string reason;
            bool ok;

            if (intent.Action == PanelAction.Toggle)
            {
                // 第一次 Toggle 只打开轮盘；打开期间 Toggle 用于预览某个整点，不直接改时间。
                if (!_wheelOpen)
                {
                    _wheelOpen = true;
                    _previewHour = CurrentHour;
                }
                else
                {
                    _previewHour = NormalizeHour((int)(intent.Id % 24u));
                }
                Refresh();
                return;
            }

            if (intent.Action == PanelAction.Cancel)
            {
                // Esc / 返回扎营：只关闭轮盘，保留当前时间，下一次 Esc 才关闭扎营面板。
                if (!_wheelOpen) return;
                _wheelOpen = false;
                _previewHour = CurrentHour;
                Refresh();
                return;
            }

            if (intent.Action == PanelAction.Confirm)
            {
                if (!_wheelOpen) return;
                int targetHour = _previewHour;
                string targetText = FormatHour(targetHour) + " · " + DayNightClock.DescribeRunHour(targetHour);
                // 安全区、次数等限制全部由 CampService 判定；失败时保留轮盘并显示服务给出的原因。
                ok = CampService.TrySetTime(targetHour, out reason);
                if (ok)
                {
                    _wheelOpen = false;
                    _previewHour = CurrentHour;
                }
                Refresh();
                Message(ok ? "时辰已调整到 " + targetText : reason ?? "当前无法调整时间", ok);
                return;
            }

            if (intent.Action == PanelAction.Heal) ok = CampService.TryHeal(out reason);
            else if (intent.Action == PanelAction.Refill) ok = CampService.TryRefill(out reason);
            else return;
            Refresh();
            Message(ok ? (intent.Action == PanelAction.Heal ? "完全治疗完成：hp 已回满，消耗 1 药" : "倒地门客已半血复活，消耗 2 药") : reason, ok);
        }
    }
}
