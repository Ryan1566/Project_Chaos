---
uid: 7d2b4f1c
id: project-chaos.mvp.view.save-load.panel.business-actions
parent: project-chaos.mvp.view.save-load.panel
tags: [mvp, ui, save]
name: {zh: "三个业务动作", en: "Business Actions"}
description:
  zh: >
      三个业务动作：空档位新建、读取档位（写 GameSession 后切到 LoadingScene，回调必须传非空委托——LoadScene 会无条件调 action()，传 null 必 NRE）、删除选中档位。落盘统一走 SaveSlotService。
      
  en: >
      The three save actions: create a new save in an empty slot, load a slot (write GameSession, then switch to LoadingScene with a non-null delegate — LoadScene invokes the callback unconditionally, so passing null would NRE), and delete the selected slot. All persistence goes through SaveSlotService.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.763Z"
fingerprint: 476e33b9f31278b56b525f8ffb3a6299b5e9caa0d329cae08c07e8e1ad4cacec
source:
  - path: "Assets/Scripts/Runtime/MVP/View/SLPanel.cs"
    line: 451
    end_line: 509
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L454-L473"
    description:
      zh: >
          空档位新建存档。
          
      en: >
          Creates a new save in an empty slot.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L474-L495"
    description:
      zh: >
          读取档位并切到读条场景进入游戏。
          
      en: >
          Loads a slot and enters the loading scene.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L496-L509"
    description:
      zh: >
          删除选中档位的存档。
          
      en: >
          Deletes the selected slot's save.
          
deps:
  - kind: call
    to: project-chaos.bootstrap.scene-loading.manager
    from_api: "file:Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L474-L495"
    to_api: "rpc:ScenesLoadManager.LoadScene"
    label: {zh: "切到读条场景", en: "Switch to loading scene"}
---
