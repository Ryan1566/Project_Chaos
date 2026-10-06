---
uid: 7d2b4f0f
id: project-chaos.mvp.view.settings.pages.gameplay
parent: project-chaos.mvp.view.settings.pages
tags: [mvp, ui, settings]
name: {zh: "游戏性设置页", en: "Gameplay Page"}
description:
  zh: >
      游戏性页：目前只有语言切换一项，档位下标即 LanguageType 枚举值，直接存 SettingsData.language。档位文案来自 LocalizationManager.GetAllLanguages（只返回已启用语言），启用新语言不用改这里——但 prefab 是摆好的，需要重新生成。
      
  en: >
      Gameplay page: today it only holds the language selector, whose index doubles as the LanguageType enum value stored in SettingsData.language. Option texts come from LocalizationManager.GetAllLanguages so enabling a new language needs no change here — but the prefab is hand-built and must be regenerated.
      
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:03:05.767Z"
fingerprint: 0aab054c3618b0dc2abea1348ad74b1388b626fe9602777274795e50eebfc0e1
source:
  - path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/GameplaySettingsPage.cs"
    line: 18
    end_line: 49
apis:
  - protocol: rpc
    path: "GameplaySettingsPage.OnBind"
    description:
      zh: >
          绑定语言选择器。
          
      en: >
          Binds the language selector.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SettingsPage/GameplaySettingsPage.cs#L18-L49"
    description:
      zh: >
          语言行与它的档位文案生成（只列已启用语言）。
          
      en: >
          Language row and its display-name options.
          
deps:
  - kind: reference
    to: project-chaos.mvp.view.settings.page-base.lifecycle
    from_api: "rpc:GameplaySettingsPage.OnBind"
    to_api: "rpc:SettingsPageBase.OnBind"
    label: {zh: "继承设置页基类", en: "Extends settings page base"}
---
