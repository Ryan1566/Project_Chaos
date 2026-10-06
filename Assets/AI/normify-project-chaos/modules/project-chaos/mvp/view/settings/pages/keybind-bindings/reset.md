---
uid: 7d2b4f15
id: project-chaos.mvp.view.settings.pages.keybind-bindings.reset
parent: project-chaos.mvp.view.settings.pages.keybind-bindings
tags: [mvp, ui, keybind]
name: {zh: "恢复默认", en: "Reset Defaults"}
description:
  zh: >
      恢复默认的两条路径：单行（键盘/手柄/鼠标格，按段名处理）与整方案（清掉当前设备的全部覆盖，再把结果序列化回 Pending.inputOverridesJson）。先改实时资产，界面才能立刻反映出来。
      
  en: >
      Restore-defaults paths: per row (keyboard, gamepad or mouse slot, honouring the part name) and whole scheme (clear every override of the active device, then serialise the resulting overrides back into Pending.inputOverridesJson). Clearing happens on the live asset first so the UI reflects it immediately.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.768Z"
fingerprint: 03908ed9b1f158b11dcd01ea8368f47711a172a7e5cf15c0bd5f51e2eebf2f50
source:
  - path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/KeybindBindingsPage.cs"
    line: 518
    end_line: 580
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/KeybindBindingsPage.cs#L518-L546"
    description:
      zh: >
          单行恢复默认：调 InputManager.ResetBinding。
          
      en: >
          Resets one row via InputManager.ResetBinding.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/KeybindBindingsPage.cs#L547-L580"
    description:
      zh: >
          整方案恢复默认，并把当前覆盖同步回暂存区。
          
      en: >
          Resets a whole scheme and syncs overrides.
          
---
