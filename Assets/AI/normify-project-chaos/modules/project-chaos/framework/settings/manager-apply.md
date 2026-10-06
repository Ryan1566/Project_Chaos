---
uid: f1c0003c
id: project-chaos.framework.settings.manager-apply
parent: project-chaos.framework.settings
name: {zh: "设置应用", en: "Settings Application"}
description:
  zh: >
      设置应用：把已生效设置推给 QualitySettings、音频管理器、输入覆盖项与本地化管理器，含选项映射辅助。
      
  en: >
      Settings application: pushes applied settings into QualitySettings, AudioManager, InputManager overrides and LocalizationManager, plus option mapping helpers.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.756Z"
fingerprint: a58af6ec57f3504b214f118edda3f6ebe55cc20e6545fda34175d3a94f17020e
source:
  - path: "Assets/Scripts/Runtime/GameBase/SettingsManager/SettingsManager.cs"
    line: 240
    end_line: 338
apis:
  - protocol: rpc
    path: "SettingsManager.ApplyToRuntime"
    description:
      zh: >
          把设置对象应用到画面/声音/输入/本地化。
          
      en: >
          Applies a settings object to graphics, audio, input and localization.
          
  - protocol: rpc
    path: "SettingsManager.MapWindowMode"
    description:
      zh: >
          窗口模式选项→FullScreenMode。
          
      en: >
          Maps the window-mode option to FullScreenMode.
          
  - protocol: rpc
    path: "SettingsManager.MapQualityLevel"
    description:
      zh: >
          画质选项→质量等级下标。
          
      en: >
          Maps the quality option to a quality level index.
          
deps:
  - kind: call
    to: project-chaos.framework.audio.channel-volume
    from_api: "rpc:SettingsManager.ApplyToRuntime"
    to_api: "rpc:AudioManager.SetVolume"
    label: {zh: "应用音量", en: "Apply volume"}
  - kind: call
    to: project-chaos.framework.localization.manager
    from_api: "rpc:SettingsManager.ApplyToRuntime"
    to_api: "rpc:LocalizationManager.ChangeLanguage"
    label: {zh: "应用语言", en: "Apply language"}
  - kind: call
    to: project-chaos.framework.input.overrides
    from_api: "rpc:SettingsManager.ApplyToRuntime"
    to_api: "rpc:InputManager.LoadOverridesJson"
    label: {zh: "应用改键", en: "Apply bindings"}
  - kind: reference
    to: project-chaos.framework.settings.resolution
    from_api: "rpc:SettingsManager.ApplyToRuntime"
    to_api: "rpc:ResolutionHelper.GetAvailableOptions"
    label: {zh: "分辨率选项", en: "Resolution options"}
  - kind: reference
    to: project-chaos.framework.settings.enums
    from_api: "rpc:SettingsManager.ApplyToRuntime"
    to_api: "rpc:SettingsLabels.Get"
    label: {zh: "选项标签", en: "Option labels"}
  - kind: event
    to: project-chaos.framework.events.const-names
    from_api: "rpc:SettingsManager.ApplyToRuntime"
    to_api: "rpc:EventConstName.SaveSetting"
    label: {zh: "设置已保存事件", en: "Settings saved"}
  - kind: call
    to: project-chaos.framework.logging.core
    from_api: "rpc:SettingsManager.ApplyToRuntime"
    to_api: "rpc:ChaosLog.Write"
    label: {zh: "设置日志", en: "Settings logging"}
---
