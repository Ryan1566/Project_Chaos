---
uid: f1c00050
id: project-chaos.framework.events.const-names
parent: project-chaos.framework.events
name: {zh: "事件名常量", en: "Event Name Constants"}
description:
  zh: >
      EventConstName 事件名常量：按键松开/按下、设置保存/读取、场景加载进度；旧存档/任务事件名已注释保留。
      
  en: >
      EventConstName: string constants for the event bus (key up/down, save/load settings, scene load progress). Several legacy names are commented out.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:56:54.870Z"
fingerprint: b0c7b3042692c51b557ffea6b3e5a9ed8c36ecde05908d8a50ca0a1cfed4e3de
source:
  - path: "Assets/Scripts/Runtime/GameBase/EventCenter/EventConstName.cs"
    line: 1
    end_line: 31
apis:
  - protocol: rpc
    path: "EventConstName.GetKeyDown"
    description:
      zh: >
          按键按下事件名。
          
      en: >
          Key-down event name.
          
  - protocol: rpc
    path: "EventConstName.SaveSetting"
    description:
      zh: >
          设置已应用并落盘事件名。
          
      en: >
          Settings-saved event name.
          
  - protocol: rpc
    path: "EventConstName.LoadSetting"
    description:
      zh: >
          设置已读取事件名。
          
      en: >
          Settings-loaded event name.
          
  - protocol: rpc
    path: "EventConstName.LoadProgress"
    description:
      zh: >
          场景加载进度事件名。
          
      en: >
          Scene loading progress event name.
          
---
