---
uid: 7d13c9a4
id: project-chaos.project-infra.python-docs.setting-panel-doc
parent: project-chaos.project-infra.python-docs
tags: [infra, docs, python, settings]
name: {zh: "设置面板说明文档生成", en: "Setting Panel Doc Generator"}
description:
  zh: >
      Tools/gen_setting_panel_doc.py（1253 行）：用 python-docx 生成《设置面板设计方案与实现说明》Word 文档，排版风格对齐 Assets/Learn 下已有中文说明文档（Title + Heading1 中文序号 + Heading2 1.1/2.3 + Normal/List Bullet/Table Grid，正文微软雅黑 10.5pt），输出到 Assets/Story。不在 DocsSrc 目录下。
      
  en: >
      Tools/gen_setting_panel_doc.py (1253 lines) builds the settings-panel design/implementation Word doc with python-docx, matching the layout of the existing Chinese manuals in Assets/Learn (Title + Heading1 Chinese numerals + Heading2 1.1/2.3 + Normal/List Bullet/Table Grid, body in Microsoft YaHei 10.5pt), writing into Assets/Story. It lives outside DocsSrc.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T09:03:54.698Z"
fingerprint: 0a0204ceeca31c14dab994ce24efb95b39d62d71c67a5be69c25a52d30379cdc
source:
  - path: "Tools/gen_setting_panel_doc.py"
apis:
  - protocol: rpc
    path: "gen_setting_panel_doc.main"
    description:
      zh: >
          入口：无参数运行，生成设置面板说明 .docx
          
      en: >
          Entry: run with no arguments to emit the settings-panel .docx
          
  - protocol: rpc
    path: "gen_setting_panel_doc.build_document"
    description:
      zh: >
          按 Learn 文档风格拼装章节、列表与表格
          
      en: >
          Assemble headings, bullets and tables in the Learn manual style
          
deps:
  - kind: reference
    to: project-chaos.project-infra.python-docs.docx-kit
    label: {zh: "复用 docx 样式工具", en: "Reuses docx style kit"}
---
