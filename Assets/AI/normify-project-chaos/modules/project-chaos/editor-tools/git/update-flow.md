---
uid: e0a10203
id: project-chaos.editor-tools.git.update-flow
parent: project-chaos.editor-tools.git
name: {zh: "更新检查与拉取流程", en: "Update Check and Pull Flow"}
description:
  zh: >
      窗口的 git 流程：刷新仓库信息；本地无远端分支记录时联网探测上游；检查更新并构建预览；执行拉取，或丢弃已跟踪改动后拉取（本地有未推送提交时直接中止）。
      
  en: >
      The window's git flows: refresh repo information, probe the remote for the upstream branch when no local record exists, check for updates and build the preview, then pull, or discard tracked changes and pull (aborting if local commits are unpushed).
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.734Z"
fingerprint: d766f0fb475c3b23d1c92ab3b4f89c07c7b3b1735449fc2912052bd11baa00bb
source:
  - path: "Assets/Scripts/Editor/GitTool/GitToolWindow.cs"
    line: 18
    end_line: 494
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/GitTool/GitToolWindow.cs#L18-L494"
    description:
      zh: >
          仓库刷新、更新检查与拉取流程所在行段。
          
      en: >
          Line range holding repo refresh, update check and pull flows.
          
  - protocol: rpc
    path: "GitToolWindow.RefreshRepoInfo"
    description:
      zh: >
          刷新分支、上游与脏文件状态。
          
      en: >
          Refreshes branch, upstream and dirty-file state.
          
  - protocol: rpc
    path: "GitToolWindow.CheckForUpdates"
    description:
      zh: >
          检查远端更新并构建预览。
          
      en: >
          Checks the remote for updates and builds a preview.
          
  - protocol: rpc
    path: "GitToolWindow.PullUpdates"
    description:
      zh: >
          确认后拉取更新。
          
      en: >
          Pulls updates after confirmation.
          
  - protocol: rpc
    path: "GitToolWindow.DiscardAndPull"
    description:
      zh: >
          先丢弃已跟踪改动再拉取。
          
      en: >
          Discards tracked changes then pulls.
          
deps:
  - kind: call
    to: project-chaos.editor-tools.git.repo-core
    from_api: "rpc:GitToolWindow.RefreshRepoInfo"
    to_api: "rpc:GitHelper.GetCurrentBranch"
    label: {zh: "读取当前分支", en: "Reads current branch"}
  - kind: call
    to: project-chaos.editor-tools.git.change-preview
    from_api: "rpc:GitToolWindow.CheckForUpdates"
    to_api: "rpc:GitHelper.BuildPreview"
    label: {zh: "构建更新预览", en: "Builds update preview"}
  - kind: call
    to: project-chaos.editor-tools.git.change-preview
    from_api: "rpc:GitToolWindow.PullUpdates"
    to_api: "rpc:GitHelper.Pull"
    label: {zh: "执行拉取", en: "Performs the pull"}
---
