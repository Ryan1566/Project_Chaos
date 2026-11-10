---
uid: b2c00200
id: project-chaos.bootstrap.w5-gameplay.day-night
parent: project-chaos.bootstrap.w5-gameplay
tags: [gameplay, day-night, time]
name: {zh: "昼夜时钟", en: "Day/Night Clock"}
description:
  zh: >
      昼夜的唯一真相：村庄时间读写存档、局内分钟由扎营推进，对外提供昼/夜/晨/昏采样与本地化时钟文案，供 HUD 与天空使用。
      
  en: >
      Single source of day/night truth: keeps village time from the save and run minutes from camping, exposes Day/Night/Dusk/Dawn sampling and localised clock text for HUD and sky.
      
revision: d12129f7c898c717078ccbcc210fe5eaf82aa2f3
updated_at: "2026-11-10T14:52:00.884Z"
fingerprint: 96a975ce5fffd38f12c25a9bd6a675d2e272fec429765fd0a1ecaabc60db8dda
source:
  - path: "Assets/Scripts/Runtime/GameBase/Meta/DayNightClock.cs"
apis:
  - protocol: rpc
    path: "Meta.DayNightClock.BeginRun"
    description:
      zh: >
          开始局内计时
          
      en: >
          Start the run clock
          
  - protocol: rpc
    path: "Meta.DayNightClock.ClearRun"
    description:
      zh: >
          清理局内计时
          
      en: >
          Reset the run clock
          
  - protocol: rpc
    path: "Meta.DayNightClock.Evaluate"
    description:
      zh: >
          取某一时刻的昼夜采样
          
      en: >
          Sample the phase at a minute
          
  - protocol: rpc
    path: "Meta.DayNightClock.DescribeRunHour"
    description:
      zh: >
          小时/分钟文案
          
      en: >
          Localised hour/minute text
          
  - protocol: file
    path: "file:Assets/Scripts/Runtime/GameBase/Meta/DayNightClock.cs"
    description:
      zh: >
          昼夜时钟
          
      en: >
          Day/night clock
          
deps:
  - kind: reference
    to: project-chaos.bootstrap.w5-gameplay.village
    label: {zh: "村庄天气源", en: "Village weather source"}
  - kind: reference
    to: project-chaos.bootstrap.w5-gameplay.run-flow
    label: {zh: "局内时间回环", en: "Run time rewind"}
---
