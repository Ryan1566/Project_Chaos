using System;
using ChaosDebug;
using UnityEngine;

namespace Chaos.MVP.Model.Startup
{
    public sealed class MainMenuModel : IDisposable
    {
        public void ShowUpdates() { ChaosLog.Info(LogChannel.UI, "[主菜单] 更新公告暂未开放"); }
        public void Quit() { Application.Quit(); }
        public void Dispose() { }
    }
}
