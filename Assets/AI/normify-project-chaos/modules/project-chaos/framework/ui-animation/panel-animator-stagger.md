---
uid: f1c0001b
id: project-chaos.framework.ui-animation.panel-animator-stagger
parent: project-chaos.framework.ui-animation
name: {zh: "依次入场实现", en: "Stagger Internals"}
description:
  zh: >
      UIPanelAnimator 依次入场实现：收集 staggerRoot 子物体或 UIPanelStaggerItem 标记项，按 order/层级与 staggerInterval 构建逐项补间，并维护项缓存。
      
  en: >
      UIPanelAnimator stagger internals: collects marked/child items, builds per-item tweens with interval ordering, and prunes the item cache.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.759Z"
fingerprint: ca438f41c73fb26879bac743ccf5c9ab68e78458e1ca3ba696fe70887649c502
source:
  - path: "Assets/Scripts/Runtime/GameBase/UIManager/UIPanelAnimator.cs"
    line: 518
    end_line: 677
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/UIManager/UIPanelAnimator.cs#L518-L677"
    description:
      zh: >
          子元素依次入场的实现段（收集项、构建序列、缓存与清理）。
          
      en: >
          Staggered child entrance implementation block.
          
deps:
  - kind: reference
    to: project-chaos.framework.ui-animation.stagger-item
    to_api: "rpc:UIPanelStaggerItem.order"
    label: {zh: "依次入场标记", en: "Stagger marker"}
---
