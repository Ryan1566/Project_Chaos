---
uid: cf000307
id: project-chaos.config-pipeline.generated-models.meta-skill-tree
parent: project-chaos.config-pipeline.generated-models
tags: [config, generated]
name: {zh: "局外技能树数据类", en: "Skill Tree Data Class"}
description:
  zh: >
      家族树技能棋盘、节点、效果与文案对应的 C# 数据类。
      
  en: >
      C# data classes for the family-tree skill board, nodes, effects and text.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:48:14.439Z"
fingerprint: ab118cbdd482e5bb291013ad42d355f3f28afe165f7e61eba1c3b3adb750a529
source:
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/MetaSkillTreeTable_FamilyTreeConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/MetaSkillTreeTable_FamilyTreeBoardConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/MetaSkillTreeTable_FamilyTreeEffectConfig.cs"
  - path: "Assets/Scripts/Runtime/MVP/Model/ConfigData/MetaSkillTreeTable_FamilyTreeTextConfig.cs"
apis:
  - protocol: file
    path: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/MetaSkillTreeTable_FamilyTreeConfig.cs"
    description:
      zh: >
          技能树数据类
          
      en: >
          Skill tree data class
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.generated-json.meta-skill-tree
    label: {zh: "对应运行时 Json", en: "Mirrors runtime JSON"}
---
