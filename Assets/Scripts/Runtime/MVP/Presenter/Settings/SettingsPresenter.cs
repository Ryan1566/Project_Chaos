using System;
using System.Collections.Generic;
using Chaos.MVP.Model.Settings;

namespace Chaos.MVP.Presenter.Settings
{
    public enum SettingsAction { Category, Apply, Reset, Return, ConfirmApply, ConfirmDiscard, ConfirmCancel }
    public readonly struct SettingsIntent
    {
        public readonly SettingsAction Action;
        public readonly int Category;
        public SettingsIntent(SettingsAction action, int category = 0) { Action = action; Category = category; }
    }
    public sealed class SettingsState
    {
        public int Category;
        public bool CanApply;
        public bool Confirmation;
        public bool RefreshPage;
    }
    public interface ISettingsView : IPanelView<SettingsState, SettingsIntent>
    {
        bool CanShowConfirmation { get; }
        bool IsSubPageOpen { get; }
        void BindEditing(SettingsPresenter presenter);
        void CancelPendingEdit();
        void CloseSubPage();
    }
    public sealed class SettingsPresenter : PresenterBase<ISettingsView>
    {
        private readonly SettingsEditingModel _model;
        private bool _confirming;
        public SettingsPresenter(SettingsEditingModel model) { _model = model ?? throw new ArgumentNullException(nameof(model)); }
        protected override void OnAttach()
        {
            View.BindEditing(this);
            _model.EnsureEdit();
            View.Intent += Handle;
            _model.Changed += PendingChanged;
            Render(true);
        }
        protected override void OnDetach()
        {
            _model.Changed -= PendingChanged;
            if (View != null) View.Intent -= Handle;
        }
        private void PendingChanged() { if (!IsDisposed) Render(false); }
        private void Render(bool refresh)
        { View.Render(new SettingsState { Category = _model.Category, CanApply = _model.HasChanges, Confirmation = _confirming, RefreshPage = refresh }); }
        private void Handle(SettingsIntent intent)
        {
            View.CancelPendingEdit();
            switch (intent.Action)
            {
                case SettingsAction.Category:
                    if (intent.Category < 0 || intent.Category >= 4) return;
                    _model.Category = intent.Category;
                    break;
                case SettingsAction.Apply: _model.Commit(); _confirming = false; break;
                case SettingsAction.Reset: _model.ResetCategory(); break;
                case SettingsAction.Return:
                    if (View.IsSubPageOpen) { View.CloseSubPage(); Render(false); return; }
                    if (_model.HasChanges && View.CanShowConfirmation) { _confirming = true; break; }
                    _model.Finish(false); UIManager.Instance.PopPanel(); return;
                case SettingsAction.ConfirmApply: _model.Finish(true); UIManager.Instance.PopPanel(); return;
                case SettingsAction.ConfirmDiscard: _model.Finish(false); UIManager.Instance.PopPanel(); return;
                case SettingsAction.ConfirmCancel: _confirming = false; break;
            }
            if (!IsDisposed) Render(true);
        }
        public SettingsData Pending => IsDisposed ? null : _model.Pending;
        public void NotifyPendingChanged() { if (!IsDisposed) _model.NotifyPendingChanged(); }
        public void PreviewVolume(AudioChannel channel, int value) { if (!IsDisposed) _model.PreviewVolume(channel, value); }
        public List<ResolutionOption> GetAvailableResolutions() { return _model.GetAvailableResolutions(); }
        public void SnapResolution(List<ResolutionOption> options) { if (!IsDisposed) _model.SnapResolution(options); }
        public bool IsRebinding => !IsDisposed && _model.IsRebinding;
        public InputManager.BindingVisual GetBindingVisual(string action, InputManager.BindingKind kind, InputDeviceType device, string part = null)
        { return _model.GetBindingVisual(action, kind, device, part); }
        public List<InputManager.RebindStep> GetRebindSteps(string action, InputManager.BindingKind kind, string part = null)
        { return _model.GetRebindSteps(action, kind, part); }
        public void BeginRebind(string action, int index, InputManager.BindingKind kind, Action<InputManager.RebindResult> callback)
        { if (!IsDisposed) _model.BeginRebind(action, index, kind, callback); }
        public void CancelRebind() { if (!IsDisposed) _model.CancelRebind(); }
        public bool ResetBinding(string action, InputManager.BindingKind kind, string part = null)
        { return !IsDisposed && _model.ResetBinding(action, kind, part); }
        public void ClearOverridesFor(InputManager.BindingKind kind) { if (!IsDisposed) _model.ClearOverridesFor(kind); }
        public string SaveOverridesJson() { return _model.SaveOverridesJson(); }
        public void SyncOverrides() { if (!IsDisposed) _model.SyncOverrides(); }
    }
}
