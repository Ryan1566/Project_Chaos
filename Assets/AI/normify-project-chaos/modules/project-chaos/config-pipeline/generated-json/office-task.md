---
uid: cf000208
id: project-chaos.config-pipeline.generated-json.office-task
parent: project-chaos.config-pipeline.generated-json
tags: [config, generated]
name: {zh: "办事任务 Json", en: "Office Task JSON"}
description:
  zh: >
      办事任务、选项与时代门槛的运行时 Json 产物。
      
  en: >
      Runtime JSON for office tasks, options and era gates.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.416Z"
fingerprint: f5f39dde9bf4dc8f974b3644a3519c00e78a9e7a8b14b35011a23f9fbb331ff4
source:
  - path: "Assets/Resources/Data/Json/Runtime/OfficeTaskTable_OfficeTask.json"
  - path: "Assets/Resources/Data/Json/Runtime/OfficeTaskTable_OfficeTaskOption.json"
  - path: "Assets/Resources/Data/Json/Runtime/OfficeTaskTable_OfficeEraGate.json"
apis:
  - protocol: file
    path: "file:Assets/Resources/Data/Json/Runtime/OfficeTaskTable_OfficeTask.json"
    description:
      zh: >
          办事任务运行时 Json
          
      en: >
          Office task runtime JSON
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.office-task
    label: {zh: "由源表导出", en: "Exported from workbook"}
---
