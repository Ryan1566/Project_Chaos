---
uid: 7d2b4f14
id: project-chaos.mvp.view.settings.pages.keybind-bindings.rebind
parent: project-chaos.mvp.view.settings.pages.keybind-bindings
tags: [mvp, ui, keybind]
name: {zh: "改键采集流程", en: "Rebind Flow"}
description:
  zh: >
      改键采集流程：先防重入（已有协程或 InputManager.IsRebinding 就拒绝），再逐步走 InputManager.GetRebindSteps 给出的提示，结果经 OnStepFinished 回传。等待按键期间整页锁住防误触；同步写 InputActionAsset 是刻意的——不写进去玩家看不到自己刚按的键。
      
  en: >
      Rebind capture flow: guards against re-entry (an in-flight routine or InputManager.IsRebinding), walks the steps given by InputManager.GetRebindSteps, and passes a rebound result back through OnStepFinished. While waiting for a key the whole page is locked; the immediate write to the InputActionAsset is deliberate so the player sees the key they just pressed.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:43:40.750Z"
fingerprint: c76a0438dd82a40c8850121ab0cf4a1a08a30f3205d0b778845844c50e5253ef
source:
  - path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/KeybindBindingsPage.cs"
    line: 347
    end_line: 517
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/KeybindBindingsPage.cs#L347-L374"
    description:
      zh: >
          防重入检查后开始一次按键采集。
          
      en: >
          Guards re-entry, then starts one capture step.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/KeybindBindingsPage.cs#L375-L458"
    description:
      zh: >
          RebindRoutine：逐步采集一条绑定的各段。
          
      en: >
          RebindRoutine: walks the steps for one row.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/KeybindBindingsPage.cs#L459-L517"
    description:
      zh: >
          采集期间整页锁住，结束后清理监听与状态。
          
      en: >
          Locks the page while capturing and cleans up.
          
---
