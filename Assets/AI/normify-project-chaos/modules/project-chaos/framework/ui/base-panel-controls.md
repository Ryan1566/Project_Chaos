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
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:47.489Z"
fingerprint: 12e65e87641cb322ea210e0c11f8ee7f7cc3eef99bd353328e85b9c0cc5d75eb
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
