---
uid: b39d5c10
id: project-chaos.assets.ui-prefabs
parent: project-chaos.assets
name: {zh: "UI 面板预制体", en: "UI Panel Prefabs"}
description:
  zh: >
      从 Resources/UIPanels 加载的运行时 UI 预制体：主菜单、设置、存档面板，以及列表子项 RecordCell。
      
  en: >
      Runtime UI prefabs loaded from Resources/UIPanels: MainMenuPanel, SettingPanel, SLPanel plus the RecordCell list item.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.685Z"
fingerprint: 76d00873ad00c387d1306ccbfa4705e25bec2a022c6462a975266aac61d1403c
source:
  - path: "Assets/Resources/UIPanels/MainMenuPanel.prefab"
  - path: "Assets/Resources/UIPanels/SettingPanel.prefab"
  - path: "Assets/Resources/UIPanels/SLPanel.prefab"
  - path: "Assets/Resources/UIPanels/SubUI_Prefab/RecordCell.prefab"
apis:
  - protocol: file
    path: "Assets/Resources/UIPanels/MainMenuPanel.prefab"
    description:
      zh: >
          主菜单面板预制体（52KB）。
          
      en: >
          Main menu panel prefab (52 KB).
          
  - protocol: file
    path: "Assets/Resources/UIPanels/SettingPanel.prefab"
    description:
      zh: >
          设置面板预制体（880KB，最重的 UI 资产）。
          
      en: >
          Settings panel prefab (880 KB, the heaviest UI asset).
          
  - protocol: file
    path: "Assets/Resources/UIPanels/SLPanel.prefab"
    description:
      zh: >
          存档面板预制体。
          
      en: >
          Save/load panel prefab.
          
  - protocol: file
    path: "Assets/Resources/UIPanels/SubUI_Prefab/RecordCell.prefab"
    description:
      zh: >
          列表子项 RecordCell 预制体。
          
      en: >
          Record cell prefab used by list sub-views.
          
deps:
  - kind: reference
    to: project-chaos.assets.setting-row-prefabs
    from_api: "file:Assets/Resources/UIPanels/SettingPanel.prefab"
    to_api: "file:Assets/Prefabs/UI/SettingRows/SettingRow_Slider.prefab"
    label: {zh: "设置面板内嵌设置行", en: "Panel embeds setting rows"}
---
