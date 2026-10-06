---
uid: f1c0005d
id: project-chaos.framework.dialogue.typing
parent: project-chaos.framework.dialogue
name: {zh: "对话内部实现", en: "Dialogue Playback Internals"}
description:
  zh: >
      对话播放内部实现：打字机协程、自动跳过协程、选项按钮生成与清理、选项选择处理与 DialogueEvent 分发。
      
  en: >
      Dialogue playback internals: typewriter coroutine, auto-skip coroutine, option button spawning/clearing, option selection and DialogueEvent dispatch.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.743Z"
fingerprint: 1fa134b12c96248ccdfb2d4f0793b0cdde93715c85edca41380eb735cccfe524
source:
  - path: "Assets/Scripts/Runtime/GameBase/DialogueManager/DialogueManager.cs"
    line: 220
    end_line: 416
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/DialogueManager/DialogueManager.cs#L220-L416"
    description:
      zh: >
          打字机、自动跳过、选项 UI 与事件分发实现段。
          
      en: >
          Typewriter, auto-skip, options UI and event dispatch internals.
          
deps:
  - kind: reference
    to: project-chaos.framework.dialogue.data
    from_api: "file:Assets/Scripts/Runtime/GameBase/DialogueManager/DialogueManager.cs#L220-L416"
    to_api: "rpc:DialogueEntry.dialogueText"
    label: {zh: "条目文本", en: "Entry text"}
---
