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
            try
            {
                uint id = 0;
                bool ok = MetaCommandRunner.Execute("Retainer.Recruit", (out string why) => {
                    var recruited = RetainerService.Instance.RecruitRandom();
                    why = recruited == null ? (RetainerService.Instance.Count >= RetainerService.MaxRetainers ? "门客已达上限" : "招募失败，请稍后重试") : null;
                    if (recruited != null) id = recruited.id;
                    return recruited != null;
                }, out reason);
                recruitedId = ok ? id : 0; return ok;
            }
            finally { _busy = false; }
        }
    }
}
