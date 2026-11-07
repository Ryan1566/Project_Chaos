---
uid: cf000211
id: project-chaos.config-pipeline.generated-json.world-resource
parent: project-chaos.config-pipeline.generated-json
tags: [config, generated]
name: {zh: "资源经营 Json", en: "World Resource JSON"}
description:
  zh: >
      资源产地与产出的运行时 Json 产物。
      
  en: >
      Runtime JSON for resource facilities and outputs.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.432Z"
fingerprint: bc202ac51a71e2be7ac7d6335929912e2e46a815873fd653d6048b54c43a2d2a
source:
  - path: "Assets/Resources/Data/Json/Runtime/WorldResourceTable_ResourceFacility.json"
  - path: "Assets/Resources/Data/Json/Runtime/WorldResourceTable_ResourceOutput.json"
apis:
  - protocol: file
    path: "file:Assets/Resources/Data/Json/Runtime/WorldResourceTable_ResourceFacility.json"
    description:
      zh: >
          资源经营运行时 Json
          
      en: >
          World resource runtime JSON
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.world-resource
    label: {zh: "由源表导出", en: "Exported from workbook"}
---
