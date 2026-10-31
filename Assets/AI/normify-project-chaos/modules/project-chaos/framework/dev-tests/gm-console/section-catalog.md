---
uid: 9f3c1a72
id: project-chaos.framework.dev-tests.gm-console.section-catalog
parent: project-chaos.framework.dev-tests.gm-console
tags: [dev-tool, ui]
name: {zh: "GM 功能清单与页面生成", en: "GM Catalog and Page Building"}
description:
  zh: >
      GM 面板的调试板块/动作清单。2026-10-12 新增“返回局外”项（SetTargetScene("EnterScene") + LoadingScene）——在读档不再切场景之后，它是局内返回局外层的最小入口。
      
  en: >
      GM panel catalogue of debug sections/actions. 2026-10-12 added a “return to meta” entry (SetTargetScene("EnterScene") + LoadingScene) — with the save panel no longer switching scenes, this GM item is the minimal in-run way back to the meta layer.
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:20:37.067Z"
fingerprint: ff6d43a6c38a0a5e5a7eb3d7a4ca8b2488e488c1481911b835bf5e81898295f8
source:
  - path: "Assets/Scripts/Runtime/GM/GMPanel.cs"
    line: 161
    end_line: 229
  - path: "Assets/Scripts/Runtime/GM/GMPanel.cs"
    line: 339
    end_line: 454
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GM/GMPanel.cs"
    description:
      zh: >
          功能清单与页面控件生成所在文件。
          
      en: >
          File holding the GM catalog and the page/control builders.
          
deps:
  - kind: call
    to: project-chaos.framework.ui.manager
    label: {zh: "打开设置 / 存档面板", en: "Opens settings/save panels"}
  - kind: call
    to: project-chaos.framework.localization.manager
    label: {zh: "切换运行时语言", en: "Switches the runtime language"}
---
