---
uid: f1c00014
id: project-chaos.framework.ui.base-panel
parent: project-chaos.framework.ui
name: {zh: "BasePanel 基类", en: "BasePanel Lifecycle"}
description:
  zh: >
      BasePanel 面板基类：OnEnter/OnExit 生命周期钩子、Show/Hide 带显隐动画（可 instant）、Animator 属性暴露动画器。
      
  en: >
      BasePanel base class: OnEnter/OnExit hooks, Show/Hide with optional animation, and the cached Animator property.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:47.489Z"
fingerprint: 12e65e87641cb322ea210e0c11f8ee7f7cc3eef99bd353328e85b9c0cc5d75eb
source:
  - path: "Assets/Scripts/Runtime/GameBase/UIManager/BasePanel.cs"
    line: 1
    end_line: 113
apis:
  - protocol: rpc
    path: "BasePanel.OnEnter"
    description:
      zh: >
          面板显示时调用，子类重写。
          
      en: >
          Called when the panel is shown.
          
  - protocol: rpc
    path: "BasePanel.OnExit"
    description:
      zh: >
          面板隐藏时调用，子类重写。
          
      en: >
          Called when the panel is hidden.
          
  - protocol: rpc
    path: "BasePanel.Show"
    description:
      zh: >
          显示面板（可 instant 跳过动画）。
          
      en: >
          Shows the panel (optionally instantly) with animation.
          
  - protocol: rpc
    path: "BasePanel.Hide"
    description:
      zh: >
          隐藏面板（可 instant 跳过动画）。
          
      en: >
          Hides the panel (optionally instantly) with animation.
          
  - protocol: rpc
    path: "BasePanel.Animator"
    description:
      zh: >
          动画器访问属性（缓存的 UIPanelAnimator）。
          
      en: >
          Animator accessor (cached UIPanelAnimator).
          
deps:
  - kind: call
    to: project-chaos.framework.ui-animation.panel-animator-play
    from_api: "rpc:BasePanel.Show"
    to_api: "rpc:UIPanelAnimator.PlayEnter"
    label: {zh: "显隐动画", en: "Show-hide animation"}
---
