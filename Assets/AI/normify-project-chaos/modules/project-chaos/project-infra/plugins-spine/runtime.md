---
uid: a91c6d58
id: project-chaos.project-infra.plugins-spine.runtime
parent: project-chaos.project-infra.plugins-spine
name: {zh: "Spine 运行时", en: "Spine Runtime"}
description:
  zh: >
      Spine 运行时（97 个 C# 文件）：spine-csharp 核心 + spine-unity 组件。Citizen_001 与示例骨架都经此程序集加载。
      
  en: >
      Spine runtime (97 C# files): spine-csharp core plus spine-unity components. Citizen_001 and the example skeletons load through this assembly.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.784Z"
fingerprint: e0357fb4d4686a0fb02f5a8cc7aa972f665c8d86eb9b8bb920e01751cd688b83
source:
  - path: "Assets/Plugins/Spine/Runtime/spine-unity.asmdef"
  - path: "Assets/Plugins/Spine/Runtime/spine-unity/Components/SkeletonAnimation.cs"
  - path: "Assets/Plugins/Spine/Runtime/spine-unity/Components/SkeletonGraphic.cs"
  - path: "Assets/Plugins/Spine/Runtime/spine-unity/ISkeletonAnimation.cs"
  - path: "Assets/Plugins/Spine/Runtime/spine-csharp/AnimationState.cs"
apis:
  - protocol: file
    path: "Assets/Plugins/Spine/Runtime/spine-unity.asmdef"
    description:
      zh: >
          spine-unity 运行时程序集定义。
          
      en: >
          spine-unity runtime assembly definition.
          
  - protocol: file
    path: "Assets/Plugins/Spine/Runtime/spine-unity/Components/SkeletonAnimation.cs"
    description:
      zh: >
          SkeletonAnimation 组件，Spine 在 2D 精灵上的主入口。
          
      en: >
          SkeletonAnimation MonoBehaviour, the main Spine component for 2D sprites.
          
  - protocol: file
    path: "Assets/Plugins/Spine/Runtime/spine-unity/Components/SkeletonGraphic.cs"
    description:
      zh: >
          SkeletonGraphic 组件，用于 UGUI 下的 Spine 渲染。
          
      en: >
          SkeletonGraphic component for UGUI-based Spine rendering.
          
  - protocol: file
    path: "Assets/Plugins/Spine/Runtime/spine-unity/ISkeletonAnimation.cs"
    description:
      zh: >
          ISkeletonAnimation 接口，所有 Spine 动画组件共同实现。
          
      en: >
          ISkeletonAnimation interface implemented by all Spine animation components.
          
  - protocol: file
    path: "Assets/Plugins/Spine/Runtime/spine-csharp/AnimationState.cs"
    description:
      zh: >
          AnimationState：spine-csharp 核心状态机，驱动轨道与事件。
          
      en: >
          AnimationState: the core spine-csharp state machine driving tracks and events.
          
---
