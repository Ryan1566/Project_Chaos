---
uid: e0a10302
id: project-chaos.editor-tools.scene-ui-overview.preview-panel
parent: project-chaos.editor-tools.scene-ui-overview
name: {zh: "预览面板渲染", en: "Preview Panel Rendering"}
description:
  zh: >
      预览面板渲染：绘制世界包围盒与面板边框，用普通相机 + RenderTexture 渲染根 Canvas（PreviewRenderUtility 看不到场景 Canvas），把 Overlay 画布切到探测相机，抑制非 UI 渲染器，并在渲染前临时激活停用面板、事后还原。
      
  en: >
      Preview panel rendering: draws world bounds and the panel frame, renders root canvases into a RenderTexture through a normal camera (PreviewRenderUtility cannot see scene canvases), switches overlay canvases to a probe camera, suppresses non-UI renderers, and temporarily activates inactive panels before restoring them.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.738Z"
fingerprint: 9aaebbddb0f3dd27a279c563d1b8952bbdcffe7c594e2baeb5a3b6b8cac08707
source:
  - path: "Assets/Scripts/Editor/SceneUIOverviewTool/SceneUIOverviewTool.cs"
    line: 371
    end_line: 669
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/SceneUIOverviewTool/SceneUIOverviewTool.cs#L371-L669"
    description:
      zh: >
          预览面板、相机配置与抑制所在行段。
          
      en: >
          Line range holding the preview panel, camera setup and suppression.
          
  - protocol: rpc
    path: "SceneUIOverviewTool.DrawPreviewPanel"
    description:
      zh: >
          绘制带包围盒与关闭按钮的左侧预览面板。
          
      en: >
          Draws the left preview panel with bounds and close button.
          
  - protocol: rpc
    path: "SceneUIOverviewTool.ConfigurePreviewCamera"
    description:
      zh: >
          把预览相机对准指定根 Canvas。
          
      en: >
          Points the preview camera at a root canvas.
          
  - protocol: rpc
    path: "SceneUIOverviewTool.SuppressNonUi"
    description:
      zh: >
          抑制遮住预览的非 UI 渲染器。
          
      en: >
          Suppresses non-UI renderers occluding the preview.
          
  - protocol: rpc
    path: "SceneUIOverviewTool.ActivateInactivePanels"
    description:
      zh: >
          渲染前临时激活停用的面板。
          
      en: >
          Temporarily activates inactive panels for rendering.
          
---
