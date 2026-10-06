---
uid: f1c00051
id: project-chaos.framework.events.manager
parent: project-chaos.framework.events
name: {zh: "事件总线", en: "Event Manager"}
description:
  zh: >
      EventManager 事件总线：按字符串事件名增删监听、带/不带参数触发、清空全部监听，基于 C# EventHandler。
      
  en: >
      EventManager: the string-keyed event bus (add/remove/listen, trigger with or without args, clear) using C# EventHandler.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.744Z"
fingerprint: 9854ccf84a246e351f41673ad509aba51fb3f8672672b40b8349e2d762ae9b2a
source:
  - path: "Assets/Scripts/Runtime/GameBase/EventCenter/EventManager.cs"
    line: 24
    end_line: 78
apis:
  - protocol: rpc
    path: "EventManager.AddListener"
    description:
      zh: >
          为事件名添加监听。
          
      en: >
          Adds a listener for an event name.
          
  - protocol: rpc
    path: "EventManager.RemoveListener"
    description:
      zh: >
          移除监听。
          
      en: >
          Removes a listener.
          
  - protocol: rpc
    path: "EventManager.TriggerEvent"
    description:
      zh: >
          触发事件（可带参数）。
          
      en: >
          Triggers an event (with optional args).
          
  - protocol: rpc
    path: "EventManager.Clear"
    description:
      zh: >
          清空全部监听（防切场景内存泄漏）。
          
      en: >
          Clears all listeners (scene transition hygiene).
          
---
