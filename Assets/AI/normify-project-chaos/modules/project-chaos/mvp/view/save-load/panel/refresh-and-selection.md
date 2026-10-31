---
uid: 7d2b4f1a
id: project-chaos.mvp.view.save-load.panel.refresh-and-selection
parent: project-chaos.mvp.view.save-load.panel
tags: [mvp, ui, save]
name: {zh: "刷新与选中", en: "Refresh & Selection"}
description:
  zh: >
      刷新与选中：整列表只取一次存档信息（不是每格取一次），分发到 3 个格子，刷选中高亮，并在选中档位为空时置灰删除按钮。OnEnter 只走这一块。另含点击档位、删除与返回的处理。
      
  en: >
      Refresh and selection: reads the save-slot info once for the whole list (not once per cell), pushes it into each RecordCell, highlights the selected slot, and greys the delete button when the selected slot is empty. OnEnter lands here and nothing else. Also handles cell click, delete and return.
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:20:47.036Z"
fingerprint: 8764e7ed4dd67258938b541f97356744a3fff520f4a047557c4ef8eb1f7199cc
source:
  - path: "Assets/Scripts/Runtime/MVP/View/SLPanel.cs"
    line: 273
    end_line: 368
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L276-L320"
    description:
      zh: >
          统一取一次档位信息并分发给 3 个格子。
          
      en: >
          Pulls slot info once and paints all three cells.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L323-L368"
    description:
      zh: >
          点击档位、删除与返回的处理。
          
      en: >
          Cell click, delete and return handlers.
          
deps:
  - kind: call
    to: project-chaos.mvp.view.save-load.record-cell
    from_api: "file:Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L276-L320"
    to_api: "rpc:RecordCell.SetData"
    label: {zh: "把档位数据分发给格子", en: "Push slot data into a cell"}
  - kind: call
    to: project-chaos.mvp.view.save-load.panel.confirm-state-machine
    from_api: "file:Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L323-L368"
    to_api: "file:Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L371-L411"
    label: {zh: "删除/返回先弹二次确认", en: "Delete or return asks confirm"}
---
