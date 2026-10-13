---
uid: aa910015
id: project-chaos.mvp.view.world-map.prefab
parent: project-chaos.mvp.view.world-map
name: {zh: "世界观地图预制体", en: "World Map Prefab"}
description:
  zh: >
      Resources/UIPanels/WorldMapPanel.prefab，文件名必须与 PanelType 成员同名（UIManager 用枚举名拼资源路径）。层级：BaseMap（Image 直接引用美术 Sprite——该 PNG 不在 Resources 下）、FactionMask（RawImage + WorldFactionMask）、IconLayer、InfoBar、BottomBar 按钮组。
      
  en: >
      WorldMapPanel.prefab under Resources/UIPanels, filename identical to the PanelType member because UIManager builds the resource path from the enum name. Hierarchy: BaseMap (Image referencing the art sprite directly — the PNG is outside Resources), FactionMask (RawImage with WorldFactionMask), IconLayer, InfoBar, BottomBar buttons.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:30:54.570Z"
fingerprint: cbd7ae5cd56bdddc57051e355b8c5068eec3af8d26a424557e4ffb4228e41807
source:
  - path: "Assets/Resources/UIPanels/WorldMapPanel.prefab"
apis:
  - protocol: file
    path: "Assets/Resources/UIPanels/WorldMapPanel.prefab"
    description:
      zh: >
          面板预制体（BaseMap / FactionMask / IconLayer / InfoBar / BottomBar），由 UIManager 实例化。
          
      en: >
          Panel prefab (BaseMap / FactionMask / IconLayer / InfoBar / BottomBar) instantiated by UIManager.
          
---
