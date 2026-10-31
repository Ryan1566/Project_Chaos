using System;
using UnityEngine;

namespace Chaos.Gameplay.Interaction
{
    /// <summary>Opens a content session for a presentation listener; contains no UI or panel types.</summary>
    public static class RunContentService
    {
        public static RunContentSession Current { get; private set; }
        public static event Action<RunContentSession> OpenRequested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            Current = null;
            OpenRequested = null;
        }

        internal static bool RequestOpen(RunContentInteractable target, GameObject actor, out string reason)
        {
            reason = string.Empty;
            if (target == null || actor == null || target.Definition == null || target.Map == null)
            { reason = "交互内容尚未配置"; return false; }
            if (!target.isActiveAndEnabled || !target.IsRegistered)
            { reason = "交互目标当前不可用"; return false; }
            var game = GameSession.Instance;
            if (game == null || !game.HasSession) { reason = "没有有效存档会话"; return false; }
            var listener = OpenRequested;
            if (listener == null) { reason = "当前没有界面接收交互请求"; return false; }
            if (Current != null) Current.Close();
            var session = new RunContentSession(target, actor);
            target.BindSession(session);
            Current = session;
            try { listener(session); }
            catch (Exception e)
            {
                Debug.LogException(e);
                session.Close();
                reason = "交互界面打开失败";
                return false;
            }
            if (Current != session) { reason = "交互界面未保持会话"; return false; }
            return true;
        }

        internal static void CloseForTarget(RunContentInteractable target)
        {
            var session = Current;
            if (session != null && session.Target == target) session.Close();
        }

        internal static void NotifySessionClosed(RunContentSession session)
        {
            if (Current == session) Current = null;
        }
    }
}
