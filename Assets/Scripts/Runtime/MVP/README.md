# 运行时 MVP

界面输入通过强类型意图进入 Presenter；Presenter 调用业务 Model／既有玩法服务，把结果投影为展示快照交给 View。

```text
View（控件、布局、输入、动画）
  ↓ Intent                     ↑ Render(snapshot)
Presenter（交互流程、展示映射、导航）
  ↓                          ↑ 结果／变化通知
Model（业务状态、事务、规则） → GameBase（存档、配置、输入、音频等基础能力）
```

- `Presenter/PresenterBase.cs`：纯 C#，构造时不操作 View；`Attach` 幂等，`Dispose` 成对退订。销毁后不复用同一 Presenter。
- `Presenter/Gameplay/`：库存、市场、门客、派遣、剧情、村庄、家族树、时代、扎营、结算及队伍／深度 HUD。交互面板实现 `IGameplayView<TState>`，按钮发 `PanelIntent`。
- `Presenter/Startup/`、`SaveSlots/`、`WorldMap/`、`Settings/`：各入口的强类型 View 契约、操作意图和 Presenter。
- `Model/Gameplay/`：市场报价条件、招募保存事务、结算事务、战斗控制适配、HUD数据读取，以及跨子面板覆盖保留的局内菜单暂停状态。
- `Model/Settings/`：设置编辑事务；`Model/SaveSlots/`：槽位操作；`Model/WorldMap/`：地图数据；`Model/Startup/`：启动相关应用动作。
- `View/`：现有面板与Prefab脚本继续使用原类型、GUID及序列化字段；`RunTerrainToast`是被动显示效果。
- `Model/ConfigData/`、`Model/ModelData/`：导出器生成目录，禁止手写业务类。

View在构建控件后，于`OnEnter`创建并Attach Presenter；`OnExit`同步Dispose，`OnDestroy`兜底。UIManager覆盖旧面板也会调用OnExit，恢复时重新OnEnter。按钮绑定一次，lambda向View的Intent事件发送意图，不捕获可能已经Dispose的旧Presenter。

业务事务的生命周期由业务决定。结算Model跨界面覆盖保留已选物品、奖励已应用及待保存状态；设置Model跨覆盖保留pending，仅实际退出才提交／回滚。Render、重新Attach均不得发奖、扣费或重置事务。

Presenter不持有Unity控件，不继承MonoBehaviour，不读取Resources、文件或PlayerPrefs；Model不管理UI栈、不引用具体View／Presenter。View可完成界面组合与模型／Presenter构造接线，业务命令通过Presenter执行。`GameBase/Meta`既有玩法服务本轮继续复用为业务后端，未进行批量改名迁移；新手写状态／事务放在Model下。

Settings行控件沿用原DTO字段绑定委托，通过Presenter访问编辑区并通知变更；完整不可变字段快照不是本轮已完成的能力。运行时验收见`Assets/Chaos_Story/Story/05_程序/07_MVP分层改造与验收.md`；边界检查：`python Assets/QA/MVP/check_boundaries.py`（从工程根运行）。

正式开发入口为 `Assets/Scenes/EnterScene.unity`。局外地图提供管理、保存和派遣入口；局内右上角行囊/菜单进入 `RunMenuPanel`，其 `RunMenuModel` 在扎营覆盖时继续持有暂停状态，真正关闭时恢复。GM保留测试环境配置入口。迁移与验收见 `Assets/Chaos_Story/Story/05_程序/08_EnterScene正式玩法入口迁移.md`。

玩法输入统一读取`InputManager`克隆的动作表，和Settings编辑区共用绑定覆盖；UIManager和GM分别持有输入屏蔽，角色禁用时清理待处理输入。暂停/观战快捷导航由局级`GameplayShortcutDriver`驱动；View不直接轮询设备。输入重定向与验收见`Assets/Chaos_Story/Story/05_程序/09_玩法输入重定向与验收.md`。
