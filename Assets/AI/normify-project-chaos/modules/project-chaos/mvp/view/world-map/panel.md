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
      
revision: 7230b3db925288f8a01b1f2255de1e42f792f0cd
updated_at: "2026-10-31T08:58:40.812Z"
fingerprint: d9e200b35faee8bd357db17115e34412c3be51c496d9116dedb04a8a2d792ee7
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
