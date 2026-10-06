---
uid: c1a20501
id: project-chaos.config-pipeline.runtime-load.config-loader
parent: project-chaos.config-pipeline.runtime-load
name: {zh: "配置加载器", en: "Config Loader"}
description:
  zh: >
      常量表的运行时入口：LoadData<T> 用 ResManager.Load<TextAsset> 拼 GlobalPath.data_JsonPathToRead + 类型名，再解析为 DataList<T>；LoadDataAsync 走异步加载并回调，取不到资源时打警告。DataList<T> 就是 JsonUtility 需要的 [Serializable] {datas:[...]} 外壳。
      
  en: >
      The runtime entry point for constant tables: LoadData<T> resolves ResManager.Load<TextAsset> on GlobalPath.data_JsonPathToRead plus the type name and parses it into DataList<T>; LoadDataAsync does the same through the async loader with a UnityAction callback and warns when the asset is missing. DataList<T> is the [Serializable] {datas:[...]} wrapper JsonUtility needs.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.730Z"
fingerprint: f92e08ea90db9b78145a652479389c3dc62fb1fd85fceff61eec0dfc92c130b0
source:
  - path: "Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigLoader.cs"
    line: 11
    end_line: 56
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigLoader.cs#L11-L56"
    description:
      zh: >
          同步/异步配置加载器及其泛型列表外壳。
          
      en: >
          The sync/async config loader and its generic list wrapper.
          
  - protocol: rpc
    path: "ConfigLoader.LoadData"
    description:
      zh: >
          同步加载并解析一张配置表。
          
      en: >
          Synchronously loads and parses one config table.
          
  - protocol: rpc
    path: "ConfigLoader.LoadDataAsync"
    description:
      zh: >
          异步加载一张表并回调。
          
      en: >
          Asynchronously loads a table and invokes a callback.
          
  - protocol: rpc
    path: "DataList"
    description:
      zh: >
          包裹 datas 列表的泛型外壳。
          
      en: >
          Generic wrapper holding the datas list.
          
deps:
  - kind: dataflow
    to: project-chaos.config-pipeline.generated-json.test-table
    from_api: "rpc:ConfigLoader.LoadData"
    to_api: "file:Assets/Resources/Data/Json/Runtime/TestTableConfig.json"
    label: {zh: "读取导出的 Json", en: "Reads the exported JSON"}
  - kind: reference
    to: project-chaos.config-pipeline.generated-models.test-table-config
    from_api: "rpc:ConfigLoader.LoadData"
    to_api: "file:Assets/Scripts/Runtime/MVP/Model/ConfigData/TestTableConfig.cs"
    label: {zh: "反序列化为数据类", en: "Deserialises into data class"}
---
