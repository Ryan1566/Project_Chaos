---
uid: f1c0005c
id: project-chaos.framework.dialogue.flow
parent: project-chaos.framework.dialogue
name: {zh: "对话播放流程", en: "Dialogue Flow"}
description:
  zh: >
      DialogueManager 播放流程：StartDialogue/EndDialogue、PlayNextEntry 推进条目、JumpToEntry 跳转，并驱动开始/结束 UnityEvent。
      
  en: >
      DialogueManager flow: StartDialogue/EndDialogue, PlayNextEntry and JumpToEntry driving the current entry index and start/end UnityEvents.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.743Z"
fingerprint: 1fa134b12c96248ccdfb2d4f0793b0cdde93715c85edca41380eb735cccfe524
source:
  - path: "Assets/Scripts/Runtime/GameBase/DialogueManager/DialogueManager.cs"
    line: 1
    end_line: 219
apis:
  - protocol: rpc
    path: "DialogueManager.StartDialogue"
    description:
      zh: >
          开始播放一份对话资产。
          
      en: >
          Starts playing a dialogue asset.
          
  - protocol: rpc
    path: "DialogueManager.EndDialogue"
    description:
      zh: >
          结束当前对话。
          
      en: >
          Ends the current dialogue.
          
  - protocol: rpc
    path: "DialogueManager.PlayNextEntry"
    description:
      zh: >
          播放下一条目。
          
      en: >
          Advances to the next entry.
          
  - protocol: rpc
    path: "DialogueManager.JumpToEntry"
    description:
      zh: >
          按 ID 跳转到指定条目。
          
      en: >
          Jumps to an entry by id.
          
  - protocol: rpc
    path: "DialogueManager.IsPlaying"
    description:
      zh: >
          是否正在播放对话。
          
      en: >
          Whether a dialogue is playing.
          
deps:
  - kind: reference
    to: project-chaos.framework.dialogue.data
    from_api: "rpc:DialogueManager.StartDialogue"
    to_api: "rpc:DialogueData.dialogueId"
    label: {zh: "对话资产", en: "Dialogue asset"}
  - kind: call
    to: project-chaos.framework.logging.core
    from_api: "rpc:DialogueManager.StartDialogue"
    to_api: "rpc:ChaosLog.Write"
    label: {zh: "对话日志", en: "Dialogue logging"}
  - kind: call
    to: project-chaos.framework.dialogue.typing
    from_api: "rpc:DialogueManager.PlayNextEntry"
    to_api: "file:Assets/Scripts/Runtime/GameBase/DialogueManager/DialogueManager.cs#L220-L416"
    label: {zh: "播放内部实现", en: "Playback internals"}
---
