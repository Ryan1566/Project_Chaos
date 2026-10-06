---
uid: e0a10103
id: project-chaos.editor-tools.localization.collector-import
parent: project-chaos.editor-tools.localization
name: {zh: "配置导入与选中态", en: "Config Import and Selection"}
description:
  zh: >
      把勾选条目的 Key/中文写入目标本地化配置资产（已存在则更新），刷新已存在/Key 冲突标记，并在重扫后按条目 ID 恢复选中；含一批 ForTest 钩子用于脚本驱动端到端验证。
      
  en: >
      Writes selected entries' keys and Chinese text into the target localization config asset (updating existing rows), refreshes the exists/collision flags, and restores selection by item id after rescanning; includes ForTest hooks for scripted end-to-end verification.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.735Z"
fingerprint: 2ec00da630a7367162e0191a090516c894c4f0b514fd97c19677514061967710
source:
  - path: "Assets/Scripts/Editor/LocalizationEditor/PrefabTextCollectorWindow.cs"
    line: 930
    end_line: 1312
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/LocalizationEditor/PrefabTextCollectorWindow.cs#L930-L1312"
    description:
      zh: >
          导入配置、状态刷新与选中恢复所在行段。
          
      en: >
          Line range holding config import, status refresh and selection restore.
          
  - protocol: rpc
    path: "PrefabTextCollectorWindow.ImportToConfig"
    description:
      zh: >
          把当前勾选条目导入目标本地化配置。
          
      en: >
          Imports the currently selected entries into the target config.
          
  - protocol: rpc
    path: "PrefabTextCollectorWindow.ImportToConfigInternal"
    description:
      zh: >
          导入实现：逐条写入或更新配置条目。
          
      en: >
          Import implementation that writes or updates each entry.
          
  - protocol: rpc
    path: "PrefabTextCollectorWindow.RefreshConfigStatus"
    description:
      zh: >
          刷新「已存在于配置」与 Key 冲突标记。
          
      en: >
          Refreshes the already-in-config and duplicate-key flags.
          
  - protocol: rpc
    path: "PrefabTextCollectorWindow.RunPipelineForTest"
    description:
      zh: >
          脚本驱动端到端管线（扫描→导入→回写）的测试钩子。
          
      en: >
          Test hook running the whole scan-import-writeback pipeline.
          
deps:
  - kind: dataflow
    to: project-chaos.editor-tools.localization.merger
    from_api: "rpc:PrefabTextCollectorWindow.ImportToConfig"
    to_api: "rpc:LocalizationMerger.Merge"
    label: {zh: "写子配置后并入整表", en: "Sub-configs feed the merger"}
---
