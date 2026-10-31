---
uid: e0a10108
id: project-chaos.editor-tools.localization.merger
parent: project-chaos.editor-tools.localization
name: {zh: "本地化子配置合并器", en: "Localization Sub-config Merger"}
description:
  zh: >
      编辑期按模块拆在 Assets/Data/Localization/Sub_LD，运行期只读一张整表：本工具整份重建合并产物 ChaosLocalizationConfig.asset（先清空旧条目再按子配置全量重写，幂等、无孤儿条目）。窗口展示每个子配置的条目数与告警。
      
  en: >
      Sub-configs are split per module under Assets/Data/Localization/Sub_LD while runtime reads a single table: this tool rebuilds ChaosLocalizationConfig.asset from scratch (clears old entries, rewrites everything, idempotent, no orphans). The window lists per-file entry counts and warnings.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:22.612Z"
fingerprint: f24057e8920891fafee6f41679d6a0e21c05668784bc2bdf700f3124ae2c78c1
source:
  - path: "Assets/Scripts/Editor/LocalizationEditor/LocalizationMerger.cs"
    line: 1
    end_line: 564
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/LocalizationEditor/LocalizationMerger.cs#L1-L564"
    description:
      zh: >
          合并器窗口与合并实现所在文件。
          
      en: >
          The merger window and merge implementation.
          
  - protocol: rpc
    path: "Menu/Tools/本地化/合并子配置"
    description:
      zh: >
          打开子配置合并窗口的菜单项。
          
      en: >
          Menu item opening the sub-config merger window.
          
  - protocol: rpc
    path: "LocalizationMerger.Merge"
    description:
      zh: >
          把所有子配置合并成运行期整表（可 dryRun）。
          
      en: >
          Merges all sub-configs into the runtime table (dryRun supported).
          
  - protocol: rpc
    path: "LocalizationMerger.SilentMode"
    description:
      zh: >
          静默开关：脚本驱动时不弹模态框。
          
      en: >
          Silent switch: no modal dialogs under automation.
          
  - protocol: rpc
    path: "LocalizationMerger.OutputPath"
    description:
      zh: >
          合并产物路径常量 ChaosLocalizationConfig.asset。
          
      en: >
          Constant path of the merged output asset.
          
---
