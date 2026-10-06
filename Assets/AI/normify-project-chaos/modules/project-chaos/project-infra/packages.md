---
uid: 1c6a9b30
id: project-chaos.project-infra.packages
parent: project-chaos.project-infra
name: {zh: "包依赖清单", en: "Package Manifest"}
description:
  zh: >
      Unity 包清单与锁定文件。gittool README 指出 Packages/ 被 .gitignore 忽略，导致各机器包版本漂移。
      
  en: >
      Unity package manifest and lock file. gittool's README warns Packages/ is gitignored, so versions drift between machines.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.773Z"
fingerprint: 258bc094c3c383d9113bcc9f62a300ef0b2ad7475b8241bfcdbb0ffaf561b97e
source:
  - path: "Packages/manifest.json"
  - path: "Packages/packages-lock.json"
apis:
  - protocol: file
    path: "Packages/manifest.json"
    description:
      zh: >
          包依赖清单：46 条依赖（2D/Cinemachine/InputSystem/URP/TMP/Timeline + Unity 内置模块 + MCP 插件 + 2d-extras git 包）。
          
      en: >
          Package manifest: 46 dependencies (2D/Cinemachine/InputSystem/URP/TMP/Timeline + Unity modules + MCP plugin + 2d-extras git URL).
          
  - protocol: file
    path: "Packages/packages-lock.json"
    description:
      zh: >
          解析后的包锁定文件，记录确切版本与哈希。
          
      en: >
          Resolved package lock with exact versions and hashes.
          
---
