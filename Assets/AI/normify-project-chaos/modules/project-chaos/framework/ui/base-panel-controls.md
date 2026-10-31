---
uid: f1c00015
id: project-chaos.framework.ui.base-panel-controls
parent: project-chaos.framework.ui
name: {zh: "面板光标与暂停", en: "BasePanel Cursor & Pause"}
description:
  zh: >
      BasePanel 的光标与时间辅助：CursorEnable/CursorHide 控制鼠标、PauseTime/ResumeTime 控制 Time.timeScale、IsShowing 暴露显示状态。
      
  en: >
      BasePanel cursor and time helpers: CursorEnable/CursorHide, PauseTime/ResumeTime, and the IsShowing flag.
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:20:37.067Z"
fingerprint: ebb1fa882d35195590c780bc18136eef7d893cae33ecdea1ab78c58c6e32b01b
source:
  - path: "Assets/Scripts/Runtime/GameBase/UIManager/BasePanel.cs"
    line: 114
    end_line: 155
apis:
  - protocol: rpc
    path: "BasePanel.CursorEnable"
    description:
      zh: >
          显示鼠标光标。
          
      en: >
          Shows the cursor.
          
  - protocol: rpc
    path: "BasePanel.CursorHide"
    description:
      zh: >
          隐藏鼠标光标。
          
      en: >
          Hides the cursor.
          
  - protocol: rpc
    path: "BasePanel.PauseTime"
    description:
      zh: >
          暂停/恢复游戏时间（本面板范围内）。
          
      en: >
          Pauses/unpauses game time for this panel.
          
  - protocol: rpc
    path: "BasePanel.ResumeTime"
    description:
      zh: >
          恢复时间缩放。
          
      en: >
          Restores time scale.
          
  - protocol: rpc
    path: "BasePanel.IsShowing"
    description:
      zh: >
          面板当前是否显示中。
          
      en: >
          Whether the panel is currently showing.
          
---
