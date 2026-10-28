# Project Chaos · 架构学习资料库

这份资料库围绕当前 Unity 项目的系统和底层实现，目标是帮助读者理解代码如何协作、为什么这样设计、有哪些代价，以及哪些能力适合带到新项目。核查日期为 2026-10-26；基于当前本地源码静态阅读，未在本轮执行 Unity 构建或运行验证。

## 如何使用

每篇系统文档均按“系统定位→实现逻辑→具体调用层→优点→缺点与边界→源码导读与学习练习→可复用轮子”组织。每类最后的小标题统一为“可复用轮子”，其中给出可迁移范围、所需依赖与迁移前要处理的问题。复用建议属于架构判断，不是已经提取并验证好的独立包。

源码位置以 `Assets/相对路径:行号` 表示；它是 2026-10-26 快照的定位信息，客户端页面不会因此自动获得本机文件访问权。类名与方法名可用于本地搜索，源码修改后行号需要重核。资料中的历史方案只作背景，当前行为优先以实际调用链为准。

## 学习目录

| 分组 | 系统篇 | 主要解决的问题 |
| --- | --- | --- |
| 底层架构与数据 | 01 总体架构；02 启动场景与加载；03 MVP；04 UI；05 Excel 配置；06 存档 | 程序从哪里启动、界面如何进入业务、数据如何落盘 |
| 输入设置与基础框架 | 07 输入与重绑定；08 设置；09 本地化；10 单例事件与调度；11 资源与对象池；12 音频；13 日志 | 基础服务如何协调，生命周期和错误边界在哪里 |
| 地图导航与战斗 | 14 局外世界地图；15 连续地图；16 寻路与敌友 AI；17 战斗遭遇；18 统一交互 | 世界数据如何变成场景与角色行为 |
| 玩法与成长系统 | 19 库存掉落市场与结算；20 门客派遣扎营；21 村庄生产；22 家族效果时代；23 剧情对话 | 玩法如何修改当前局与长期进度 |
| 工具与验证 | 24 编辑器工具与验证体系 | 资产怎么生产、代码与运行结果怎么分别验证 |

## 建议阅读顺序

第一轮先读 01、02、03、05、06，建立入口、分层与数据寿命概念。第二轮选“市场购买”串起 04、08、19，练习从 UI 意图追到存档。第三轮读 15、16、17、18，理解计划、导航、物理与业务状态的区别。最后用 21、22、23 学跨系统规则，再读 24 理解验证。

## 贯穿项目的调用链

```text
EnterScene：UI → 输入动作 → 设置应用 → 主菜单
  → 选择存档 → GameSession.Begin
  → LoadingScene → MainScene 实体村庄
  → 地图/派遣 → 保存出征信息 → LoadingScene
  → NaturalMapBuild → NaturalMapAssembler → WorldScene
  → 遭遇/战斗/交互/掉落 → 结算 Model → 保存 → 返回村庄
```

## 术语与常见混淆

| 术语 | 当前含义 | 不应混淆为 |
| --- | --- | --- |
| ConfigData | Excel 生成的只读定义字段类 | 手写业务 Model |
| SavePayload | 一个存档会话的长期玩家数据 | 每个界面各自拥有一份数据 |
| RunInventory | 当前局临时库存 | 已经成功带出到长期库存 |
| Presenter | 意图处理、显示映射、交互编排 | Unity 控件或持久化后端 |
| GameBase/Meta | 既有玩法服务 | 全部可直接搬走的通用框架 |
| NaturalMapPlan | 当前连续地图计划 | 已装配、碰撞可用的 Unity 场景 |
| RunMapGenerator | 仍保留的旧房间图算法 | 当前正式 Bootstrap 的生成主线 |
| BFS | 当前 TryPath 的队列搜索 | A*、NavMesh 或最短耗时保证 |
| 资料库来源 | 已添加到当前项目/对话可用的资料 | 所有聊天都会自动检索整个 Space |

## 已核对的关键边界

- 当前正式地图入口使用 NaturalMapBuild；旧房间生成代码仍在仓库。
- 世界地图元数据 provider、MapInspectable 等内容仍有占位性质。
- 设置和结算的事务寿命比一次 Presenter 连接更长；刷新不能重做奖励。
- 音频目前通过 AudioSource 音量相乘，无 AudioMixer 总线；PauseSound 未实现。
- 正式槽位位于 persistentDataPath；旧 Recorder 不应当作正式存档方案。
- 无真实会话时 SaveProgress 仍可能写原型槽 3，学习检查不能默认不会落盘。
- 村庄生产按现场实际工作时间累计，不能按旧设计推断已有离线生产。
- 有些源码文件使用旧编码，编辑前应保留原编码或明确转换，不要根据终端乱码直接改文本。

## 向 ChatGPT 提问的约定

建议把本页、五份分组正文与“源码证据摘录”添加为同一个 ChatGPT 项目的来源，或在提问时明确引用所需页面。保存到 Space 与自动成为每个聊天的可用来源是不同步骤；完成后用真实问题检查来源引用。

可采用以下项目指令：

> 回答 Project Chaos 架构问题时，先查当前项目已添加的资料，说明使用了哪个系统篇。讲实现先解释职责和调用层，再给源码类与方法位置。区分源码事实、架构评价、复用建议与尚未验证的推断。资料没有覆盖时明确说明，不把规划、占位或历史 QA 结果说成当前已实现/已通过。代码与资料冲突时，以最新代码核查为准并指出版本差异。涉及迁移的内容使用“可复用轮子”标题。

示例问题：“为什么第二次进入 EnterScene 不能只依赖 Start？”“结算页被设置覆盖后怎样避免重复发奖？”“寻路为什么是 BFS？”“村庄两个工人为什么不会让同一建筑双倍生产？”“想复用输入重绑定，必须一起迁移哪些依赖？”

## 维护规则

每次更新对应系统时，核对入口、调用者、状态持有者、保存/失败路径和资源配置依赖；更新核查日期与源码位置。重新上传文件时明确替换旧版本，避免同一项目同时保留互相冲突的旧正文。维护频率不等于已经配置自动同步或定时任务。

本地导入稿与客户端内容状态分别记录。只有页面/文件已保存并能重新打开阅读，才算写入成功；只有目标聊天实际可引用这些资料，才算完成问答接入。


---

## 01 总体架构：从玩家操作到存档

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

本工程是 Unity 2022.3 的 2D 项目。理解它应先区分三件事：Unity 场景中的对象与组件、纯 C# 的界面流程和业务规则、可序列化的配置与存档数据。目录名不完全等于职责边界：`GameBase/Meta` 中实际放了库存、门客、派遣、村庄等业务服务，并非全是可通用的底层框架。

### 实现逻辑

正式运行主线是：`EnterScene` 初始化框架并显示主菜单，玩家选择存档后进入 `MainScene` 实体村庄，从地图与派遣界面出发，经 `LoadingScene` 准备地图，进入 `WorldScene` 战斗，结算后回村庄。

界面主要采用 MVP：View 接收按钮等输入并发出意图，Presenter 编排操作和生成显示状态，Model 承担状态、事务或服务适配。Model 之外，已有业务 Service 继续使用；因此当前架构是“MVP + 既有单例服务”的渐进结构。

数据有三种不同寿命。配置行描述物品、地图、建筑的定义；`SavePayload` 保存玩家长期进度；`RunInventory`、遭遇对象和地图计划承担当前一局的临时状态。把它们混用，容易出现换档串数据、退出界面丢失事务或重复结算。

### 具体调用层

```text
Unity UI / 组件输入
  → View.Intent
  → Presenter.Handle / Execute
  → Model 或业务 Service
  → GameSession.LoadedPayload / 当前局状态
  → MetaProgressService.SaveProgress
  → SaveSlotService.Save → persistentDataPath/Saves
```

这张图描述常见路径，不代表所有脚本都已符合这一层次。设置的行控件仍通过 DTO 与委托访问编辑区；战斗、导航等组件直接参与 Unity 帧循环。

### 优点

表现、流程、长期数据已有基本分离。配置表和存档各有入口，能追踪一次操作最终修改了什么。正式流程与 GM 调试入口并存，便于开发时局部验证。

### 缺点与边界

