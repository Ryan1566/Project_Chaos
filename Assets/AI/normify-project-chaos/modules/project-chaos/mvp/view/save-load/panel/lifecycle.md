---
uid: 7d2b4f18
id: project-chaos.mvp.view.save-load.panel.lifecycle
parent: project-chaos.mvp.view.save-load.panel
tags: [mvp, ui, save]
name: {zh: "面板生命周期", en: "Panel Lifecycle"}
description:
  zh: >
      SLPanel 生命周期：Awake 找节点并建 3 个格子（只做一次），OnEnter 只调 Refresh，OnExit 不额外收尾。之所以守这条 R3 纪律，是因为 UIManager 复用缓存的面板实例——把 AddListener 或 Instantiate 放 OnEnter 会导致第 2 次打开就叠加回调、列表里堆重复格子。
      
  en: >
      SLPanel lifecycle: Awake binds nodes and builds the three cells exactly once, OnEnter only refreshes, OnExit does nothing else. The R3 discipline exists because UIManager reuses the cached panel — putting AddListener or Instantiate in OnEnter would stack duplicate listeners and duplicate cells on every open.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.764Z"
fingerprint: 476e33b9f31278b56b525f8ffb3a6299b5e9caa0d329cae08c07e8e1ad4cacec
source:
  - path: "Assets/Scripts/Runtime/MVP/View/SLPanel.cs"
    line: 108
    end_line: 138
apis:
  - protocol: rpc
    path: "SLPanel.OnEnter"
    description:
      zh: >
          每次打开只刷新，不做绑定。
          
      en: >
          Refresh-on-open; binding never happens here.
          
  - protocol: rpc
    path: "SLPanel.OnExit"
    description:
      zh: >
          面板被关闭时调用。
          
      en: >
          Panel closed by UIManager.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L108-L138"
    description:
      zh: >
          Awake 绑定一次；OnEnter 只刷新（R3 纪律）。
          
      en: >
          Awake binds once; OnEnter only refreshes.
          
deps:
  - kind: call
    to: project-chaos.mvp.view.save-load.panel.node-and-cell-factory
    from_api: "file:Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L108-L138"
    to_api: "file:Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L210-L272"
    label: {zh: "绑定与建格子只在 Awake", en: "Bind and build cells in Awake"}
  - kind: call
    to: project-chaos.mvp.view.save-load.panel.refresh-and-selection
    from_api: "rpc:SLPanel.OnEnter"
    to_api: "file:Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L276-L320"
    label: {zh: "OnEnter 只刷新", en: "OnEnter only refreshes"}
---
