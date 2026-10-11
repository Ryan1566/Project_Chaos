---
uid: 7f0a6c95
id: project-chaos.project-infra.plugins-dlls
parent: project-chaos.project-infra
name: {zh: "第三方 DLL", en: "Third-Party DLLs"}
description:
  zh: >
      Assets/Plugins 下的两个独立第三方 DLL：EPPlus 供编辑器侧 Excel 导出，LitJson 供运行时 JSON 解析。
      
  en: >
      Two loose third-party DLLs under Assets/Plugins: EPPlus for Excel export (editor side) and LitJson for runtime JSON parsing.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.732Z"
fingerprint: efd85ee5837e9ef4df49472caa584703b65c1950d0edc5c6b17ab7eaf91148e3
source:
  - path: "Assets/Plugins/EPPlus.dll"
  - path: "Assets/Plugins/LitJson.dll"
apis:
  - protocol: file
    path: "Assets/Plugins/EPPlus.dll"
    description:
      zh: >
          EPPlus 1.3MB：Excel（.xlsx）读写库，供编辑器配置表导出使用。
          
      en: >
          EPPlus 1.3 MB: Excel (.xlsx) read/write library, used by the editor config-table export.
          
  - protocol: file
    path: "Assets/Plugins/LitJson.dll"
    description:
      zh: >
          LitJson 50KB：轻量 JSON 解析/生成库，供运行时读取配置。
          
      en: >
          LitJson 50 KB: lightweight JSON parser/generator for runtime config loading.
          
---