业务服务仍普遍通过 `Instance` 互相寻找，依赖不容易从构造函数看全。静态局内状态、场景生命周期和常驻对象共同工作，跨场景清理比普通对象调用更复杂。`MetaProgressService` 无真实存档时可退到内存态，调试成功不等于真实存档链路成功。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/Entry.cs:38` — `private void Start()`
- `Assets/Scripts/Runtime/GameBase/Meta/MetaProgressService.cs:67` — `public bool SaveProgress(`
- `Assets/Scripts/Runtime/GameBase/SaveManager/GameSession.cs:47` — `public void Begin(`
- `Assets/Scripts/Runtime/MVP/Presenter/PresenterBase.cs:6` — `public abstract class PresenterBase`

练习：沿“市场购买一个物品”画出 View、Presenter、Model、Service、Payload、磁盘六个位置。分别标注谁负责显示、校验、扣钱与保存；不要只按文件夹猜职责。

### 可复用轮子

优先复用“分层规则和生命周期契约”，再迁移实现。MVP 基类、设置编辑事务、存档槽位壳可分别抽取；不建议把整个 `GameBase` 目录直接搬走。迁移前列出 Unity 包、资源路径、单例和场景名依赖，并在新项目建立一个最小完整操作验证链。


---

## 02 启动、常驻对象与场景加载

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

负责一次性初始化、按存档会话路由、出征和返乡，以及地图准备期间的可见进度。`MainScene` 是村庄，`WorldScene` 是局内；不要把名字中的 Main 理解成战斗主场景。

### 实现逻辑

`Entry.Start` 依次初始化 UI、输入、设置。输入先初始化，设置才能把保存的重绑定覆盖应用到动作表。静态 `_booted` 限制重复初始化；`DontDestroy` 通过身份键阻止重复常驻根。

二次加载入口场景时，新 `Managers` 可能被判重销毁，其下的新 Entry 不会执行 Start，所以返程路由还依赖静态 `sceneLoaded` 监听。这个例子说明：对象“出现在场景文件中”不意味着它每次都能活到 Start。

`VillageSceneFlow` 校验会话和场景配置，村庄初始化失败时提供回槽位界面的恢复路径。`RunSceneFlow` 用 Idle、LoadingRun、Active、Returning 阶段阻止重入，用过渡版本号拒绝过时回调。第 2、3 阶段重新走加载流程，并保留队伍和整局种子。

`LoadingController` 控制目标场景激活，同时准备轮播资源和地图。地图先建到跨场景容器，激活后再搬入目标场景。激活前进度按 0.9 归一化并夹紧；显示值单调推进，准备未完成时不能只看场景加载比例就放行。

### 具体调用层

```text
存档界面 → VillageSceneFlow.EnterFromSave → LoadingScene → MainScene
DispatchService.Depart → RunSceneFlow.Begin
  → RunMapBootstrap.RequestForScene → LoadingController
  → BuildIntoPendingSteps → FinalizePendingInto
  → RunEncounter.EnterPreparedScene
```

`ScenesLoadManager` 是通用加载入口；`LoadingController` 则拥有正式读条的额外准备逻辑。两者不是可以任意替换的同一层。

### 优点

一次初始化、常驻判重、流程阶段和准备完成校验共同减少重复对象及跨场景竞态。地图分帧准备避免把所有工作推到战斗场景 Start。

### 缺点与边界

仍依赖固定场景名、层级节点和 Build Settings。失败恢复在村庄和局内路径中的处理并不完全一致，不能笼统承诺所有加载失败都有按钮可返回。静态事件要成对解除，场景加载成功也不等于玩家、敌人和碰撞已准备成功。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/DontDestroy.cs:38` — `private void Awake()`
- `Assets/Scripts/Runtime/GameBase/Village/VillageSceneFlow.cs:24` — `public static void EnterFromSave()`
- `Assets/Scripts/Runtime/GameBase/Meta/RunSceneFlow.cs:34` — `public static bool Begin(`
- `Assets/Scripts/Runtime/GameBase/ScenesLoader/LoadingController.cs:287` — `private IEnumerator Run()`
- `Assets/Scripts/Runtime/GameBase/RunMap/RunMapBootstrap.cs:131` — `public static void FinalizePendingInto(`

练习：解释为什么不能只在 Entry.Start 中处理返乡，以及为什么搬入目标场景的是根容器。再找出读条完成与地图验证完成的两个不同判据。

### 可复用轮子

可抽出常驻对象身份判重、阶段状态机、过期回调版本号，以及“准备容器→激活→接管”模式。把场景名和准备任务改为注入参数；迁移时用空场景验证往返、重复加载、准备失败与取消，避免依赖本工程节点名。


---

## 03 MVP：界面流程与事务生命周期

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

MVP 的价值是把“如何显示”与“操作意味着什么”分开。当前 View 通常仍负责创建控件、组装 Model 与 Presenter；Presenter 是纯 C#，不应持有 Unity 按钮、直接读文件或负责 Resources 加载。

### 实现逻辑

`PresenterBase.Attach` 幂等，并在挂接失败时执行清理；`Dispose` 幂等，调用 OnDetach 后清空 View。一个已销毁 Presenter 不再复用。`IPanelView<TState,TIntent>` 通过 Intent 事件输入，通过 Render 状态输出。

`GameplayPresenter<TState>` 把刷新、关闭和业务动作分开：刷新只投影显示，业务动作进入 Execute。以市场为例，Model 读取定义、余额和堆叠条件，Presenter 把阻塞原因映射成文本，View 按状态渲染。

面板被覆盖会 OnExit，恢复时再 OnEnter，因此 Presenter 的生命常比业务事务短。设置的 pending、结算的物品选择和已发奖标志不能跟随 Presenter 重新创建。按钮也应只绑定一次，并向 View 的事件发意图，避免闭包捕获已 Dispose 的 Presenter。

### 具体调用层

```text
MarketPanel 按钮 → PanelIntent(Buy, id)
  → MarketPresenter.Execute → MarketModel.Buy
  → MarketService.TryBuy → 保存/失败恢复
  → Presenter.Refresh → MarketState → View.Render
```

```text
RunSettlementModel.Begin → Current 保留事务
打开/覆盖/恢复面板 → 新 Presenter 连接同一事务
Confirm → 首次应用奖励 → 保存重试 → Completed
```

### 优点

意图和显示状态让流程比散落的按钮 lambda 更容易阅读。成对订阅清理减少重复响应。把事务与面板连接分开，可以防止重新显示界面就再次扣钱或发奖。

### 缺点与边界

Presenter 仍会直接依赖部分单例服务和本地化入口，尚未全面依赖注入。通用 `PanelIntent` 的 action/id 对复杂操作的表达有限。State 中存在可变 DTO，不能把所有状态都称为严格不可变快照。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/MVP/Presenter/PresenterBase.cs:11` — `public void Attach(`
- `Assets/Scripts/Runtime/MVP/Presenter/Gameplay/GameplayPresenter.cs:27` — `public abstract class GameplayPresenter`
- `Assets/Scripts/Runtime/MVP/Presenter/Gameplay/MarketPresenter.cs:46` — `protected override void Execute(`
- `Assets/Scripts/Runtime/MVP/Model/Gameplay/RunSettlementModel.cs:36` — `public static void Begin(`

练习：把“被覆盖后恢复结算页”画成时序图，标出哪些对象重建、哪些状态必须保留。确认 Render 与 Attach 路径没有发奖。

### 可复用轮子

`PresenterBase`、类型化 View 契约、Attach/Dispose 配对和长寿命事务模式值得独立提取。新项目建议先给业务服务定义窄接口，再注入 Presenter/Model；保留本项目生命周期约束，移除具体 PanelType、本地化键和单例耦合。


---

## 04 UI 管理、面板栈与显隐动画

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

`UIManager` 管理缓存面板、栈式导航、局外 UI 隐藏恢复与玩法输入屏蔽。`BasePanel` 表达面板生命周期；`UIPanelAnimator` 负责位置、缩放、透明度与错峰动画，不负责业务奖励。

### 实现逻辑

面板由 PanelType 和 Resources 路径关联，实例缓存于字典，当前导航关系存于栈。Push 与 Pop 不仅是 SetActive：还需要触发生命周期以断开旧 Presenter、接入新 Presenter，并刷新输入屏蔽。

`SetRootPanel` 用于替换整条导航根；`CloseAllPanels` 用于清理面板状态。进入局内时记录已隐藏面板及画布，返回时恢复。标记为 `GameplayCanvas` 的画布不随局外画布一起隐藏，避免误关战斗 HUD。

动画脚本记录静止姿态，进入/退出时构造 DOTween Sequence，并处理快速切换时的 tween 中断与姿态归位。错峰动画收集子节点并缓存起始状态。布局系统与动画同时写 RectTransform 时，应分清谁控制静止布局、谁控制动画偏移。

### 具体调用层

```text
Presenter / 导航入口 → UIManager.PushPanel / PopPanel
  → BasePanel.OnEnter / OnExit
  → View 创建/Dispose Presenter
  → UIPanelAnimator.PlayEnter / PlayExit
  → UIManager.RefreshGameplayInputBlock
```

源码还存在 `OtherUIManager` 目录中的另一套实现。阅读时必须核对命名空间和当前调用，不能因为同名就把它当作正式 UI 管理器。

### 优点

统一导航和生命周期使“覆盖、返回、退出”有共同语义。动画从业务面板分离，风格可以配置；输入屏蔽放在管理层，减少每个界面自行禁用角色的重复代码。

### 缺点与边界

PanelType、预制体命名和路径存在约定耦合；公开的栈允许外部绕开管理器。缓存面板有助于减少创建，但也要求订阅、事务状态和动态子节点清理正确。动画正常播放并不能证明面板已完成业务退出。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/GameBase/UIManager/UIManager.cs:98` — `public BasePanel PushPanel(`
- `Assets/Scripts/Runtime/GameBase/UIManager/UIManager.cs:248` — `public void HideAllPanelsForGameplay()`
- `Assets/Scripts/Runtime/GameBase/UIManager/BasePanel.cs:14` — `class BasePanel`
- `Assets/Scripts/Runtime/GameBase/UIManager/UIPanelAnimator.cs:147` — `public void PlayEnter(`

练习：追踪“局内菜单→设置→返回菜单→关闭菜单”，分别记录栈顶、Presenter、角色输入和暂停状态。它们是相互关联的不同状态，不能只测面板是否可见。

### 可复用轮子

可复用面板栈生命周期与独立动画组件。迁移需带 DOTween、CanvasGroup 和姿态恢复逻辑，并把面板标识与资源提供者改为可配置。不要复制另一套同名管理器；给快速开关、覆盖恢复和暂停中动画建立最小验证场景。


---

## 05 Excel 配置、生成代码与运行时加载

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

配置管线把策划表转换为 Unity 可读取的 JSON 与 C# 字段类。它与玩家存档是两条路径；Excel 导出的 `Data` 类也不等于 MVP 中手写的业务 Model。

### 实现逻辑

`ConfigManager` 使用 EPPlus 读取 xlsx 的第一个工作表。第 3 行备注，第 4 行字段名，第 5 行端标记，第 6 行类型，第 7 行开始数据。导出 JSON 包装为 `datas` 列表，由 `DataList<T>` 对应读取。

Config 模式写 `Resources/Data/Json/Runtime` 和 `MVP/Model/ConfigData`；Data 模式写 Records 与 `ModelData`。类名、表名和读取路径依赖命名约定，文件名在下划线前截断。手写业务逻辑应避开生成目录，否则重新导出可能覆盖。

类型转换不是“Excel 能写什么就支持什么”。导出器有类型白名单；bool 归一化为 JSON 字面值。string 分支直接包双引号，特殊字符转义需要格外检查。数值型分支也不能当作完整的数值范围校验器。

### 具体调用层

```text
xlsx → ConfigManager.ExportConfigs / ExportModels
  → ExportJson + ExportClass → JSON / C# 字段类
  → ConfigLoader.LoadData<T> → ResManager → Resources
  → JsonUtility.FromJson<DataList<T>> → 业务 Service
```

部分业务服务自行做安全加载，例如 WorldMapService 在表缺失时返回空集；基础 ConfigLoader 的同步入口直接访问 TextAsset.text，缺失时可能空引用。两种错误策略不可混为一谈。

### 优点

定义和字段类来自同一表格，减少手工维护结构的重复劳动。配置、生成类、业务规则有分工，便于追踪某个字段如何进入玩法。

### 缺点与边界

手工拼 JSON、固定行号、单工作表和命名截断都限制了扩展性。导出器位于 Runtime 目录且含编辑器相关引用；实际构建边界需要独立检查，目录名不保证 Player 安全。本次不运行导出，避免覆盖现有产物。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigManager.cs:182` — `private static void ExportJson(`
- `Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigManager.cs:364` — `private static string Convert(`
- `Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigLoader.cs:18` — `public DataList<T> LoadData<T>()`
- `Assets/Scripts/Runtime/Path&Type/GlobalPath.cs:147` — `public const string data_JsonPathToRead`

练习：挑一行 BuildingConfig，从表格字段对应到 JSON、生成类、VillageService 的消费位置。再设计含引号与换行的字符串用例，说明应在哪一层发现错误。

### 可复用轮子

可复用“表头契约→代码生成→运行时 DTO”的管线思路。抽取前优先改用可靠 JSON 序列化、增加结构及引用校验、隔离 Editor 代码，并将输出路径和类型映射配置化。EPPlus 是外部依赖，迁移需单独核查所用版本与许可。


---

## 06 存档槽位、会话与失败一致性

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

`GameSession` 保存当前运行中选中的槽与 Payload；`SaveSlotService` 负责文件读写。业务进度通常通过 `MetaProgressService.SaveProgress` 汇入同一路径。旧 `Recorder` 是表格式 record 工具，不是正式玩家存档。

### 实现逻辑

正式槽位使用 `persistentDataPath/Saves/Slot1.json` 至 Slot3.json，文件包含 meta 和 payload。Payload 使用可序列化 public 字段保存资源、门客、建筑、家族节点、局数和物品等数据。属性不能替代这些字段参与 JsonUtility 序列化。

Save 先写对应槽位的 `.tmp` 临时文件，旧槽存在时调用 File.Replace，不存在时 Move。这样可以减少直接覆盖过程中损坏旧档的风险，但临时文件名并非每次随机生成，也不等于有备份历史、跨进程锁或任意崩溃下的完整事务保证。

业务上的一致性仍由调用方管理。市场、村庄命令等会保存操作前 Payload JSON，保存失败后覆盖恢复。结算则保留“奖励已应用、等待保存”状态，重试只保存而不再次发奖。两者是不同策略，不能看到 Save 返回 bool 就认为所有调用都能自动回滚。

### 具体调用层

```text
SaveSlotsPresenter → SaveSlotsModel → SaveSlotService 读/建/删槽
  → GameSession.Begin → 村庄入口
业务命令 → 修改同一 Payload → MetaProgressService.SaveProgress
  → SaveSlotService.Save → 临时文件 → 替换正式槽
```

`Recorder` 仍基于 Application.dataPath 下 record 数据和缓存，打包写入位置及缓存刷新语义都不同。不要将它作为正式存档的推荐复制对象。

特别注意：MetaProgressService 没有真实会话时，Payload 可用内存态，但 SaveProgress 仍会写原型槽 3，并非自动禁止落盘。因此学习或验证时不要直接执行会保存进度的 GM 操作，更不能默认它们不会影响真实槽文件。

### 优点

会话和文件职责分离，玩家进度集中在一个可追踪结构。临时文件替换、坏档标记以及业务级失败恢复比直接 WriteAllText 更易定位问题。

### 缺点与边界

Payload 可变且被多个服务共享，任意字段变化都可能影响整个会话。当前版本号存在，不代表所有未来版本迁移都已实现；新增字段的默认值和旧档归一化需要逐项验证。结算中 AddItem 失败原因未逐项处理，满库存场景不能凭结算完成状态就断定物品全部入库。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/GameBase/SaveManager/SaveSlotService.cs:239` — `public bool Save(`
- `Assets/Scripts/Runtime/GameBase/SaveManager/SaveSlotData.cs:44` — `public class SavePayload`
- `Assets/Scripts/Runtime/GameBase/SaveManager/GameSession.cs:47` — `public void Begin(`
- `Assets/Scripts/Runtime/MVP/Model/Gameplay/RunSettlementModel.cs:72` — `public bool Confirm(`

练习：对比“市场写盘失败”与“结算写盘失败”后内存、磁盘、界面各是什么状态。检查重试、重复点击和换档三种情况。

### 可复用轮子

槽位壳、会话对象、临时写入后替换以及“已应用/待保存”事务可独立抽取。把文件系统与序列化封装为接口，增加迁移、备份、校验及隔离测试目录；业务 Payload 留在业务项目，避免通用存档包依赖本作建筑和门客字段。


---

## 07 输入动作、重绑定与界面隔离

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

`InputManager` 集中管理 Input System 动作表、设备绑定显示、重绑定和玩法输入屏蔽。正式角色与快捷导航读取这一入口；旧 DialogueManager 仍有旧式 Input 调用，不能把重定向范围说成所有脚本。

### 实现逻辑

运行时使用动作资源的实例，设置中的 overrides JSON 应用到同一份动作表。这样玩家修改按键后，角色读取和按键提示能遵循同一绑定来源，而不必修改工程原始资源。

屏蔽由多个 owner 构成的集合表达，UIManager、GM 或其他系统分别占有屏蔽。某个界面释放自己的屏蔽，不会解除另一界面持有的屏蔽。重绑定期间也算屏蔽。

仅有一个 blocked 布尔值仍不够。打开/关闭界面的当帧可能已经检测到按键边沿，持续按住攻击也可能穿透。因此实现还包含输入边界帧、持续攻击抑制与指针 UI 命中检查。角色 OnDisable 和收到屏蔽通知时清理缓存的移动、跳跃请求，避免关界面后补执行旧输入。

`GameplayShortcutDriver` 是局级快捷键入口，角色倒地后仍可驱动菜单；它与角色移动组件分开，避免菜单可用性依附存活角色。

### 具体调用层

```text
设备 → 克隆动作表 → InputManager.ReadMove / WasGameplayPressed
  → PlayerLocomotion / PlayerAttack
SettingsEditingModel → BeginRebind → overrides JSON
  → SettingsManager.Pending → Commit / Revert
UIManager / GM → SetGameplayInputBlocked(owner, value)
```

导航动作与玩法动作的允许条件不同：菜单关闭需要在 UI 持有屏蔽时仍能读取导航，但仍须尊重其他 owner 和重绑定状态。

### 优点

多 owner 屏蔽适合叠加 UI，绑定配置与实际输入共用数据。边界帧与清理机制关注真实输入时序，解决单纯禁用 Update 无法覆盖的穿透问题。

### 缺点与边界

动作名仍是字符串约定；绑定组合、设备类别与图标资源必须一致。owner 未释放会留下长期屏蔽；旧输入脚本仍需逐个迁移。注入测试输入不能替代真实设备、UI 点击、持续按住和焦点变化测试。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/GameBase/InputManager/InputManager.cs:41` — `partial class InputManager`
- `Assets/Scripts/Runtime/GameBase/InputManager/InputManager.Gameplay.cs:23` — `public void SetGameplayInputBlocked(`
- `Assets/Scripts/Runtime/GameBase/InputManager/InputManager.Gameplay.cs:61` — `public bool WasGameplayPressed(`
- `Assets/Scripts/Runtime/Player/GameplayShortcutDriver.cs:6` — `private void Update()`
- `Assets/Scripts/Runtime/Player/PlayerLocomotion.cs:108` — `private void ClearInput()`

练习：画出 UI、GM 同时占有屏蔽，再按相反顺序关闭的状态变化。解释“鼠标点击关闭面板后攻击一次”为什么需要边界帧保护。

### 可复用轮子

可提取多 owner 输入闸门、边界帧失效、重绑定封装与绑定提示模型。新项目应将动作标识、UI 命中策略和设备图标表替换为自己的适配层；为屏蔽 owner 提供生命周期绑定的释放机制，减少忘记解除的风险。


---

## 08 设置编辑事务与可组合行控件

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

设置包括图像、声音、玩法和按键绑定。`SettingsManager` 保存 Applied 与 Pending，`SettingsEditingModel` 管理一次编辑流程，Presenter 对接 SettingPanel 与各页。行控件承担 Toggle、Slider、Selector、Keybind 等通用输入表现。

### 实现逻辑

Applied 是已接受的设置，Pending 是当前编辑副本。BeginEdit 克隆 Applied；界面改动写 Pending；CommitEdit 接受副本、保存并推给 Screen、音频、输入和本地化。RevertEdit 丢弃编辑，并把按键覆盖和音量试听恢复到 Applied。

按键与音量说明了“编辑”不等于完全没有运行时影响：重绑定会先改动作实例，音量拖动会实时试听。因此取消除了丢弃数据，还必须撤回副作用。设置页被覆盖时保留同一编辑事务，实际退出才决定提交或回滚。

`SettingRowBase` 及其子类通过绑定委托读写字段，减少各设置页重复拼控件。这是一种 UI 组合方式，不应据此把业务状态塞进行控件。分辨率通过 ResolutionHelper 提供候选并修正当前选择，展示选项与引擎支持值需要对齐。

### 具体调用层

```text
SettingRow → 页绑定委托 → SettingsPresenter / SettingsEditingModel
  → SettingsManager.Pending → NotifyPendingChanged → 刷新
应用 → CommitEdit → Save → ApplyToRuntime → 引擎子系统
取消 → RevertEdit → 恢复输入覆盖和音频
```

设置文件位于 `persistentDataPath/Config/settings.json`，与三个游戏槽位分开；换存档不应被理解为换一套图像/按键配置。

### 优点

双副本让应用、取消、恢复默认有清晰语义。通用行减少界面代码，实时预览改善试听和重绑定体验。Model 的编辑生命周期与 Presenter 生命周期分开，覆盖界面不必丢改动。

### 缺点与边界

Save 捕获写入异常，CommitEdit 未建立与槽位存档同等的磁盘事务确认，不能承诺写盘失败会自动撤回 Applied。行绑定仍使用可变对象与委托，调试时需追踪写入者。图形选项映射与目标平台有关，选项存在不证明所有硬件均支持。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/GameBase/SettingsManager/SettingsManager.cs:171` — `public void CommitEdit()`
- `Assets/Scripts/Runtime/GameBase/SettingsManager/SettingsManager.cs:198` — `public void RevertEdit()`
- `Assets/Scripts/Runtime/MVP/Model/Settings/SettingsEditingModel.cs:22` — `public void EnsureEdit()`
- `Assets/Scripts/Runtime/GameBase/SettingRows/SettingRowBase.cs:28` — `class SettingRowBase`

练习：找出 Pending 改动中哪些会立即影响引擎、哪些只在应用时生效。设计“改音量→进入子页→返回→取消”的检查过程。

### 可复用轮子

双副本编辑事务、设置行与分辨率整理器具有较强通用性。抽取时把存储和运行时应用接口分开，使提交能报告成功或失败；预览副作用记录原值并可回滚。页面标题、分类与本作 SettingIds 保留为业务配置。


---

## 09 本地化、动态文本与字体切换

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

本地化将文字键与语言内容分离。`LocalizationManager` 提供查词与语言变化，`LocalizedText` 将键绑定到 TMP，`LanguageFontMap` 与 `LocalizedTextFont` 负责语言对应字体。`GameplayText` 服务代码生成的玩法文本。

### 实现逻辑

管理器从 LocalizationData 建立当前语言字典；切换语言后重建缓存并通知文本刷新。固定 UI 节点可挂 LocalizedText，记录 key 与格式参数；动态物品名称、提示和行文本由 GameplayText 获取带后备值的字符串。

文字正确与字体正确是两件事。同一个 key 可能查到正确语言内容，但 TMP 字体没有相应字形仍会显示方块。字体映射应与语言切换同步；代码直接写 label.text 的位置也要考虑语言变化后的再次渲染。

局内读条、交互和村庄还存在表驱动文本入口：RunMapLoadingText、RunInteractionText、VillageText。它们与 LocalizationData 不是完全相同的资料管线。扩充语言前要逐条确认来源，不应只翻译一张表就宣布完成。

### 具体调用层

```text
SettingsManager.ApplyToRuntime → LocalizationManager.ChangeLanguage
  → BuildCache → 文本刷新/通知
LocalizedText.SetKey / SetFormatArgs → GetText → TMP
Presenter.CreateState → GameplayText.Text / ItemName → View.Render
```

编辑侧由文本收集器从预制体提取信息，编辑子配置、导入导出 CSV，再合并到运行时配置。子表路径与默认写入目标由 GlobalPath 描述，但运行时仍需正确引用最终资产。

### 优点

静态文本组件与动态代码入口互补，减少硬编码文本。字体映射独立出来，能解释“翻译已存在却显示错误”的问题。按模块编辑子表便于组织内容。

### 缺点与边界

存在多种文本来源，容易遗漏刷新或翻译。键碰撞、CSV 列契约、合并顺序及缺失后备策略都需要验证。旧文档中的历史缺陷不能直接当成当前定论；应回到当前收集器和导入导出函数核查。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/GameBase/LocalizationManager/LocalizationManager.cs:83` — `private void BuildCache()`
- `Assets/Scripts/Runtime/GameBase/LocalizationManager/LocalizedText.cs:101` — `public void UpdateText()`
- `Assets/Scripts/Runtime/GameBase/LocalizationManager/GameplayText.cs:9` — `public static string Text(`
- `Assets/Scripts/Runtime/GameBase/LocalizationManager/LocalizedTextFont.cs:30` — `class LocalizedTextFont`
- `Assets/Scripts/Editor/LocalizationEditor/PrefabTextCollectorWindow.cs:32` — `class PrefabTextCollectorWindow`

练习：选择物品名称，找出配置中的 NameKey、查词调用、界面显示和字体来源。再列出“缺键、空译文、缺字形”各应检查哪一层。

### 可复用轮子

可复用 key 组件、格式化文本绑定、语言字体映射和预制体收集器。抽取时统一查词接口与缺键诊断，明确动态文本的刷新协议；CSV 管线应先做往返校验，避免把工具约定隐含在列下标中。


---

## 10 单例、事件中心与 Mono 调度

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

这一层解决纯 C# 服务如何被找到、如何借用 Unity Update/协程，以及系统如何发送变化通知。三种单例基类、EventManager、MonoManager 的生命周期不同，应分别理解。

### 实现逻辑

`SingletonBase<T>` 使用 new() 延迟创建普通 C# 实例，不自动获得 Unity 生命周期。`SingletonMono<T>` 在场景组件 Awake 中登记实例，通过 GetInstance 取值；没有场景对象时不会自动生成。`SingletonAutoMono<T>` 才会创建 GameObject、AddComponent 并常驻。

MonoManager 持有 MonoController，把 Update 监听与协程能力提供给普通服务。例如 AudioManager 构造时注册 Update，用于清理播放结束的音效。服务本身不是 MonoBehaviour，不代表它没有帧循环成本。

EventManager 用字符串键映射 EventHandler，参数走 EventArgs，扩展方法可让 sender 直接 TriggerEvent。它是同步委托分发；不是消息队列，也没有持久化或订阅自动回收。项目还用普通 C# event，例如 MetaRuntimeDriver.Changed，不能把所有变化都归入 EventManager。

### 具体调用层

```text
业务代码 → SingletonBase<T>.Instance → 普通服务
普通服务 → MonoManager → MonoController.Update / 协程
sender.TriggerEvent → EventManager.TriggerEvent
  → 同一调用链内执行各 EventHandler
```

`EventCenter` 已标记 Obsolete(error:true)，应使用 EventManager；它保留在源码中不代表仍是可用公共入口。

### 优点

调用简短，纯 C# 服务能复用 Unity 帧循环；同步事件便于广播设置变化等通知。对小型项目而言接入成本低，阅读单个功能很直接。

### 缺点与边界

依赖隐藏于 Instance，单例状态会影响隔离测试和重进 Play。基类不提供线程安全或统一重置。事件键拼错可能静默无响应；异常、解除订阅与 Clear 的作用范围都由调用者承担。全局 Clear 会影响其他系统正在监听的事件。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/GameBase/Singleton/SingletonBase.cs:6` — `public class SingletonBase<T>`
- `Assets/Scripts/Runtime/GameBase/Singleton/SingletonMono.cs:9` — `public static T GetInstance()`
- `Assets/Scripts/Runtime/GameBase/Singleton/SingletonAutoMono.cs:10` — `public static T GetInstance()`
- `Assets/Scripts/Runtime/GameBase/MonoTool/MonoManager.cs:9` — `class MonoManager`
- `Assets/Scripts/Runtime/GameBase/EventCenter/EventManager.cs:29` — `public void AddListener(`

练习：分别说明三种单例首次访问时是否会产生 GameObject、何时实例可用、场景卸载后由谁清理。再为一个事件写出订阅与退订的配对位置。

### 可复用轮子

小型 Mono 调度桥和同步事件总线可按需抽出。新项目建议显式管理订阅令牌、异常策略与服务重置；尽量用类型化事件代替字符串，避免把所有服务都默认做成全局单例。迁移的是有限工具，不是整套依赖组织方式。


---

## 11 Resources 加载与对象池

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

ResManager 封装 Resources，同步或异步取得资源；GameObject 类型会直接实例化。PoolManager 在此基础上按名字复用场景对象。它们是轻量封装，没有资源引用计数或完整生命周期管理。

### 实现逻辑

`Load<T>` 对非 GameObject 返回资源，对 GameObject 返回新实例。这意味着调用者不能把它无条件理解为“只读加载原始资产”。`LoadAsync<T>` 使用 MonoManager 协程等待 ResourceRequest，再回调资源或实例。

PoolManager 以 name 为键，命中空闲列表时取出、激活、脱离池父节点；未命中时异步加载 `path + name`。回收时挂回分类父节点并禁用。对象的业务状态并未自动重置，因此速度、血量、监听和粒子等仍需调用方管理。

GetObj 的返回局部变量未赋予实际取出的对象，调用者应通过 callback 接收结果；不能写成 `var obj = GetObj(...)` 后立即使用。Clear 只清字典并置空池根引用，并未遍历销毁场景中的池对象。

### 具体调用层

```text
调用方 → ResManager.Load<T> → Resources.Load<T>
调用方 → PoolManager.GetObj(path, name, callback)
  → 有空闲：PoolData.GetPool → callback
  → 无空闲：ResManager.LoadAsync<GameObject> → callback
归还 → PoolManager.PushObj → PoolData.PushObj
```

相同 name 配合不同 path 仍使用同一个池键，可能造成类型或资源碰撞。场景卸载后单例字典中的 Unity 对象引用也需要重新检查。

### 优点

接口少，容易理解资源→实例→回收的基本流程。热路径能够复用对象，适合作为学习对象池机制的起点。

### 缺点与边界

异步加载中在 yield 之前检查 asset 可能产生过早告警。池采用 List 删除首元素，有移动成本；缺少容量、预热、重复归还保护、清理和 OnRent/OnReturn 协议。未统一取消过期异步回调，场景变化后回调仍可能创建对象。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/GameBase/ResourcesLoader/ResManager.cs:17` — `public T Load<T>(`
- `Assets/Scripts/Runtime/GameBase/ResourcesLoader/ResManager.cs:51` — `private IEnumerator LoadAsyncFunc<T>(`
- `Assets/Scripts/Runtime/GameBase/GameObjectPool/PoolManager.cs:56` — `public GameObject GetObj(`
- `Assets/Scripts/Runtime/GameBase/GameObjectPool/PoolManager.cs:108` — `public void Clear()`

练习：追踪首次取得与再次取得同名对象的两条时序。列出“上一场景已销毁”和“同一个对象归还两次”分别会污染哪些数据结构。

### 可复用轮子

可复用池的接口思路，当前实现不建议原样迁移。先修复返回语义，统一资源身份，使用 Queue/Stack，补租借回收钩子、容量、销毁和取消机制。若新项目使用 Addressables，应重写资源适配层而非同时维护两套资源所有权。


---

## 12 音频播放与四路音量

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

AudioManager 管理 BGM、游戏音效、UI 音效，并提供 Master、Music、Sfx、UI 四路音量。实现基于 AudioSource.volume 相乘，没有使用 AudioMixer 总线。

### 实现逻辑

四路设置采用 0～100 整数。实际音量为 Master/100 × 通道/100；因此 Master 为零时其他通道自然静音。改音量时重新作用于当前 BGM 和正在播放的所有音效，设置页可以立即试听。

BGM 延迟创建专用 AudioSource，异步加载后循环播放。音效在共用 SoundPlayer 上增加 AudioSource，并记录所属通道；Update 从后向前检查播放结束的项，销毁 Source 并移出列表。当前音效并没有接入 PoolManager，不应把它描述为音频对象池。

PlayUISound 单独使用 UI 通道，普通游戏音效使用 Sfx。旧浮点音量接口转换到整数通道接口，属于兼容入口。

### 具体调用层

```text
SettingsEditingModel.PreviewVolume / SettingsManager.ApplyAudio
  → AudioManager.SetVolume → ApplyVolumes → AudioSource.volume
PlayBGM / PlaySound / PlayUISound
  → ResManager.LoadAsync<AudioClip> → AudioSource.Play
  → MonoManager 的 Update → 回收结束的 AudioSource
```

取消设置试听时，由 SettingsManager 恢复 Applied 的通道值；AudioManager 本身不保存设置文件。

### 优点

总音量与分类音量的关系明确，接入成本低。通道信息跟随活跃音效，改设置能影响已播放声音。表现播放与设置存储职责分开。

### 缺点与边界

未提供混响、压缩、ducking 等混音总线能力。BGM 路径仍指向测试音乐常量，迁移时必须核对。声音 GameObject 本身未在此实现中统一常驻，纯 C# 单例仍持有旧引用时跨场景清理需关注。重复异步 PlayBGM 也缺少请求版本控制，回调先后可能覆盖预期曲目。

PauseSound 标记 Obsolete(error:true)，且没有实现；不能把它列为可用能力。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/GameBase/AudioManager/AudioManager.cs:120` — `private float EffectiveVolume(`
- `Assets/Scripts/Runtime/GameBase/AudioManager/AudioManager.cs:129` — `public void PlayBGM(`
- `Assets/Scripts/Runtime/GameBase/AudioManager/AudioManager.cs:183` — `public void PlayUISound(`
- `Assets/Scripts/Runtime/GameBase/AudioManager/AudioManager.cs:220` — `public void PauseSound(`

练习：计算 Master=50、Music=80 时的实际 volume；再说明为什么将 UI 音效走 Sfx 会让设置页的 UI 拉条显得失效。

### 可复用轮子

四路音量模型与实时应用逻辑可直接作为轻量音频模块的起点。迁移前补跨场景所有权、加载取消、空 clip 处理及并发请求版本；需要总线特效的新项目应保留外部接口，将后端切为 AudioMixer，而非不断增加每个 Source 的特判。


---

## 13 ChaosLog 日志与诊断

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

ChaosLog 将日志级别、模块通道、颜色、调用位置和堆栈格式统一起来，帮助沿一次操作追踪 UI、场景、配置和存档。LogBuilder 提供链式构造接口；LogBootstrap 负责运行时配置入口。

### 实现逻辑

输出先经过 Enabled、MinLevel、EnableDebug 等门槛；通道表示日志来自哪个模块。CallerMemberName、CallerFilePath、CallerLineNumber 由编译器补充调用点，避免业务代码手工填写位置。

Debug、Info、Success 等 void 入口带 Conditional 编译属性，调用在非编辑器、非开发包中可被编译器省略。Warn 和 Error 保留在正式包，再由运行时开关过滤。需要区分“调用根本未编译进去”和“调用存在但被过滤”两种情况。

Write 返回 LogBuilder，不能使用只允许 void 方法的 Conditional 机制，因此链式入口与便捷 void 入口的编译成本语义不同。字符串插值或昂贵对象 Dump 在调用处发生时，运行时过滤不一定避免构造成本。

### 具体调用层

```text
业务调用 ChaosLog.Info / Warn / Error / Write
  → 编译期条件（部分入口）
  → 运行时级别/开关过滤
  → 消息格式、通道、调用点/堆栈
  → Unity Debug 输出
```

日志记录“已进入某函数”与“后续保存成功”是不同证据。分析失败时应检查实际返回值和文件状态，不能只找到一条成功前的流程日志就结束。

### 优点

统一格式和通道提高定位效率，调用点自动生成；正式包保留告警和错误有利于排查。现有业务服务的 DumpState 可提供状态摘要，适合与关键日志共同阅读。

### 缺点与边界

静态全局开关可能影响整个工程的可观测性；链式与便捷接口的成本并不相同。当前不能据此推断已有远程收集、持久文件日志、去重限流或隐私脱敏流水线。传入对象上下文与调用点格式也需匹配 Unity Console 的定位方式。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/GameBase/LogManager/ChaosLog.cs:59` — `public static class ChaosLog`
- `Assets/Scripts/Runtime/GameBase/LogManager/LogBuilder.cs:54` — `struct LogBuilder`
- `Assets/Scripts/Runtime/GameBase/LogManager/LogBootstrap.cs:17` — `class LogBootstrap`
- `Assets/Scripts/Runtime/GameBase/LogManager/LogChannel.cs:12` — `enum LogChannel`

练习：分别预测编辑器、Development Build、正式包中 Info 与 Warn 是否存在；找出调用处插值和延迟构造消息的成本差别。

### 可复用轮子

ChaosLog 是较独立的迁移候选，可一起带走通道、颜色、Builder 与 Bootstrap。新项目需要重定通道和默认级别，明确正式包策略；如需文件或远程日志，可在输出端增加 sink 接口，避免业务代码直接依赖某个收集服务。


---

## 14 局外世界地图与势力区域

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

局外地图展示点位、时期和势力区域，提供管理与派遣等入口。它不是局内程序生成地图，也不是角色自动寻路地图。地图空间中的位置使用归一化坐标，和局内世界坐标是不同契约。

### 实现逻辑

WorldMapService 懒加载 WorldMap、WorldFaction 配置，缓存点位和按时期组织的区域。启用点位按 Layer 排序；势力顶点按 Stage、FactionId、RegionId 分组，再按 PointOrder 排成多边形。顶点不足的区域丢弃，缺失或损坏配置退化为空数据。

同一势力可有多块不相连领地。TryGetFactionAt 遍历区域，只要命中其中一块就返回 factionId 和具体 regionId，因此飞地无需连成一个多边形。若多个区域重叠，当前遍历的首次命中决定结果，不应假定有面积或优先级裁决。

地图点位只保存类别、引用和位置。IWorldMapMetaProvider 负责查名称和图标，当前默认实现恒返回 false，说明地点/兴趣点的完整元数据后端仍是占位。

### 具体调用层

```text
WorldMapPanel → WorldMapPresenter → WorldMapModel.Snapshot
  → WorldMapService.Points / Areas / TryGetFactionAt
  → JSON 配置 → WorldMapPoint / WorldFactionArea
显示元数据 → IWorldMapMetaProvider.TryGetMeta
```

WorldFactionMask、WorldMapPointIcon 等处理表现。编辑器 WorldMapTool 维护草稿并导出表，运行时服务消费导出产物，而不是直接读取编辑器草稿。

### 优点

空间配置、元数据和显示分离，支持时期变化与飞地。表缺失可退化，不会因一个 TextAsset 为空立即让面板崩溃。接口允许未来接入真正地点表。

### 缺点与边界

缓存返回 List，调用方误改会污染共享数据。区域重叠、边界命中与绘制先后有约定，不能根据视觉叠层推断逻辑归属。元数据占位意味着“界面能显示点”不等于地点玩法已经完成。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/GameBase/WorldMap/WorldMapService.cs:82` — `public bool TryGetFactionAt(`
- `Assets/Scripts/Runtime/GameBase/WorldMap/WorldFactionArea.cs:12` — `class WorldFactionArea`
- `Assets/Scripts/Runtime/GameBase/WorldMap/WorldMapMetaProvider.cs:14` — `public interface IWorldMapMetaProvider`
- `Assets/Scripts/Runtime/MVP/Model/WorldMap/WorldMapModel.cs:29` — `public WorldMapSnapshot Snapshot()`

练习：给同一 factionId 配两块 region，解释分组、绘制和点击查询为何都需要 regionId。再找出缺表时提示与空数据的边界。

### 可复用轮子

多区域势力模型、点位空间配置与元数据 provider 可迁移到策略地图或关卡选择界面。抽取前固定坐标与边界契约，改为只读集合，并明确重叠裁决；图标与背景贴图仍由新项目提供。


---

## 15 连续地图：计划、装配与验证

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

正式地图入口 RunMapBootstrap 当前使用 NaturalMapBuild，而不是旧 RunMapGenerator 的房间图生成主线。旧类仍保留部分验证、模板和兼容元数据用途；文件存在不代表当前正式流程正在调用。

### 实现逻辑

NaturalMapBuild 持有整局 seed、阶段、参数、结构模板、角色通行能力和内容扩展 pass。Steps 分阶段规划区域、生成连续地表、雕刻地下通道、嵌入手工结构、生成导航信息、安排内容并验证；Generate 是消费 Steps 的同步便捷入口。

随机性按用途分流，减少某种内容增减干扰其他生成结果。参数中的每帧工作预算影响调度，不应改变计划本身；现有 ContractVerification 包含针对预算与扩展的验证入口。重复生成的可复现性需同时固定种子、配置、角色能力、模板和算法版本，仅保留 seed 不足以永久还原。

NaturalMapPlan 是计划数据；NaturalMapAssembler 将其落成 Grid、Tilemap、碰撞、结构和占位内容。Bootstrap 在生成与装配成功后发布运行时计划，并创建旧 MapLayout 元数据供部分既有系统兼容读取。

### 具体调用层

```text
LoadingController → RunMapBootstrap.BuildIntoPendingSteps
  → LoadParameters / CaptureActorProfile / LoadStructures
  → NaturalMapBuild.Steps → NaturalMapValidation.Validate
  → NaturalMapAssembler.Steps → 场景碰撞校验
  → NaturalMapRuntime.Publish → RunEncounter 接管
```

IMapPopulationPass 用于扩展内容安排；MapInspectable、NPC、敌人等后续系统消费计划中的锚点。占位内容不自动拥有完整交易或剧情业务。

### 优点

计划与 Unity 装配分离，能先检查结构再落地；分帧步骤适合接入读条。角色通行能力进入生成条件，优于只保证几何上连通。独立随机流和稳定标识有助于回归比较。

### 缺点与边界

算法、导航与角色运动参数仍紧密关联；地图有效不意味着刚体实际运行必然成功。兼容新旧地图表示增加维护成本。回退尝试和验证必须看实际结果，不能把生成器有 fallback 理解为永不失败。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/GameBase/RunMap/RunMapBootstrap.cs:174` — `public static IEnumerable<string> BuildSceneSteps(`
- `Assets/Scripts/Runtime/GameBase/RunMap/NaturalMapGenerator.cs:47` — `public IEnumerable<string> Steps()`
- `Assets/Scripts/Runtime/GameBase/RunMap/NaturalMapData.cs:154` — `class NaturalMapPlan`
- `Assets/Scripts/Runtime/GameBase/RunMap/NaturalMapAssembler.cs:17` — `public IEnumerable<string> Steps(`
- `Assets/Scripts/Editor/RunMapTool/NaturalMapContractVerification.cs:10` — `class NaturalMapContractVerification`

练习：给每个生成阶段写出输入与输出。解释为何必须分别检查计划通行、装配碰撞与运行时角色通行；再说明改变工作预算为何不应改变 seed 对应布局。

### 可复用轮子

最值得抽取的是“确定性计划→分帧装配→运行时发布”流水线、随机流和内容 pass 契约。建议先抽纯计划与校验，再做 Unity Tilemap 适配。不要连同旧房间体系、Kenney 资源路径、战斗单例和本作配置 ID 整包迁移。


---

## 16 平台寻路与敌友 AI

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

本项目局内使用自定义 2D 平台通行图。RunMapPathfinder 负责求路径，RunMapNavigator 将路径变成刚体运动，EnemyPlaceholder 与 AllyController 决定目标和行为。安装了 AI Navigation 包不代表这条路径使用 NavMesh。

### 实现逻辑

Pathfinder 从可站立点建立按需缓存的邻接边，结合速度、跳跃力、重力和地图占据判断行走、跳跃或落差通行。TryPath 使用 Queue 和 visited 集合进行 BFS，并有展开预算、允许区域和最近点搜索。当前不是 A*；对不同代价动作也不提供加权最短时间保证。

缓存按地图和运动能力签名组织，角色能力不同可能得到不同的可达边。Navigator 管理当前路径、重规划、目标容差和卡住检测，再给 Rigidbody2D 施加横向速度和跳跃。安全恢复需要占据验证，不是任意传送到目标位置。

敌人有 Patrol、Chase、Windup、Recover、Return 状态，追击受视线和活动范围限制；前摇后再次判断是否能命中。友军跟随受控角色、选择敌人、攻击，并保留旧地图移动兼容分支。受击或禁用要中断待执行攻击。

### 具体调用层

```text
EnemyPlaceholder.UpdateAi / AllyController.Update
  → 选择目标与行为 → RunMapNavigator.MoveTo
  → RunMapPathfinder.For / TryPath / Neighbors
  → 刚体速度与跳跃 → RunMapPhysics 物理检查
```

CombatQuery 的视线/近战检查和导航可达是不同判据：能规划走到某人附近，不代表现在可以隔墙打中。

### 优点

路线与实际平台运动参数相关，敌友可复用导航执行层；BFS 有预算，邻接边缓存降低重复构图。前摇和恢复态让攻击具有可观察时序，也便于中断。

### 缺点与边界

BFS 的展开量与图规模有关，很多不同能力的单位可能增加缓存和计算。离散通行模型与物理碰撞存在误差；运行中参数变化、动态地形和狭窄边缘都要验证。类名 EnemyPlaceholder 表明仍带原型性质，不能据状态数量推断已有完整敌人行为框架。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/GameBase/RunMap/RunMapPathfinder.cs:68` — `public bool TryPath(`
- `Assets/Scripts/Runtime/GameBase/RunMap/RunMapNavigator.cs:54` — `public bool MoveTo(`
- `Assets/Scripts/Runtime/NPC_AI/EnemyPlaceholder.cs:236` — `private void UpdateAi()`
- `Assets/Scripts/Runtime/NPC_AI/AllyController.cs:298` — `private bool TryRecover(`

练习：沿 TryPath 中队列与 parents 还原一条路径，指出预算耗尽如何失败。比较“路径不可达、角色卡住、目标被墙阻挡”三种情况属于哪一层。

### 可复用轮子

可复用运动能力签名、通行图查询、导航执行器与行为状态机分层。迁移前把地图占据、物理查询和角色控制做成接口；对动态地形增加版本失效机制。AI 策略保留在业务层，避免导航模块依赖敌人名单和遭遇计数。


---

## 17 战斗、遭遇推进与队伍生命

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

PlayerAttack 处理受控角色攻击，IDamageable 表达受伤能力，CombatQuery 提供目标身体、视线与近战条件。RunEncounter 负责整局队伍、阶段、敌人和清场推进，多个 partial 文件是同一个静态类的职责拆分。

### 实现逻辑

玩家攻击按输入和冷却发起，在面前 OverlapBox 收集碰撞体，查找父级 IDamageable 并用集合去重，避免一个目标多个碰撞体重复受伤。友军和敌人有前摇、命中检查、恢复与受击中断。RunCombatFeedback 用线条表现攻击提示，不是完整技能动画系统。

正式进入阶段时，RunEncounter 校验准备好的自然地图种子与阶段一致，再生成角色和对应区域的敌人。必须在生成完成后才允许清场判断；否则敌人列表尚空就可能被误判为已经胜利。

敌人死亡通知先更新区域列表，统一 RefreshRooms 决定清区和清阶段。自然地图按区域组织，旧地图仍有 RoomNode 兼容分支。当前局最多三个阶段；推进下一阶段在独立时机处理，避免死亡调用链中立即重建全场景。

### 具体调用层

```text
PlayerAttack → IDamageable.TakeDamage → EnemyPlaceholder.Die
  → RunEncounter.NotifyEnemyKilled → 掉落 + 列表更新
  → RefreshRooms → OnSceneCleared → 下一阶段或结算
PlayerHealth 倒地 → RunEncounter.OnPlayerDowned
  → 观战/队伍全灭判定 → TriggerDefeat
```

队伍倒地与长期门客重伤分开：局内对象记录当前状态，退场/通关/失败时按规则转换为 RetainerService.Wound。失败路径使用全体门客处理的规则，应按当前代码理解，不能自行替换成仅当前出征者。

### 优点

伤害能力通过接口表达；攻击去重和统一遭遇推进减少重复结算。静态 partial 拆分方便按敌人、区域、队伍阅读。玩家倒地后仍能观战，菜单由局级驱动保持可用。

### 缺点与边界

RunEncounter 持有大量静态可变状态，重进、换阶段和换档必须正确清理。OverlapBoxAll 等查询会分配，复杂战斗还需优化。普通敌人、Boss 门槛与最终清场在当前进度中的联系具有原型约定，不能直接推断已有独立完整 Boss 系统。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/Player/PlayerAttack.cs:67` — `private void DoAttack()`
- `Assets/Scripts/Runtime/Combat/CombatQuery.cs:56` — `public static bool CanMelee(`
- `Assets/Scripts/Runtime/Player/RunEncounter.cs:72` — `private static bool EnterSceneInternal(`
- `Assets/Scripts/Runtime/Player/RunEncounter.Rooms.cs:50` — `private static void OnSceneCleared()`
- `Assets/Scripts/Runtime/Player/RunEncounter.Team.cs:122` — `public static int ApplyRunWounds(`

练习：画出最后一个敌人死亡到进入下一阶段的调用链。分别标记即时回调、后续 Tick 和场景加载，检查重复死亡通知如何影响掉落与清场。

### 可复用轮子

IDamageable、近战视线查询、单次攻击目标去重与分阶段遭遇控制值得抽取。新项目建议把静态局状态封装成一次 Run 的对象，注入生成和进度接口；把倒地、死亡、长期受伤的转换规则作为可替换业务策略。


---

## 18 统一交互入口与内容占位

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

RunInteractable 表达可交互对象，RunInteractionService 选择目标并再次校验执行条件，Presenter/Model 将交互输入与 HUD 连接。当前 MapInspectable 是一次检查、无奖励的占位实现，不代表 NPC 交易和剧情内容已经接入。

### 实现逻辑

交互物用 StableId 登记到静态注册表。重复 ID 直接报错；禁用时解除登记，并检查登记者身份，避免旧对象清掉新对象。StableId 与地图放置记录关联，比临时 GameObject 名字更适合确定性选择。

Select 先确认当前角色可控制，再遍历激活目标，按范围和视线筛选最近者，距离相同时按 StableId 排序，减少不稳定的枚举顺序影响。它不替代目标自己的 CanInteract 业务判断。

TryExecute 不信任之前的选择结果，再检查角色、目标注册状态、距离和视线，然后让目标执行 CanInteract→Perform。静态 executedFrame 限制同一帧重复执行，保护多个入口同时触发的情况。成功执行才增加 ExecutionCount。

### 具体调用层

```text
输入动作 → RunInteractionModel / Presenter
  → RunInteractionService.Select → HUD 提示
执行意图 → TryExecute → CanControl / 距离 / 视线
  → RunInteractable.Execute → CanInteract → Perform
```

CanControl 包括自然地图就绪、当前受控玩家、未过渡、未结算/失败、未暂停、未屏蔽及未倒地条件。这些保护属于共享执行入口，不能只在 HUD 隐藏提示时处理。

### 优点

选择、执行保护和具体内容分离，后续可增加采集、对话或机关子类。稳定 ID、执行时复核和同帧去重提高可预测性。UI 展示不直接发奖励。

### 缺点与边界

注册表逐个遍历，目标很多时需要空间索引。当前同帧限制是全局的，对多人或多个独立控制者未必适合。选择阶段可能选到业务上不可用但距离最近的对象，交互失败提示与候选优先级需结合需求设计。MapInspectable 的已检查标志只是运行时状态。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/GameBase/Interaction/RunInteractable.cs:18` — `public void Configure(`
- `Assets/Scripts/Runtime/GameBase/Interaction/RunInteractionService.cs:23` — `public RunInteractable Select(`
- `Assets/Scripts/Runtime/GameBase/Interaction/RunInteractionService.cs:38` — `public bool TryExecute(`
- `Assets/Scripts/Runtime/GameBase/Interaction/MapInspectable.cs:14` — `protected override bool Perform(`

练习：在选中目标后让其禁用或移到墙后，解释为何执行仍必须复核。再设计一个需要消耗钥匙的交互，指出扣钥匙应放在哪一层。

### 可复用轮子

交互基类、稳定注册表和选择/执行分离是很好的通用候选。迁移时将当前角色、视线、范围和暂停条件注入，按 actor 而非全局定义去重范围；可复用的是交互契约，不是占位物的具体业务。


---

## 19 库存、掉落、市场与带出结算

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

InventoryService 操作长期资源和物品；RunInventory 操作局内临时物品；InventoryRules 提供容量与堆叠算法。DropService 决定掉落，MarketService 处理交易，RunSettlementModel 将局内成果转换为长期进度。

### 实现逻辑

资源是 Payload 上的数值字段，物品是 itemId/count 堆叠列表。容量受基础格位、效果加成和物品 StackLimit 共同限制。InventoryRules 把“能否容纳/最多添加多少”与具体存档对象隔开，通过适配器支持字段式 ItemStackSave。

敌人死亡根据掉落组过滤 Enable 和 Weight，再以 runSeed 与 killSeq 构造随机源，组内加权选择一行，并抽取数量。可复现性依赖相同击杀序与相同表；只有相同 seed、不同击杀顺序时，不能保证每个敌人得到相同掉落。

市场买卖每次处理一个物品，检查价格、余额、库存和容量。执行前记录 Payload 快照，写盘失败恢复操作前状态。界面展示的 CanBuy 是提示，执行入口仍需要复核，不能用按钮灰显代替业务校验。

结算保存独立事务：成功时选择带出物品，失败时保留一部分物品；当前失败保留使用固定 System.Random 种子，不是从整局 seed 推导。Confirm 首次应用奖励后设置待保存，失败重试不再发一次奖。

### 具体调用层

```text
Enemy.Die → RunEncounter.NotifyEnemyKilled → DropService.RollForEnemy
  → 地面 RunDropItem → RunInventory.AddItem
MarketPanel → MarketPresenter → MarketModel → MarketService
  → InventoryService / InventoryRules → SaveProgress
SettlementPresenter → RunSettlementModel.Confirm
  → 长期库存 + 进度 → SaveProgress → 返回村庄
```

### 优点

长期与局内库存隔离，明确“拿到”和“带出”的区别；共享堆叠算法减少规则分叉。交易失败恢复与结算幂等分别保护扣费和奖励。

### 缺点与边界

库存列表可变，外部修改仍可绕过规则。掉落权重和数量使用 uint/int 转换，极端配置需要额外范围校验。结算把 AddItem 的失败原因丢弃，满库存时可能无法完整带出；这属于需验证的实现边界，不应把所有奖励描述为原子提交。

结算的选择集合按 itemId 组织，理解“件数上限”时应看实际按种类/堆叠处理的语义，不要自动理解为逐个数量选择。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/GameBase/Meta/InventoryRules.cs:100` — `public static uint MaxAddableStacks(`
- `Assets/Scripts/Runtime/GameBase/Meta/RunInventory.cs:47` — `public static bool AddItem(`
- `Assets/Scripts/Runtime/GameBase/Meta/DropService.cs:49` — `public bool RollForEnemy(`
- `Assets/Scripts/Runtime/GameBase/Meta/MarketService.cs:225` — `private static bool SaveTrade(`
- `Assets/Scripts/Runtime/MVP/Model/Gameplay/RunSettlementModel.cs:72` — `public bool Confirm(`

练习：比较“堆叠没满但格位满”和“相同物品已达上限”的添加结果。再追踪保存失败后连续点两次结算，为何不会双倍加 clears，但仍可能有入库失败问题。

### 可复用轮子

InventoryRules 的容量预估与实际添加配套算法值得优先抽取，交易事务和加权掉落也可独立。新项目需要将定义查询、随机源和存储注入；结算先预演所有入库结果并明确部分成功策略，再作为通用奖励模块复用。


---

## 20 门客、派遣与扎营

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

RetainerService 管理长期门客实例与定义，DispatchService 管理出征选择和进出局流程，CampService 操作当前局的治疗和补员。长期门客记录与场景中的 AllyController 不是同一个对象。

### 实现逻辑

RetainerConfig 描述原型，RetainerSave 记录实例 id、protoId、生命、信心和重伤剩余秒数。可派遣判断结合重伤、生命和信心等状态；最终属性读取效果聚合器加成。恢复重伤采用运行中剩余秒数递减，旧 UTC 时间字段仍保留但不作为离线恢复依据。

派遣选择不等于已出发。Depart 再次检查队伍大小、门客是否存在及可用性，写 runTeam 和 runs，再尝试保存与场景过渡。正式路径切 LoadingScene；TestScene 仍保留就地生成原型。首次保存失败会恢复 Payload，过渡启动失败也尝试恢复并补写。

扎营从 RunInventory 扣药。治疗恢复受控角色 HP；补员需要对应效果解锁，恢复本局倒地友军至一半最大生命向上取整。已经判负后不能借补员逆转结算。CampUsed 是运行时限制；当前 RunEncounter 在第 1 阶段初始化和清局时调用 ResetForNewRun，不是在每个阶段都重置。应以调用点而非旧注释判断实际寿命。

### 具体调用层

```text
RetainerPresenter → RetainerRecruitmentModel → RetainerService
DispatchPresenter → DispatchService.Toggle / Depart
  → 门客复核 → Payload → SaveProgress → RunSceneFlow
CampPresenter → CampService.CanHeal / CanRefill
  → 扣局内药品 → PlayerHealth / AllyController.Revive
```

招募、派遣、扎营都有各自事务/运行时状态；不能用一次 UI 刷新替代这些状态持久性判断。

### 优点

原型定义和玩家实例分离，队伍出发时复核可减少过期选择。长期受伤与局内倒地分开，使通关、失败、撤离可以映射不同规则。

### 缺点与边界

服务互相通过单例与静态列表关联，换档需统一清理。过渡失败后的补写仍可能失败，不能承诺磁盘一定恢复原状态。扣药遍历列表并移除物品的细节、CampUsed 的实际重置次数都应有定向检查；原型分支不能作为正式流程验收替代。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/GameBase/Meta/RetainerService.cs:216` — `public bool IsDeployable(`
- `Assets/Scripts/Runtime/GameBase/Meta/RetainerService.cs:262` — `public void Tick(`
- `Assets/Scripts/Runtime/GameBase/Meta/DispatchService.cs:129` — `public bool Depart(`
- `Assets/Scripts/Runtime/Player/CampService.cs:155` — `public static bool TryRefill(`
- `Assets/Scripts/Runtime/MVP/Model/Gameplay/RetainerRecruitmentModel.cs:7` — `class RetainerRecruitmentModel`

练习：列出门客“可派遣、局内倒地、长期重伤”的不同判据。搜索 ResetForNewRun 的全部调用，推导扎营次数到底在哪些时机重置。

### 可复用轮子

角色定义/实例分离、派遣时再校验与剩余秒计时可复用。把队伍、招募策略、治疗目标和物品消耗抽成接口；新项目不要继承本作固定人数、信心门槛或重伤规则。跨场景进入失败的补偿策略需先明确再抽成通用流程。


---

## 21 实体村庄、建筑与生产行为树

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

MainScene 保存固定地面与四块地块，VillageSceneBootstrap 根据存档创建玩家、建筑表现、工人和 HUD。VillageService 管建筑规则与生产数据，VillageProductionNpc 表现工人行为，VillageWorkforce 管排他工作权。

### 实现逻辑

启动先校验配置、Tilemap 范围和世界坐标、地块数量和位置，防止配置与固定场景几何失配后仍进入 Ready。四块地块对应 2001 开始的固定建筑编号，当前并不是任意地块数的通用城市系统。

建筑建造/升级使用 remainSeconds，MetaRuntimeDriver 把帧时间累计为整秒再推进。生产另走现场工人路径：ReactiveSelector 选择工作、休息或闲逛；工作分支用 Guard 和 Sequence 获取岗位、走到入口、执行班次。禁用或退出分支时释放岗位和工作状态。

VillageWorkforce 只允许一个 owner 占有一栋建筑。CanProduce 要求当前在就绪村庄、有存档、不在出征、owner 匹配且 Working。TickProduction 累计实际工作秒数，完成周期后加 pendingOutput；Harvest 只取已有产物，不凭点击生成新产量。

### 具体调用层

```text
MainScene.Start → VillageSceneBootstrap → 校验地面/地块 → 创建角色和工人
VillageProductionNpc.Update → 行为树 → WorkShift
  → VillageWorkforce.CanProduce → VillageService.TickProduction
  → BuildingSave.productionSeconds / pendingOutput
VillagePresenter / VillageWorldPresenter → VillageService.Execute
  → Build / Upgrade / Harvest → SaveProgress → 失败恢复
```

建造计时和生产计时不同，前者由全局驱动递减，后者必须现场实际工作。当前不做离线生产补算。

### 优点

规则、岗位所有权和 NPC 表现分离；排他岗位避免重复 NPC 放大产量。生产积累与收取拆开，减少重复点击发奖。启动时几何校验能尽早暴露固定场景过期。

### 缺点与边界

建筑 ID、四块地和 NPC 出生分配有硬编码。工人运动、Spine 动画和标签绑定 Unity；行为树仅有本任务需要的少量节点，未提供完整可视化编辑器、黑板和节点序列化。静态岗位注册表需要随场景退出清理。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/GameBase/Village/VillageSceneBootstrap.cs:77` — `public static void ValidateSceneLayout(`
- `Assets/Scripts/Runtime/GameBase/Village/VillageWorkforce.cs:21` — `public static bool CanProduce(`
- `Assets/Scripts/Runtime/NPC_AI/Production/VillageBehaviorTree.cs:39` — `public sealed class ReactiveSelector`
- `Assets/Scripts/Runtime/NPC_AI/Production/VillageProductionNpc.cs:47` — `private Status WorkShift(`
- `Assets/Scripts/Runtime/GameBase/Meta/VillageService.cs:276` — `public void TickProduction(`

练习：跟踪两个工人争同一岗位时谁能生产。解释暂停、出征、NPC 禁用、建筑升级分别如何影响剩余计时、岗位和待收产物。

### 可复用轮子

小型行为树核心、带 owner 的排他岗位，以及“工作累计→待收产物→事务收取”值得抽取。行为树节点基本不依赖 Unity，适合先迁移；生产规则应注入时钟与岗位校验。固定地块、Spine 表现和建筑编号留给新项目重新接线。


---

## 22 家族树、效果聚合、时代与难度

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

FamilyTreeService 控制节点解锁；EffectAggregator 将已解锁来源转换为统一效果快照；MetaProgressService 管时代、深度和里程碑；EraConfigService 提供难度参数，FactionService 提供本局阵营标签和倍率入口。

### 实现逻辑

节点解锁依次检查定义、启用、是否已解锁、前置与资源。成功后扣资源、把 nodeId 加入 Payload.familyTreeNodes，标脏效果并重算。Unlock 本身没有直接写文件，当前 FamilyTreePresenter.Execute 也只是调用解锁再刷新；这条操作需依赖后续自动保存或手动保存，不具有市场交易那样“立即写盘失败就恢复”的同等保证。

聚合过程按 FamilyTreeEffect 关联已解锁节点与 Effect，本作针对格位、带出上限、门客属性、扎营补员和农田建造/产量分别处理，部分预留类型尚未消费。Snapshot 是缓存的整份重算结果，MarkDirty 后下次访问更新；不能据注释把它当成支持任意 Buff 的通用引擎。

深度上限是当前时代；时代 1～5 只能向前，推进需要本局最终阶段清理标记与局外里程碑共同达标。推进后清除本局门槛标记，防止一次通关连续跨多个时代。难度从 Era/Depth 表取得；阵营倍率目前恒 1.00，不能重复当作已启用的难度放大项。

### 具体调用层

```text
FamilyTreePresenter → FamilyTreeService.CanUnlock / Unlock
  → Payload 节点集合 → EffectAggregator.MarkDirty → Snapshot
  → InventoryService / RetainerService / VillageService / CampService
结算 → MetaProgressService.MarkBossCleared / TryAdvanceDepth / TryAdvanceEra
  → EraConfigService 的门槛与参数 → SaveProgress
```

### 优点

效果只有一处聚合，减少背包、角色和建筑各算一遍导致的分歧。时代/深度约束在运行时执行，配置不能直接突破边界。快照版本有助于识别重算与刷新。

### 缺点与边界

效果类型和 Target 由数值约定，农田编号写死，新增效果要补 switch 与消费者。所谓只读快照的字段仍公开可变。Recompute 会通过加载函数读配置，没有充分依据把注释中的“表已在内存”当成零重复解析保证。解锁和保存的原子边界需要查上层，不能凭 Unlock 返回成功就承诺持久化。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/GameBase/Meta/FamilyTreeService.cs:81` — `public bool Unlock(`
- `Assets/Scripts/Runtime/MVP/Presenter/Gameplay/FamilyTreePresenter.cs:31` — `protected override void Execute(`
- `Assets/Scripts/Runtime/GameBase/Meta/EffectAggregator.cs:69` — `public MetaEffectSnapshot Recompute()`
- `Assets/Scripts/Runtime/GameBase/Meta/MetaProgressService.cs:118` — `public bool TryAdvanceEra()`
- `Assets/Scripts/Runtime/GameBase/Meta/EraConfigService.cs:97` — `public float GetFinalDifficulty(`
- `Assets/Scripts/Runtime/GameBase/Meta/FactionService.cs:59` — `public static float GetFactionMul(`

练习：追踪一个背包加格节点从表到 TotalSlots 的路径。再解释时代推进前后 depth、cap 和本局门槛标记如何变化。

### 可复用轮子

依赖节点解锁、脏标记聚合和规则约束/配置参数分离可复用。抽取时先把效果定义缓存、来源查询与计算规则独立，把快照改为真正受控的只读结构；将建筑 ID 和效果 Target 改为项目自定义策略，避免把整套枚举数字复制到新项目。


---

## 23 剧情事件队列与对话播放

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

EventService 是“条件达成后弹出事件并发奖励”的业务服务；DialogueManager 是“逐句文字、角色、选项和打字机”的播放组件。两套系统并存，当前不能把后者的存在视为已统一接入前者。

### 实现逻辑

EventService 当前识别三种触发：出征次数、最高已通深度、已解锁家族节点。返乡时检查，按 Priority 降序、Index 升序排序，最多收集 5 个；队列非空时不重复收集，一次只展示一个。一次性事件写 firedEvents，可重复事件使用本轮检查的运行时去重集合。

TryChooseOption 验证选项属于当前事件，记录操作前 Payload，再按资源→物品→门客→信心发放。单项业务奖励失败不回滚其他成功奖励，但整个结果写盘失败时恢复 Payload。保存成功后记住 resolvedEventIndex，结果页继续或重复点击不应再次发奖。TryCompleteCurrent 保存完成后才移出队首。

DialogueManager 直接操作 TMP、头像、选项按钮和协程。nextDialogueId 当前按条目索引字符串查找，并不是真正稳定内容 ID；语音播放代码仍被注释。它使用旧 Input.GetKey/GetKeyDown，并在结束时把 timeScale 设为 1，不能安全推断与所有暂停系统兼容。

### 具体调用层

```text
返乡完成 → EventService.CheckAndShowOnReturnToMeta
  → CheckAndCollect → EventPanel / EventPresenter
  → TryChooseOption → ApplyOption → RecordDisposition → SaveProgress
  → 结果页 → TryCompleteCurrent → 下一事件
独立对话：StartDialogue → PlayEntry → TypeText → 选项/下一条 → EndDialogue
```

### 优点

事件触发、排序、一次性记录和发奖集中管理，避免每帧重复弹窗；已解决状态保护结果页重复发奖。对话播放的数据与显示流程有基本分离，容易看懂打字机实现。

### 缺点与边界

事件规则数量固定，扩展需要代码；奖励的部分成功策略可能不适合所有业务。旧对话的索引跳转、旧输入和暂停恢复都限制了复用，当前语音并未真正播放。配置中写了事件名不表示已有业务处理器执行完整效果。

### 源码导读与学习练习

- `Assets/Scripts/Runtime/GameBase/Meta/EventService.cs:196` — `public List<EventConfig> CheckAndCollect()`
- `Assets/Scripts/Runtime/GameBase/Meta/EventService.cs:307` — `public bool TryChooseOption(`
- `Assets/Scripts/Runtime/GameBase/Meta/EventService.cs:350` — `public bool TryCompleteCurrent(`
- `Assets/Scripts/Runtime/GameBase/DialogueManager/DialogueManager.cs:374` — `private int FindEntryIndexById(`

练习：比较“背包满导致一项奖励失败”和“整个事件写盘失败”的恢复语义。再说明为什么对话结束直接 timeScale=1 会破坏另一个系统仍持有的暂停。

### 可复用轮子

事件的有序队列、一次性记录与已解决结果缓存较值得复用。抽取时注入条件与奖励处理器，明确事务策略。对话播放应先改稳定 ID、统一输入、可组合暂停令牌和真实音频适配，再作为独立叙事模块迁移。


---

## 24 编辑器工具、生成产物与验证体系

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

### 系统定位

Scripts/Editor 中的工具承担内容生产和静态/场景验证；QA 目录保存部分检查脚本和历史结果，GameTest 与 GM 提供运行中测试入口。工具“存在”和当前版本“通过测试”是两件事。

### 实现逻辑

WorldMapToolWindow 编辑草稿，通过 Geometry、RegionTree 和 Validator 组织区域、形状与错误，再由 XlsxExporter 输出配置表。草稿与导出表是不同产物，修改草稿后需走正确导出与运行时配置更新链。

本地化工具负责收集、编辑、CSV 与合并，InputIcon 工具维护设备键位图标映射；UIPanelAnimatorTool 和 SceneUIOverviewTool 辅助调整动画及场景 UI 显示。IconImportPostprocessor 与 IconAtlasBuilder 处理图标资产规范。DialogueEditorWindow 编辑旧对话数据，GitTool 是编辑器内的仓库辅助入口。

VillageSceneBuilder 按配置重建固定村庄；NaturalMapVerification、ContractVerification、RunBehaviorVerification 提供地图多种子、确定性契约和行为检查；VillageVerification 对配置和保存的场景几何做检查。这些生成工具可能写入场景、预制体或表格，阅读学习不需要执行它们。

### 具体调用层

```text
EditorWindow / MenuItem → 草稿/参数 → 校验 → 生成/导出资产
  → 运行时 JSON / Prefab / Scene → 正式业务入口
QA 静态检查 → 检查分层或配置契约
运行中 GM/QA → 触发正式函数 → 观察状态/文件/场景
```

验证分三层：源码边界和字段契约、纯计划与确定性、Unity 场景与实际输入/物理。某一层通过不能替代另外两层。历史 QA 记录只证明当时的样本，需核对版本、场景、种子和是否恢复测试环境。

### 优点

工具链降低手工资产维护成本，配置/几何校验把问题前移。分层检查能够将“代码错、配置错、场景错、物理执行错”分别定位。现有 QA 为后续回归提供入口参考。

### 缺点与边界

部分大型 EditorWindow 同时承担状态、绘制和导出，修改范围大。输出资产、工作表与运行时 JSON 可能不同步，缓存也需要 Reload。历史记录不是当前成功证据；本资料库本轮只做静态核查，没有运行地图千种子或 Player 构建。

### 源码导读与学习练习

- `Assets/Scripts/Editor/WorldMapTool/WorldMapToolXlsxExporter.cs:46` — `class WorldMapToolXlsxExporter`
- `Assets/Scripts/Editor/WorldMapTool/WorldMapToolValidator.cs:42` — `class WorldMapToolValidator`
- `Assets/Scripts/Editor/RunMapTool/NaturalMapVerification.cs:13` — `class NaturalMapVerification`
- `Assets/Scripts/Editor/VillageSceneTool/VillageVerification.cs:13` — `class VillageVerification`
- `Assets/QA/MVP/check_boundaries.py:9` — `def code_only(text):`

练习：挑一个生成工具，列出它读哪些文件、写哪些文件、影响哪个运行时入口。再为“配置改了但游戏不变”列出导出、资源引用与服务缓存三个检查点。

### 可复用轮子

优先抽取纯 Geometry、区域树与 Validator，再把 EditorWindow 做成薄壳；资产生成工具保留预览、变更清单与可恢复输出。验证工具应接受显式输入和隔离输出，避免硬编码本作目录、测试槽位和场景名。不要为了学习资料生成而运行重建工具。
