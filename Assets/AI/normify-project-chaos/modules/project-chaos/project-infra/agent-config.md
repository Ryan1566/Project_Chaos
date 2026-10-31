---
uid: f59c2d84
id: project-chaos.project-infra.agent-config
parent: project-chaos.project-infra
name: {zh: "Agent 配置（Claude/Codex）", en: "Agent Config (Claude/Codex)"}
description:
  zh: >
      仓库内的 Agent 工具配置：Assets 内的 Claude Code 本地设置（白名单包含 env 命令与 Unity MCP 实例/执行类工具），以及仓库根目录的 Codex CLI 配置。
      
  en: >
      Agent tooling config found in the repo: the Claude Code local settings file (inside Assets) whitelisting the env command and Unity MCP instance/execute tools, plus the Codex CLI config at the repo root.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T09:03:54.712Z"
fingerprint: f5bb28fc72ef435944581fbac02f3fa7dd140ec7c7b3ae8663c2607d1a72d7bf
source:
  - path: "Assets/.claude/settings.local.json"
  - path: ".codex/config.toml"
apis:
  - protocol: file
    path: "Assets/.claude/settings.local.json"
    description:
      zh: >
          Claude Code 本地权限白名单：Bash(env) 加三个 Unity MCP 工具。
          
      en: >
          Claude Code local permission allowlist: Bash(env) plus three Unity MCP tools.
          
  - protocol: file
    path: ".codex/config.toml"
    description:
      zh: >
          仓库根的 Codex CLI 配置（在同一工作副本里作业的 Codex agent 的模型/提供商设置）。
          
      en: >
          Codex CLI config at the repo root (model/provider settings for the Codex agent working in the same checkout).
          
---
