---
uid: 8a4e10fd
id: project-chaos.project-infra.git-helper
parent: project-chaos.project-infra
name: {zh: "Git 拉取助手", en: "Git Pull Helper"}
description:
  zh: >
      安全拉取助手：检查更新 → 看清影响 → 确认拉取。与 Assets/Scripts/Editor/GitTool 的编辑器窗口成对。
      
  en: >
      Safe git-pull helper: inspect → see impact → confirm. Mirrored by the Unity editor window at Assets/Scripts/Editor/GitTool.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.771Z"
fingerprint: 1613e934b478843f942246f0026a9eaf0d52f105a109d0d224df78d5319fecb3
source:
  - path: "gittool/pull.sh"
  - path: "gittool/pull.bat"
  - path: "gittool/README.md"
apis:
  - protocol: file
    path: "gittool/pull.sh"
    description:
      zh: >
          Bash 主脚本（全部逻辑）：变更分级、Unity 占用保护、--dry-run/--yes/--discard。
          
      en: >
          Bash entry script (all logic): classify changes, guard against Unity open, --dry-run/--yes/--discard.
          
  - protocol: file
    path: "gittool/pull.bat"
    description:
      zh: >
          Windows 双击入口：定位 Git 自带 bash.exe 并转交 pull.sh。
          
      en: >
          Windows double-click wrapper that locates Git's bash.exe and forwards to pull.sh.
          
  - protocol: file
    path: "gittool/README.md"
    description:
      zh: >
          gittool 手册：影响分级表、--discard 语义、GBK 编辑规则。
          
      en: >
          gittool manual: impact classification table, --discard semantics, GBK editing rules.
          
deps:
  - kind: reference
    to: project-chaos.project-infra.repo-root
    from_api: "file:gittool/pull.sh"
    to_api: "file:.gitignore"
    label: {zh: "对照忽略清单评估影响", en: "Uses ignore list for impact"}
---
