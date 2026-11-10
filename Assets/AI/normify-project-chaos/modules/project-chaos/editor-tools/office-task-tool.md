---
uid: b2c00212
id: project-chaos.editor-tools.office-task-tool
parent: project-chaos.editor-tools
tags: [editor, tool, office-task]
name: {zh: "办事任务校验器", en: "Office Task Verification"}
description:
  zh: >
      编辑器侧的办事任务校验：按表格与时代门槛核对任务链路，输出通过/失败状态，给内容配置改完后的自检用。
      
  en: >
      Editor-side office-task verification: walks the task chain against the workbook and era gates and reports pass/fail status for post-edit self-checks.
      
revision: 08f9a356150c4617b28273d13535c6f14ae4d299
updated_at: "2026-11-10T14:57:10.678Z"
fingerprint: 21b33cac8fcbd5b49ebdd4c5601756ae61785ecd4fe245f8ba8f6f190c7a4f72
source:
  - path: "Assets/Scripts/Editor/OfficeTaskTool/OfficeTaskVerification.cs"
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/OfficeTaskTool/OfficeTaskVerification.cs"
    description:
      zh: >
          办事任务校验器
          
      en: >
          Office task verifier
          
  - protocol: rpc
    path: "rpc:OfficeTaskVerification.Start"
    description:
      zh: >
          启动校验
          
      en: >
          Starts verification
          
  - protocol: rpc
    path: "rpc:OfficeTaskVerification.Status"
    description:
      zh: >
          最近一次校验状态
          
      en: >
          Last verification status
          
deps:
  - kind: reference
    to: project-chaos.config-pipeline.table-repo.office-task
    label: {zh: "校验任务表", en: "Verifies task table"}
  - kind: reference
    to: project-chaos.bootstrap.w5-gameplay.meta-events
    label: {zh: "对照运行时任务服务", en: "Against runtime task service"}
---
