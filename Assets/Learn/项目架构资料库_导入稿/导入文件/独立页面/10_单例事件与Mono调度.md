# 10 单例、事件中心与 Mono 调度

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

## 系统定位

这一层解决纯 C# 服务如何被找到、如何借用 Unity Update/协程，以及系统如何发送变化通知。三种单例基类、EventManager、MonoManager 的生命周期不同，应分别理解。

## 实现逻辑

`SingletonBase<T>` 使用 new() 延迟创建普通 C# 实例，不自动获得 Unity 生命周期。`SingletonMono<T>` 在场景组件 Awake 中登记实例，通过 GetInstance 取值；没有场景对象时不会自动生成。`SingletonAutoMono<T>` 才会创建 GameObject、AddComponent 并常驻。

MonoManager 持有 MonoController，把 Update 监听与协程能力提供给普通服务。例如 AudioManager 构造时注册 Update，用于清理播放结束的音效。服务本身不是 MonoBehaviour，不代表它没有帧循环成本。

EventManager 用字符串键映射 EventHandler，参数走 EventArgs，扩展方法可让 sender 直接 TriggerEvent。它是同步委托分发；不是消息队列，也没有持久化或订阅自动回收。项目还用普通 C# event，例如 MetaRuntimeDriver.Changed，不能把所有变化都归入 EventManager。

## 具体调用层

```text
业务代码 → SingletonBase<T>.Instance → 普通服务
普通服务 → MonoManager → MonoController.Update / 协程
sender.TriggerEvent → EventManager.TriggerEvent
  → 同一调用链内执行各 EventHandler
```

`EventCenter` 已标记 Obsolete(error:true)，应使用 EventManager；它保留在源码中不代表仍是可用公共入口。

## 优点

调用简短，纯 C# 服务能复用 Unity 帧循环；同步事件便于广播设置变化等通知。对小型项目而言接入成本低，阅读单个功能很直接。

## 缺点与边界

依赖隐藏于 Instance，单例状态会影响隔离测试和重进 Play。基类不提供线程安全或统一重置。事件键拼错可能静默无响应；异常、解除订阅与 Clear 的作用范围都由调用者承担。全局 Clear 会影响其他系统正在监听的事件。

## 源码导读与学习练习

- `Assets/Scripts/Runtime/GameBase/Singleton/SingletonBase.cs:6` — `public class SingletonBase<T>`
- `Assets/Scripts/Runtime/GameBase/Singleton/SingletonMono.cs:9` — `public static T GetInstance()`
- `Assets/Scripts/Runtime/GameBase/Singleton/SingletonAutoMono.cs:10` — `public static T GetInstance()`
- `Assets/Scripts/Runtime/GameBase/MonoTool/MonoManager.cs:9` — `class MonoManager`
- `Assets/Scripts/Runtime/GameBase/EventCenter/EventManager.cs:29` — `public void AddListener(`

练习：分别说明三种单例首次访问时是否会产生 GameObject、何时实例可用、场景卸载后由谁清理。再为一个事件写出订阅与退订的配对位置。

## 可复用轮子

小型 Mono 调度桥和同步事件总线可按需抽出。新项目建议显式管理订阅令牌、异常策略与服务重置；尽量用类型化事件代替字符串，避免把所有服务都默认做成全局单例。迁移的是有限工具，不是整套依赖组织方式。
