---
uid: f59c2d84
id: project-chaos.project-infra.agent-config
parent: project-chaos.project-infra
name: {zh: "Agent 配置", en: "Agent Config"}
description:
  zh: >
      藏在 Assets 内的 Agent 工具配置：Claude Code 本地设置，白名单包含 env 命令与 Unity MCP 实例/执行类工具。
      
  en: >
      Agent tooling config found inside Assets: a Claude Code local settings file whitelisting the env command and Unity MCP instance/execute tools.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.769Z"
fingerprint: a452556c17a02358d4f5b8013c51512bbc470f8ca557b31c3e1f776f2612a668
source:
  - path: "Assets/.claude/settings.local.json"
apis:
  - protocol: file
    path: "Assets/.claude/settings.local.json"
    description:
      zh: >
          Claude Code 本地权限白名单：Bash(env) 加三个 Unity MCP 工具。
          
      en: >
          Claude Code local permission allowlist: Bash(env) plus three Unity MCP tools.
          
---
