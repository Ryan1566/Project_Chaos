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
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-10T14:57:10.697Z"
fingerprint: 9e7db4515ca30bd2dd2135e6f936d432f065a9e646536eef7c41ae6fc4d66943
source:
  - path: "Assets/Resources/UIPanels/MainMenuPanel.prefab"
  - path: "Assets/Resources/UIPanels/SettingPanel.prefab"
  - path: "Assets/Resources/UIPanels/SLPanel.prefab"
  - path: "Assets/Resources/UIPanels/SubUI_Prefab/RecordCell.prefab"
  - path: "Assets/Resources/UIPanels/BattlePreparationPanel.prefab"
  - path: "Assets/Resources/UIPanels/RunResumeRecoveryPanel.prefab"
  - path: "Assets/Resources/UIPanels/BattlePreparationPanel.prefab"
  - path: "Assets/Resources/UIPanels/RunResumeRecoveryPanel.prefab"
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
          
  - protocol: file
    path: "Assets/Resources/UIPanels/BattlePreparationPanel.prefab"
    description:
      zh: >
          战备装载面板预制体
          
      en: >
          Battle preparation panel prefab
          
  - protocol: file
    path: "Assets/Resources/UIPanels/RunResumeRecoveryPanel.prefab"
    description:
      zh: >
          局内恢复提示面板预制体
          
      en: >
          Run resume recovery panel prefab
          
deps:
  - kind: reference
    to: project-chaos.assets.setting-row-prefabs
    from_api: "file:Assets/Resources/UIPanels/SettingPanel.prefab"
    to_api: "file:Assets/Prefabs/UI/SettingRows/SettingRow_Slider.prefab"
    label: {zh: "设置面板内嵌设置行", en: "Panel embeds setting rows"}
---
