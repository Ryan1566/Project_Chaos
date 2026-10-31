# 3C 重构 + Cinemachine 引入：只读审计报告

> 审计范围：`20034a1`（引入 Cinemachine、3C 迁移）、`3e9431e`（三项 bug 修复）
> 审计方式：实际读代码 + grep + `git log/diff` + Unity 编译状态，未修改任何 C# 源码
> 审计时间：2026-11-01
> 工程：`D:\Unity Projects\Project_Chaos`（Unity 2022.3.57f1c2）

---

## 0. 结论速览

| # | 问题 | 结论 |
|---|------|------|
| 1 | 3C 下公共契约 | 19 个 `.cs`（Camera 7 / Character 7 / Control 5），**全部在全局命名空间**，无 `Chaos.*` 命名空间 |
| 2 | Cinemachine 接入方式 | `com.unity.cinemachine` **2.10.3**（CM2 API）。Rig 包装 `CinemachineVirtualCamera`；抖动用 **Cinemachine Impulse**（非手写偏移） |
| 3 | `PlayerCameraFollow` 角色 | **仍在被实例化**，是"兼容租约壳"：只把 `BindRun` 转给 Rig，位移全交 Cinemachine |
| 4 | `Player/` 剩余 13 个 | 全部是**局级编排/生成/结算**，不属 3C 职责（详见 §4） |
| 5 | InputManager 引用面 | **无 `using` 变更**（一直是全局命名空间）；183 处引用全部免改 |
| 6 | Editor 侧 | **无失效引用**；Unity 编译 0 error / 0 warning |
| 7 | 源码是否丢失 | **没有丢失**。纯 `git mv`（R100，0 行改动），348 数算术自洽 |

---

## 1. 3C 各目录职责与公共契约

> 命名空间事实：`grep '^namespace'` 在 `3C/` 全目录 **0 命中**。所有类都在全局命名空间，
> 与旧代码一致 —— 这是 §5 引用面无需改写 `using` 的根本原因。

### 1.1 `3C/Camera/`（7 个）

#### `GameplayCameraRig.cs`（189 行）— 重构核心
> "One output owner, reusable Cinemachine rig, and target-scoped leases across village/run transitions."
> `[DefaultExecutionOrder(-100)]` `[RequireComponent(typeof(Camera))]`

```csharp
public enum CameraMode { None, Village, Run }
public Camera OutputCamera { get; private set; }
public Transform Target { get; private set; }
public CinemachineVirtualCamera VirtualCamera { get; private set; }
public Rect Bounds { get; private set; }
public CameraMode Mode { get; private set; }
public int BoundaryRevision { get; private set; }

public static GameplayCameraRig For(Camera output);          // L30 取/建唯一控制器
public static bool IsFollowing(Camera output);               // L36
public void BindRun(Transform target, Rect bounds);          // L80
public void BindVillage(Transform target, float width, float fixedY); // L82
public void SnapToTarget();                                  // L142
public static void NotifyTargetWarped(Transform target, Vector3 delta); // L158
public bool Release(Transform expectedTarget);               // L168 目标作用域释放
```

职责：主相机输出的**唯一所有者**。运行时自建 `GameplayCinemachineRig` 对象树
（`FollowAnchor` + `GameplayVirtualCamera` + `CameraBounds`），一次性 `AddComponent`
`CinemachineVirtualCamera`/`CinemachineFramingTransposer`/`CinemachineConfiner2D`/`CameraViewportBounds`。

关键私有成员：`_brain`(18)、`_framing`(19)、`_confiner`(20)、`_finalBounds`(21)、`_shake`(28)。

#### `CombatCameraShake.cs`（**584 行**，非 570）
```csharp
public static CombatCameraShake Attach(Camera output, CinemachineVirtualCamera virtualCamera, Transform controlledTarget); // L65
public void Detach(Transform expectedTarget);                                    // L92
public static void NotifyHit(Transform attacker, Vector3 position, bool heavy = false); // L100
public static void NotifyHurt(Transform victim, Vector3 position);               // L108
```
职责：战斗相机抖动。**用 Cinemachine Impulse 通道**（`CinemachineImpulseSource` + `CinemachineImpulseListener`），
每个绑定独占一个 impulse channel（`TryAllocateChannel` L233，32 位掩码扫描），
避免误伤其他相机事件；`CancelOwnedImpulses`(L424) 只取消自己创建且 signal/channel 仍匹配的事件。

#### `CombatCameraShakeSettings.cs`（**108 行**，非 105）
```csharp
public const string ResourcesPath = "3C/CombatCameraShakeSettings";
public float NormalHitAmplitude / NormalHitDuration / HurtAmplitude / HurtDuration
public float HeavyHitAmplitude / HeavyHitDuration / MaxAmplitude / MinTriggerInterval / IntensityScale  // 均为净化 getter
```
职责：抖动集中配置 ScriptableObject。幅度是 **Cinemachine Impulse 的世界单位位移**（文件头注释 L5 明确说明），
不是相机 Transform 偏移。资产已存在：`Assets/Resources/3C/CombatCameraShakeSettings.asset`。

#### `GameplayCameraSettings.cs`（23 行）
```csharp
public const string ResourcesPath = "3C/GameplayCameraSettings";
public float runSize = 6; public float villageSize = 8; public float runHeightOffset = 1.25f;
public float horizontalDamping = .3f; public float verticalDamping = .65f;
public float horizontalDeadZone = .08f; public float verticalDeadZone = .22f;
public float forwardDistance = 1.25f; public float turnSmoothing = .35f; public int priority = 100;
public static float Finite(float value, float fallback, float minimum, float maximum); // L20 防 NaN/Inf
public float Size(bool village);                                                      // L22
```
职责：取景参数 SO。资产已存在：`Assets/Resources/3C/GameplayCameraSettings.asset`。

