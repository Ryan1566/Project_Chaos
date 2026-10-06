---
uid: 7d2b4f06
id: project-chaos.mvp.view.settings.panel.node-finder
parent: project-chaos.mvp.view.settings.panel
tags: [mvp, ui, settings]
name: {zh: "节点查找", en: "Node Finder"}
description:
  zh: >
      设置面板的硬约定节点查找：主选列表页签、ContentArea 页面节点、底部按钮条与可缺省的 ConfirmGroup 确认浮层，以及共用的 Button 查找工具。Awake 里校验页签/页面名字表长度与 SettingCategory 枚举一致。
      
  en: >
      Hard-coded node lookup for the panel: main-list tab buttons, ContentArea pages, the bottom button bar and the optional ConfirmGroup overlay, plus the shared Button finder. Awake validates that the tab/page name tables match the SettingCategory enum length.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.769Z"
fingerprint: f6e00a704a92904810632d475db69fb4c2a10cbaf9072f1b9a5976923e8002f3
source:
  - path: "Assets/Scripts/Runtime/MVP/View/SettingPanel.cs"
    line: 136
    end_line: 248
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingPanel.cs#L136-L192"
    description:
      zh: >
          FindTabsAndPages：按名字找主选按钮与页面，并校验表长。
          
      en: >
          Tabs/pages lookup plus the length check.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingPanel.cs#L193-L248"
    description:
      zh: >
          FindBottomBar/FindConfirmGroup/FindButton：底部与可选确认浮层节点。
          
      en: >
          Bottom bar, confirm group and Button helper.
          
deps:
  - kind: reference
    to: project-chaos.mvp.view.settings.page-base
    from_api: "file:Assets/Scripts/Runtime/MVP/View/SettingPanel.cs#L136-L192"
    label: {zh: "按节点取设置页基类", en: "Resolve page base component"}
---
