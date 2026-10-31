---
uid: 7d3e9a12
id: project-chaos.project-infra.dsh-toolchain
parent: project-chaos.project-infra
tags: [infra, agent, mcp, docs]
name: {zh: "本地 Agent 工具链", en: "Local Agent Toolchain (Tools/)"}
description:
  zh: >
      仓库根 Tools/ 下的 Agent 工具链：dsh-unity-mcp 是一个本地 DSH bundle（Unity MCP 桥接，把 Editor 工具以 mcp__unity__* 暴露给模型），gen_setting_panel_doc.py 是设置面板文档的 python-docx 生成器。都不属于 Unity 运行时资源，不进任何 asmdef。
      
  en: >
      Agent toolchain under repo-root Tools/: dsh-unity-mcp is a local DSH bundle bridging Unity's MCP server (surfacing editor tools as mcp__unity__*), and gen_setting_panel_doc.py is a python-docx generator for the settings-panel doc. Neither is a Unity runtime asset and neither joins an asmdef.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T09:02:00.000Z"
fingerprint: 56c0e80689d4d79171f505896db9082125e711fdbfd65182b0ffb2ceb1717aca
source:
  - path: "Tools/dsh-unity-mcp/package.json"
  - path: "Tools/dsh-unity-mcp/cordis.patch.yml"
  - path: "Tools/gen_setting_panel_doc.py"
apis:
  - protocol: file
    path: "Tools/dsh-unity-mcp/package.json"
    description:
      zh: >
          本地 DSH bundle 的 npm 包声明：private、type=module，dsh.bundle.patch 指向 ./cordis.patch.yml。
          
      en: >
          npm meta for the local DSH bundle: private, type=module, points the dsh.bundle.patch at ./cordis.patch.yml.
          
  - protocol: file
    path: "Tools/dsh-unity-mcp/cordis.patch.yml"
    description:
      zh: >
          真正生效的 bundle 补丁：插入 mcp-unity 行，以 stdio 转发到 AnkleBreaker Unity MCP 服务；failOnStartupError=false 使 Unity 未开时 DSH 仍可启。
          
      en: >
          The actual bundle patch: inserts the mcp-unity row pointing at the AnkleBreaker Unity MCP server over stdio; failOnStartupError=false keeps DSH usable when Unity is closed.
          
  - protocol: file
    path: "Tools/gen_setting_panel_doc.py"
    description:
      zh: >
          独立运行 python-docx 的文档生成器，产出设置面板设计方案 docx；产物是派生物，不手改。
          
      en: >
          Standalone python-docx generator for the settings-panel design doc; output is derived, not hand-edited.
          
---