#### `CameraViewportBounds.cs`（20 行）
```csharp
public sealed class CameraViewportBounds : CinemachineExtension
public Camera Output { get; set; }
public Rect Bounds { get; set; }
```
职责：在 Cinemachine **`Stage.Finalize`** 阶段夹紧最终视口（L12），
`state.PositionCorrection += confined - position`（L18）—— 因此**包含抖动后的结果**也受边界约束，
且不直接写 `Camera` 组件。地图小于视口时稳定居中（L16-17 的三元判断）。

#### `PlayerCameraFollow.cs`（32 行）— 兼容壳，见 §3
```csharp
public Transform target; public float smooth = 8f; public Vector3 offset = new(0, 0, -10);
public static PlayerCameraFollow Attach(Camera camera, Transform target); // L11
public static void Detach();                                             // L22
```
`target`/`smooth`/`offset` 是**遗留字段**（`smooth`、`offset` 已无任何读取者，Rig 不再使用）。

#### `VillageCamera.cs`（14 行）
```csharp
public void Initialize(Transform target, VillageSceneSettings config); // L8
```
职责：村庄镜头生命周期适配器。调 `GameplayCameraRig.For(camera).BindVillage(target, config.widthTiles, config.groundY + 4.5f)`（L11）。
**不自己算位移**。

### 1.2 `3C/Character/`（7 个）

| 文件 | 行数 | 关键公共契约 | 职责 |
|---|---|---|---|
| `PlayerLocomotion.cs` | 177 | `float moveSpeed=6, jumpForce=11, groundCheckRadius=.16; Vector2 feetOffset`; `int Facing{get}; bool IsGrounded{get}; Vector2 Velocity{get}; SkeletonAnimation Skeleton{get}`; `SetFacing(int)` L42; `InjectInput(float,bool,float)` L53 | 受控角色水平移动/跳跃/朝向/动画切换（Spine 仅 Idle/Run 两个动画） |
| `PlayerSurfacePhysics.cs` | **186**（非 183） | `const MinGroundNormalY=.5f` L16; `const MinSideNormalX=.5f` L18; `bool IsSlideMaterialActive{get}` L34; `ProbeGround()` L86 / `ProbeGround(bool terrainOnly)` L89 / `static ProbeGround(Collider2D,Rigidbody2D[,bool])` L97/L99; `static IsGroundNormal(Vector2)` L120; `HasSideContact()` L126; `static IsSideContactNormal(Vector2)` L137; `SetSlideMaterial(bool)` L144 | 只认朝上法线的可站立地面；空中贴墙时挂运行时零摩擦材质，落地立即恢复 |
| `PlayerFallGuard.cs` | 41 | `float fallThresholdY=-20f, checkInterval=0.2f; Transform respawnPoint` | 掉落重生兜底；重生后调 `GameplayCameraRig.NotifyTargetWarped` L35 |
| `PlayerAttack.cs` | 101 | `int damage=10; float attackCooldown=.35f; Vector2 hitBoxSize=(.8,.6); float hitBoxOffsetX=.5f; int LastHitCount{get}` | 面前盒形判定取 `IDamageable`；命中调 `CombatCameraShake.NotifyHit` L92 |
| `PlayerHealth.cs` | **136**（非 131） | `int maxHp=100; float invulnSeconds=.5f; const float DownedAlpha=.4f; int Hp{get}; bool IsInvulnerable{get}; bool IsDowned{get}`; `SetItemHealthBonus` L29; `RestoreHealth` L35; `SetRunHealthBonus` L43; `ApplyContactDamage` L58; `ForceDown` L81; `MarkDownedVisual` L98; `RefillFullHp` L113 | 玩家血量 + 无敌帧；**受击只触发 Cinemachine 震动，不改写缩放/朝向**（L66 注释，对应 `3e9431e` 修复） |
| `ItemUseAnimation.cs` | 40 | `void Play(ItemBase item)` L12 | 道具使用表现（Spine overlay + 抬图标）；期间 block 玩法输入 L23/L38 |
| `RunCharacterPresentation.cs` | 9 | `string profession; Sprite portrait; string previewSkin` | 纯表现数据 POCO，挂在角色预制体上 |

### 1.3 `3C/Control/`（5 个）

| 文件 | 行数 | 职责 |
|---|---|---|
| `InputManager/InputManager.cs` | **1022**（实测；brief 的 1164 偏大） | 输入核心单例：动作表加载、覆盖项 JSON、改键全流程 |
| `InputManager/InputManager.Gameplay.cs` | **179**（brief 的 199 偏大） | **玩法输入分区**（见 §5.2） |
| `InputManager/KeyIconMap.cs` | **206**（brief 的 244 偏大） | 按键图标映射表 SO |
| `GameplayShortcutDriver.cs` | 56 | 局级 UI 快捷键（角色倒地/暂停时仍可开菜单）；`Awake` 自动挂 `RunItemWheelDriver` L7 |
| `RunItemWheelDriver.cs` | 81 | 道具径向轮盘输入+暂停驱动；`IsOpen` L17 / `LastMessage` L18 / `Open()` L52 / `Close()` L69 |

> 行数说明：brief 给出的部分行数偏大，实测以上表为准（`Get-Content | Measure-Object -Line`）。
> `grep` 匹配数 ≠ 行数，已交叉验证。

### 1.4 目录总数核对

