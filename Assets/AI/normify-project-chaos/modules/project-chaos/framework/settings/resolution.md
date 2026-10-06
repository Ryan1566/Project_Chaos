---
uid: f1c00038
id: project-chaos.framework.settings.resolution
parent: project-chaos.framework.settings
name: {zh: "分辨率工具", en: "Resolution Helper"}
description:
  zh: >
      ResolutionHelper 分辨率工具：内置预设、显示器最大值、可用选项构建、下标查找/最接近匹配与最佳可用挑选。
      
  en: >
      ResolutionHelper: built-in presets, display max resolution, available-option building, index lookup/closest match and best-available picking.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.758Z"
fingerprint: 023436fd97e1c4ba95195e4be5985569e59e225bc5f957c0885a272c9974a9f8
source:
  - path: "Assets/Scripts/Runtime/GameBase/SettingsManager/ResolutionHelper.cs"
    line: 1
    end_line: 190
apis:
  - protocol: rpc
    path: "ResolutionHelper.PresetCount"
    description:
      zh: >
          内置分辨率预设数量。
          
      en: >
          Number of built-in resolution presets.
          
  - protocol: rpc
    path: "ResolutionHelper.GetPresetAt"
    description:
      zh: >
          按下标取预设。
          
      en: >
          Gets a preset by index.
          
  - protocol: rpc
    path: "ResolutionHelper.GetDisplayMax"
    description:
      zh: >
          取显示器最大分辨率。
          
      en: >
          Gets the display's max resolution.
          
  - protocol: rpc
    path: "ResolutionHelper.GetAvailableOptions"
    description:
      zh: >
          构建可用分辨率选项列表。
          
      en: >
          Builds the available resolution option list.
          
  - protocol: rpc
    path: "ResolutionHelper.GetBestAvailable"
    description:
      zh: >
          挑选当前可用的最佳分辨率。
          
      en: >
          Picks the best available resolution.
          
---
