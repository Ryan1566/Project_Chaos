---
uid: 5c1a7e16
id: project-chaos.bootstrap.build-settings
parent: project-chaos.bootstrap
tags: [bootstrap, build, scene]
name: {zh: "构建场景清单", en: "Build Scene List"}
description:
  zh: >
      Scenes In Build：0 = EnterScene（启动/局外）、1 = WorldScene（局内）、2 = LoadingScene。TestScene 已2 026-10-12 从列表移除（文件未删）。只改变量仅改内存，必须 File/Save Project 才能落盘。
      
  en: >
      Scenes In Build: 0 = EnterScene (boot/meta), 1 = WorldScene (run), 2 = LoadingScene. TestScene was removed from the list on 2026-10-12; the file is untouched. Changing this list only affects memory — it must be persisted with File/Save Project.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:57:59.776Z"
fingerprint: af22c5fd740d926ddec738616b6daf4d9dfe74de3b24c959c291529f08ea88a5
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
