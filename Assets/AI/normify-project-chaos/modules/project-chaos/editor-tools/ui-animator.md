---
uid: e0a10007
id: project-chaos.editor-tools.ui-animator
parent: project-chaos.editor-tools
name: {zh: "面板动画组件批量工具", en: "UIPanelAnimator Batch Tool"}
description:
  zh: >
      一次性编辑器工具，把面板显隐动画组件固化进预制体：运行时自动补挂的组件不会被保存，Inspector 参数永远是字段初始值。逐个在隔离场景打开面板预制体，跳过没有 BasePanel 或已挂动画组件的对象，在单次资源编辑作用域内批量保存。
      
  en: >
      One-shot editor tool that materialises panel entrance/exit animation components into the prefabs: runtime auto-added components are never saved, so their inspector values stay at field defaults. Loads each panel prefab in isolation, skips non-BasePanel or already-equipped objects, saves in bulk under a single asset-editing scope.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.739Z"
fingerprint: d97467c137d2dea498637d8efe04962bf704570cd71a4d8c604e0a027da0296e
source:
  - path: "Assets/Scripts/Editor/UIPanelAnimatorTool/UIPanelAnimatorTool.cs"
    line: 15
    end_line: 86
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/UIPanelAnimatorTool/UIPanelAnimatorTool.cs#L15-L86"
    description:
      zh: >
          批量面板预制体动画组件工具所在文件。
          
      en: >
          The batch panel-prefab animator component tool.
          
  - protocol: rpc
    path: "Menu/Tool/UI/为所有面板预制体添加动画组件"
    description:
      zh: >
          为所有面板预制体添加 UIPanelAnimator 的菜单项。
          
      en: >
          Menu item adding UIPanelAnimator to all panel prefabs.
          
  - protocol: rpc
    path: "UIPanelAnimatorTool.AddAnimatorToAllPanelPrefabs"
    description:
      zh: >
          给面板预制体补 UIPanelAnimator，已有的跳过。
          
      en: >
          Adds UIPanelAnimator to panel prefabs, skipping ones that have it.
          
  - protocol: rpc
    path: "UIPanelAnimatorTool.ValidateAddAnimatorToAllPanelPrefabs"
    description:
      zh: >
          仅非运行且非编译时才启用菜单项。
          
      en: >
          Enables the menu item only outside play and compile.
          
---
