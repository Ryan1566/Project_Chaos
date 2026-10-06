---
uid: b18f4c72
id: project-chaos.project-infra.python-docs.doc-builders
parent: project-chaos.project-infra.python-docs
name: {zh: "文档生成器脚本", en: "Doc Builder Scripts"}
description:
  zh: >
      顶层文档脚本：两个生成器（本地化、MVP 分层）与一个就地修订器（Excel 手册）。均为线性脚本，无函数定义。
      
  en: >
      Top-level doc scripts: two generators (localization, MVP layers) and one in-place patcher for the Excel manual. All are linear scripts with no functions.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.792Z"
fingerprint: aa971e9d6f0dc313ee3f27e30efbaf16111a3f03b0b1fec25a91591154a3cdbd
source:
  - path: "Assets/DocsSrc/gen_localization_doc.py"
  - path: "Assets/DocsSrc/gen_mvp_data_layers_doc.py"
  - path: "Assets/DocsSrc/update_excel_doc_model_layer.py"
apis:
  - protocol: file
    path: "Assets/DocsSrc/gen_localization_doc.py"
    description:
      zh: >
          生成 Assets/Learn/本地化系统使用说明.docx（十三章 + 两附录，脚本 54KB）。
          
      en: >
          Generates Assets/Learn/本地化系统使用说明.docx (13 chapters + 2 appendices, 54 KB script).
          
  - protocol: file
    path: "Assets/DocsSrc/gen_mvp_data_layers_doc.py"
    description:
      zh: >
          生成《MVP架构与配置数据分层说明》手册（脚本 38KB）。
          
      en: >
          Generates the MVP architecture & config data-layer manual (38 KB script).
          
  - protocol: file
    path: "Assets/DocsSrc/update_excel_doc_model_layer.py"
    description:
      zh: >
          就地修订 Excel 配置表工具说明：重写段落与表格单元格（脚本 23KB）。
          
      en: >
          Patches the existing Excel-tool manual in place by rewriting paragraphs and table cells (23 KB script).
          
deps:
  - kind: call
    to: project-chaos.project-infra.python-docs.docx-kit
    from_api: "file:Assets/DocsSrc/gen_localization_doc.py"
    to_api: "rpc:docx_kit.new_document"
    label: {zh: "生成器调用文档脚手架", en: "Builder uses docx kit"}
  - kind: dataflow
    to: project-chaos.project-infra.learn-manuals.system-guides
    from_api: "file:Assets/DocsSrc/gen_localization_doc.py"
    to_api: "file:Assets/Learn/本地化系统使用说明.docx"
    label: {zh: "生成本地化使用说明", en: "Writes localization guide"}
  - kind: dataflow
    to: project-chaos.project-infra.learn-manuals.pipeline-guides
    from_api: "file:Assets/DocsSrc/gen_mvp_data_layers_doc.py"
    to_api: "file:Assets/Learn/MVP架构与配置数据分层说明.docx"
    label: {zh: "生成 MVP 分层说明", en: "Writes MVP layer guide"}
  - kind: dataflow
    to: project-chaos.project-infra.learn-manuals.pipeline-guides
    from_api: "file:Assets/DocsSrc/update_excel_doc_model_layer.py"
    to_api: "file:Assets/Learn/Excel配置表工具使用说明.docx"
    label: {zh: "就地修订 Excel 手册", en: "Patches Excel manual"}
---
