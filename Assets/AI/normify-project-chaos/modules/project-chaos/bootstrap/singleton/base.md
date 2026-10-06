---
uid: 5c1a7e06
id: project-chaos.bootstrap.singleton.base
parent: project-chaos.bootstrap.singleton
tags: [bootstrap, singleton]
name: {zh: "普通单例基类", en: "Plain Singleton"}
description:
  zh: >
      不继承 MonoBehaviour 的单例基类：Instance 首次访问时 new T()，之后复用同一实例。MonoManager、ScenesLoadManager 都走这一套。
      
  en: >
      Singleton base for classes that do not extend MonoBehaviour: Instance lazily news up T on first access and reuses it afterwards. MonoManager and ScenesLoadManager both use it.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.726Z"
fingerprint: 96397cc082258e1aebdaf04ee1d3d1773a36b96959defa2eaa1838906ad9c71d
source:
  - path: "Assets/Scripts/Runtime/GameBase/Singleton/SingletonBase.cs"
    line: 6
    end_line: 20
apis:
  - protocol: rpc
    path: "SingletonBase.Instance"
    description:
      zh: >
          懒加载的全局唯一实例。
          
      en: >
          Lazily created global instance.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/Singleton/SingletonBase.cs#L6-L20"
    description:
      zh: >
          普通单例基类全文。
          
      en: >
          Whole plain singleton base source.
          
---
