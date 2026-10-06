---
uid: f1c00028
id: project-chaos.framework.input-icons.map-asset
parent: project-chaos.framework.input-icons
name: {zh: "按键图标映射表", en: "Key Icon Map Asset"}
description:
  zh: >
      KeyIconMap 资产数据：IconStyle 枚举、Entry 条目（控制键/设备家族/图标/来源）、页签图标与着色设置。
      
  en: >
      KeyIconMap asset data: IconStyle enum, Entry list (controlKey/family/icon), tab icons and tint settings.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.745Z"
fingerprint: dce2712ad67121c6bd9ec2cabd8da2255fe257ba39608aa239a2482f97fe189e
source:
  - path: "Assets/Scripts/Runtime/GameBase/InputManager/KeyIconMap.cs"
    line: 1
    end_line: 120
apis:
  - protocol: rpc
    path: "KeyIconMap.style"
    description:
      zh: >
          图标样式（实心/描边）。
          
      en: >
          Icon rendering style (filled/outline).
          
  - protocol: rpc
    path: "KeyIconMap.preferGlyphVariant"
    description:
      zh: >
          优先使用字形变体图标。
          
      en: >
          Prefer glyph variant icons when available.
          
  - protocol: rpc
    path: "KeyIconMap.tint"
    description:
      zh: >
          未使用原色图标时的着色。
          
      en: >
          Tint color applied to raw-color icons.
          
  - protocol: rpc
    path: "KeyIconMap.keyboardTabIcon"
    description:
      zh: >
          键鼠页签图标。
          
      en: >
          Keyboard/mouse tab icon.
          
deps:
  - kind: reference
    to: project-chaos.framework.paths.global-path
    from_api: "rpc:KeyIconMap.style"
    to_api: "rpc:GlobalPath.res_KeyIconMapPath"
    label: {zh: "图标表路径", en: "Icon map path"}
---
