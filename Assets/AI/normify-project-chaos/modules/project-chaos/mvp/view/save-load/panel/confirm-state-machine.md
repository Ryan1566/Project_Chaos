---
uid: 7d2b4f1b
id: project-chaos.mvp.view.save-load.panel.confirm-state-machine
parent: project-chaos.mvp.view.save-load.panel
tags: [mvp, ui, save]
name: {zh: "二次确认状态机", en: "Confirm State Machine"}
description:
  zh: >
      共用的二次确认浮层：空档位开始与删除复用同一个 ConfirmGroup，所以用 ConfirmPurpose 枚举记住「这次确认是为了什么」——没有状态机，OK 回调就不知道要执行新建还是删除。浮层可整组缺省，缺省时退化为直接执行。
      
  en: >
      The shared confirm overlay: create-from-empty and delete both reuse one ConfirmGroup, so a ConfirmPurpose enum records why it was opened — without it the OK callback could not tell whether to create or delete. The overlay is optional; when the prefab has no ConfirmGroup the panel degrades to acting immediately.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.763Z"
fingerprint: 476e33b9f31278b56b525f8ffb3a6299b5e9caa0d329cae08c07e8e1ad4cacec
source:
  - path: "Assets/Scripts/Runtime/MVP/View/SLPanel.cs"
    line: 369
    end_line: 450
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L371-L411"
    description:
      zh: >
          复用同一个确认浮层：显隐与文案设置。
          
      en: >
          Shows/hides the shared confirm group and its text.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L412-L450"
    description:
      zh: >
          确认/取消回调与 ExecuteConfirm 的分派。
          
      en: >
          Confirm callbacks and ExecuteConfirm dispatch.
          
deps:
  - kind: call
    to: project-chaos.mvp.view.save-load.panel.business-actions
    from_api: "file:Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L412-L450"
    to_api: "file:Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L454-L473"
    label: {zh: "确认后执行对应动作", en: "Runs the action after confirm"}
---
