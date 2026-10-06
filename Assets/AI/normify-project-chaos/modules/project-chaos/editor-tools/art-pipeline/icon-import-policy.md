---
uid: e0a10402
id: project-chaos.editor-tools.art-pipeline.icon-import-policy
parent: project-chaos.editor-tools.art-pipeline
name: {zh: "图标导入策略", en: "Icon Import Policy"}
description:
  zh: >
      按键图标的导入期口径：仅在预处理阶段接管 Assets/Art/icon 下（前缀匹配而非子串匹配）的贴图，强制 sprite 类型、关 mipmap、开 alphaIsTransparency、不做 NPOT 缩放、双线性过滤并限制最大尺寸；菜单项对存量图库重施同一口径。
      
  en: >
      Import-time policy for key icons: on preprocess, only paths under Assets/Art/icon (prefix match, not substring) are forced to sprite type with mipmaps off, alphaIsTransparency on, no NPOT scaling, bilinear filtering and a sane max size. A menu action re-applies the same policy to the existing library.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.732Z"
fingerprint: f5e6e581490536ff4b1338ecf5e41e78951285fd5cf3b2e09f1613be1ee836a1
source:
  - path: "Assets/Scripts/Editor/ArtPipeline/IconImportPostprocessor.cs"
    line: 34
    end_line: 187
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/ArtPipeline/IconImportPostprocessor.cs#L34-L187"
    description:
      zh: >
          导入策略后处理器与其强制重扫菜单所在文件。
          
      en: >
          The import policy postprocessor and its force-rescan menu action.
          
  - protocol: rpc
    path: "Menu/Tools/图标/应用图标导入设置"
    description:
      zh: >
          对全部图标重施导入设置的菜单项。
          
      en: >
          Menu item re-applying icon import settings to all icons.
          
  - protocol: rpc
    path: "IconImportPostprocessor.IconRoot"
    description:
      zh: >
          图标根目录常量 Assets/Art/icon。
          
      en: >
          Constant icon root folder Assets/Art/icon.
          
  - protocol: rpc
    path: "IconImportPostprocessor.ApplyPolicy"
    description:
      zh: >
          把图标导入口径写进单个 importer。
          
      en: >
          Writes the icon import policy into one importer.
          
  - protocol: rpc
    path: "IconImportPostprocessor.ApplyToAll"
    description:
      zh: >
          对存量图标重新套用导入口径。
          
      en: >
          Re-applies the policy to every existing icon.
          
---
