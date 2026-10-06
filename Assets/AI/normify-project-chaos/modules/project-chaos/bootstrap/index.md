---
uid: 9d2e7c41
id: project-chaos.bootstrap
parent: project-chaos
tags: [bootstrap, scene, loading]
name: {zh: "启动与场景骨架", en: "Bootstrap & Scene Shell"}
description:
  zh: >
      游戏启动链路与场景骨架：Entry 入口脚本、MonoManager/MonoController 心跳驱动、三个单例基类、ScenesLoadManager 与 LoadingController 的异步场景加载与读条轮播、Scenes/ 下 4 个场景资产、EditorBuildSettings 场景清单。顺序：Entry → LoadingScene → 目标场景。
      
  en: >
      Bootstrap chain and scene shell: the Entry script, MonoManager/MonoController tick driver, three singleton bases, async scene loading with the carousel loading screen (ScenesLoadManager + LoadingController), the four scenes under Scenes/, and the EditorBuildSettings scene list.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:02:57.251Z"
fingerprint: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
source: []
deps:
  - kind: call
    to: project-chaos.framework
    label: {zh: "启动驱动框架", en: "Entry drives services"}
  - kind: reference
    to: project-chaos.assets
    label: {zh: "场景引用资源", en: "Scenes use assets"}
---
