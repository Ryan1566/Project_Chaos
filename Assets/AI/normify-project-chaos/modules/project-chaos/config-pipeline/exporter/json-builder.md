---
uid: c1a20304
id: project-chaos.config-pipeline.exporter.json-builder
parent: project-chaos.config-pipeline.exporter
name: {zh: "Json 文本拼装", en: "JSON Text Builder"}
description:
  zh: >
      Json 文本拼装，带一个关键脆弱点：重组行时是把整段紧凑文本按逗号 Split 再重排，因此某个 string 单元格里出现英文逗号会静默写坏整表 Json（日志却显示成功）；字符串内的双引号与换行同样不做转义。
      
  en: >
      JSON text assembly with an important fragility: rows are regrouped by splitting the whole compact text on commas, so a string cell containing a comma silently corrupts the whole table's JSON while the log still reports success. Quotes and newlines inside strings are not escaped either.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.727Z"
fingerprint: 8317ca6bb94bcacbc47c1bf98e79594b0f8559b3aa88a4c40c48d717703dcd5f
source:
  - path: "Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigManager.cs"
    line: 415
    end_line: 473
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigManager.cs#L415-L473"
    description:
      zh: >
          Json 文本拼装函数所在行段。
          
      en: >
          Line range of the JSON text assemblers.
          
  - protocol: rpc
    path: "ConfigManager.GetJsonK_VFromKeyAndValues"
    description:
      zh: >
          拼一条 key:value 片段。
          
      en: >
          Formats one key:value pair.
          
  - protocol: rpc
    path: "ConfigManager.GetJsonFromJsonK_V"
    description:
      zh: >
          把扁平键值重组为 datas 数组。
          
      en: >
          Reshapes flat pairs into a datas array.
          
  - protocol: rpc
    path: "ConfigManager.GetUnityJsonFromJson"
    description:
      zh: >
          包成 JsonUtility.FromJson 可读的外壳。
          
      en: >
          Wraps the array for JsonUtility.FromJson.
          
---
