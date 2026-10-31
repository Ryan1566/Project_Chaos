---
uid: f1c00013
id: project-chaos.framework.ui.manager
parent: project-chaos.framework.ui
name: {zh: "UIManager 面板栈", en: "UIManager Panel Stack"}
description:
  zh: >
      面板实例字典 + 显示堆栈，靠静态单例跨场景存活。SetRootPanel(type) 先把栈内面板全部 Hide 并清栈再 Push 目标，这就是“存档面板被局外主界面替换、而不是叠在主菜单之上”的实现。
      
  en: >
      Panel instance dictionary + display stack, with cross-scene survival via a static singleton. SetRootPanel(type) hides and clears the whole stack before pushing the target, which is how the save panel is replaced by the meta main screen instead of stacking on top of the main menu.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:47.490Z"
fingerprint: c0b2348612e030518d4b0b11be03508212120c6632226b827e88ff56f2fb99ab
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
