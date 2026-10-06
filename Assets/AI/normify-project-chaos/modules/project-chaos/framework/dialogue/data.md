---
uid: f1c0005b
id: project-chaos.framework.dialogue.data
parent: project-chaos.framework.dialogue
name: {zh: "对话数据", en: "Dialogue Data"}
description:
  zh: >
      对话数据模型：DialogueData 配置资产（人物、对话条目、跳过/自动/历史/暂停开关）与人物/条目/选项/事件子结构。
      
  en: >
      Dialogue data model: DialogueData ScriptableObject (characters, entries, flags) plus DialogueCharacter/Entry/Option/Event structs.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.742Z"
fingerprint: ec8bceeceb35a63db776963906f254e0cc50dcf92eed709a09cbf797c67e08f2
source:
  - path: "Assets/Scripts/Runtime/GameBase/DialogueManager/DialogueData.cs"
    line: 1
    end_line: 75
apis:
  - protocol: rpc
    path: "DialogueData.dialogueId"
    description:
      zh: >
          对话 ID 字段。
          
      en: >
          Dialogue id field.
          
  - protocol: rpc
    path: "DialogueData.characters"
    description:
      zh: >
          参与对话的人物列表。
          
      en: >
          Participating characters list.
          
  - protocol: rpc
    path: "DialogueEntry.dialogueText"
    description:
      zh: >
          单条对话的文本。
          
      en: >
          Text of one dialogue entry.
          
  - protocol: rpc
    path: "DialogueOption.optionText"
    description:
      zh: >
          单个选项的文本。
          
      en: >
          Text of one dialogue option.
          
---
