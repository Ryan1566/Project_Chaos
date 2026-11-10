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
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-10T14:54:04.857Z"
fingerprint: 3661985bf935e7a7fae047e2850f497ff7811ed9c7286021a63ce4de57a6cfa0
source:
  - path: "Assets/Fonts/cn/MSYH SDF.asset.meta"
  - path: "Assets/Fonts/cn/MSYH.TTC.meta"
  - path: "Assets/Fonts/cn/XiangcuiDengcusong SDF.asset.meta"
  - path: "Assets/Fonts/cn/XiangcuiDengcusong.ttf.meta"
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
