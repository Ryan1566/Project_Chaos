---
uid: 5c1a7e0b
id: project-chaos.bootstrap.loading-screen
parent: project-chaos.bootstrap
name: {zh: "读条界面", en: "Loading Screen"}
description:
  zh: >
      LoadingScene 上的 LoadingController：一次性预热轮播图、开始加载目标场景、把 0.9 进度归一化到进度条，等「进度满 + 轮播图就绪 + 至少渲染 1 帧」后激活场景。全程用 unscaled 时间，Time.timeScale=0 也不会卡死。
  en: >
      LoadingController on LoadingScene: preloads carousel sprites in one pass, starts loading the target scene, normalises the 0.9 progress into the bar, and activates the scene only after progress is full, carousel assets are ready and at least one frame has rendered. Everything uses unscaled time so a frozen timeScale cannot deadlock it.
revision: 75df668e8bb1abec0b4352190c3082c585e2deef
updated_at: "2026-10-06T12:10:00Z"
fingerprint: e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855
source: []
---
