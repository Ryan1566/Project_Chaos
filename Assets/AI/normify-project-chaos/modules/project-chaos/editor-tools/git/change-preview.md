---
uid: e0a10202
id: project-chaos.editor-tools.git.change-preview
parent: project-chaos.editor-tools.git
name: {zh: "改动分级与拉取", en: "Change Impact and Pull"}
description:
  zh: >
      改动影响模型与更新路径：把每个改动路径归入影响等级（代码/资源/工程设置），构建 ahead/behind 预览与分类条目，汇总并统计高影响改动，最后执行 pull 或丢弃已跟踪改动。
      
  en: >
      Change impact model and update path: classifies each changed path into an impact level (code vs assets vs project settings), builds the ahead/behind preview with categorized items, summarizes and counts high-impact changes, then performs pull or discard-tracked-changes.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.733Z"
fingerprint: 8a47f9f083458046c167adb12ae2541546c57eec2897256e7614d1e5318e5920
source:
  - path: "Assets/Scripts/Editor/GitTool/GitHelper.cs"
    line: 408
    end_line: 582
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/GitTool/GitHelper.cs#L408-L582"
    description:
      zh: >
          改动分级、更新预览与拉取操作所在行段。
          
      en: >
          Line range holding change classification, preview and pull operations.
          
  - protocol: rpc
    path: "GitHelper.Classify"
    description:
      zh: >
          按影响等级对改动路径分类。
          
      en: >
          Classifies a changed path by impact level.
          
  - protocol: rpc
    path: "GitHelper.BuildPreview"
    description:
      zh: >
          构建当前分支的更新预览。
          
      en: >
          Builds the update preview for the current branch.
          
  - protocol: rpc
    path: "GitHelper.Summarize"
    description:
      zh: >
          按分类汇总预览条目。
          
      en: >
          Summarizes preview items by category.
          
  - protocol: rpc
    path: "GitHelper.Pull"
    description:
      zh: >
          从上游拉取 fast-forward 更新。
          
      en: >
          Pulls fast-forward changes from the upstream.
          
  - protocol: rpc
    path: "GitHelper.DiscardTrackedChanges"
    description:
      zh: >
          仅丢弃已跟踪文件的本地改动。
          
      en: >
          Discards tracked local changes only.
          
---
