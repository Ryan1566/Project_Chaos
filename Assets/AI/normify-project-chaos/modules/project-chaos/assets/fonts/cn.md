---
uid: 3a71b48e
id: project-chaos.assets.fonts.cn
parent: project-chaos.assets.fonts
name: {zh: "中文字体", en: "Chinese Fonts"}
description:
  zh: >
      中文字体：MSYH SDF（主）与 XiangcuiDengcusong SDF，及各自 TTC/TTF 原始源文件。
      
  en: >
      Chinese fonts: MSYH SDF (primary) and XiangcuiDengcusong SDF, with their raw TTC/TTF sources.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:08:07.154Z"
fingerprint: fecf5c2148612d4f2f56a47784b9d4536c98132460839e673a7cb8c3200ab625
source:
  - path: "Assets/Fonts/cn/MSYH SDF.asset"
  - path: "Assets/Fonts/cn/MSYH.TTC"
  - path: "Assets/Fonts/cn/XiangcuiDengcusong SDF.asset"
  - path: "Assets/Fonts/cn/XiangcuiDengcusong.ttf"
apis:
  - protocol: file
    path: "Assets/Fonts/cn/MSYH SDF.asset"
    description:
      zh: >
          MSYH SDF：中文字体资产（主用）。
          
      en: >
          MSYH SDF TMP font asset (main Chinese font).
          
  - protocol: file
    path: "Assets/Fonts/cn/MSYH.TTC"
    description:
      zh: >
          MSYH.TTC 原始字体源。
          
      en: >
          Raw MSYH.TTC source font.
          
  - protocol: file
    path: "Assets/Fonts/cn/XiangcuiDengcusong SDF.asset"
    description:
      zh: >
          XiangcuiDengcusong SDF：备用中文字体资产。
          
      en: >
          XiangcuiDengcusong SDF TMP font asset (secondary).
          
  - protocol: file
    path: "Assets/Fonts/cn/XiangcuiDengcusong.ttf"
    description:
      zh: >
          XiangcuiDengcusong.ttf 原始字体源。
          
      en: >
          Raw XiangcuiDengcusong.ttf source font.
          
deps:
  - kind: reference
    to: project-chaos.assets.tmp.settings
    from_api: "file:Assets/Fonts/cn/MSYH SDF.asset"
    to_api: "file:Assets/TextMesh Pro/Resources/TMP Settings.asset"
    label: {zh: "受 TMP 设置管辖", en: "Governed by TMP settings"}
---
