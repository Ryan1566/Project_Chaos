---
uid: 7d2b4f19
id: project-chaos.mvp.view.save-load.panel.node-and-cell-factory
parent: project-chaos.mvp.view.save-load.panel
tags: [mvp, ui, save]
name: {zh: "节点与格子工厂", en: "Nodes & Cell Factory"}
description:
  zh: >
      节点查找与格子工厂：按硬约定名字找 RecordsList / BottomBar / ConfirmGroup，EnsureCells 从 Resources 取 RecordCell 预制体（ResManager 内部已 Instantiate），列表非空时复用已有子物体，并把档位号、点击回调与选中标记只绑一次。
      
  en: >
      Node lookup and the cell factory: Locates RecordsList / BottomBar / ConfirmGroup by hard-coded names, then EnsureCells loads RecordCell from Resources (ResManager already instantiates it), reuses an existing child when the list is not empty, and binds slot number, click callback and selection marker exactly once.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.764Z"
fingerprint: 476e33b9f31278b56b525f8ffb3a6299b5e9caa0d329cae08c07e8e1ad4cacec
source:
  - path: "Assets/Scripts/Runtime/MVP/View/SLPanel.cs"
    line: 139
    end_line: 272
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L141-L198"
    description:
      zh: >
          按硬约定节点名找 RecordsList、底部按钮与确认浮层。
          
      en: >
          Finds RecordsList, bottom bar and confirm nodes.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L198-L209"
    description:
      zh: >
          绑定删除/返回按钮与确认浮层的三个按钮。
          
      en: >
          Binds delete/return and the confirm buttons.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L210-L272"
    description:
      zh: >
          EnsureCells：只例化 3 个格子并长期复用。
          
      en: >
          EnsureCells: instantiates three cells once.
          
deps:
  - kind: call
    to: project-chaos.mvp.view.save-load.record-cell
    from_api: "file:Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L210-L272"
    to_api: "rpc:RecordCell.Bind"
    label: {zh: "例化格子并绑定", en: "Instantiate and bind a cell"}
---
