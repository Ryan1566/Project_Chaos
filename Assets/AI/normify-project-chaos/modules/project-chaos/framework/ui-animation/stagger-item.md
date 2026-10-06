---
uid: f1c0001c
id: project-chaos.framework.ui-animation.stagger-item
parent: project-chaos.framework.ui-animation
name: {zh: "依次入场标记", en: "Stagger Item Marker"}
description:
  zh: >
      UIPanelStaggerItem 标记组件：挂在需要参与依次入场的子物体上，可自定义先后顺序；一旦子树中存在该标记，就只动被标记项。
      
  en: >
      UIPanelStaggerItem: optional marker placed on children that should participate in staggered entrance with a custom order.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.760Z"
fingerprint: 53e965d82b557323d141625d7f08c183cb8c4a694a74fbca947c6c3ebf7605cf
source:
  - path: "Assets/Scripts/Runtime/GameBase/UIManager/UIPanelStaggerItem.cs"
    line: 1
    end_line: 16
apis:
  - protocol: rpc
    path: "UIPanelStaggerItem.order"
    description:
      zh: >
          本项的入场顺序（越小越先入场）。
          
      en: >
          Sort order for this item (smaller enters first).
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/UIManager/UIPanelStaggerItem.cs"
    description:
      zh: >
          可选的依次入场标记组件脚本。
          
      en: >
          Optional marker component script asset.
          
---
