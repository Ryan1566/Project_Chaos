---
uid: f1c00052
id: project-chaos.framework.events.extension
parent: project-chaos.framework.events
name: {zh: "事件触发拓展", en: "Event Trigger Extension"}
description:
  zh: >
      EventTriggerExt 拓展方法：任意对象一行代码即可通过总线触发事件（带或不带 EventArgs）。
      
  en: >
      EventTriggerExt: extension methods letting any object raise events through the bus in one line (with or without EventArgs).
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.743Z"
fingerprint: 9854ccf84a246e351f41673ad509aba51fb3f8672672b40b8349e2d762ae9b2a
source:
  - path: "Assets/Scripts/Runtime/GameBase/EventCenter/EventManager.cs"
    line: 1
    end_line: 23
apis:
  - protocol: rpc
    path: "EventTriggerExt.TriggerEvent"
    description:
      zh: >
          TriggerEvent 拓展方法（this 对象作为发送者）。
          
      en: >
          TriggerEvent extension method on any object (sender becomes this).
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/EventCenter/EventManager.cs#L1-L23"
    description:
      zh: >
          触发事件拓展方法实现段。
          
      en: >
          Trigger extension methods script block.
          
deps:
  - kind: call
    to: project-chaos.framework.events.manager
    from_api: "rpc:EventTriggerExt.TriggerEvent"
    to_api: "rpc:EventManager.TriggerEvent"
    label: {zh: "触发事件", en: "Trigger event"}
---
