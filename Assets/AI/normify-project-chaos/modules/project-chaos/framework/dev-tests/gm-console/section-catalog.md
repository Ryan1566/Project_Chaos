---
uid: 9f3c1a72
id: project-chaos.framework.dev-tests.gm-console.section-catalog
parent: project-chaos.framework.dev-tests.gm-console
tags: [dev-tool, ui]
name: {zh: "GM 功能清单与页面生成", en: "GM Catalog and Page Building"}
description:
  zh: >
      GM 面板的功能清单与控件工厂：分类→版块的静态表（界面 / 系统 / 道具），以及页面控件生成（动作按钮、提示文本、按钮外观）。加一个新测试功能只需要往这张表里加一条 GmSection，场景不用动；语言切换、打开设置/存档面板这类动作都定义在这里。
      
  en: >
      GM panel catalog and widget factory: the static category-to-section table (UI / System / Items) plus page builders (action buttons, hint text, button styling). Adding a new test feature is one GmSection entry; no scene edits. Panel-opening and language-switch actions are defined here.
      
revision: b0eba89e88d383bc0589b06eee323cac5a625fff
updated_at: "2026-10-11T16:04:35.707Z"
fingerprint: 07ceed9bdb9694deba7f828640afcba72d609509c58db0db64b7f17bdc5dc472
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