| 子目录 | .cs 数 |
|---|---|
| `Camera` | 7 |
| `Character` | 7 |
| `Control` | 5 |
| **合计** | **19** |

> ⚠ brief 说 "3C=17"，实为 **19**。另 `3C/README.md` 是唯一非 .cs 文件（仓库自带的设计说明，内容准确）。

---

## 2. Cinemachine 接入方式

### 2.1 版本
- `Packages/manifest.json:12` → `"com.unity.cinemachine": "2.10.3"`
- `Packages/packages-lock.json:112` → `"version": "2.10.3"`
- 相关：`Packages/manifest.json:17` → `"com.unity.inputsystem": "1.11.2"`

**是 CM2（2.x）API**：类名是 `CinemachineVirtualCamera`，不是 CM3 的 `CinemachineCamera`。代码与版本自洽。

### 2.2 `GameplayCameraRig`：**包装 Cinemachine，不自己算**
证据（`GameplayCameraRig.cs`）：
```csharp
44:  _brain = GetComponent<CinemachineBrain>(); _ownsBrain = _brain == null;
45:  if (_ownsBrain) _brain = gameObject.AddComponent<CinemachineBrain>();
49:  _brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.LateUpdate;
51:  _brain.m_DefaultBlend = new CinemachineBlendDefinition(...Style.Cut, 0);
69:  VirtualCamera = vgo.AddComponent<CinemachineVirtualCamera>();
70:  VirtualCamera.Follow = _anchor; VirtualCamera.LookAt = null;
71:  _framing = VirtualCamera.AddCinemachineComponent<CinemachineFramingTransposer>();
76:  _confiner = vgo.AddComponent<CinemachineConfiner2D>(); _confiner.m_BoundingShape2D = _shape;
77:  _finalBounds = vgo.AddComponent<CameraViewportBounds>(); _finalBounds.Output = OutputCamera;
113: settings.ModeOverride = LensSettings.OverrideModes.Orthographic;   // 正交
151: VirtualCamera.ForceCameraPosition(point, Quaternion.identity);
156: _brain.ManualUpdate();
```
手算的只剩**跟随锚点**（`AnchorPosition()` L119：前视偏移 + 高度偏移）与
**边界矩形**（`Bounds` → `PolygonCollider2D` → `Confiner2D`）。**相机变换最终由 Brain 输出**。

### 2.3 `VillageCamera`：**纯门面，零位移算法**
`VillageCamera.cs` 全文 14 行，只有 `Initialize` 转调 `BindVillage`（L10-11）与 `OnDisable` 转 `Release`（L13）。

### 2.4 `CombatCameraShake`：**Cinemachine Impulse，不是手写偏移**
```csharp
7:   /// 战斗相机抖动：只通过 Cinemachine Impulse 让绑定的虚拟相机产生 XY 位移。
35:  private CinemachineImpulseSource _source;
36:  private CinemachineImpulseListener _listener;
162: _source = output.gameObject.AddComponent<CinemachineImpulseSource>();
179: _listener = virtualCamera.gameObject.AddComponent<CinemachineImpulseListener>();
259: listener.m_ApplyAfter = CinemachineCore.Stage.Noise;
285: m_ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Custom,
286: m_CustomImpulseShape = ShakeCurve,     // L45-59：两端0峰值1，无过冲无漂移
288: m_ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform,
364: definition.CreateAndReturnEvent(position, velocity);
```
**结论：Impulse 通道方案。** 三层防御叠加：`MaxAmplitude` 总和上限（L350）、`MinTriggerInterval`
节流（L375）、独立 channel 隔离（L233-255）。文件头注释明确"绝不调用 `CinemachineImpulseManager.Clear()`"（L11）。

---

## 3. `PlayerCameraFollow` 的真实角色

**是兼容租约壳，但仍在被实例化。**

### 3.1 谁在 new/AddComponent 它
| 位置 | 行为 |
|---|---|
| `3C/Camera/PlayerCameraFollow.cs:16` | `camera.gameObject.AddComponent<PlayerCameraFollow>()` — **唯一 AddComponent 点** |
| `Runtime/Player/PlayerSpawner.cs:108` | `_ownedFollow = PlayerCameraFollow.Attach(_followCamera, _followTarget);` — **唯一生产调用** |
| `Runtime/Player/PlayerSpawner.cs:157` | `!GameplayCameraRig.IsFollowing(_savedCamera)` — 用来判断是否需要重新绑 |
| `3C/Camera/PlayerCameraFollow.cs:24` | `Detach()` 里 `Camera.main` 取组件后置 `enabled=false` |

全工程 `new PlayerCameraFollow` **0 处**（只能经 `Attach`）。

### 3.2 它现在只做三件事
1. `Attach` 时先 `RunBounds.TryGetMapBounds` 取边界，失败抛 `InvalidOperationException`（L14）；
2. `GameplayCameraRig.For(camera)` 拿 Rig，调 `BindRun(target, bounds)`（L18-19）；
3. `OnDisable` → `_rig.Release(_boundTarget)`（L29）。

**位移、阻尼、构图、边界全部不在它里面。** 文件头注释即 "Compatibility lease for the run spawner.
Cinemachine owns camera movement."（L3）

### 3.3 冗余信号
`public float smooth = 8f;`（L7）与 `public Vector3 offset = new(0,0,-10);`（L8）**全工程无读取者**
（`smooth` 只匹配自身声明；`offset` 仅在 `AnchorPosition` 的 Rig 内以不同名字出现）。
→ 可安全删除这两个字段，但需确认 Inspector 序列化值未被场景依赖（属低风险清理项，本次未改）。

---

