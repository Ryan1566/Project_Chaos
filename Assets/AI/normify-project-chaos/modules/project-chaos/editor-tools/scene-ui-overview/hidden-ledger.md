---
uid: e0a10303
id: project-chaos.editor-tools.scene-ui-overview.hidden-ledger
parent: project-chaos.editor-tools.scene-ui-overview
name: {zh: "隐藏账本与 Overlay", en: "Hidden Ledger and Overlay"}
description:
  zh: >
      隐藏对象的持久记忆：Library 下的账本（与 SceneVisibilityState 同寿命）记录场景路径与层级路径，工具被强杀时按账本自愈，场景打开时查账本。同时包含暴露开关的 Scene 视图 Overlay。
      
  en: >
      Persistent memory for hidden canvases: the ledger under Library (same lifetime as SceneVisibilityState) records scene path plus hierarchy path, is healed when the tool was killed while enabled, and is consulted on scene open. Also holds the Scene-view Overlay exposing the toggle.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.738Z"
fingerprint: 9aaebbddb0f3dd27a279c563d1b8952bbdcffe7c594e2baeb5a3b6b8cac08707
source:
  - path: "Assets/Scripts/Editor/SceneUIOverviewTool/SceneUIOverviewTool.cs"
    line: 670
    end_line: 879
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/SceneUIOverviewTool/SceneUIOverviewTool.cs#L670-L879"
    description:
      zh: >
          隐藏账本与 Overlay 入口所在行段。
          
      en: >
          Line range holding the hidden ledger and the overlay entry.
          
  - protocol: rpc
    path: "SceneUIOverviewTool.SaveLedger"
    description:
      zh: >
          写入本工具隐藏的 Canvas 账本。
          
      en: >
          Writes the ledger of canvases hidden by this tool.
          
  - protocol: rpc
    path: "SceneUIOverviewTool.HealLedger"
    description:
      zh: >
          按账本修复强杀后遗留的孤儿隐藏对象。
          
      en: >
          Heals orphan hidden canvases recorded in the ledger.
          
  - protocol: rpc
    path: "SceneUIOverviewTool.FindByPath"
    description:
      zh: >
          按场景内层级路径查找 Transform。
          
      en: >
          Finds a transform by scene path.
          
  - protocol: rpc
    path: "SceneUIOverviewOverlay.CreatePanelContent"
    description:
      zh: >
          构建 Scene 视图 Overlay 面板内容。
          
      en: >
          Builds the Scene-view overlay panel content.
          
---
