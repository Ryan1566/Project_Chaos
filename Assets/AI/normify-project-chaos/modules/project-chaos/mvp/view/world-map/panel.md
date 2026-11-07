---
uid: aa910014
id: project-chaos.mvp.view.world-map.panel
parent: project-chaos.mvp.view.world-map
name: {zh: "世界观地图面板", en: "World Map Panel"}
description:
  zh: >
      作为局外主界面的 BasePanel：从 GameSession 取当前时期，向 WorldMapService 要点位与势力区域，生成点位图标（运行时生成的圆点，可按 Kind 覆盖）与势力蒙版，点击点位显示 Kind/Type/RefId，底部按钮负责返回主菜单、进入边缘空间与调试切时期。
      
  en: >
      BasePanel shown as the meta main screen: reads the current era from GameSession, asks WorldMapService for points and faction areas, instantiates point icons (runtime-generated circles, overridable per Kind) and the faction mask, shows Kind/Type/RefId on click, and owns the bottom buttons (back to menu, enter the run, debug era switch).
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-07T13:53:11.989Z"
fingerprint: 0632989a7f4ef9ef540a0bf431c0bc2eb8d1de125ca075a6ec0a257cb55936e5
source:
  - path: "Assets/Scripts/Runtime/MVP/View/WorldMapPanel.cs"
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/MVP/View/WorldMapPanel.cs"
    description:
      zh: >
          渲染当前时期底图、势力蒙版与点位的面板。
          
      en: >
          Panel that renders the base map, faction mask and icons for the current era.
          
---
