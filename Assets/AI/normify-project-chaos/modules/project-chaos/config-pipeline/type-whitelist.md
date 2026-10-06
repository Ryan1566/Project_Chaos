---
uid: c1a20004
id: project-chaos.config-pipeline.type-whitelist
parent: project-chaos.config-pipeline
name: {zh: "类型白名单", en: "Type Whitelist"}
description:
  zh: >
      权威类型门禁，只由 Convert 的 switch 定义：int、uint、long、ulong、byte、sbyte、short、ushort、float、double、bool、string。类型必须同时是合法 C# 类型名（第 6 行文本原样拼进类）且能被 JsonUtility 正确往返。int32/int64/long long 已移除，现在导出期就失败；decimal 因 JsonUtility 静默丢弃而严禁；char 能往返但存成数字。bool 只认 true/false、1/0、是/否 并归一化为裸小写 true/false。
      
  en: >
      Authoritative type gate defined by Convert's switch alone: int, uint, long, ulong, byte, sbyte, short, ushort, float, double, bool, string. A type must be a legal C# name (row 6 is pasted verbatim into the class) and JsonUtility round-trippable. int32/int64/long long were removed and now fail at export; decimal is banned (silently dropped by JsonUtility); char survives but stores as a number. bool accepts only true/false, 1/0 and yes/no, normalised to bare lowercase.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.732Z"
fingerprint: 8317ca6bb94bcacbc47c1bf98e79594b0f8559b3aa88a4c40c48d717703dcd5f
source:
  - path: "Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigManager.cs"
    line: 324
    end_line: 414
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigManager.cs#L324-L414"
    description:
      zh: >
          GetType 与 Convert 白名单 switch 所在行段。
          
      en: >
          Line range holding GetType and the Convert whitelist switch.
          
  - protocol: rpc
    path: "ConfigManager.GetType"
    description:
      zh: >
          读第 6 行类型文本并 Trim。
          
      en: >
          Reads row 6 type text, trimmed.
          
  - protocol: rpc
    path: "ConfigManager.Convert"
    description:
      zh: >
          权威的 12 类型白名单与 bool 归一化。
          
      en: >
          The authoritative 12-type whitelist and bool normalisation.
          
---
