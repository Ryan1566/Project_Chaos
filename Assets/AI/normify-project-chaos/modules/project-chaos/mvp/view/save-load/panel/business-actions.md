---
uid: 7d2b4f1c
id: project-chaos.mvp.view.save-load.panel.business-actions
parent: project-chaos.mvp.view.save-load.panel
tags: [mvp, ui, save]
name: {zh: "三个业务动作", en: "Business Actions"}
description:
  zh: >
      三个业务动作：空档位新建、读取档位、删除选中档位。自 2026-10-12 起，读档/新档改为 GameSession.Begin(slot, payload, null) + UIManager.SetRootPanel(WorldMapPanel)，不再切 LoadingScene/WorldScene——读档进的是局外，不再生成局内地图。
      
  en: >
      The three save actions: create a new save in an empty slot, load a slot, delete the selected slot. Since 2026-10-12 loading or creating a save writes GameSession.Begin(slot, payload, null) and then UIManager.SetRootPanel(WorldMapPanel) — it no longer switches to LoadingScene/WorldScene, so reading a save enters the meta layer instead of generating the run map.
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:54:47.490Z"
fingerprint: cac4aab04c917cfe98e29bcdc0566e6d3adaeb58e9b8923482f66acdb1acd514
source:
  - path: "Assets/Scripts/Runtime/MVP/View/SLPanel.cs"
    line: 451
    end_line: 509
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L454-L473"
    description:
      zh: >
          空档位新建存档。
          
      en: >
          Creates a new save in an empty slot.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L474-L495"
    description:
      zh: >
          读取档位并切到读条场景进入游戏。
          
      en: >
          Loads a slot and enters the loading scene.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L496-L509"
    description:
      zh: >
          删除选中档位的存档。
          
      en: >
          Deletes the selected slot's save.
          
deps:
  - kind: call
    to: project-chaos.bootstrap.scene-loading.manager
    from_api: "file:Assets/Scripts/Runtime/MVP/View/SLPanel.cs#L474-L495"
    to_api: "rpc:ScenesLoadManager.LoadScene"
    label: {zh: "切到读条场景", en: "Switch to loading scene"}
---
