---
uid: 7d2b4f01
id: project-chaos.mvp.view
parent: project-chaos.mvp
name: {zh: "视图层", en: "View Layer"}
description:
  zh: >
      MVP/View 下的三个界面域：主菜单、设置（面板 + 5 个设置页）、存档读档（SLPanel + RecordCell 格子）。所有面板都继承框架的 BasePanel，由 UIManager 缓存并复用实例——所以 Awake 只跑一次、OnEnter 每次打开都跑。
  en: >
      The three UI domains under MVP/View: main menu, settings (panel plus five settings pages) and save/load (SLPanel plus the RecordCell slot widget). Every panel extends the framework BasePanel and is cached and reused by UIManager, so Awake runs once while OnEnter runs on every open.
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:10:00Z"
fingerprint: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
source: []
---
