---
uid: e0a10401
id: project-chaos.editor-tools.art-pipeline.icon-atlas
parent: project-chaos.editor-tools.art-pipeline
name: {zh: "图标图集构建与体检", en: "Icon Atlas Build and Inspect"}
description:
  zh: >
      构建与体检 IconAtlas_Input.spriteatlas：套用各平台打包设置，只把按键映射表引用到的图打进图集，回读打包后的页纹理，按平台格式估算显存，确保输出目录存在并输出人读报告。
      
  en: >
      Builds and inspects IconAtlas_Input.spriteatlas: applies platform packing settings, packs only sprites referenced by the key icon map, reads back packed page textures, estimates GPU bytes per platform format, ensures the output folder exists and formats human-readable reports.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.732Z"
fingerprint: 051e876fe544d55a0a113398b5fafbc0176041fd027378a7701f27b881c7eaad
source:
  - path: "Assets/Scripts/Editor/ArtPipeline/IconAtlasBuilder.cs"
    line: 49
    end_line: 397
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/ArtPipeline/IconAtlasBuilder.cs#L49-L397"
    description:
      zh: >
          图集构建、设置应用与显存估算所在文件。
          
      en: >
          The atlas builder, settings application and byte estimation.
          
  - protocol: rpc
    path: "Menu/Tools/图标/重建图标图集"
    description:
      zh: >
          重建图标图集的菜单项。
          
      en: >
          Menu item rebuilding the icon atlas.
          
  - protocol: rpc
    path: "IconAtlasBuilder.Rebuild"
    description:
      zh: >
          只打包映射表引用的图并重建图集。
          
      en: >
          Rebuilds the atlas packing only mapped icons.
          
  - protocol: rpc
    path: "IconAtlasBuilder.Inspect"
    description:
      zh: >
          体检已有图集并报告页数与显存。
          
      en: >
          Inspects an existing atlas and reports pages and memory.
          
  - protocol: rpc
    path: "IconAtlasBuilder.BuildReport"
    description:
      zh: >
          图集构建报告结构（数量、尺寸、估算）。
          
      en: >
          Atlas build report struct (counts, size, estimate).
          
deps:
  - kind: reference
    to: project-chaos.editor-tools.art-pipeline.icon-import-policy
    from_api: "rpc:IconAtlasBuilder.Rebuild"
    to_api: "rpc:IconImportPostprocessor.IconRoot"
    label: {zh: "图集口径取自图标目录", en: "Uses the icon root path"}
---
