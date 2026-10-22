using System;
using System.Collections.Generic;

namespace Chaos.MVP.Model.Settings
{
    /// <summary>Owns one settings edit transaction, including temporary audio/input effects.</summary>
    public sealed class SettingsEditingModel : IDisposable
    {
        private readonly SettingsManager _settings;
        private bool _opened;
        public bool Completed { get; private set; }
        public int Category { get; set; }
        public event Action Changed;
        public SettingsEditingModel()
        {
            _settings = SettingsManager.Instance;
            _settings.OnPendingChanged += HandleChanged;
        }
        private void HandleChanged() { Changed?.Invoke(); }
        public SettingsData Pending => _settings.Pending;
        public bool HasChanges => _settings.HasPendingChanges;
        public void EnsureEdit()
        {
            if (_opened) return;
            _opened = true;
            _settings.BeginEdit();
        }
        public void Commit() { _settings.CommitEdit(); }
        public void ResetCategory() { _settings.ResetPendingSection((SettingCategory)Category); }
        public void NotifyPendingChanged() { _settings.NotifyPendingChanged(); }
        public void Finish(bool apply)
        {
            if (apply) _settings.CommitEdit(); else _settings.RevertEdit();
            Completed = true;
        }
        public void PreviewVolume(AudioChannel channel, int value) { AudioManager.Instance.SetVolume(channel, value); }
        public List<ResolutionOption> GetAvailableResolutions() { return ResolutionHelper.GetAvailableOptions(); }
        public void SnapResolution(List<ResolutionOption> options)
        {
            SettingsData data = Pending;
            if (data == null || options == null || options.Count == 0
                || ResolutionHelper.IndexOf(options, data.resolutionWidth, data.resolutionHeight) >= 0) return;
            int index = ResolutionHelper.IndexOfClosest(options, data.resolutionWidth, data.resolutionHeight);
            ResolutionOption closest = options[Math.Max(0, Math.Min(index, options.Count - 1))];
            data.resolutionWidth = closest.Width;
            data.resolutionHeight = closest.Height;
            NotifyPendingChanged();
        }
        public bool IsRebinding => InputManager.Instance.IsRebinding;
        public InputManager.BindingVisual GetBindingVisual(string action, InputManager.BindingKind kind, InputDeviceType device, string part = null)
        { return InputManager.Instance.GetBindingVisual(action, kind, device, part); }
        public List<InputManager.RebindStep> GetRebindSteps(string action, InputManager.BindingKind kind, string part = null)
        { return InputManager.Instance.GetRebindSteps(action, kind, part); }
        public void BeginRebind(string action, int index, InputManager.BindingKind kind, Action<InputManager.RebindResult> callback)
        { InputManager.Instance.BeginRebind(action, index, kind, callback); }
        public void CancelRebind() { InputManager.Instance.CancelRebind(); }
        public bool ResetBinding(string action, InputManager.BindingKind kind, string part = null)
        { return InputManager.Instance.ResetBinding(action, kind, part); }
        public void ClearOverridesFor(InputManager.BindingKind kind) { InputManager.Instance.ClearOverridesFor(kind); }
        public string SaveOverridesJson() { return InputManager.Instance.SaveOverridesJson(); }
        public void SyncOverrides()
        {
            if (Pending == null) return;
            Pending.inputOverridesJson = SaveOverridesJson();
            NotifyPendingChanged();
        }
        public void Dispose()
        {
            _settings.OnPendingChanged -= HandleChanged;
            CancelRebind();
            if (_opened && !Completed) _settings.RevertEdit();
            Changed = null;
        }
    }
}
