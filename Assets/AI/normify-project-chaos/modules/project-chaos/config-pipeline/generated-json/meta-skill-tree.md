---
uid: cf000207
id: project-chaos.config-pipeline.generated-json.meta-skill-tree
parent: project-chaos.config-pipeline.generated-json
tags: [config, generated]
name: {zh: "局外技能树 Json", en: "Meta Skill Tree JSON"}
description:
  zh: >
      家族树技能棋盘、节点、效果与文案的运行时 Json 产物。
      
  en: >
      Runtime JSON for the family-tree skill board, nodes, effects and text.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.414Z"
fingerprint: 86149f9819f980e2397138b6fbc43daef343031adcd3c2191a9b0139ca955dd2
source:
  - path: "Assets/Resources/Data/Json/Runtime/MetaSkillTreeTable_FamilyTree.json"
  - path: "Assets/Resources/Data/Json/Runtime/MetaSkillTreeTable_FamilyTreeBoard.json"
  - path: "Assets/Resources/Data/Json/Runtime/MetaSkillTreeTable_FamilyTreeEffect.json"
  - path: "Assets/Resources/Data/Json/Runtime/MetaSkillTreeTable_FamilyTreeText.json"
apis:
  - protocol: file
    path: "file:Assets/Resources/Data/Json/Runtime/MetaSkillTreeTable_FamilyTree.json"
    description:
      zh: >
          局外技能树运行时 Json
          
      en: >
          Meta skill tree runtime JSON
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.table-repo.meta-skill-tree
    label: {zh: "由源表导出", en: "Exported from workbook"}
---
