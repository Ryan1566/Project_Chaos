---
uid: e0a10301
id: project-chaos.editor-tools.scene-ui-overview.overview-core
parent: project-chaos.editor-tools.scene-ui-overview
name: {zh: "开关状态与隐藏", en: "Overview State and Hiding"}
description:
  zh: >
      工具状态与可见性：静态 InitializeOnLoad 入口、基于 SessionState 的开启开关与变更事件、根 Canvas 收集、用 SceneVisibilityManager 做对象级隐藏/还原（不按 Layer，因为面板预制体在 Default 层），以及场景打开时接管隐藏对象。
      
  en: >
      Tool state and visibility: static InitializeOnLoad entry, SessionState-backed enabled flag with change event, root-canvas discovery, hide/restore through SceneVisibilityManager (object-level, not layer-based, because panel prefabs stay on the Default layer), and adoption of hidden canvases after a scene opens.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.738Z"
fingerprint: 9aaebbddb0f3dd27a279c563d1b8952bbdcffe7c594e2baeb5a3b6b8cac08707
source:
  - path: "Assets/Scripts/Editor/SceneUIOverviewTool/SceneUIOverviewTool.cs"
    line: 40
    end_line: 370
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/SceneUIOverviewTool/SceneUIOverviewTool.cs#L40-L370"
    description:
      zh: >
          工具开关状态、隐藏与可见性接管所在行段。
          
      en: >
          Line range holding tool state, hiding and visibility adoption.
          
  - protocol: rpc
    path: "Menu/Tool/UI/UI 与场景分离显示（Scene 视图）"
    description:
      zh: >
          切换 Scene 视图 UI 分离显示的菜单项。
          
      en: >
          Menu item toggling UI separation in the Scene view.
          
  - protocol: rpc
    path: "SceneUIOverviewTool.Enabled"
    description:
      zh: >
          读写持久化的开启状态。
          
      en: >
          Read/write the persisted enabled flag.
          
  - protocol: rpc
    path: "SceneUIOverviewTool.EnabledChanged"
    description:
      zh: >
          开启状态变化时触发的事件。
          
      en: >
          Event raised when the enabled state changes.
          
  - protocol: rpc
    path: "SceneUIOverviewTool.ApplyHide"
    description:
      zh: >
          用 SceneVisibilityManager 隐藏根 Canvas。
          
      en: >
          Hides root canvases via SceneVisibilityManager.
          
  - protocol: rpc
    path: "SceneUIOverviewTool.RestoreHide"
    description:
      zh: >
          还原先前隐藏的 Canvas。
          
      en: >
          Restores previously hidden canvases.
          
deps:
  - kind: call
    to: project-chaos.editor-tools.scene-ui-overview.preview-panel
    from_api: "rpc:SceneUIOverviewTool.Enabled"
    to_api: "rpc:SceneUIOverviewTool.DrawPreviewPanel"
    label: {zh: "开启后重绘预览", en: "Drives the preview panel"}
  - kind: call
    to: project-chaos.editor-tools.scene-ui-overview.hidden-ledger
    from_api: "rpc:SceneUIOverviewTool.RestoreHide"
    to_api: "rpc:SceneUIOverviewTool.SaveLedger"
    label: {zh: "账本记录隐藏集", en: "Ledger tracks hidden set"}
---
