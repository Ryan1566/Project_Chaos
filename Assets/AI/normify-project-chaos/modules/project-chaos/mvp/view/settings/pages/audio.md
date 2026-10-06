---
uid: 7d2b4f0d
id: project-chaos.mvp.view.settings.pages.audio
parent: project-chaos.mvp.view.settings.pages
tags: [mvp, ui, settings]
name: {zh: "声音设置页", en: "Audio Page"}
description:
  zh: >
      声音页：全局/音乐/音效/UI 四路音量，0~100 步进 1。音量是「改完点应用才生效」的刻意例外——拖动时立即改变实际输出（听不到声音的拉条没法调），但存档值仍只写进暂存区，点应用才落盘。
      
  en: >
      Audio page: master/music/sfx/ui volume, all 0-100 step 1. Volume is the one deliberate exception to apply-on-commit — dragging changes the live AudioSource output immediately (a slider you cannot hear is unusable) while the saved value still only lands in the pending buffer until Apply.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.767Z"
fingerprint: 41ca215db524c24f825f6ec76874e51640b74da07da4876f57a59658cc3671ee
source:
  - path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/AudioSettingsPage.cs"
    line: 18
    end_line: 50
apis:
  - protocol: rpc
    path: "AudioSettingsPage.OnBind"
    description:
      zh: >
          声明全局/音乐/音效/UI 四路音量拉条。
          
      en: >
          Declares the four volume sliders.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/AudioSettingsPage.cs#L18-L50"
    description:
      zh: >
          四路音量拉条 0~100 步进 1，拖动时立即试听。
          
      en: >
          Four volume sliders, 0-100 step 1, with live preview.
          
deps:
  - kind: reference
    to: project-chaos.mvp.view.settings.page-base.lifecycle
    from_api: "rpc:AudioSettingsPage.OnBind"
    to_api: "rpc:SettingsPageBase.OnBind"
    label: {zh: "继承设置页基类", en: "Extends settings page base"}
---
