---
uid: e0a10602
id: project-chaos.editor-tools.input-icon.keymap-prefab-patch
parent: project-chaos.editor-tools.input-icon
name: {zh: "预制体按键行补位", en: "Prefab Key Row Patching"}
description:
  zh: >
      按键图标的预制体侧：遍历面板预制体，在每个按键行下补建或修复 KeyIconText + Icon 子节点，应用自动尺寸，解析设备页签图标文件名，并提供窗口与 ForTest 钩子使用的批量补位/页签入口。
      
  en: >
      Prefab side of key icons: walks panel prefabs, adds or repairs the KeyIconText plus Icon child under each key row, applies auto-size, resolves device tab icon file names, and drives batch patch/tab-icon entry points used by the window and by ForTest hooks.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.735Z"
fingerprint: 96486d7e832cc7190baf94ca5cbad33b062a22120e8091b257d09d7d65b9a001
source:
  - path: "Assets/Scripts/Editor/InputIcon/KeyIconMapWindow.cs"
    line: 365
    end_line: 815
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/InputIcon/KeyIconMapWindow.cs#L365-L815"
    description:
      zh: >
          预制体按键行补位与页签图标所在行段。
          
      en: >
          Line range holding prefab key-row patching and tab icons.
          
  - protocol: rpc
    path: "KeyIconMapWindow.PatchAllPrefabs"
    description:
      zh: >
          给所有预制体的按键行补图标显示位。
          
      en: >
          Adds icon slots to every prefab key row.
          
  - protocol: rpc
    path: "KeyIconMapWindow.EnsureIconSlot"
    description:
      zh: >
          确保单个按键行有键名文本与图标位。
          
      en: >
          Ensures one key row has a key text plus icon slot.
          
  - protocol: rpc
    path: "KeyIconMapWindow.ApplyTabIcons"
    description:
      zh: >
          把映射表套到设备页签按钮上。
          
      en: >
          Applies the map to device tab buttons.
          
  - protocol: rpc
    path: "KeyIconMapWindow.CreateMap"
    description:
      zh: >
          在默认路径新建一份 KeyIconMap 资产。
          
      en: >
          Creates a fresh KeyIconMap asset at the default path.
          
deps:
  - kind: call
    to: project-chaos.editor-tools.input-icon.icon-naming
    from_api: "rpc:KeyIconMapWindow.ApplyTabIcons"
    to_api: "rpc:IconNaming.TabFileName"
    label: {zh: "解读页签图标文件名", en: "Resolves tab icon files"}
---
