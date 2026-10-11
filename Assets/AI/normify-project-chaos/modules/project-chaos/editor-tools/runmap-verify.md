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
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.704Z"
fingerprint: dbe873ee854ef7f71e23f9cec4531c595c4a4ff95149cb6e1d86ba541c0c4669
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
