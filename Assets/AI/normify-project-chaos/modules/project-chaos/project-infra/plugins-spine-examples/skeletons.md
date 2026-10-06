---
uid: f36e9d02
id: project-chaos.project-infra.plugins-spine-examples.skeletons
parent: project-chaos.project-infra.plugins-spine-examples
name: {zh: "示例骨架资源", en: "Example Skeletons"}
description:
  zh: >
      168 个示例骨架文件（Dragon、Eyes、Raptor、Spineboy 等）及各自图集、JSON 与材质。
      
  en: >
      168 example skeleton files (Dragon, Eyes, Raptor, Spineboy, ...) with their atlases, JSON and materials.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.781Z"
fingerprint: 7a88129b3bcaf6a37a1047d46bbfec573450d831cb727320ad68e751bca734aa
source:
  - path: "Assets/Plugins/Spine Examples/Spine Skeletons/Dragon/dragon_SkeletonData.asset"
  - path: "Assets/Plugins/Spine Examples/Spine Skeletons/Eyes/eyes.json"
  - path: "Assets/Plugins/Spine Examples/Spine Skeletons/Runtime Template Material.mat"
apis:
  - protocol: file
    path: "Assets/Plugins/Spine Examples/Spine Skeletons/Dragon/dragon_SkeletonData.asset"
    description:
      zh: >
          Dragon 骨架数据（168 个示例骨架文件的代表）。
          
      en: >
          Dragon skeleton data (168-file example skeleton set, representative).
          
  - protocol: file
    path: "Assets/Plugins/Spine Examples/Spine Skeletons/Eyes/eyes.json"
    description:
      zh: >
          Eyes 骨架 JSON 源文件。
          
      en: >
          Eyes skeleton JSON source.
          
  - protocol: file
    path: "Assets/Plugins/Spine Examples/Spine Skeletons/Runtime Template Material.mat"
    description:
      zh: >
          示例骨架共用的运行时模板材质。
          
      en: >
          Shared runtime template material for example skeletons.
          
deps:
  - kind: reference
    to: project-chaos.project-infra.plugins-spine.runtime
    from_api: "file:Assets/Plugins/Spine Examples/Spine Skeletons/Dragon/dragon_SkeletonData.asset"
    to_api: "file:Assets/Plugins/Spine/Runtime/spine-unity.asmdef"
    label: {zh: "骨架由运行时加载", en: "Skeletons load via runtime"}
---
