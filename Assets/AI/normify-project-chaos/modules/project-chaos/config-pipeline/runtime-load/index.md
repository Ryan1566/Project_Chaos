---
uid: c1a20005
id: project-chaos.config-pipeline.runtime-load
parent: project-chaos.config-pipeline
name: {zh: "运行时配置读取", en: "Runtime Config Loading"}
description:
  zh: >
      运行时读取链：ConfigLoader 通过 ResManager 加载 Json 并用 JsonUtility 反序列化为 DataList<T>；Recorder 负责 .record 存档（构造时全量读入缓存、ForceSave 统一写盘、按索引增删改查）。已知缺陷：GlobalPath.data_JsonPathToRead 是 Json/Runtime/，而产物实际在 Resources/Data/Json/Runtime 下，Resources.Load 解析不到。
  en: >
      Runtime reading chain: ConfigLoader loads JSON through ResManager and deserialises it into DataList<T> with JsonUtility; Recorder owns the .record saves (loads everything into a cache on construction, ForceSave flushes, plus index-based CRUD). Known defect: GlobalPath.data_JsonPathToRead is Json/Runtime/ while the artifacts live under Resources/Data/Json/Runtime, so Resources.Load cannot resolve them.
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:10:00Z"
fingerprint: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
source: []
---
