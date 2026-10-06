---
uid: e0a10201
id: project-chaos.editor-tools.git.repo-core
parent: project-chaos.editor-tools.git
name: {zh: "Git 命令与仓库查询", en: "Git Command Core"}
description:
  zh: >
      Git 命令核心：定位仓库根与 git 可执行文件，带超时执行 git，并回答仓库级问题（是否仓库、当前分支、上游、建议上游、脏文件、进行中的操作、Unity 是否在运行、fetch）。
      
  en: >
      Git command core: locates the repository root and git executable, runs git with a timeout, and answers repo-level questions (is a repo, current branch, upstream, suggested upstream, dirty files, in-progress operation, is Unity running, fetch).
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.734Z"
fingerprint: 8a47f9f083458046c167adb12ae2541546c57eec2897256e7614d1e5318e5920
source:
  - path: "Assets/Scripts/Editor/GitTool/GitHelper.cs"
    line: 20
    end_line: 407
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/GitTool/GitHelper.cs#L20-L407"
    description:
      zh: >
          git 进程调用与仓库/分支查询所在行段。
          
      en: >
          Line range holding git process execution and repo/branch queries.
          
  - protocol: rpc
    path: "GitHelper.Run"
    description:
      zh: >
          带超时执行 git.exe 并捕获标准输出/错误。
          
      en: >
          Runs git.exe with a timeout and captures stdout/stderr.
          
  - protocol: rpc
    path: "GitHelper.IsRepository"
    description:
      zh: >
          判断工程目录是否为 git 工作区。
          
      en: >
          Checks whether the project folder is a git work tree.
          
  - protocol: rpc
    path: "GitHelper.GetCurrentBranch"
    description:
      zh: >
          返回当前分支名。
          
      en: >
          Returns the current branch name.
          
  - protocol: rpc
    path: "GitHelper.Fetch"
    description:
      zh: >
          从远端 fetch（不合并）。
          
      en: >
          Fetches from the remote without merging.
          
---
