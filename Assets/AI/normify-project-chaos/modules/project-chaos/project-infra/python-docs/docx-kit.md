---
uid: a56d0e38
id: project-chaos.project-infra.python-docs.docx-kit
parent: project-chaos.project-infra.python-docs
name: {zh: "docx_kit 样式脚手架", en: "docx_kit Scaffold"}
description:
  zh: >
      样式脚手架，复刻 Learn 既有文档的排版；另提供 bullet/spacer/table。共 220 行。
      
  en: >
      Style scaffold that reproduces the existing Learn manuals' formatting; also exposes bullet/spacer/table. 220 lines.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.793Z"
fingerprint: e668e923a14bda7d99178521edccc82323e045e79504b7eb7684e53cd8440671
source:
  - path: "Assets/DocsSrc/docx_kit.py"
    line: 65
    end_line: 238
apis:
  - protocol: rpc
    path: "docx_kit.new_document"
    description:
      zh: >
          new_document()：建带页面设置与标准样式的根 Document。
          
      en: >
          new_document(): builds the base Document with page setup and the standard styles.
          
  - protocol: rpc
    path: "docx_kit.title"
    description:
      zh: >
          title(doc, text)：按既有排版写文档标题。
          
      en: >
          title(doc, text): writes the document title in the house style.
          
  - protocol: rpc
    path: "docx_kit.h1"
    description:
      zh: >
          h1(doc, text)：一级标题（#365F91）。
          
      en: >
          h1(doc, text): level-1 heading in colour #365F91.
          
  - protocol: rpc
    path: "docx_kit.para"
    description:
      zh: >
          para(doc, text, bold)：正文段落（微软雅黑 10.5pt）。
          
      en: >
          para(doc, text, bold): body paragraph in Microsoft YaHei 10.5pt.
          
  - protocol: rpc
    path: "docx_kit.code"
    description:
      zh: >
          code(doc, lines)：带底纹的代码块（Consolas 9pt + F4F4F4）。
          
      en: >
          code(doc, lines): shaded code block (Consolas 9pt on F4F4F4).
          
---
