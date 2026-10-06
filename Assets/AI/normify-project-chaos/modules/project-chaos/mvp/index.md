---
uid: e73c05d9
id: project-chaos.mvp
parent: project-chaos
tags: [mvp, ui, view]
name: {zh: "MVP 视图层", en: "MVP View Layer"}
description:
  zh: >
      Assets/Scripts/Runtime/MVP 的视图层：View/ 下面板与子控件（MainMenuPanel、SettingPanel、SLPanel 与 5 个设置页、RecordCell）与 Model/DialogueRuntimeData.cs；Presenter/ 仍为空，面板走 View → Service 直连。生成的 ConfigData/ModelData 类归 config-pipeline。
  en: >
      The view layer under Assets/Scripts/Runtime/MVP: panels and sub-widgets in View/ (MainMenuPanel, SettingPanel, SLPanel with five settings pages, RecordCell) and Model/DialogueRuntimeData.cs; Presenter/ is still empty, so panels call services directly (View → Service). Generated ConfigData/ModelData classes live in config-pipeline.
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:02.296Z"
fingerprint: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
source: []
deps:
  - kind: call
    to: project-chaos.framework
    label: {zh: "视图调服务", en: "Views call services"}
  - kind: dataflow
    to: project-chaos.config-pipeline
    label: {zh: "读配置数据", en: "Reads config data"}
  - kind: reference
    to: project-chaos.assets
    label: {zh: "加载面板预制体", en: "Loads panel prefabs"}
---
