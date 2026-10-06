---
uid: e0a10503
id: project-chaos.editor-tools.dialogue-editor.data-model
parent: project-chaos.editor-tools.dialogue-editor
name: {zh: "对话数据模型", en: "Dialogue Data Model"}
description:
  zh: >
      对话编辑器与运行时共用的可序列化数据模型：DialogData（对话 ID、名称、描述、人物、条目、可否跳过、自动播放、显示历史、暂停游戏）及 DialogCharacter / DialogEntry / DialogOption / DialogEvent，均标 [Serializable]。
      
  en: >
      Serializable data model shared by the dialogue editor and runtime: DialogData (dialogue id, name, description, characters, entries, canSkip, autoPlay, showHistory, pauseGame) plus DialogCharacter, DialogEntry, DialogOption and DialogEvent, all [Serializable] for Unity serialization.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.733Z"
fingerprint: c33733d11e852487a14edfb5b00eb60fa7ad7e94fc1f0c032dd7ffc13e62906f
source:
  - path: "Assets/Scripts/Editor/DialogueEditor/DialogData.cs"
    line: 8
    end_line: 75
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/DialogueEditor/DialogData.cs#L8-L75"
    description:
      zh: >
          DialogData 资产类型及其嵌套数据类。
          
      en: >
          The DialogData ScriptableObject and its nested data classes.
          
  - protocol: rpc
    path: "DialogData"
    description:
      zh: >
          可序列化的对话资产根（ID、条目、开关）。
          
      en: >
          Serializable dialogue asset root (id, entries, flags).
          
  - protocol: rpc
    path: "DialogCharacter"
    description:
      zh: >
          参与对话的人物（ID、名称、颜色、头像、语音）。
          
      en: >
          A participating character (id, name, colour, avatar, voice).
          
  - protocol: rpc
    path: "DialogEntry"
    description:
      zh: >
          一条台词及选项、事件与下一条 ID。
          
      en: >
          One dialogue entry with options, events and next id.
          
  - protocol: rpc
    path: "DialogOption"
    description:
      zh: >
          一个对话选项及其事件与跳转目标。
          
      en: >
          A branch option with its own events and jump target.
          
  - protocol: rpc
    path: "DialogEvent"
    description:
      zh: >
          带字符串参数的事件条目。
          
      en: >
          A named event with a string parameter.
          
---
