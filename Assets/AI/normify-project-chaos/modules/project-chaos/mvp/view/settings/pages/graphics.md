---
uid: 7d2b4f0e
id: project-chaos.mvp.view.settings.pages.graphics
parent: project-chaos.mvp.view.settings.pages
tags: [mvp, ui, settings]
name: {zh: "画面设置页", en: "Graphics Page"}
description:
  zh: >
      画面页：分辨率/视窗模式/帧率上限/垂直同步/画质。分辨率是唯一「选项列表运行时才定」的一项（超出显示器能力的档位被 ResolutionHelper 裁掉），所以选择器下标只是缓存表的下标；存档分辨率在当前显示器不存在时吸附到最近一档并写进暂存区。
      
  en: >
      Graphics page: resolution, window mode, frame-rate cap, vsync and quality. Resolution is the only list built at runtime (options beyond the monitor are filtered out by ResolutionHelper), so the selector index is just an offset into a cached table; a saved resolution that does not exist on the current monitor is snapped to the closest entry and written into the pending buffer.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.767Z"
fingerprint: 0aad273bcd2b597ed908a4410ba98561e9bb973169ea827d944b160e7fd96192
source:
  - path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/GraphicsSettingsPage.cs"
    line: 22
    end_line: 116
apis:
  - protocol: rpc
    path: "GraphicsSettingsPage.OnBind"
    description:
      zh: >
          绑定分辨率/视窗模式/帧率上限/垂直同步/画质五步。
          
      en: >
          Binds resolution, window mode, fps, vsync, quality.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/GraphicsSettingsPage.cs#L35-L62"
    description:
      zh: >
          OnBind 与分辨率档位绑定（选项列表运行时才定）。
          
      en: >
          OnBind plus the resolution option binding.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/GraphicsSettingsPage.cs#L64-L82"
    description:
      zh: >
          存档分辨率在当前显示器不存在时吸附到最近一档。
          
      en: >
          Snaps a saved resolution missing on this monitor.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/GraphicsSettingsPage.cs#L84-L115"
    description:
      zh: >
          视窗模式/帧率/垂直同步/画质四项绑定。
          
      en: >
          Window mode, frame rate, vsync and quality rows.
          
deps:
  - kind: reference
    to: project-chaos.mvp.view.settings.page-base.lifecycle
    from_api: "rpc:GraphicsSettingsPage.OnBind"
    to_api: "rpc:SettingsPageBase.OnBind"
    label: {zh: "继承设置页基类", en: "Extends settings page base"}
---
