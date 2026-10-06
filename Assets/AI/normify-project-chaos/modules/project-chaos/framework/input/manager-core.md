---
uid: f1c00020
id: project-chaos.framework.input.manager-core
parent: project-chaos.framework.input
name: {zh: "输入管理器核心", en: "InputManager Core"}
description:
  zh: >
      InputManager 核心：Player 映射与 Move/Attack/Jump 动作常量、Init 加载 InputActionAsset、设备族转换、改键中判定。
      
  en: >
      InputManager core: Player map constants, Init loading the InputActionAsset, device-family conversion and the IsRebinding flag.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.745Z"
fingerprint: efe37e8935ef64f4a05c1a643ffb319ec12c7e252ac2c96d7411d8937425f466
source:
  - path: "Assets/Scripts/Runtime/GameBase/InputManager/InputManager.cs"
    line: 1
    end_line: 200
apis:
  - protocol: rpc
    path: "InputManager.MapName"
    description:
      zh: >
          输入映射名常量 Player。
          
      en: >
          Input action map name constant ("Player").
          
  - protocol: rpc
    path: "InputManager.KindOf"
    description:
      zh: >
          把设备类型映射为按键组（键盘/鼠标/手柄）。
          
      en: >
          Maps an InputDeviceType to a binding group kind.
          
  - protocol: rpc
    path: "InputManager.Init"
    description:
      zh: >
          初始化管理器并加载 InputActions 资源。
          
      en: >
          Initializes the manager and loads the InputActionAsset.
          
  - protocol: rpc
    path: "InputManager.IsRebinding"
    description:
      zh: >
          当前是否处于改键监听中。
          
      en: >
          Whether a rebinding operation is running.
          
deps:
  - kind: call
    to: project-chaos.framework.resources.manager
    from_api: "rpc:InputManager.Init"
    to_api: "rpc:ResManager.Load"
    label: {zh: "加载输入资源", en: "Load input asset"}
  - kind: reference
    to: project-chaos.framework.paths.global-path
    from_api: "rpc:InputManager.Init"
    to_api: "rpc:GlobalPath.res_InputActionsPath"
    label: {zh: "输入资源路径", en: "Input asset path"}
---
