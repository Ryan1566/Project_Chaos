---
uid: 0f1d5b46
id: project-chaos.editor-tools.runmap-verify
parent: project-chaos.editor-tools
tags: [editor, runmap, verify, exploration, day-night]
name: {zh: "地图生成校验工具", en: "RunMap Verify Tool"}
description:
  zh: >
      Assets/Scripts/Editor/RunMapTool 下的地图工具集：RunMapVerifyTool 做生成器多种子压测与整图预览；ExplorationMapSetup / ExplorationMapVerification / ExplorationFlowVerification 把生成器切到探索式 v3 并做基线与端到端流程校验；DayNightFlowVerification 校验昼夜相位与时间推进。
      
  en: >
      Editor map toolset under Assets/Scripts/Editor/RunMapTool: RunMapVerifyTool stress-tests the generator over many seeds and renders whole-map previews; ExplorationMapSetup, ExplorationMapVerification and ExplorationFlowVerification switch to the v3 exploration generator and run baseline plus end-to-end flow checks; DayNightFlowVerification checks day/night phases and time advancement.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:54:15.852Z"
fingerprint: 09fe01f4dcf4f43d9726edea022776a577185585d74171aff012fd3740b7a0a8
source:
  - path: "Assets/Scripts/Editor/RunMapTool/RunMapVerifyTool.cs"
    line: 1
    end_line: 294
  - path: "Assets/Scripts/Editor/RunMapTool/ExplorationMapSetup.cs"
  - path: "Assets/Scripts/Editor/RunMapTool/ExplorationMapVerification.cs"
  - path: "Assets/Scripts/Editor/RunMapTool/ExplorationFlowVerification.cs"
  - path: "Assets/Scripts/Editor/RunMapTool/DayNightFlowVerification.cs"
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/RunMapTool/RunMapVerifyTool.cs#L1-L294"
    description:
      zh: >
          多种子压测、单预设压测、以及整图结构预览 PNG 渲染
          
      en: >
          Multi-seed stress test, single-preset stress test, and whole-map structure preview PNG rendering
          
  - protocol: rpc
    path: "Menu/Tools/局内地图/3. 校验生成器（多种子压测）"
    description:
      zh: >
          菜单：多种子压测生成器
          
      en: >
          Menu: stress-test the generator over many seeds
          
  - protocol: rpc
    path: "Menu/Tools/局内地图/4. 生成整图预览 PNG"
    description:
      zh: >
          菜单：生成整图结构预览 PNG
          
      en: >
          Menu: render whole-map structure preview PNGs
          
  - protocol: rpc
    path: "ExplorationMapSetup.Apply"
    description:
      zh: >
          把地图生成器切到探索式布局
          
      en: >
          Switch the map generator to the exploration layout
          
  - protocol: rpc
    path: "ExplorationMapVerification.StartV3"
    description:
      zh: >
          在 N 个样本下对比 v2 与 v3 生成器
          
      en: >
          Compare the v2 and v3 generators over N samples
          
  - protocol: rpc
    path: "ExplorationFlowVerification.Start"
    description:
      zh: >
          端到端跑一整局探索流程
          
      en: >
          Drive one exploration flow end to end
          
  - protocol: rpc
    path: "DayNightFlowVerification.Start"
    description:
      zh: >
          设置局内时间并校验昼夜切换
          
      en: >
          Set the run clock and verify day/night switching
          
  - protocol: file
    path: "file:Assets/Scripts/Editor/RunMapTool/ExplorationMapVerification.cs"
    description:
      zh: >
          探索地图校验
          
      en: >
          Exploration map verification
          
  - protocol: file
    path: "file:Assets/Scripts/Editor/RunMapTool/ExplorationFlowVerification.cs"
    description:
      zh: >
          探索流程校验
          
      en: >
          Exploration flow verification
          
  - protocol: file
    path: "file:Assets/Scripts/Editor/RunMapTool/DayNightFlowVerification.cs"
    description:
      zh: >
          昼夜流程校验
          
      en: >
          Day/night flow verification
          
deps:
  - kind: reference
    to: project-chaos.framework.runmap.exploration
    label: {zh: "演练探索地图", en: "Exercises exploration map"}
  - kind: reference
    to: project-chaos.bootstrap.w5-gameplay.day-night
    label: {zh: "演练昼夜系统", en: "Exercises day and night"}
---
