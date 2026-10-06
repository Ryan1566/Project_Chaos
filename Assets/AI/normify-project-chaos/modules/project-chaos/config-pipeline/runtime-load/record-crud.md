---
uid: c1a20503
id: project-chaos.config-pipeline.runtime-load.record-crud
parent: project-chaos.config-pipeline.runtime-load
name: {zh: "存档增删改查", en: "Record CRUD"}
description:
  zh: >
      基于索引的存档增删改查：LoadData<T> 把缓存文本反序列化为 DataList<T>；SaveData 用 JsonUtility 序列化并美化后入缓存、可选落盘；Create/Update/Read/Delete 直接操作 datas 列表。异常会带原文重新抛出，因此缓存里缺少对应 key 会直接报错。
      
  en: >
      Index-based record CRUD over the cached .record payloads: LoadData<T> deserialises the cached text into DataList<T>, SaveData serialises with JsonUtility and pretty-prints it before storing and optionally writing, and Create/Update/Read/Delete operate on the datas list. Errors are rethrown with their original text, so a missing cache key surfaces as an exception.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.731Z"
fingerprint: a4f45a2229b4a5a8f6377de9fa5c8685696b8a5d9c964b0b8c110e062bc07823
source:
  - path: "Assets/Scripts/Runtime/GameBase/ConfigManager/SL/Recorder.cs"
    line: 93
    end_line: 231
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ConfigManager/SL/Recorder.cs#L93-L231"
    description:
      zh: >
          存档增删改查与 Json 序列化所在行段。
          
      en: >
          Line range holding record CRUD and JSON serialisation.
          
  - protocol: rpc
    path: "Recorder.LoadData"
    description:
      zh: >
          按类型名从缓存读一份数据列表。
          
      en: >
          Loads a cached record list by type name.
          
  - protocol: rpc
    path: "Recorder.SaveData"
    description:
      zh: >
          把列表序列化进缓存，可选立即写盘。
          
      en: >
          Serialises a list into the cache, optionally writing it out.
          
  - protocol: rpc
    path: "Recorder.CreateData"
    description:
      zh: >
          追加一条记录并保存。
          
      en: >
          Appends one record and saves.
          
  - protocol: rpc
    path: "Recorder.UpdateData"
    description:
      zh: >
          按索引替换一条记录并保存。
          
      en: >
          Replaces one record by index and saves.
          
  - protocol: rpc
    path: "Recorder.DeleteData"
    description:
      zh: >
          按值或索引删除记录并保存。
          
      en: >
          Removes a record by value or index and saves.
          
deps:
  - kind: call
    to: project-chaos.config-pipeline.runtime-load.record-format
    from_api: "rpc:Recorder.SaveData"
    to_api: "rpc:Recorder.FormatJson"
    label: {zh: "美化存档 Json", en: "Pretty-prints the record"}
  - kind: call
    to: project-chaos.config-pipeline.exporter.file-io
    from_api: "rpc:Recorder.SaveData"
    to_api: "rpc:FileUtil.SaveFile"
    label: {zh: "写存档文件", en: "Writes the record file"}
  - kind: dataflow
    to: project-chaos.config-pipeline.record-store
    from_api: "rpc:Recorder.SaveData"
    to_api: "file:Assets/Data/Records/TestTableData.record"
    label: {zh: "持久化 .record", en: "Persists the .record"}
---
