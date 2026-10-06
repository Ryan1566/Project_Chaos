---
uid: 7d2b4f1d
id: project-chaos.mvp.view.save-load.record-cell
parent: project-chaos.mvp.view.save-load
tags: [mvp, ui, save]
name: {zh: "档位格子", en: "Record Cell"}
description:
  zh: >
      存档列表里的一个档位格子（RecordCell.prefab 的脚本）。Bind 与 SetData 刻意分开：SLPanel 只例化 3 个格子并长期复用，所以绑定必须只执行一次，而数据每次刷新都要重画。本类不碰文件系统——数据由 SLPanel 统一取一次再分发。
      
  en: >
      One save-slot cell (the RecordCell.prefab script). Bind and SetData are deliberately separate: SLPanel instantiates three cells once and reuses them forever, so binding must happen exactly once while data is repainted on every refresh. The cell never touches the file system — SLPanel fetches the data once and distributes it.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.765Z"
fingerprint: 7966e1f2e9ae4a8554954d790b56ff83a8053cddbf9aade70be7f6e78fadc40b
source:
  - path: "Assets/Scripts/Runtime/MVP/View/SLPanel_Record/RecordCell.cs"
    line: 29
    end_line: 209
apis:
  - protocol: rpc
    path: "RecordCell.Bind"
    description:
      zh: >
          只执行一次的绑定：档位号、点击回调与选中标记。
          
      en: >
          One-time bind of slot number and click callback.
          
  - protocol: rpc
    path: "RecordCell.SetData"
    description:
      zh: >
          把当前档位状态画到界面，每次刷新都可调。
          
      en: >
          Paints the slot state; safe to call every refresh.
          
  - protocol: rpc
    path: "RecordCell.SetSelected"
    description:
      zh: >
          切换选中高亮标记。
          
      en: >
          Toggles the selected highlight marker.
          
  - protocol: rpc
    path: "RecordCell.FormatLocalTime"
    description:
      zh: >
          把 UTC ticks 格式化成当地时间 yyyy/MM/dd HH:mm。
          
      en: >
          Formats UTC ticks as local yyyy/MM/dd HH:mm.
          
  - protocol: rpc
    path: "RecordCell.Slot"
    description:
      zh: >
          本格子代表的档位号。
          
      en: >
          The slot number this cell represents.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SLPanel_Record/RecordCell.cs#L29-L209"
    description:
      zh: >
          档位格子控件全文。
          
      en: >
          Whole RecordCell widget source.
          
---