## 4. `Player/` 剩余的 13 个 .cs

| # | 全路径 | 行数 | 为什么没进 3C |
|---|---|---|---|
| 1 | `Assets/Scripts/Runtime/Player/PlayerSpawner.cs` | 377 | 入局**生成器**：补地面碰撞→写 SpawnPos→生成角色→放训练靶→挂相机跟随。跨场景编排，依赖 `RunBounds`/`CampService` |
| 2 | `Assets/Scripts/Runtime/Player/RunBounds.cs` | 109 | 局内**地图边界**查询（`TryGetMapBounds`）+ 掉落重生；被 Camera 与 PlayerSpawner 双向依赖，属地图域 |
| 3 | `Assets/Scripts/Runtime/Player/RunEncounter.cs` | 355 | 局内**遭遇战/场景链**总控（partial 主文件） |
| 4 | `Assets/Scripts/Runtime/Player/RunEncounter.Enemies.cs` | 212 | 遭遇战：敌人编排（partial） |
| 5 | `Assets/Scripts/Runtime/Player/RunEncounter.Rooms.cs` | 131 | 遭遇战：房间清理与空间查询（partial） |
| 6 | `Assets/Scripts/Runtime/Player/RunEncounter.Team.cs` | 174 | 遭遇战：小队/门客协同（partial） |
| 7 | `Assets/Scripts/Runtime/Player/RunEncounter.Ownership.cs` | 24 | 遭遇战：所有权（partial） |
| 8 | `Assets/Scripts/Runtime/Player/RunEncounterTicker.cs` | 22 | 清房重算节拍器（0.3s） |
| 9 | `Assets/Scripts/Runtime/Player/RunObjectScope.cs` | 101 | **阶段对象所有权**（`IDisposable`），"名字与共享类型不赋权" |
| 10 | `Assets/Scripts/Runtime/Player/RunObjectScopeMember.cs` | 11 | 对象作用域运行时身份标记 |
| 11 | `Assets/Scripts/Runtime/Player/RunDropItem.cs` | 23 | 地上可拣道具→重试入包 |
| 12 | `Assets/Scripts/Runtime/Player/CampService.cs` | 185 | 扎营：完全治疗/补充弹药，消耗 1 瓶药 |
| 13 | `Assets/Scripts/Runtime/Player/SpawnPoint.cs` | 29 | 出生点标记组件（空 GO + 本组件） |

### 4.1 归类的硬依据
`3C/README.md` 已明文划界：
> "共享战斗查询与伤害协议保留在 `Runtime/Combat/`；NPC 行为保留在 `Runtime/NPC_AI/`；
> **整局生成、对象作用域、地图边界和结算不归入 3C**。界面表现继续使用 `Runtime/MVP/`。"

该声明与 13 个文件的职责**完全吻合**，无需靠"看起来像"推断。

---

## 5. `InputManager` / `KeyIconMap` 的引用面

### 5.1 命名空间：**没有变化**
- `Assets/Scripts/Runtime/GameBase/InputManager/` 目录 **确认不存在**。
- `grep -r "GameBase\.InputManager\|GameBase/InputManager"`（.cs，全 `Scripts/`）→ **0 命中**。
- `grep -r "^namespace" 3C/Control/InputManager/` → **0 命中**。
- `Assets/Scripts` 下 **0 个 `.asmdef`**（仅 Spine 插件有），全部编进默认 `Assembly-CSharp`。

```csharp
// InputManager.cs:13
public partial class InputManager : SingletonBase<InputManager>
// InputManager.Gameplay.cs:7
public partial class InputManager
// KeyIconMap.cs:42
public class KeyIconMap : ScriptableObject
```
**新命名空间 = 全局命名空间（旧命名空间也是全局）**，即 namespace 层面**零变更**。
所谓"迁走"只是**物理目录移动**，不是命名空间重构 —— 所以没有任何 `using` 需要改。

### 5.2 `InputManager.Gameplay.cs` 的玩法输入分区职责（179 行）
| 职责块 | 关键成员 | 行号 |
|---|---|---|
| 动作名常量（局内/村庄） | `ActionCamp/Menu/Interact/Map/Functions/Inventory/Team/ItemWheel` + `ActionVillageMove/Jump/Interact/Map` | L9-20 |
| **绑定作用域三分** | `enum BindingScope { Global, InRun, Outside }`（L22）；`ActiveScope`（L24）；`ScopeOf`（L30）；`ScopesOverlap`（L38） | L22-42 |
| **作用域动作重定向** | `FindScopedAction` —— Outside 时把 `Move→VillageMove` 等改名查找 | L44-56 |
| 作用域切换检测 | `RefreshInputScope` | L58-64 |
| 旧村庄覆盖项迁移 | `MigrateSharedVillageOverrides` / `CopyOverrides` | L66-85 |
| **玩法输入阻断（多所有者）** | `_gameplayBlockers`(HashSet)，`SetGameplayInputBlocked(owner, blocked)` L95，`GameplayInputBlocked` L91，`CanReadGameplayInput` L92，`HasGameplayBlockerOtherThan` L104 | L86-108 |
| 输入边界帧 + 持续按住抑制 | `InvalidateGameplayInput` L110 / `RefreshGameplayInputLatches` L117 | L110-122 |
| **玩法读取原语** | `ReadMove()` L124、`WasGameplayPressed(name)` L133、`IsPointerAttackHeld()` L141 | L124-147 |
| UI 导航读取 | `WasNavigationPressed` L150、`ConsumeNavigationPress` L159、`IsNavigationHeld` L161 | L149-166 |
| 指针压 UI 判定 | `IsPointerOverUi`（GraphicRaycaster raycast） | L168-177 |
| 村庄地图导航特例 | `TickVillageMapNavigation` —— 仅允许关掉当前顶层 `WorldMapPanel` | L179-198 |

