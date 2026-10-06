---
uid: f1c0004c
id: project-chaos.framework.logging.channels
parent: project-chaos.framework.logging
name: {zh: "日志频道", en: "Log Channels"}
description:
  zh: >
      LogChannel 频道枚举（通用/网络/UI/音频/配置/对话/本地化/输入/对象池/场景/存档/战斗/编辑器）与名称、颜色两张对照表。
      
  en: >
      LogChannel enum (General/Network/UI/Audio/Config/Dialogue/Localization/Input/Pool/Scene/Save/Battle/Editor) with name and color tables.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.750Z"
fingerprint: fa1163b8f3f94ce90ba40968fb08d66c86691d38b6cd63863ab09532fc7f54bf
source:
  - path: "Assets/Scripts/Runtime/GameBase/LogManager/LogChannel.cs"
    line: 1
    end_line: 116
apis:
  - protocol: rpc
    path: "LogChannelInfo.Count"
    description:
      zh: >
          已定义频道数量。
          
      en: >
          Number of defined channels.
          
  - protocol: rpc
    path: "LogChannelInfo.GetName"
    description:
      zh: >
          取频道中文短名。
          
      en: >
          Chinese short name of a channel.
          
  - protocol: rpc
    path: "LogChannelInfo.GetColor"
    description:
      zh: >
          取频道颜色（#RRGGBB）。
          
      en: >
          Hex color of a channel.
          
  - protocol: rpc
    path: "LogChannelInfo.GetTaggedName"
    description:
      zh: >
          取预拼好的带色 [频道] 富文本片段。
          
      en: >
          Pre-built colored [Channel] rich-text fragment.
          
---
