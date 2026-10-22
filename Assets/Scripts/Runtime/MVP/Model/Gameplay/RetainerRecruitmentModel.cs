using System;
using UnityEngine;

namespace Chaos.MVP.Model.Gameplay
{
    /// <summary>Recruitment is a saved transaction, independent of the panel showing it.</summary>
    public sealed class RetainerRecruitmentModel
    {
        private bool _busy;
        private int _lastFrame = -1;
        public bool CanRecruit => !_busy && RetainerService.Instance.Count < RetainerService.MaxRetainers;

        public bool TryRecruit(out uint recruitedId, out string reason)
        {
            recruitedId = 0;
            reason = null;
            if (_busy || _lastFrame == Time.frameCount) { reason = "招募处理中，请稍候"; return false; }
            _lastFrame = Time.frameCount;
            _busy = true;
            SavePayload payload = MetaProgressService.Instance.Payload;
            string before = null;
            try
            {
                before = JsonUtility.ToJson(payload);
                RetainerSave recruited = RetainerService.Instance.RecruitRandom();
                if (recruited == null)
                {
                    reason = RetainerService.Instance.Count >= RetainerService.MaxRetainers ? "门客已达上限" : "招募失败，请稍后重试";
                    return false;
                }
                if (!MetaProgressService.Instance.SaveProgress(out reason))
                {
                    JsonUtility.FromJsonOverwrite(before, payload);
                    return false;
                }
                recruitedId = recruited.id;
                return true;
            }
            catch (Exception error)
            {
                if (before != null) JsonUtility.FromJsonOverwrite(before, payload);
                reason = "招募失败：" + error.Message;
                return false;
            }
            finally { _busy = false; }
        }
    }
}
