---
uid: e0a10204
id: project-chaos.editor-tools.git.window-ui
parent: project-chaos.editor-tools.git
name: {zh: "Git 窗口展示层", en: "Git Window Presentation"}
description:
  zh: >
      窗口展示层：带错误样式的状态栏、仓库信息块、警告块（Unity 运行中、detached head、进行中的操作）、操作按钮区、最多列 300 个文件的分类预览列表，以及打开终端的辅助入口。
      
  en: >
      Window presentation layer: status message with error styling, repo info block, warning block (Unity running, detached head, in-progress operation), action buttons, categorized preview list capped at 300 files, and an open-terminal helper.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.734Z"
fingerprint: d766f0fb475c3b23d1c92ab3b4f89c07c7b3b1735449fc2912052bd11baa00bb
source:
  - path: "Assets/Scripts/Editor/GitTool/GitToolWindow.cs"
    line: 495
    end_line: 732
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/GitTool/GitToolWindow.cs#L495-L732"
    description:
      zh: >
          窗口绘制与终端辅助所在行段。
          
      en: >
          Line range holding the window drawing and terminal helper.
          
  - protocol: rpc
    path: "Menu/Tool/Git拉取助手"
    description:
      zh: >
          打开 Git 拉取助手窗口的菜单项。
          
      en: >
          Menu item opening the Git pull assistant window.
          
  - protocol: rpc
    path: "GitToolWindow.ShowWindow"
    description:
      zh: >
          打开 Git 拉取助手窗口。
          
      en: >
          Opens the Git pull assistant window.
          
  - protocol: rpc
    path: "GitToolWindow.DrawActions"
    description:
      zh: >
          绘制操作按钮区。
          
      en: >
          Draws the action buttons section.
          
  - protocol: rpc
    path: "GitToolWindow.DrawPreview"
    description:
      zh: >
          绘制分类后的更新预览列表。
          
      en: >
          Draws the categorized update preview list.
          
deps:
  - kind: call
    to: project-chaos.editor-tools.git.update-flow
    from_api: "rpc:GitToolWindow.DrawActions"
    to_api: "rpc:GitToolWindow.CheckForUpdates"
    label: {zh: "按钮触发更新流程", en: "Buttons run the flows"}
---
