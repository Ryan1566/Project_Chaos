---
uid: f1c00013
id: project-chaos.framework.ui.manager
parent: project-chaos.framework.ui
name: {zh: "UIManager 面板栈", en: "UIManager Panel Stack"}
description:
  zh: >
      UIManager 面板栈管理：按名字找 Canvas、按 PanelType 动态实例化面板、Push/Pop 栈式显隐、实例缓存复用。注意缓存导致 OnEnter 会多次触发。
      
  en: >
      UIManager: finds Canvas by name, instantiates panels by PanelType, keeps a panel stack with push/pop, and caches instances for reuse.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.761Z"
fingerprint: ca79a1693ec313648593b4857b97bbb57739fd0dcc17ad458506762f333c53ce
source:
  - path: "Assets/Scripts/Runtime/GameBase/UIManager/UIManager.cs"
    line: 1
    end_line: 200
apis:
  - protocol: rpc
    path: "UIManager.OnInit"
    description:
      zh: >
          按名字找到 Canvas 并初始化面板字典。
          
      en: >
          Finds the Canvas by name and initializes the panel dictionary.
          
  - protocol: rpc
    path: "UIManager.PushPanel"
    description:
      zh: >
          按 PanelType 显示面板并返回 BasePanel 实例。
          
      en: >
          Shows a panel by PanelType, returning the BasePanel instance.
          
  - protocol: rpc
    path: "UIManager.PopPanel"
    description:
      zh: >
          弹出并隐藏栈顶面板。
          
      en: >
          Pops and hides the top panel.
          
  - protocol: rpc
    path: "UIManager.SpawnPanel"
    description:
      zh: >
          实例化面板预制体（带缓存复用）。
          
      en: >
          Instantiates a panel prefab (cached for reuse).
          
  - protocol: rpc
    path: "UIManager.panelStack"
    description:
      zh: >
          公开的面板栈，持有已打开面板。
          
      en: >
          Public panel stack holding active panels.
          
deps:
  - kind: call
    to: project-chaos.framework.resources.manager
    from_api: "rpc:UIManager.SpawnPanel"
    to_api: "rpc:ResManager.Load"
    label: {zh: "加载面板预制体", en: "Load panel prefab"}
  - kind: reference
    to: project-chaos.framework.ui.base-panel
    from_api: "rpc:UIManager.panelStack"
    to_api: "rpc:BasePanel.OnEnter"
    label: {zh: "面板栈元素", en: "Panel stack item"}
  - kind: reference
    to: project-chaos.framework.paths.panel-type
    from_api: "rpc:UIManager.PushPanel"
    to_api: "file:Assets/Scripts/Runtime/Path&Type/PanelType.cs"
    label: {zh: "面板类型", en: "Panel type"}
  - kind: reference
    to: project-chaos.framework.paths.global-path
    from_api: "rpc:UIManager.SpawnPanel"
    to_api: "rpc:GlobalPath.ui_PanelPrefabSearchPaths"
    label: {zh: "面板搜索路径", en: "Panel search paths"}
  - kind: call
    to: project-chaos.framework.logging.core
    from_api: "rpc:UIManager.OnInit"
    to_api: "rpc:ChaosLog.Write"
    label: {zh: "框架日志", en: "Framework logging"}
---
