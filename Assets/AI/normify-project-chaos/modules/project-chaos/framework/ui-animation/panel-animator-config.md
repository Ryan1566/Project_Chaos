---
uid: f1c00018
id: project-chaos.framework.ui-animation.panel-animator-config
parent: project-chaos.framework.ui-animation
name: {zh: "动画器配置", en: "Animator Config"}
description:
  zh: >
      UIPanelAnimator 的 Inspector 配置与运行时状态：进场/退场类型与缓动、滑入距离、依次入场参数、过渡期输入遮罩、播放状态。
      
  en: >
      UIPanelAnimator inspector configuration: enter/exit types, easing, slide distance, stagger settings, input blocking and runtime state.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.758Z"
fingerprint: ca438f41c73fb26879bac743ccf5c9ab68e78458e1ca3ba696fe70887649c502
source:
  - path: "Assets/Scripts/Runtime/GameBase/UIManager/UIPanelAnimator.cs"
    line: 1
    end_line: 145
apis:
  - protocol: rpc
    path: "UIPanelAnimator.enterType"
    description:
      zh: >
          进场动画类型（序列化字段）。
          
      en: >
          Enter animation type (serialized field).
          
  - protocol: rpc
    path: "UIPanelAnimator.exitType"
    description:
      zh: >
          退场动画类型（序列化字段）。
          
      en: >
          Exit animation type (serialized field).
          
  - protocol: rpc
    path: "UIPanelAnimator.staggerEnabled"
    description:
      zh: >
          是否启用子元素依次入场。
          
      en: >
          Whether staggered child entrance is enabled.
          
  - protocol: rpc
    path: "UIPanelAnimator.IsPlaying"
    description:
      zh: >
          当前是否有动画在播放。
          
      en: >
          Whether an animation is currently playing.
          
deps:
  - kind: reference
    to: project-chaos.framework.ui-animation.panel-anim-type
    from_api: "rpc:UIPanelAnimator.enterType"
    to_api: "file:Assets/Scripts/Runtime/GameBase/UIManager/PanelAnimType.cs"
    label: {zh: "动画类型", en: "Animation type"}
---