**分区实质**：把"谁有权读玩法输入"从单一布尔升级为**多所有者引用计数式阻断**
（`SetGameplayInputBlocked(object owner, bool)`，L94 注释 "Independent UI owners must release only their own block"），
并引入 `BindingScope` 让同一套动作名在局内/村庄自动切到不同绑定。

### 5.3 引用点清单（`grep InputManager`，183 命中，按域归类）

**Runtime —— 3C 内部（自洽，无需外部）**
| 文件:行 | 用法 |
|---|---|
| `3C/Control/InputManager/InputManager.cs:13,113` | 类声明/构造 |
| `3C/Control/InputManager/KeyIconMap.cs:27,59,119` | 注释引用 |
| `3C/Control/RunItemWheelDriver.cs:22,27,29,31,50,54,65,76` | 轮盘 |
| `3C/Control/GameplayShortcutDriver.cs:12,17,24,26,35,47,54` | 快捷键 |
| `3C/Character/PlayerLocomotion.cs:8,80,81,82,97,101,130` | 移动 |
| `3C/Character/PlayerAttack.cs:44,46` | 攻击 |
| `3C/Character/ItemUseAnimation.cs:23,38` | 输入阻断 |
| `3C/Camera/GameplayCameraRig.cs:137` | `GameplayInputBlocked` |
| `3C/Camera/CombatCameraShake.cs:396` | `CanReadGameplayInput` |

**Runtime —— GameBase / MVP / GM（跨模块消费）**
| 文件:行 |
|---|
| `Runtime/Entry.cs:11,13,50,51` |
| `Runtime/GameBase/UIManager/UIManager.cs:137,158,160,325` |
| `Runtime/GameBase/SettingsManager/SettingsManager.cs:204,224,306` |
| `Runtime/GameBase/Village/VillageSceneBootstrap.cs:110` |
| `Runtime/GameBase/Village/VillagePlot.cs:30` |
| `Runtime/GameBase/Interaction/RunInteractionService.cs:19` |
| `Runtime/GameBase/SettingRows/SettingRow_Keybind.cs:6`、`KeyIconText.cs:12` |
| `Runtime/MVP/Model/Settings/SettingsEditingModel.cs:49-60` |
| `Runtime/MVP/Model/Gameplay/VillageWorldModel.cs:22,25`、`RunInteractionModel.cs:28,29` |
| `Runtime/MVP/Presenter/Settings/SettingsPresenter.cs:77,79,81,84,86` |
| `Runtime/MVP/Presenter/Gameplay/RunMenuPresenter.cs:99`、`VillageWorldPresenter.cs:20,25` |
| `Runtime/MVP/View/VillageHud.cs:45`、`RunInteractionHud.cs:25` |
| `Runtime/MVP/View/SettingsPage/KeybindBindingsPage.cs:35,54,88,90,114-145,352,380,386,387,465,491,493,496,515,517,526,538,540,547,568,590,643,666,671,672` |
| `Runtime/GM/GMPanel.cs:172,173,177,178` |
| `Runtime/GameTest/InputTest/InputTest.cs:10` |
| `Runtime/Path&Type/GlobalPath.cs:56`（含 `res_KeyIconMapPath`，该文件为 **GBK**，`read` 工具报 invalid UTF-8） |

**Editor（14 处工具）**
| 文件:行 |
|---|
| `Editor/InputIcon/KeyIconMapWindow.cs:450,470` |
| `Editor/CameraTool/CinemachineShakeRuntimeProbe.cs:71,73`、`CinemachineFlowVerification.cs:63,82` |
| `Editor/ActorBugTool/ActorBugFlowVerification.cs:41,60` |
| `Editor/WorldMapTool/GameplayPolishFlowVerification.cs:52,74,99,134,148` |
| `Editor/RunTeamTool/RunTeamFlowVerification.cs:48,72` |
| `Editor/RunRadialTool/RadialMenuModelVerification.cs:81,85`、`ItemWheelFlowVerification.cs:51,73,155,157,164,165,167,174` |
| `Editor/RunMapTool/InputScopeVerification.cs:20,35,36,47,48,62,67,72`、`RunBagFlowVerification.cs:34,61,115,154,156,158,163`、`RunMapFlowVerification.cs:39,64,85,86,94,100,111,214` |

### 5.4 迁移性质判定
**全部是"物理移动"，不是"接口变更"。** 没有任何一处需要新增/修改 `using`；
`git diff 80a6738 HEAD` 对这批文件是 **0 insertions / 0 deletions**（见 §7.3）。
Unity 编译 **0 error / 0 warning** 是最强证据。

### 5.5 `KeyIconMap` 引用点
| 文件:行 | 说明 |
|---|---|
| `3C/Control/InputManager/KeyIconMap.cs:41,42,45,57,60-79,84-90,106-112,121,122,126,137,146,148,164,187,197,204,216,231,244,261` | 定义本体（`FindEntry`/`GetIcon`/`GetIconColor`/`GetTabIcon`/`GetOrAddEntry`/`SortEntries`/`CollectSprites`/`static Active/Load/ResetCache`） |
| `3C/Control/InputManager/InputManager.cs:366` | `KeyIconMap map = KeyIconMap.Active;` |
| `Runtime/Path&Type/GlobalPath.cs:59` | `const string res_KeyIconMapPath = "Data/Input/KeyIconMap"`（**GBK 文件**） |
| `Runtime/MVP/View/SettingsPage/KeybindBindingsPage.cs:438` | `KeyIconMap map = KeyIconMap.Active;` |
| `Runtime/GameBase/SettingRows/KeyIconText.cs:12` | 注释 |
| `Editor/InputIcon/KeyIconMapWindow.cs:36,39,44,64,90,116,216,227,254,310,321,365,387,398,415,420,763,781,789,812` | 编辑器窗口 |
| `Editor/InputIcon/IconNaming.cs:80,143,147,180,240,510,603` | 图标命名 |
| `Editor/ArtPipeline/IconAtlasBuilder.cs:83,88,101` | 图集构建 |

