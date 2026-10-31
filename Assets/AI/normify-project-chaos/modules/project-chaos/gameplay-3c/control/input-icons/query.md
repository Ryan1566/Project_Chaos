---
uid: f1c0002a
id: project-chaos.gameplay-3c.control.input-icons.query
parent: project-chaos.gameplay-3c.control.input-icons
name: {zh: "图标查询", en: "Icon Lookup"}
description:
  zh: >
      图标查询：按控制键 + 设备家族查条目，取图标与颜色，取键鼠/手柄页签图标。
      
  en: >
      Icon lookup: FindEntry by control key plus device family, GetIcon/GetIconColor, and GetTabIcon for keyboard/gamepad tabs.
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:26:25.548Z"
fingerprint: 2023bd6e048d39c1e0e4e46f86e528400a6368e0a22057657e7f1dfae14dab60
source:
  - path: "Assets/Scripts/Runtime/3C/Control/InputManager/KeyIconMap.cs"
    line: 164
    end_line: 215
apis:
  - protocol: rpc
    path: "KeyIconMap.FindEntry"
    description:
      zh: >
          按控制键与设备家族查找条目。
          
      en: >
          Finds an entry by control key and device family.
          
  - protocol: rpc
    path: "KeyIconMap.GetIcon"
    description:
      zh: >
          取控制键对应的图标。
          
      en: >
          Gets the sprite for a control key.
          
  - protocol: rpc
    path: "KeyIconMap.GetIconColor"
    description:
      zh: >
          取控制键图标的颜色。
          
      en: >
          Gets the tint color for a control key.
          
  - protocol: rpc
    path: "KeyIconMap.GetTabIcon"
    description:
      zh: >
          取键盘/手柄页签图标。
          
      en: >
          Gets the keyboard/gamepad tab icon.
          
deps:
  - kind: reference
    to: project-chaos.gameplay-3c.control.input-icons.map-asset
    from_api: "rpc:KeyIconMap.GetIcon"
    to_api: "rpc:KeyIconMap.style"
    label: {zh: "图标映射表", en: "Icon map asset"}
---
