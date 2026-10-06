---
uid: 7d2b4f16
id: project-chaos.mvp.view.save-load
parent: project-chaos.mvp.view
name: {zh: "存档读档", en: "Save & Load"}
description:
  zh: >
      存档读档界面：SLPanel 画固定 3 个档位并把玩家意图转成新建/读取/删除，RecordCell 是它只例化一次并长期复用的格子控件。两者都不碰文件系统——落盘统一走 SaveSlotService。
  en: >
      Save/load UI: SLPanel draws three fixed slots and turns player intent into create/load/delete, and RecordCell is the reusable slot widget it instantiates once and keeps. Neither touches the file system — all persistence goes through SaveSlotService.
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:10:00Z"
fingerprint: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
source: []
---
