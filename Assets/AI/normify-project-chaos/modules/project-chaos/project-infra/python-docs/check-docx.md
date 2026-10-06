---
uid: 72fa5e46
id: project-chaos.project-infra.python-docs.check-docx
parent: project-chaos.project-infra.python-docs
name: {zh: "docx 自检脚本", en: "docx Self-Check"}
description:
  zh: >
      生成文档的自检：包完整性、84 字符代码行宽上限，以及两份 TMP 字体缺失的字符。
      
  en: >
      Self-check for generated manuals: package integrity, the 84-character code-line limit, and characters missing from the two TMP fonts.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.792Z"
fingerprint: c616b95671386e8f0efdac74ef6dd5f2df74cb021c2ead9a4b4d4b5ea71f2dc1
source:
  - path: "Assets/DocsSrc/check_docx.py"
    line: 32
    end_line: 143
apis:
  - protocol: rpc
    path: "check_docx.find_font_assets"
    description:
      zh: >
          find_font_assets(start)：定位用于字形覆盖检查的 TMP 字体资产。
          
      en: >
          find_font_assets(start): locates the TMP font assets used for glyph-coverage checks.
          
  - protocol: rpc
    path: "check_docx.glyph_coverage"
    description:
      zh: >
          glyph_coverage(paths)：读取各 TMP 字体的字符集覆盖。
          
      en: >
          glyph_coverage(paths): reads the character set covered by each TMP font.
          
  - protocol: rpc
    path: "check_docx.check"
    description:
      zh: >
          check(path, cov)：校验 ZIP/XML 完整性、代码块行宽与缺失字形。
          
      en: >
          check(path, cov): validates ZIP/XML integrity, code-block line width and missing glyphs.
          
  - protocol: rpc
    path: "check_docx.main"
    description:
      zh: >
          main()：接收 .docx 路径的命令行入口。
          
      en: >
          main(): CLI entry point taking the .docx path.
          
deps:
  - kind: reference
    to: project-chaos.project-infra.learn-manuals.system-guides
    from_api: "rpc:check_docx.check"
    to_api: "file:Assets/Learn/本地化系统使用说明.docx"
    label: {zh: "自检生成结果", en: "Checks generated docx"}
---
