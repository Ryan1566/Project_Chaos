---
uid: e73a25bc
id: project-chaos.project-infra.plugins-spine.package-meta
parent: project-chaos.project-infra.plugins-spine
name: {zh: "Spine 包元数据", en: "Spine Package Meta"}
description:
  zh: >
      Spine 包元数据：版本声明与完整变更日志，位于 Assets/Plugins/Spine 内。
      
  en: >
      Spine package metadata: version declaration and the full changelog kept inside Assets/Plugins/Spine.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.783Z"
fingerprint: d22393bcdc4c9034e315388c4d3f7b50bc729a84ec9a8da53174dd767c2eca55
source:
  - path: "Assets/Plugins/Spine/package.json"
  - path: "Assets/Plugins/Spine/version.txt"
  - path: "Assets/Plugins/Spine/CHANGELOG.md"
apis:
  - protocol: file
    path: "Assets/Plugins/Spine/package.json"
    description:
      zh: >
          Spine package.json，声明插件版本。
          
      en: >
          Spine package.json declaring the plugin version.
          
  - protocol: file
    path: "Assets/Plugins/Spine/version.txt"
    description:
      zh: >
          Spine version.txt（143 字节）。
          
      en: >
          Spine version.txt (143 bytes).
          
  - protocol: file
    path: "Assets/Plugins/Spine/CHANGELOG.md"
    description:
      zh: >
          Spine 变更日志（121KB），供升级比对。
          
      en: >
          Spine changelog (121 KB) kept for upgrade reference.
          
---
