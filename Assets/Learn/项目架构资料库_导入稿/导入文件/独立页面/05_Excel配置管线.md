# 05 Excel 配置、生成代码与运行时加载

> 项目：Project Chaos；核查日期：2026-10-26；依据：当前本地源码静态核查。未在本轮执行 Unity 运行回归。

## 系统定位

配置管线把策划表转换为 Unity 可读取的 JSON 与 C# 字段类。它与玩家存档是两条路径；Excel 导出的 `Data` 类也不等于 MVP 中手写的业务 Model。

## 实现逻辑

`ConfigManager` 使用 EPPlus 读取 xlsx 的第一个工作表。第 3 行备注，第 4 行字段名，第 5 行端标记，第 6 行类型，第 7 行开始数据。导出 JSON 包装为 `datas` 列表，由 `DataList<T>` 对应读取。

Config 模式写 `Resources/Data/Json/Runtime` 和 `MVP/Model/ConfigData`；Data 模式写 Records 与 `ModelData`。类名、表名和读取路径依赖命名约定，文件名在下划线前截断。手写业务逻辑应避开生成目录，否则重新导出可能覆盖。

类型转换不是“Excel 能写什么就支持什么”。导出器有类型白名单；bool 归一化为 JSON 字面值。string 分支直接包双引号，特殊字符转义需要格外检查。数值型分支也不能当作完整的数值范围校验器。

## 具体调用层

```text
xlsx → ConfigManager.ExportConfigs / ExportModels
  → ExportJson + ExportClass → JSON / C# 字段类
  → ConfigLoader.LoadData<T> → ResManager → Resources
  → JsonUtility.FromJson<DataList<T>> → 业务 Service
```

部分业务服务自行做安全加载，例如 WorldMapService 在表缺失时返回空集；基础 ConfigLoader 的同步入口直接访问 TextAsset.text，缺失时可能空引用。两种错误策略不可混为一谈。

## 优点

定义和字段类来自同一表格，减少手工维护结构的重复劳动。配置、生成类、业务规则有分工，便于追踪某个字段如何进入玩法。

## 缺点与边界

手工拼 JSON、固定行号、单工作表和命名截断都限制了扩展性。导出器位于 Runtime 目录且含编辑器相关引用；实际构建边界需要独立检查，目录名不保证 Player 安全。本次不运行导出，避免覆盖现有产物。

## 源码导读与学习练习

- `Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigManager.cs:182` — `private static void ExportJson(`
- `Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigManager.cs:364` — `private static string Convert(`
- `Assets/Scripts/Runtime/GameBase/ConfigManager/Config/ConfigLoader.cs:18` — `public DataList<T> LoadData<T>()`
- `Assets/Scripts/Runtime/Path&Type/GlobalPath.cs:147` — `public const string data_JsonPathToRead`

练习：挑一行 BuildingConfig，从表格字段对应到 JSON、生成类、VillageService 的消费位置。再设计含引号与换行的字符串用例，说明应在哪一层发现错误。

## 可复用轮子

可复用“表头契约→代码生成→运行时 DTO”的管线思路。抽取前优先改用可靠 JSON 序列化、增加结构及引用校验、隔离 Editor 代码，并将输出路径和类型映射配置化。EPPlus 是外部依赖，迁移需单独核查所用版本与许可。
