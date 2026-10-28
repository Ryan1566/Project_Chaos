# 03 MVP：界面流程与事务生命周期

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

## 系统定位

MVP 的价值是把“如何显示”与“操作意味着什么”分开。当前 View 通常仍负责创建控件、组装 Model 与 Presenter；Presenter 是纯 C#，不应持有 Unity 按钮、直接读文件或负责 Resources 加载。

## 实现逻辑

`PresenterBase.Attach` 幂等，并在挂接失败时执行清理；`Dispose` 幂等，调用 OnDetach 后清空 View。一个已销毁 Presenter 不再复用。`IPanelView<TState,TIntent>` 通过 Intent 事件输入，通过 Render 状态输出。

`GameplayPresenter<TState>` 把刷新、关闭和业务动作分开：刷新只投影显示，业务动作进入 Execute。以市场为例，Model 读取定义、余额和堆叠条件，Presenter 把阻塞原因映射成文本，View 按状态渲染。

面板被覆盖会 OnExit，恢复时再 OnEnter，因此 Presenter 的生命常比业务事务短。设置的 pending、结算的物品选择和已发奖标志不能跟随 Presenter 重新创建。按钮也应只绑定一次，并向 View 的事件发意图，避免闭包捕获已 Dispose 的 Presenter。

## 具体调用层

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

## 优点

意图和显示状态让流程比散落的按钮 lambda 更容易阅读。成对订阅清理减少重复响应。把事务与面板连接分开，可以防止重新显示界面就再次扣钱或发奖。

## 缺点与边界

Presenter 仍会直接依赖部分单例服务和本地化入口，尚未全面依赖注入。通用 `PanelIntent` 的 action/id 对复杂操作的表达有限。State 中存在可变 DTO，不能把所有状态都称为严格不可变快照。

## 源码导读与学习练习

- `Assets/Scripts/Runtime/MVP/Presenter/PresenterBase.cs:11` — `public void Attach(`
- `Assets/Scripts/Runtime/MVP/Presenter/Gameplay/GameplayPresenter.cs:27` — `public abstract class GameplayPresenter`
- `Assets/Scripts/Runtime/MVP/Presenter/Gameplay/MarketPresenter.cs:46` — `protected override void Execute(`
- `Assets/Scripts/Runtime/MVP/Model/Gameplay/RunSettlementModel.cs:36` — `public static void Begin(`

练习：把“被覆盖后恢复结算页”画成时序图，标出哪些对象重建、哪些状态必须保留。确认 Render 与 Attach 路径没有发奖。

## 可复用轮子

`PresenterBase`、类型化 View 契约、Attach/Dispose 配对和长寿命事务模式值得独立提取。新项目建议先给业务服务定义窄接口，再注入 Presenter/Model；保留本项目生命周期约束，移除具体 PanelType、本地化键和单例耦合。
