---
uid: 4a9e3b70
id: project-chaos.project-infra.project-settings.core
parent: project-chaos.project-infra.project-settings
name: {zh: "工程核心身份", en: "Core Project Identity"}
description:
  zh: >
      工程身份与编辑器基线：PlayerSettings、Unity 版本锁、EditorSettings 与 Tag/Layer 表。
      
  en: >
      Project identity and editor baseline: PlayerSettings, the Unity version pin, EditorSettings and the Tag/Layer table.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.457Z"
fingerprint: 2f2580c5ffac97e08a01760025c9e1f8bcb085ffc467d15c53dd3d08cc5722a7
source:
  - path: "ProjectSettings/ProjectSettings.asset"
  - path: "ProjectSettings/ProjectVersion.txt"
  - path: "ProjectSettings/EditorSettings.asset"
  - path: "ProjectSettings/TagManager.asset"
apis:
  - protocol: file
    path: "ProjectSettings/ProjectSettings.asset"
    description:
      zh: >
          Player 主设置（22KB）：产品名、公司、图标、API 级别、脚本后端。
          
      en: >
          Main Player settings (22 KB): product name, company, icons, API level, scripting backend.
          
  - protocol: file
    path: "ProjectSettings/ProjectVersion.txt"
    description:
      zh: >
          Unity 编辑器版本锁定：2022.3.57f1c2 (32588f90613b)。
          
      en: >
          Unity editor version pin: 2022.3.57f1c2 (32588f90613b).
          
  - protocol: file
    path: "ProjectSettings/EditorSettings.asset"
    description:
      zh: >
          编辑器偏好：序列化模式、meta 文件可见性、换行符。
          
      en: >
          Editor prefs: serialization mode, meta files, line endings.
          
  - protocol: file
    path: "ProjectSettings/TagManager.asset"
    description:
      zh: >
          Tag 与 Layer 定义（421 字节）。
          
      en: >
          Tags and layers definition (421 bytes).
          
---
