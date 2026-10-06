---
uid: f1c00019
id: project-chaos.framework.ui-animation.panel-animator-play
parent: project-chaos.framework.ui-animation
name: {zh: "动画播放接口", en: "Animator Playback API"}
description:
  zh: >
      动画公开接口：PlayEnter/PlayExit 播放与立即落位、KillTweens 中断、SnapToRest 贴回、RecaptureRest 重新录制静止状态。
      
  en: >
      Public animation API: PlayEnter/PlayExit, KillTweens, SnapToRest and RecaptureRest (rest pose recording).
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.759Z"
fingerprint: ca438f41c73fb26879bac743ccf5c9ab68e78458e1ca3ba696fe70887649c502
source:
  - path: "Assets/Scripts/Runtime/GameBase/UIManager/UIPanelAnimator.cs"
    line: 146
    end_line: 300
apis:
  - protocol: rpc
    path: "UIPanelAnimator.PlayEnter"
    description:
      zh: >
          播放进场动画（instant 直接落位）。
          
      en: >
          Plays the enter animation (instant skips tweens).
          
  - protocol: rpc
    path: "UIPanelAnimator.PlayExit"
    description:
      zh: >
          播放退场动画（instant 直接落位）。
          
      en: >
          Plays the exit animation (instant skips tweens).
          
  - protocol: rpc
    path: "UIPanelAnimator.KillTweens"
    description:
      zh: >
          杀掉所有正在跑的补间。
          
      en: >
          Kills all running tweens.
          
  - protocol: rpc
    path: "UIPanelAnimator.SnapToRest"
    description:
      zh: >
          立即贴回录制的静止状态。
          
      en: >
          Snaps the panel to its recorded rest pose.
          
  - protocol: rpc
    path: "UIPanelAnimator.RecaptureRest"
    description:
      zh: >
          把当前姿态重新录制为静止状态。
          
      en: >
          Recaptures the current pose as the rest state.
          
deps:
  - kind: call
    to: project-chaos.framework.ui-animation.panel-animator-rest
    from_api: "rpc:UIPanelAnimator.PlayEnter"
    to_api: "file:Assets/Scripts/Runtime/GameBase/UIManager/UIPanelAnimator.cs#L301-L517"
    label: {zh: "静止状态", en: "Rest state"}
  - kind: call
    to: project-chaos.framework.ui-animation.panel-animator-stagger
    from_api: "rpc:UIPanelAnimator.PlayEnter"
    to_api: "file:Assets/Scripts/Runtime/GameBase/UIManager/UIPanelAnimator.cs#L518-L677"
    label: {zh: "依次入场", en: "Staggered entrance"}
---