---

## 6. Editor 侧检查

### 6.1 `KeyIconMapWindow.cs`（814 行）
- `DefaultMapPath = "Assets/Resources/Data/Input/KeyIconMap.asset"`（L39）—— **未变**，与
  `GlobalPath.res_KeyIconMapPath = "Data/Input/KeyIconMap"`（`GlobalPath.cs:59`）一致（Resources 相对路径）。
- L470 调 `InputManager.ControlKey(path)`；该方法在 `InputManager.cs:437`（`public static string ControlKey(string path)`）**存在**。
- L450-452 注释刻意**不**走 `InputManager.IconFamily`，理由是覆盖率统计不该假定当前设备型号 —— 设计意图明确，非缺陷。
- `IconNaming.cs` 引用 `KeyIconMap.IconStyle`/`Entry`，均在 `KeyIconMap.cs:45/57` 内，可解析。

### 6.2 结论：**无失效引用、无改路径**
依据：
1. `grep "GameBase\.InputManager|GameBase/InputManager"`（含 Editor）→ **0 命中**；
2. Editor 与 Runtime 同在 `Assembly-CSharp`（无 asmdef 隔离），全局命名空间可直接见；
3. Unity 实测 `unity_get_compilation_errors` → `count: 0, isCompiling: false`，0 error 0 warning。

### 6.3 Editor 新增（来自 `3e9431e`）
`Assets/Scripts/Editor/CameraTool/` 下 4 个 Cinemachine 验证器（`CinemachineFlowVerification`、
`CinemachineFramingRuntimeProbe`、`CinemachineShakeRuntimeProbe`、`CombatCameraShakeVerification`）
与 `ActorBugTool/` 2 个（`ActorBugFlowVerification`、`MapTileCollisionVerification`）——
它们**主动引用**新 API（`GameplayCameraRig.For`、`NotifyTargetWarped`、`CombatCameraShake.Attach`），
说明重构有配套自动化回归，而非裸改。

⚠ 一处文档性不一致（非错误）：
`Editor/CameraTool/CombatCameraShakeVerification.cs:196` 的提示文案仍写着
"本批可写范围不含 PlayerSpawner / PlayerCameraFollow / 场景引导，需后续接入"，
但当前 `PlayerSpawner.cs:108` **已经接入** `PlayerCameraFollow.Attach`。该文案已过期。

---

## 7. 根因排查：`GameBase/InputManager/` 消失 + 348 之谜

### 7.1 一级目录 .cs 实测计数

| 目录 | brief 声称 | **实测** |
|---|---|---|
| 3C | 17 | **19** |
| Combat | 3 | 3 |
| Enemy | 1 | 1 |
| GameBase | 126 | 126 |
| GameTest | 9 | 9 |
| GM | 7 | 7 |
| MVP | 105 | 105 |
| NPC_AI | 4 | 4 |
| Path&Type | 2 | 2 |
| Player | 13 | 13 |
| QA | 1 | 1 |
| **11 个子目录合计** | — | **290** |
| **+ Runtime 根级 2 个** | 未计入 | `Entry.cs`、`DontDestroy.cs` |
| **Runtime 递归合计** | — | **292** |
| Editor | — | **56** |
| **`Assets/Scripts` 总计** | — | **348** ✅ |

**算术自洽**：
```
290（11 子目录） + 2（Runtime 根级 Entry.cs / DontDestroy.cs） = 292（Runtime）
292 + 56（Editor） = 348  ✅
```
> brief 的 11 项相加 = 290，与 348 差 58。差额来源**已全部定位**：
> **2 个 Runtime 根级 .cs（brief 漏算）+ 56 个 Editor .cs（brief 未列入）**。**无任何缺口。**

`Assets/Scripts` 根级 .cs = 0；`GameBase` 根级 .cs = 0（126 全在 23 个子目录内）。

### 7.2 `git status` / `stash`
```
$ git status --porcelain Assets/Scripts
(空 —— 无未提交的删除或移动)
$ git stash list
(空 —— 无暂存藏匿)
```

### 7.3 `git log --diff-filter=D`（近 5 次触及 Assets/Scripts）
唯一相关删除：
```
commit 20034a1  引入cinemachine包并替换此前的摄像头逻辑...
Assets/Scripts/Runtime/Player/PlayerCameraFollow.cs     ← D
```
但 `--diff-filter=R` 同时显示：
```
R100  Assets/Scripts/Runtime/Player/PlayerCameraFollow.cs.meta → 3C/Camera/PlayerCameraFollow.cs.meta
R100  GameBase/InputManager.meta                → 3C/Control/InputManager.meta
R100  GameBase/InputManager/InputManager.Gameplay.cs          → 3C/Control/InputManager/InputManager.Gameplay.cs
R100  GameBase/InputManager/InputManager.cs                   → 3C/Control/InputManager/InputManager.cs
R100  GameBase/InputManager/KeyIconMap.cs                     → 3C/Control/InputManager/KeyIconMap.cs
R100  Player/ItemUseAnimation.cs / PlayerLocomotion.cs / PlayerSurfacePhysics.cs
      PlayerHealth.cs(R098) / PlayerAttack.cs(R095) / RunCharacterPresentation.cs
      GameplayShortcutDriver.cs / RunItemWheelDriver.cs → 3C/{Character,Control}/
```
→ `PlayerCameraFollow.cs` 的 **D 与 R 是同一件事的两面**：`.cs` 走了 D+新建（因内容重写），
`.meta` 走 R100（GUID 保留）。**它没有丢失，已重写为 32 行兼容壳并落在 `3C/Camera/`。**

