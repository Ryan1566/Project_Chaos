---
uid: 5c1a7e0e
id: project-chaos.bootstrap.loading-screen.carousel
parent: project-chaos.bootstrap.loading-screen
tags: [bootstrap, loading, carousel]
name: {zh: "背景轮播图", en: "Background Carousel"}
description:
  zh: >
      读 LoadingTableConfig 过滤 enable 并按 sort 排序后，一次性 Resources.LoadAsync 全部轮播图；每张按自己的 showSeconds/fadeSeconds 停留与淡出，失败或缺失的图也计入进度分母（否则 assetsReady 永远达不到 1，进度条会卡死在 100% 之前）。
      
  en: >
      Reads LoadingTableConfig, filters on enable and sorts by sort, then loads every carousel sprite in one Resources.LoadAsync pass. Each entry holds and fades for its own showSeconds/fadeSeconds. Missing or failed sprites still count toward the progress denominator — otherwise assetsReady never reaches 1 and the bar deadlocks just short of 100%.
      
revision: 797aecab3ffa406505de00c7dd0baa0f15578bb7
updated_at: "2026-10-12T14:28:55.395Z"
fingerprint: 69a4a1bd4f73b2dedecbe57dd419556d295a3c3480ce4cb7287c141697c1b224
source:
  - path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs"
    line: 402
    end_line: 583
apis:
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs#L402-L473"
    description:
      zh: >
          LoadEntriesFromConfig：读表过滤排序并生成轮播条目。
          
      en: >
          LoadEntriesFromConfig: reads, filters and sorts entries.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs#L474-L485"
    description:
      zh: >
          StartSpriteRequests：一次性发起全部贴图异步请求。
          
      en: >
          StartSpriteRequests: fires all sprite requests at once.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs#L486-L520"
    description:
      zh: >
          ApplyCurrentSpriteImmediately：立即显示当前轮播图。
          
      en: >
          ApplyCurrentSpriteImmediately: shows current sprite.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs#L521-L567"
    description:
      zh: >
          AdvanceCarousel：按 unscaled 时间推进停留与淡出。
          
      en: >
          AdvanceCarousel: advances hold/fade on unscaled time.
          
  - protocol: file
    path: "Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs#L568-L583"
    description:
      zh: >
          CommitFade：提交一次淡出切换。
          
      en: >
          CommitFade: commits one fade transition.
          
---
