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
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.760Z"
fingerprint: 6f86e6829ce665ae2883c79887db6160eb8f54948fc32f05bb7e05e338fa659a
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
