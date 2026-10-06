---
uid: f1c00016
id: project-chaos.framework.ui.legacy-tanglaushi
parent: project-chaos.framework.ui
name: {zh: "已废弃旧 UI 层", en: "Legacy TangLaoShi UI"}
description:
  zh: >
      TangLaoShi 命名空间旧 UI 层：[Obsolete] 的 BasePanel（按名字查控件 GetControl<T>）与完全空壳的旧 UIManager。属死代码，无引用。
      
  en: >
      Legacy TangLaoShi namespace UI layer: an [Obsolete] BasePanel with GetControl<T> and a completely empty UIManager shell. Dead code, kept for history.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.761Z"
fingerprint: d34f715f9cbf0f5c44d97dc5c9a950a0e386a2fb02c66c08d96ca66ba696332e
source:
  - path: "Assets/Scripts/Runtime/GameBase/UIManager/OtherUIManager/BasePanel.cs"
    line: 1
    end_line: 62
  - path: "Assets/Scripts/Runtime/GameBase/UIManager/OtherUIManager/UIManager.cs"
    line: 1
    end_line: 11
apis:
  - protocol: rpc
    path: "OtherUIManagerBasePanel.OnEnter"
    description:
      zh: >
          旧版 OnEnter 虚方法（已废弃）。
          
      en: >
          Legacy virtual enter hook (obsolete).
          
  - protocol: rpc
    path: "OtherUIManagerBasePanel.OnExit"
    description:
      zh: >
          旧版 OnExit 虚方法（已废弃）。
          
      en: >
          Legacy virtual exit hook (obsolete).
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/UIManager/OtherUIManager/BasePanel.cs"
    description:
      zh: >
          TangLaoShi 命名空间下已废弃的面板基类脚本。
          
      en: >
          Obsolete TangLaoShi base panel script asset.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/UIManager/OtherUIManager/UIManager.cs"
    description:
      zh: >
          TangLaoShi 命名空间下空壳旧 UIManager（无任何成员）。
          
      en: >
          Empty obsolete TangLaoShi UIManager shell.
          
---