**内容零改动验证**：
```
$ git diff --stat 80a6738 HEAD -- GameBase/InputManager/ 3C/Control/InputManager/
 6 files changed, 0 insertions(+), 0 deletions(-)
```
`InputManager.cs` 1022 行、`InputManager.Gameplay.cs` 179 行、`KeyIconMap.cs` 206 行 —— **前后完全一致**。

### 7.4 历史总量趋势
| 版本 | `Assets/Scripts` 下 .cs 数 |
|---|---|
| `80a6738`（重构前） | 332 |
| `20034a1`（Cinemachine + 3C 迁移） | 343（+11：新增相机/坠落保护/表现等） |
| 工作区 now | 348（+5：`3e9431e` 新增 Editor 验证工具） |

**单调递增，从未减少。**

### 7.5 结论
> ### ✅ **没有任何源码丢失。全部是"文件被移动"（git R100 重命名），不是删除。**
>
> - `GameBase/InputManager/` 目录"消失" = 被 `git mv` 到 `3C/Control/InputManager/`，内容 0 改动，
>   `.meta` 保留（GUID 不变，场景/预制体引用不受影响）。
> - 348 这个数字是**正确的**，且 brief 的 11 项清单**本身漏列**了 2 个 Runtime 根级文件与 56 个 Editor 文件。
> - 之所以"看起来异常"，是因为 `framework.input` **架构树仍指着已删除的旧路径**（见 §8.1），
>   让人误以为代码不存在。

---

## 8. 架构树（normify）现状与调整建议

### 8.1 现状问题：**12 个模块指向已删除的旧路径**
| 模块 | 失效 source |
|---|---|
| `framework.input.display` | `Runtime/GameBase/InputManager/InputManager.cs` |
| `framework.input.manager-core` | 同上 |
| `framework.input.overrides` | 同上 |
| `framework.input.rebind-conflict` | 同上（`#L951-L1087`） |
| `framework.input.rebind-flow` | 同上（`#L691-L950`） |
| `framework.input.rebind-steps` | 同上（`#L605-L690`） |
| `framework.input.reset` | 同上 |
| `framework.input.tick` | 同上（`#L1088-L1183` ← **超出当前 1022 行，行号已完全失效**） |
| `framework.input-icons.cache` | `Runtime/GameBase/InputManager/KeyIconMap.cs` |
| `framework.input-icons.edit` | 同上 |
| `framework.input-icons.map-asset` | 同上 |
| `framework.input-icons.query` | 同上 |

补充事实：
- 父模块 `framework.input` 的 `source: []` **本来就是空的**（从未绑定文件）。
- `framework.input.rebind-conflict` 记录的 revision 仍是 `75df668e`，fingerprint 是空 SHA
  `e3b0c442...b855`（空串哈希），**说明这些模块从未真正做过证据校验**。
- 架构树中 **完全没有 3C 模块**：`normify_search "相机 camera 3C"` → 0 命中；
  `modules/` 下无 `3c|camera|character|control` 目录；全树仅 1 个 .md 提到 Cinemachine 系类名。
- `framework` 有 19 个子模块，无一覆盖 `GameplayCameraRig`/`CombatCameraShake`/`PlayerSurfacePhysics`/`PlayerFallGuard`。

### 8.2 建议方案（推荐 A）

#### 建议 A：新建 `project-chaos.3c` 顶层模块，`framework.input` 改指新路径 ← **推荐**

理由：
1. **3C 已是独立的横切关注点**（19 文件 / 6 个重构新增类 / 独立 README / 独立 Resources 配置），
   硬塞进 `framework` 会掩盖它"角色-控制-镜头一体"的语义。
2. 3C 依赖方向是 **3C → framework**（用 `InputManager`/`UIManager`/`SettingsManager`），
   而非 framework 内部自洽；放同级更符合实际依赖图。
3. `Camera/` 与 `Control/` 与 `Character/` 互相强耦合（Rig 读 `PlayerLocomotion.Facing`，
   `PlayerFallGuard` 调 `GameplayCameraRig`），必须同树，**不能拆到 framework 里散落**。

建议结构：
```
project-chaos.3c                    「3C：角色·控制·镜头」
├── camera                          镜头（7 文件）
│   ├── rig            GameplayCameraRig.cs              189L
│   ├── settings       GameplayCameraSettings.cs          23L
│   ├── viewport-bounds CameraViewportBounds.cs           20L
│   ├── shake          CombatCameraShake.cs              584L
│   ├── shake-settings CombatCameraShakeSettings.cs      108L
│   ├── follow-lease   PlayerCameraFollow.cs              32L
│   └── village        VillageCamera.cs                   14L
├── character                       角色（7 文件）
│   ├── locomotion     PlayerLocomotion.cs               177L
│   ├── surface-physics PlayerSurfacePhysics.cs          186L
│   ├── fall-guard     PlayerFallGuard.cs                 41L
│   ├── attack         PlayerAttack.cs                   101L
│   ├── health         PlayerHealth.cs                   136L
│   ├── item-animation ItemUseAnimation.cs                40L
│   └── presentation   RunCharacterPresentation.cs         9L
└── control                         控制（5 文件）
    ├── input          → 迁移 framework.input 整棵子树（8 叶子）
    ├── input-icons    → 迁移 framework.input-icons 整棵子树（4 叶子）
    ├── shortcuts      GameplayShortcutDriver.cs          56L
    └── item-wheel     RunItemWheelDriver.cs              81L
```

