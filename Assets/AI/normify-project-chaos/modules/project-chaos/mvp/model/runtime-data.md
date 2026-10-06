---
uid: 7d2b4f1f
id: project-chaos.mvp.model.runtime-data
parent: project-chaos.mvp.model
tags: [mvp, model, dialogue]
name: {zh: "对话运行时数据", en: "Dialogue Runtime Data"}
description:
  zh: >
      命名空间 DialogueSystem 下的对话运行时数据：五个 [Serializable] DTO（对话/角色/条目/选项/事件），对应对话编辑器在运行时导出的结构。注意：当前工程里没有任何地方构造或消费这些类型，文件自成一体。
      
  en: >
      Dialogue runtime data in namespace DialogueSystem: five [Serializable] DTOs (runtime dialogue, character, entry, option, event) that mirror what the dialogue editor would export at runtime. Note: nothing in the project currently constructs or consumes these types — the file is self-contained.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.762Z"
fingerprint: cef391333c6c0ca9b5d9a5849cc92a6bc60030ff8c36404835e5f75e23a3f6e3
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/DialogueRuntimeData.cs"
    line: 10
    end_line: 60
apis:
  - protocol: rpc
    path: "DialogueRuntimeData.dialogueEntries"
    description:
      zh: >
          对话条目列表与可跳过/自动播放/显示历史/暂停游戏等开关。
          
      en: >
          Dialogue entry list plus skip/autoplay flags.
          
  - protocol: rpc
    path: "DialogueCharacterRuntime.characterId"
    description:
      zh: >
          角色 id、显示名、名字颜色、头像与语音路径。
          
      en: >
          Character id, name, colour, avatar and voice paths.
          
  - protocol: rpc
    path: "DialogueEntryRuntime.dialogueText"
    description:
      zh: >
          一条对话：文本、打字速度、语音、选项与事件。
          
      en: >
          One line: text, typing speed, voice, options, events.
          
  - protocol: rpc
    path: "DialogueOptionRuntime.nextDialogueId"
    description:
      zh: >
          玩家选项文本与跳转目标对话 id。
          
      en: >
          Player choice text and its jump target.
          
  - protocol: rpc
    path: "DialogueEventRuntime.eventName"
    description:
      zh: >
          对话事件的名称与参数。
          
      en: >
          Name plus parameter of a dialogue event.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/Model/DialogueRuntimeData.cs#L10-L59"
    description:
      zh: >
          五个 [Serializable] 对话运行时 DTO。
          
      en: >
          The five serializable dialogue DTOs.
          
---
