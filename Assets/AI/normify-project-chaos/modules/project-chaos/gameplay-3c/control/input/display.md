---
uid: f1c00023
id: project-chaos.gameplay-3c.control.input.display
parent: project-chaos.gameplay-3c.control.input
name: {zh: "绑定显示解析", en: "Binding Display"}
description:
  zh: >
      绑定显示解析：取槽位的文本/图标/颜色、有效控制路径、稳定查表键、图标家族与可读按键名。
      
  en: >
      Binding display: resolves text/icon/tint for a slot, effective control paths, stable lookup keys, icon family and human-readable names.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:43:40.747Z"
fingerprint: c11d341f13f442ad4c81a2e57d2833d76598d5112b05d252706445ca41dec428
source:
  - path: "Assets/Scripts/Runtime/3C/Control/InputManager/InputManager.cs"
    line: 295
    end_line: 572
apis:
  - protocol: rpc
    path: "InputManager.GetBindingVisual"
    description:
      zh: >
          取绑定的显示文本/图标/颜色（供设置行渲染）。
          
      en: >
          Gets text, sprite and tint for a binding display slot.
          
  - protocol: rpc
    path: "InputManager.GetBindingPath"
    description:
      zh: >
          取绑定的有效控制路径。
          
      en: >
          Gets the effective control path of a binding.
          
  - protocol: rpc
    path: "InputManager.ControlKey"
    description:
      zh: >
          把控制路径转为稳定的查表键。
          
      en: >
          Converts a control path into a stable lookup key.
          
  - protocol: rpc
    path: "InputManager.IconFamily"
    description:
      zh: >
          判定路径所属的图标家族（键盘/鼠标/PS/Xbox）。
          
      en: >
          Resolves which icon family a path belongs to.
          
  - protocol: rpc
    path: "InputManager.DescribePath"
    description:
      zh: >
          把控制路径转为可读的按键名。
          
      en: >
          Human-readable description of a control path.
          
deps:
  - kind: call
    to: project-chaos.gameplay-3c.control.input-icons.query
    from_api: "rpc:InputManager.GetBindingVisual"
    to_api: "rpc:KeyIconMap.GetIcon"
    label: {zh: "按键图标查询", en: "Key icon lookup"}
  - kind: reference
    to: project-chaos.framework.settings.enums
    from_api: "rpc:InputManager.DescribePath"
    to_api: "rpc:SettingsLabels.GamepadButtonName"
    label: {zh: "手柄按键名", en: "Gamepad key names"}
---
