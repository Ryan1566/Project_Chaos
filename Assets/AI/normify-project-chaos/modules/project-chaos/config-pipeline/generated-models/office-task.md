---
uid: cf000308
id: project-chaos.config-pipeline.generated-models.office-task
parent: project-chaos.config-pipeline.generated-models
tags: [config, generated]
name: {zh: "办事任务数据类", en: "Office Task Data Class"}
description:
  zh: >
      办事任务、选项与时代门槛对应的 C# 数据类。
      
  en: >
      C# data classes for office tasks, options and era gates.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.440Z"
fingerprint: e46e275738fa79dac1815b6b2faaca30eb21e61bd8b88570cba1f3f96ba8989b
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/OfficeTaskTable_OfficeTaskConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/OfficeTaskTable_OfficeTaskOptionConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/OfficeTaskTable_OfficeEraGateConfig.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/OfficeTaskTable_OfficeTaskConfig.cs"
    description:
      zh: >
          办事任务数据类
          
      en: >
          Office task data class
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.generated-json.office-task
    label: {zh: "对应运行时 Json", en: "Mirrors runtime JSON"}
---
