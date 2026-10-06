---
uid: 7d2b4f17
id: project-chaos.mvp.view.save-load.panel
parent: project-chaos.mvp.view.save-load
name: {zh: "存档面板", en: "Save Panel"}
description:
  zh: >
      SLPanel（509 行）拆为五块：生命周期、节点查找与格子工厂、刷新与选中、共用的二次确认状态机、三个业务动作。因为 UIManager 会缓存并复用面板实例，绑定一律放 Awake 且幂等，OnEnter 只刷新。
  en: >
      SLPanel (509 lines) split into five: lifecycle, node lookup plus cell factory, refresh/selection, the shared confirm state machine, and the three business actions. Binding stays in Awake and is idempotent because UIManager caches and reuses the panel instance.
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:10:00Z"
fingerprint: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
source: []
---
