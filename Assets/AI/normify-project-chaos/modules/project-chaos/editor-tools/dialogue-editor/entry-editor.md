---
uid: e0a10502
id: project-chaos.editor-tools.dialogue-editor.entry-editor
parent: project-chaos.editor-tools.dialogue-editor
name: {zh: "对话条目编辑", en: "Dialogue Entry Editor"}
description:
  zh: >
      右栏编辑：单条台词的文本、打字速度、自动跳过延迟、语音覆盖与下一条 ID；嵌套的选项与触发事件编辑器；人物名/ID 查找辅助；以及新建、打开、保存与追加台词条目。
      
  en: >
      Right-panel editing: per-entry text, typing speed, auto-skip delay, voice override and next-dialogue id; nested editors for branch options and triggered events; character name/id lookup helpers; and creating, opening, saving and appending dialogue entries.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.733Z"
fingerprint: 314cafcafdf79d46ed2d2fa1c1ecbb76eb765924249343fc8410de9f53662ae7
source:
  - path: "Assets/Scripts/Editor/DialogueEditor/DialogueEditorWindow.cs"
    line: 289
    end_line: 566
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/DialogueEditor/DialogueEditorWindow.cs#L289-L566"
    description:
      zh: >
          条目/选项/事件编辑与资产读写所在行段。
          
      en: >
          Line range holding the entry/option/event editors and asset IO.
          
  - protocol: rpc
    path: "DialogueEditorWindow.DrawDialogueEntryEditor"
    description:
      zh: >
          编辑单条台词（文本、速度、语音、下一条）。
          
      en: >
          Edits one dialogue entry (text, speed, voice, next).
          
  - protocol: rpc
    path: "DialogueEditorWindow.DrawDialogueOptions"
    description:
      zh: >
          编辑某条台词的对话选项。
          
      en: >
          Edits a dialogue entry's branch options.
          
  - protocol: rpc
    path: "DialogueEditorWindow.DrawDialogueEvents"
    description:
      zh: >
          编辑某条台词触发的事件列表。
          
      en: >
          Edits the events triggered by an entry.
          
  - protocol: rpc
    path: "DialogueEditorWindow.SaveDialogue"
    description:
      zh: >
          把编辑过的对话资产写盘。
          
      en: >
          Saves the edited dialogue asset to disk.
          
  - protocol: rpc
    path: "DialogueEditorWindow.CreateNewDialogue"
    description:
      zh: >
          新建一份 DialogData 资产。
          
      en: >
          Creates a new DialogData asset.
          
deps:
  - kind: reference
    to: project-chaos.editor-tools.dialogue-editor.data-model
    from_api: "rpc:DialogueEditorWindow.SaveDialogue"
    to_api: "rpc:DialogData"
    label: {zh: "读写 DialogData 字段", en: "Persists DialogData fields"}
---
