---
uid: a3c00032
id: project-chaos.gameplay-3c.control.shortcuts
parent: project-chaos.gameplay-3c.control
tags: [input, shortcut, wheel]
name: {zh: "局内快捷键与道具轮盘", en: "Gameplay Shortcuts & Item Wheel"}
description:
  zh: >
      局内控制驱动：把菜单/背包/队伍/地图等按键翻译成 UI 请求的快捷驱动，以及开关道具轮盘的轮盘驱动。
      
  en: >
      In-run control drivers: the gameplay shortcut driver that turns menu/inventory/team/map keys into UI requests, and the item-wheel driver that opens and closes the radial item wheel.
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:29:20.165Z"
fingerprint: 48f0f9aab4bfea991a53df2465d6056c831984280f1969aacfa56b6ea957f08f
source:
  - path: "Assets/Scripts/Runtime/3C/Control/GameplayShortcutDriver.cs"
  - path: "Assets/Scripts/Runtime/3C/Control/RunItemWheelDriver.cs"
apis:
  - protocol: rpc
    path: "RunItemWheelDriver.Open"
    description:
      zh: >
          Open()：打开道具轮盘。
          
      en: >
          Open(): opens the item wheel.
          
  - protocol: rpc
    path: "RunItemWheelDriver.Close"
    description:
      zh: >
          Close()：关闭道具轮盘。
          
      en: >
          Close(): closes the item wheel.
          
  - protocol: rpc
    path: "RunItemWheelDriver.IsOpen"
    description:
      zh: >
          IsOpen：轮盘当前是否打开。
          
      en: >
          IsOpen: whether the wheel is currently open.
          
deps:
  - kind: reference
    to: project-chaos.gameplay-3c.control.input
    label: {zh: "读取快捷键输入", en: "Reads shortcut input"}
---
