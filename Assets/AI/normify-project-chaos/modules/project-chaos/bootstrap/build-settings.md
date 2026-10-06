---
uid: 5c1a7e16
id: project-chaos.bootstrap.build-settings
parent: project-chaos.bootstrap
tags: [bootstrap, build, scene]
name: {zh: "构建场景清单", en: "Build Scene List"}
description:
  zh: >
      ProjectSettings/EditorBuildSettings.asset：构建清单登记 3 个启用场景，顺序为 TestScene(0) → WorldScene(1) → LoadingScene(2)。进入构建的场景才有合法的 buildIndex，ScenesLoadManager 按名字加载时依赖这张表。
      
  en: >
      ProjectSettings/EditorBuildSettings.asset: the build list registers three enabled scenes in the order TestScene(0) → WorldScene(1) → LoadingScene(2). Only registered scenes get a valid buildIndex, and ScenesLoadManager's name-based loading depends on this table.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.720Z"
fingerprint: eef39b135d957304ec224cd71355cddfadd01f3c856006e8303f380b258e9972
source:
  - path: "ProjectSettings/EditorBuildSettings.asset"
    line: 1
    end_line: 17
apis:
  - protocol: file
    path: "ProjectSettings/EditorBuildSettings.asset"
    description:
      zh: >
          构建场景清单（3 个启用场景）。
          
      en: >
          Build scene list (three enabled scenes).
          
deps:
  - kind: dataflow
    to: project-chaos.bootstrap.scenes.test-scene
    from_api: "file:ProjectSettings/EditorBuildSettings.asset"
    to_api: "file:Assets/Scenes/TestScene.unity"
    label: {zh: "清单第 0 项（启动场景）", en: "List entry 0 (startup scene)"}
  - kind: dataflow
    to: project-chaos.bootstrap.scenes.world-scene
    from_api: "file:ProjectSettings/EditorBuildSettings.asset"
    to_api: "file:Assets/Scenes/WorldScene.unity"
    label: {zh: "清单第 1 项", en: "List entry 1"}
  - kind: dataflow
    to: project-chaos.bootstrap.scenes.loading-scene
    from_api: "file:ProjectSettings/EditorBuildSettings.asset"
    to_api: "file:Assets/Scenes/LoadingScene.unity"
    label: {zh: "清单第 2 项（读条场景）", en: "List entry 2 (loading scene)"}
---
