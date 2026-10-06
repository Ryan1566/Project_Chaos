---
uid: 2a8cfe71
id: project-chaos.project-infra.plugins-spine-examples.scripts
parent: project-chaos.project-infra.plugins-spine-examples
name: {zh: "示例脚本", en: "Example Scripts"}
description:
  zh: >
      76 个示例 C# 文件，演示 Spine 用法：入门教程控制器、Mix&Match、布娃娃与示例组件。
      
  en: >
      76 example C# files showing Spine usage: beginner tutorial controllers, mix-and-match, ragdoll and sample components.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.781Z"
fingerprint: e8936a9c9c6a788e2ae2fb52d43a9def11e650094b0ea2b1f6de884ffac3098f
source:
  - path: "Assets/Plugins/Spine Examples/spine-unity-examples.asmdef"
  - path: "Assets/Plugins/Spine Examples/Scripts/Spineboy.cs"
  - path: "Assets/Plugins/Spine Examples/Scripts/Getting Started Scripts/SpineboyBeginnerView.cs"
  - path: "Assets/Plugins/Spine Examples/Scripts/Sample Components/SkeletonUtility Modules/SkeletonRagdoll.cs"
apis:
  - protocol: file
    path: "Assets/Plugins/Spine Examples/spine-unity-examples.asmdef"
    description:
      zh: >
          spine-unity-examples 程序集定义。
          
      en: >
          spine-unity-examples assembly definition.
          
  - protocol: file
    path: "Assets/Plugins/Spine Examples/Scripts/Spineboy.cs"
    description:
      zh: >
          Spineboy 标准示例控制器。
          
      en: >
          Canonical Spineboy example controller.
          
  - protocol: file
    path: "Assets/Plugins/Spine Examples/Scripts/Getting Started Scripts/SpineboyBeginnerView.cs"
    description:
      zh: >
          入门教程的 View 层（类 MVVM 示例）。
          
      en: >
          Beginner tutorial view layer (MVVM-ish sample).
          
  - protocol: file
    path: "Assets/Plugins/Spine Examples/Scripts/Sample Components/SkeletonUtility Modules/SkeletonRagdoll.cs"
    description:
      zh: >
          SkeletonRagdoll 示例组件，常被拷到实际工程使用。
          
      en: >
          SkeletonRagdoll sample component, often copied into real projects.
          
deps:
  - kind: reference
    to: project-chaos.project-infra.plugins-spine.runtime
    from_api: "file:Assets/Plugins/Spine Examples/spine-unity-examples.asmdef"
    to_api: "file:Assets/Plugins/Spine/Runtime/spine-unity.asmdef"
    label: {zh: "示例依赖运行时程序集", en: "Examples depend on runtime"}
---
