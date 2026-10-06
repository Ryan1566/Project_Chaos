---
uid: 8b2f04c7
id: project-chaos.assets.art.citizen-001
parent: project-chaos.assets.art
name: {zh: "Citizen_001 角色骨架", en: "Citizen_001 Skeleton"}
description:
  zh: >
      Spine 导出的角色 Citizen_001：skeleton JSON、atlas 文本、贴图、SkeletonData/Atlas 资产与材质，是工程内唯一自制 Spine 角色。
      
  en: >
      Spine export of the Citizen_001 character: skeleton JSON, atlas text, texture, SkeletonData/Atlas assets and material — the only in-house Spine character.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.465Z"
fingerprint: 4d774c87cffc48d117968bcb463385517a086898270285590c4c61801ed976c2
source:
  - path: "Assets/Art/FBX/Citizen_001/citizen_001.json"
  - path: "Assets/Art/FBX/Citizen_001/citizen_001.atlas.txt"
  - path: "Assets/Art/FBX/Citizen_001/citizen_001.png"
  - path: "Assets/Art/FBX/Citizen_001/citizen_001_SkeletonData.asset"
  - path: "Assets/Art/FBX/Citizen_001/citizen_001_Atlas.asset"
  - path: "Assets/Art/FBX/Citizen_001/citizen_001_Material.mat"
apis:
  - protocol: file
    path: "Assets/Art/FBX/Citizen_001/citizen_001_SkeletonData.asset"
    description:
      zh: >
          角色骨架数据资产，SkeletonAnimation 的导入源。
          
      en: >
          Skeleton data asset, the import source for SkeletonAnimation.
          
  - protocol: file
    path: "Assets/Art/FBX/Citizen_001/citizen_001_Atlas.asset"
    description:
      zh: >
          图集资产，绑定贴图与材质。
          
      en: >
          Atlas asset binding the texture and material.
          
  - protocol: file
    path: "Assets/Art/FBX/Citizen_001/citizen_001_Material.mat"
    description:
      zh: >
          角色渲染材质。
          
      en: >
          Character render material.
          
  - protocol: file
    path: "Assets/Art/FBX/Citizen_001/citizen_001.json"
    description:
      zh: >
          Spine skeleton JSON 源文件。
          
      en: >
          Spine skeleton JSON source.
          
  - protocol: file
    path: "Assets/Art/FBX/Citizen_001/citizen_001.atlas.txt"
    description:
      zh: >
          Spine atlas 文本，记录区域与页。
          
      en: >
          Spine atlas text listing regions and pages.
          
deps:
  - kind: reference
    to: project-chaos.project-infra.plugins-spine.runtime
    from_api: "file:Assets/Art/FBX/Citizen_001/citizen_001_SkeletonData.asset"
    to_api: "file:Assets/Plugins/Spine/Runtime/spine-unity.asmdef"
    label: {zh: "由 Spine 运行时加载", en: "Loaded by Spine runtime"}
---
