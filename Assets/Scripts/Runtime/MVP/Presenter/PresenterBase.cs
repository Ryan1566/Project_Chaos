using System;

namespace Chaos.MVP.Presenter
{
    /// <summary>A panel owns its presenter attachment; cached panels create a fresh presenter on re-entry.</summary>
    public abstract class PresenterBase<TView> : IDisposable where TView : class
    {
        protected TView View { get; private set; }
        public bool IsDisposed { get; private set; }

        public void Attach(TView view)
        {
            if (IsDisposed) throw new ObjectDisposedException(GetType().Name);
            if (view == null) throw new ArgumentNullException(nameof(view));
            if (View != null) return;
            View = view;
            try { OnAttach(); }
            catch { Dispose(); throw; }
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            try { OnDetach(); }
            finally { View = null; }
        }

        protected abstract void OnAttach();
        protected abstract void OnDetach();
    }

    public interface IPanelView<TState, TIntent>
    {
        event Action<TIntent> Intent;
        void Render(TState state);
    }
}
