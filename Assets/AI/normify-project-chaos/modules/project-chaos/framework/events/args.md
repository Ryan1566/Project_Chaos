---
uid: f1c0004f
id: project-chaos.framework.events.args
parent: project-chaos.framework.events
name: {zh: "事件参数", en: "Event Args"}
description:
  zh: >
      事件中心参数族：InputArgs、存档/加载与设置事件参数、加载进度参数、委派任务参数，均继承 EventArgs。
      
  en: >
      EventArgs family for the event bus: InputArgs, Saving/LoadingEventArgs, Saving/LoadingSettingEventArgs, LoadingProgressEventArgs, DelegateQuestEventArgs.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.743Z"
fingerprint: 40124927494b7719ae70baebfcc8b18b6cd4d368177ce432df66971ef1d6c0c0
source:
  - path: "Assets/Scripts/Runtime/GameBase/EventCenter/CustomEventArgs.cs"
    line: 1
    end_line: 72
apis:
  - protocol: rpc
    path: "InputArgs.keyCodeValue"
    description:
      zh: >
          按键事件携带的 KeyCode。
          
      en: >
          KeyCode carried by the key event.
          
  - protocol: rpc
    path: "LoadingEventArgs.a_operation"
    description:
      zh: >
          加载事件携带的异步操作。
          
      en: >
          AsyncOperation carried by the loading event.
          
  - protocol: rpc
    path: "LoadingProgressEventArgs.a_sceneName"
    description:
      zh: >
          加载进度事件携带的目标场景名。
          
      en: >
          Target scene name carried by the progress event.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/EventCenter/CustomEventArgs.cs"
    description:
      zh: >
          事件参数类族脚本资产。
          
      en: >
          Event-args class family script asset.
          
deps:
  - kind: reference
    to: project-chaos.framework.settings.data
    from_api: "file:Assets/Scripts/Runtime/GameBase/EventCenter/CustomEventArgs.cs"
    to_api: "rpc:SettingsData.version"
    label: {zh: "设置数据", en: "Settings payload"}
---
