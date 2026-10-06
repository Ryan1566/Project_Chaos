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
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.765Z"
fingerprint: 476e33b9f31278b56b525f8ffb3a6299b5e9caa0d329cae08c07e8e1ad4cacec
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