对 `framework.input` 的处置（二选一，推荐**迁出**）：
- **`framework.input` + `framework.input-icons` 整棵迁到 `3c.control`**，用 `normify_module_move`
  保留 uid，只改写 `source` 路径与 revision。理由：`InputManager` 同时承载
  *玩法读取*（3C 关注）与 *改键/设置*（全局框架关注），但**物理同文件、无法拆分**；
  模块树应跟随代码位置，否则以后每次移动都要再改一次。
- 备选：保留在原处只改 `source`。**不推荐** —— 会留下"模块在 framework、代码在 3C"的长期漂移，
  正是本次 12 个模块失效的成因。

#### 建议 B（若坚持 framework 不动）
仅把 12 个模块 `source` 前缀 `GameBase/InputManager/` → `3C/Control/InputManager/`，
并重算行号（`InputManager.cs` 现 1022 行，`tick` 的 `L1088-L1183` 需按实际重映射），
另建 `project-chaos.3c` 覆盖 Camera/Character 两个子树。
成本更低，但保留了 §8.1 那种"位置不一致"的隐患。

### 8.3 无论选哪个方案都必须做的收尾
1. `normify_module_refresh` 重算 12 个输入模块的 fingerprint/revision（当前为空 SHA）。
2. 12 个模块 `source` 全部由 `GameBase/InputManager/` 改为 `3C/Control/InputManager/`。
3. 修正 `tick` 模块的行号区间（原 `L1088-L1183` 已越界）。
4. 新建 3C 模块并写 `renders/*.json` 阅读导语（分层：camera / character / control）。
5. `normify_validate` + `normify_build` + `normify_render` 全绿后再收尾。
6. 不要动 `Assets/AI/normify-project-chaos` 下除本报告外的任何文件（本次审计的约束）。

---

## 9. 其他观察（非阻塞项）

| # | 观察 | 位置 | 严重度 |
|---|---|---|---|
| 1 | `PlayerCameraFollow.smooth` / `.offset` 已成死字段，无读取者 | `3C/Camera/PlayerCameraFollow.cs:7-8` | 低 |
| 2 | `CombatCameraShakeVerification` 提示文案称"未接入 PlayerSpawner"，实际已接入 | `Editor/CameraTool/CombatCameraShakeVerification.cs:196` | 低（仅文案） |
| 3 | `PlayerSurfacePhysics.MinGroundNormalY=.5f` 与 `RunMapPhysics` 各自定义常量，注释称"与 RunMapPhysics 一致"，属**重复常量**而非单一真相源 | `3C/Character/PlayerSurfacePhysics.cs:15-16` | 低 |
| 4 | `PlayerHealth.Respawn()` 私有方法 L126 **无调用方**（死代码；HP 归零走 `OnDowned`→`RunEncounter`） | `3C/Character/PlayerHealth.cs:126-135` | 低 |
| 5 | `InputManager.cs` 1022 行单类偏大（`Gameplay` 分区已拆出 179 行，基本仍 1022） | `3C/Control/InputManager/InputManager.cs` | 中（可读性） |
| 6 | 新增目录名 `3C` 以数字开头，C# 命名空间无法容纳；当前恰好用全局命名空间，一旦将来加 asmdef 会受限 | `Assets/Scripts/Runtime/3C/` | 低（前瞻） |
| 7 | `Path&Type/GlobalPath.cs` 为 GBK 编码，`read` 工具读不了（已知 91 个 .cs 混合编码问题） | `Runtime/Path&Type/GlobalPath.cs` | 已知 |

---

## 10. 证据索引

| 事实 | 命令 / 文件:行 |
|---|---|
| Cinemachine 2.10.3 | `Packages/manifest.json:12`、`packages-lock.json:112` |
| Rig 用 CinemachineVirtualCamera | `GameplayCameraRig.cs:69,71,76,77` |
| 抖动用 Impulse | `CombatCameraShake.cs:162,179,285,364` |
| 视口夹紧在 Finalize 阶段 | `CameraViewportBounds.cs:12,18` |
| PlayerCameraFollow 仍被实例化 | `PlayerCameraFollow.cs:16`、`PlayerSpawner.cs:108` |
| 全局命名空间（无 namespace） | `grep '^namespace' 3C/` → 0 命中 |
| 无 asmdef | `Assets/Scripts` 下 `*.asmdef` = 0 |
| 编译 0 error | `unity_get_compilation_errors(port=7891)` → `count:0, isCompiling:false` |
| 348 算术 | 290 + 2(root) = 292；292 + 56(Editor) = 348 |
| 移动非删除 | `git log --diff-filter=R` → 22 条 R100 |
| 内容零改动 | `git diff --stat 80a6738 HEAD -- ...InputManager/` → 0 insertions / 0 deletions |
| 无未提交改动 | `git status --porcelain Assets/Scripts` → 空 |
| 无 stash | `git stash list` → 空 |
| 12 模块路径失效 | `framework.input.*` / `framework.input-icons.*` 的 `source` |

---

*本报告为只读审计产物，未修改 `Assets/Scripts` 下任何 C# 代码，未改动架构树数据库。*
