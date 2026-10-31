---
uid: b39d5c10
id: project-chaos.assets.ui-prefabs
parent: project-chaos.assets
name: {zh: "UI 面板预制体", en: "UI Panel Prefabs"}
description:
  zh: >
      Assets/Resources/UIPanels 下的面板预制体，UIManager 按 PanelType 名实例化（MainMenuPanel / SettingPanel / SLPanel，2026-10-12 新增 WorldMapPanel，以及子预制体 RecordCell）。缺同名预制体的 PanelType 会被 ValidatePanelPrefabs 点名。
      
  en: >
      Panel prefabs under Assets/Resources/UIPanels, instantiated by UIManager from the PanelType name (MainMenuPanel, SettingPanel, SLPanel, plus WorldMapPanel added 2026-10-12, and the RecordCell sub-prefab). A PanelType without a matching prefab is reported by ValidatePanelPrefabs.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:56:40.596Z"
fingerprint: 70275525deb162c04d741be0de972332fdc8f96de513122119b65b8e9f94e675
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
