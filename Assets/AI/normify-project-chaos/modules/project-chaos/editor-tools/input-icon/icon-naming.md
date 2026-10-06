---
uid: e0a10603
id: project-chaos.editor-tools.input-icon.icon-naming
parent: project-chaos.editor-tools.input-icon
name: {zh: "图标命名与匹配", en: "Icon Naming and Matching"}
description:
  zh: >
      图标命名规则：把设备族目录与文件名解析为设备族、token、风格与变体，通过带优先级的对照表把 token 解析成控制键，标记纯字形与未知 token，扫描全库，按风格偏好与变体惩罚选最优，并输出人读描述。
      
  en: >
      Icon naming rules: parses family folders and file names into device family, token, style and variant, resolves token to control keys through a ranked table, marks glyph-only and unknown tokens, scans the whole library, picks the best match honouring style preference and variant penalty, and describes the result.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.734Z"
fingerprint: 67619e6de8498e8d8fa4131d6c7b324c8f73b0f577705923e1a348fcc12051be
source:
  - path: "Assets/Scripts/Editor/InputIcon/IconNaming.cs"
    line: 49
    end_line: 717
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/InputIcon/IconNaming.cs#L49-L717"
    description:
      zh: >
          图标名解析、扫描与最优选择逻辑所在文件。
          
      en: >
          The icon name parser, scanner and best-pick logic.
          
  - protocol: rpc
    path: "IconNaming.TryParse"
    description:
      zh: >
          从文件名解析设备族、token、风格与变体。
          
      en: >
          Parses device family, token, style and variant from a file name.
          
  - protocol: rpc
    path: "IconNaming.Scan"
    description:
      zh: >
          扫描图标库得到 IconInfo 列表。
          
      en: >
          Scans the icon library into IconInfo records.
          
  - protocol: rpc
    path: "IconNaming.PickBest"
    description:
      zh: >
          为控制键与设备族选最优图标。
          
      en: >
          Picks the best icon for a control key and device family.
          
  - protocol: rpc
    path: "IconNaming.Describe"
    description:
      zh: >
          报告解析结果与未识别文件。
          
      en: >
          Reports parsing results and unrecognised files.
          
  - protocol: rpc
    path: "IconNaming.TabFileName"
    description:
      zh: >
          把设备族映射到页签图标文件名。
          
      en: >
          Maps a device family to its tab icon file name.
          
---
