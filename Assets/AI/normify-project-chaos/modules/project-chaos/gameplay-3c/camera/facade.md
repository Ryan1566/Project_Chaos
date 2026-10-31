---
uid: a3c00014
id: project-chaos.gameplay-3c.camera.facade
parent: project-chaos.gameplay-3c.camera
tags: [camera, facade]
name: {zh: "镜头门面与设置", en: "Camera Facades & Settings"}
description:
  zh: >
      兼容与生命周期门面，加上镜头设置资产：PlayerCameraFollow 仍由 PlayerSpawner 实例化，但只负责取控制器并调 BindRun（smooth/offset 已是死字段）；VillageCamera 是纯门面，把调用转给控制器。
      
  en: >
      Compatibility and lifecycle facades plus the camera settings asset: PlayerCameraFollow is still instantiated by PlayerSpawner but only resolves the rig and calls BindRun (its smooth/offset fields are dead), and VillageCamera is a pure facade that forwards to the rig.
      
revision: 3e9431e3172f8f81c73452994a04db100947fac0
updated_at: "2026-10-31T16:29:20.161Z"
fingerprint: 93a7e5bd7ebaebdae97ba9168cf8884757b0977c0ad8d846348816336fea3795
source:
  - path: "Assets/Scripts/Runtime/3C/Camera/PlayerCameraFollow.cs"
  - path: "Assets/Scripts/Runtime/3C/Camera/VillageCamera.cs"
  - path: "Assets/Scripts/Runtime/3C/Camera/GameplayCameraSettings.cs"
apis:
  - protocol: rpc
    path: "PlayerCameraFollow.Attach"
    description:
      zh: >
          Attach(camera, target)：兼容租约，经控制器绑定局内相机。
          
      en: >
          Attach(camera, target): compatibility lease that binds the run camera via the rig.
          
  - protocol: rpc
    path: "PlayerCameraFollow.Detach"
    description:
      zh: >
          Detach()：释放兼容租约。
          
      en: >
          Detach(): releases the compatibility lease.
          
  - protocol: rpc
    path: "VillageCamera.Initialize"
    description:
      zh: >
          Initialize(target, config)：村庄侧门面，自身不实现位姿算法。
          
      en: >
          Initialize(target, config): village-side facade, no positioning algorithm of its own.
          
  - protocol: rpc
    path: "GameplayCameraSettings.Size"
    description:
      zh: >
          Size(village)：默认正交尺寸，村庄 8、局内 6。
          
      en: >
          Size(village): default ortho size, 8 for village and 6 for a run.
          
deps:
  - kind: call
    to: project-chaos.gameplay-3c.camera.rig
    from_api: "rpc:PlayerCameraFollow.Attach"
    to_api: "rpc:GameplayCameraRig.For"
    label: {zh: "委托给唯一控制器", en: "Delegates to the single rig"}
---
