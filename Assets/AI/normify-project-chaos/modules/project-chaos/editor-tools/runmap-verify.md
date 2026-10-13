---
uid: 0f1d5b46
id: project-chaos.editor-tools.runmap-verify
parent: project-chaos.editor-tools
tags: [editor, runmap, verify]
name: {zh: "地图生成校验工具", en: "RunMap Verify Tool"}
description:
  zh: >
      Assets/Scripts/Editor/RunMapTool/RunMapVerifyTool.cs：生成器的验证与可视化——多种子压测（通过/失败/降级/确定性/一局三层覆盖）与整图结构预览 PNG。
      
  en: >
      Assets/Scripts/Editor/RunMapTool/RunMapVerifyTool.cs — verification and visualisation for the generator: multi-seed stress test (pass/fail/degraded/determinism/three-layer coverage) and whole-map structure previews rendered to PNG.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.414Z"
fingerprint: 8923e25338b3e21e6991191df8a3b20f795d4414814540963321139c4970c009
source:
  - path: "Assets/Scripts/Editor/RunMapTool/RunMapVerifyTool.cs"
    line: 1
    end_line: 294
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/RunMapTool/RunMapVerifyTool.cs#L1-L294"
    description:
      zh: >
          多种子压测（通过/失败/降级/确定性）、单预设压测、以及整图结构预览 PNG 渲染。
          
      en: >
          Multi-seed stress test (pass/fail/degraded/determinism), single-preset stress test, and whole-map structure preview PNG rendering.
          
  - protocol: rpc
    path: "Menu/Tools/局内地图/3. 校验生成器（多种子压测）"
    description:
      zh: >
          菜单：多种子压测生成器。
          
      en: >
          Menu: stress-test the generator over many seeds.
          
  - protocol: rpc
    path: "Menu/Tools/局内地图/4. 生成整图预览 PNG"
    description:
      zh: >
          菜单：生成整图结构预览 PNG。
          
      en: >
          Menu: render whole-map structure preview PNGs.
          
---
