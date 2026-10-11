---
uid: 9e0d0002
id: project-chaos.framework.dont-destroy
parent: project-chaos.framework
tags: [framework, lifecycle]
name: {zh: "跨场景常驻组件", en: "DontDestroy Helper"}
description:
  zh: >
      跨场景常驻的通用组件（10 行）：Awake 调 DontDestroyOnLoad(this)。原为 GM/GMDontDestroy.cs，2026-10 抽到 Scripts/Runtime 根目录变成通用件（GM 控制台的常驻画布仍用它，见 dev-tests.gm-console.scene-entry）。
      
  en: >
      Generic cross-scene persistence component (10 lines): Awake calls DontDestroyOnLoad(this). Originally GM/GMDontDestroy.cs, it was pulled out to the Scripts/Runtime root in 2026-10 so any scene object can persist — the GM console canvas still relies on it.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.708Z"
fingerprint: aff7dda171b4df3d637495066f6d2e43628b563f0a5bb859c5194fd3b7b8d1b3
source:
  - path: "Assets/Scripts/Runtime/DontDestroy.cs"
    line: 1
    end_line: 10
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/DontDestroy.cs"
    description:
      zh: >
          常驻组件：Awake 里调 DontDestroyOnLoad(this)，无其它逻辑。
          
      en: >
          Persistent helper: Awake calls DontDestroyOnLoad(this); no other logic.
          
---
