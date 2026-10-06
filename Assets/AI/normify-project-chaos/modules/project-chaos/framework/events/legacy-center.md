---
uid: f1c00053
id: project-chaos.framework.events.legacy-center
parent: project-chaos.framework.events
name: {zh: "已废弃 EventCenter", en: "Legacy EventCenter"}
description:
  zh: >
      旧版 EventCenter（含 IEventInfo/EventInfo<T>）已标记 [Obsolete(..., true)]：引用即编译错误，应改用 EventManager。属死代码。
      
  en: >
      Legacy EventCenter (with IEventInfo/EventInfo<T>). Marked [Obsolete(..., true)]: referencing this class is a compile error — use EventManager instead. Dead code.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.744Z"
fingerprint: 8a057c78171277edb4c32ee14fc4e3559f6c7da0b7265806ae5e4b0972b3247d
source:
  - path: "Assets/Scripts/Runtime/GameBase/EventCenter/EventCenter.cs"
    line: 1
    end_line: 105
apis:
  - protocol: rpc
    path: "EventCenter.AddEventListener"
    description:
      zh: >
          添加泛型事件监听（已废弃）。
          
      en: >
          Adds a typed event listener (obsolete).
          
  - protocol: rpc
    path: "EventCenter.RemoveEventListener"
    description:
      zh: >
          移除泛型事件监听（已废弃）。
          
      en: >
          Removes a typed event listener (obsolete).
          
  - protocol: rpc
    path: "EventCenter.EventTrigger"
    description:
      zh: >
          触发泛型事件（已废弃）。
          
      en: >
          Triggers a typed event (obsolete).
          
  - protocol: rpc
    path: "EventCenter.Clear"
    description:
      zh: >
          清空全部监听（已废弃）。
          
      en: >
          Clears all listeners (obsolete).
          
---
