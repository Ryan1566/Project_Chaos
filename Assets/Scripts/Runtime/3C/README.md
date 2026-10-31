# 3C：角色、控制与镜头

- `Character/`：受控角色运动、地面/侧墙物理、攻击与生命、角色表现、道具使用动作、掉落重生保护。
- `Control/`：输入动作与重绑定、局内界面快捷键、轮盘输入驱动。输入驱动读取意图，不承载库存或结算规则。
- `Camera/`：Cinemachine 跟随、构图、边界与战斗抖动；村庄和局内通过生命周期入口绑定目标。

共享战斗查询与伤害协议保留在 `Runtime/Combat/`；NPC 行为保留在 `Runtime/NPC_AI/`；整局生成、对象作用域、地图边界和结算不归入 3C。界面表现继续使用 `Runtime/MVP/`。

移动脚本必须保留对应 `.meta`，确保预制体和场景仍引用相同 GUID。新增 MonoBehaviour 应使用同名独立文件。调整镜头时由同一个 Cinemachine 输出链路控制主相机，避免多个跟随脚本同时写入主相机的位置和视野。

## 镜头配置与生命周期

- `Resources/3C/GameplayCameraSettings.asset`：局内/村庄尺寸、横纵阻尼、死区、转向前视与构图高度。默认局内尺寸6、村庄8；横向阻尼0.3秒、纵向0.65秒。
- `Resources/3C/CombatCameraShakeSettings.asset`：命中/受击/预留重击的幅度和时长、总幅度上限、最小触发间隔与强度倍率；倍率0可关闭抖动。重击接口预留，不代表已有重击玩法。
- `GameplayCameraRig.For(output)` 获取输出相机的唯一控制器；`BindRun` / `BindVillage` 切换模式，`Release(expectedTarget)` 仅释放仍由该目标持有的镜头。旧清理不能释放新绑定。
- `NotifyTargetWarped(target, delta)` 在重生/传送后重置锚点、阻尼和同帧状态缓存。普通移动不调用该接口。
- `PlayerCameraFollow` 和 `VillageCamera` 是兼容/生命周期门面，不再实现独立的相机位移算法。`CameraViewportBounds` 在Cinemachine最终阶段限制包括抖动后的视口；地图小于视口时稳定居中。

正式运行由Unity Camera负责渲染，CinemachineBrain负责镜头输出。配置调整不修改共享字体、材质或角色移动/攻击数值。算法放在3C层，界面层不控制相机跟随。
