---
uid: 9e0d0002
id: project-chaos.framework.dont-destroy
parent: project-chaos.framework
tags: [framework, lifecycle]
name: {zh: "跨场景常驻组件", en: "DontDestroy Helper"}
description:
  zh: >
      幂等的 DontDestroyOnLoad 包装：[SerializeField] persistKey（默认取物体名）+ 静态存活登记表——同 key 已有活实例时，重复的那个自我销毁并记一条警告。它是“二次加载 EnterScene 不会出现第二套 UIRoot/Canvas/AudioListener”的保证。
      
  en: >
      Idempotent DontDestroyOnLoad wrapper: a [SerializeField] persistKey (defaulting to the GameObject name) plus a static live registry — if an instance with the same key is already alive, the duplicate destroys itself and logs a warning. This is what keeps a second EnterScene load from creating a second UIRoot/Canvas/AudioListener.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:56:54.869Z"
fingerprint: 1fc68ee83e829397dfdcf394248e52c7e63acd1f2379a7553e237291e12640f8
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
