---
uid: e0a10501
id: project-chaos.editor-tools.dialogue-editor.window-shell
parent: project-chaos.editor-tools.dialogue-editor
name: {zh: "对话窗口与人物", en: "Dialogue Window and Characters"}
description:
  zh: >
      对话编辑器窗口外壳：资产新建/打开、编辑器样式、列出对话资产及其人物的左栏，以及人物列表编辑（ID、名称、名字颜色、头像、默认语音）。
      
  en: >
      Dialogue editor window shell: asset creation/opening, editor styles, the left panel that lists dialogue assets and their characters, and the character list editor (id, name, name colour, avatar, default voice).
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.733Z"
fingerprint: 314cafcafdf79d46ed2d2fa1c1ecbb76eb765924249343fc8410de9f53662ae7
source:
  - path: "Assets/Scripts/Editor/DialogueEditor/DialogueEditorWindow.cs"
    line: 12
    end_line: 288
apis:
  - protocol: file
    path: "Assets/Scripts/Editor/DialogueEditor/DialogueEditorWindow.cs#L12-L288"
    description:
      zh: >
          窗口外壳与人物列表面板所在行段。
          
      en: >
          Line range holding the window shell and the character list panel.
          
  - protocol: rpc
    path: "Menu/Tool/剧情对话编辑器"
    description:
      zh: >
          打开剧情对话编辑器的菜单项。
          
      en: >
          Menu item opening the dialogue editor window.
          
  - protocol: rpc
    path: "DialogueEditorWindow.ShowWindow"
    description:
      zh: >
          打开剧情对话编辑器窗口。
          
      en: >
          Opens the dialogue editor window.
          
  - protocol: rpc
    path: "DialogueEditorWindow.DrawLeftPanel"
    description:
      zh: >
          绘制左栏（资产与人物列表）。
          
      en: >
          Draws the left panel (asset list and characters).
          
  - protocol: rpc
    path: "DialogueEditorWindow.DrawCharacterList"
    description:
      zh: >
          绘制并编辑人物列表。
          
      en: >
          Draws and edits the character list.
          
deps:
  - kind: call
    to: project-chaos.editor-tools.dialogue-editor.entry-editor
    from_api: "rpc:DialogueEditorWindow.DrawLeftPanel"
    to_api: "rpc:DialogueEditorWindow.DrawDialogueEntryEditor"
    label: {zh: "左栏选中条目后编辑", en: "Opens the entry editor"}
---
