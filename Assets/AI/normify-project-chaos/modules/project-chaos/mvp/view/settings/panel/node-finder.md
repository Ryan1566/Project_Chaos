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
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:58:29.745Z"
fingerprint: 888e6ac95db88b7e0ff338fe63c68dfc9be7f391634466415f8a96dddc392858
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
