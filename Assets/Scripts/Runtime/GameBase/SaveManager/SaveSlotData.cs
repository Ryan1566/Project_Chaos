using System;
using System.Collections.Generic;

/// <summary>
/// 玩家存档的落盘结构定义 —— 【唯一】的存档结构真相源。
///
/// ══════════════ 为什么单独一个文件 ══════════════
/// 落盘结构只在这里定义一处（见 `04_架构/05_存档槽位系统落地方案.md` §3 扩展纪律②）：
/// 玩法系统**不得**自建第二条存档路径，要加玩法数据就往 <see cref="SavePayload"/> 加字段。
///
/// ══════════════ JsonUtility 的两条硬约束 ══════════════
/// 1. **必须是 `[Serializable]` + public 字段**。属性（property）会被 JsonUtility **静默丢弃** ——
///    写成属性不会报错，只会发现"存进去读出来是默认值"。
/// 2. **不要出现 `DateTime` 字段**：JsonUtility 不支持它。时间一律存 `long`（UTC ticks）。
///
/// ══════════════ 追加字段 = 零迁移（关键机制，别改成"手写迁移"） ══════════════
/// `JsonUtility.FromJson` 的语义是「**先默认构造、再用 Json 里出现的字段覆盖**」，
/// 所以**老存档里缺失的新字段会保留字段初始化值**，不会变成 0/null。
/// → 加玩法字段时**只加字段即可**，老存档照样能读（`SettingsData.cs:13-18` 已写明同一机制）。
/// → 但**不要改已有字段的语义**：那属于不兼容变更，必须 `version + 1` 并补迁移分支。
/// </summary>
[Serializable]
public class SaveSlotMeta
{
    /// <summary>槽位号（1..GlobalPath.save_SlotCount）。写进文件是为了让存档自身可识别。</summary>
    public int slotIndex;

    /// <summary>
    /// 最后保存时刻，**UTC ticks**（`DateTime.UtcNow.Ticks`）。
    /// 显示时由面板转本地时区格式化为 `yyyy/MM/dd HH:mm` —— 数据层不做 UI 文案/格式（方案 §决策 4）。
    /// 用 ticks 而不是 DateTime：JsonUtility 不支持 DateTime。
    /// </summary>
    public long lastSaveUtcTicks;

    /// <summary>存档结构版本。改字段**语义**时 +1 并补迁移；只加字段不用动它。</summary>
    public int version;
}

/// <summary>
/// 玩法存档数据（M15：局外计时器、家族树、库存、时代进度等后续加在这里）。
/// 本轮只落地槽位壳，所以只有一个 version 字段。
/// </summary>
[Serializable]
public class SavePayload
{
    /// <summary>当前结构版本。新增字段不需要改它；改变已有字段语义才 +1。</summary>
    public const int CurrentVersion = 1;

    /// <summary>写入时的结构版本，供将来做不兼容迁移判断。</summary>
    public int version = CurrentVersion;

    /// <summary>
    /// **当前时代（时期）**：1~5，与 `WorldFaction` 表的 `Stage` 同义
    /// （1 战国末期 / 2 秦朝 / 3 秦末起义初期 / 4 楚汉争霸 / 5 汉刘邦时期）。
    /// 局外世界观地图按这个值筛选要画的势力范围。
    ///
    /// ⚠ **追加字段 = 零迁移**：`JsonUtility.FromJson` 是「先默认构造、再用 Json 里出现的字段覆盖」，
    /// 所以老存档里没有 `era` 时会保留这里的初始化值 **1**，不会变成 0（见本文件顶部注释）。
    /// **不要改 `version` 的语义** —— 只加字段不需要 +1。
    /// </summary>
    public uint era = 1;
    // ══════════════════ W0 追加（M15 / M13 / M13b）══════════════════
    //⚠ **追加字段 = 零迁移**（见本文件顶部）：老存档没有这些字段时保留初始化值，不需要迁移代码。
    //   注意：只加字段。**改已有字段语义**才需要 version + 1。

    /// <summary>当前深度（M13b）：1~era。**上限 = 当前时代序号**，由 `MetaProgressService` 截断。</summary>
    public uint depth = 1;

    /// <summary>局外资源：材料（W1 资源表落地前先占位）。</summary>
    public uint resMaterial;

    /// <summary>局外资源：图纸。</summary>
    public uint resBlueprint;

    /// <summary>局外资源：货物。</summary>
    public uint resGoods;

    /// <summary>局外资源：钱币。</summary>
    public uint resMoney;

    /// <summary>门客（W0 用最小占位结构 `RetainerSave`，字段待 W1 定）。</summary>
    public List<RetainerSave> retainers = new List<RetainerSave>();

    /// <summary>建筑（W0 用最小占位结构 `BuildingSave`，字段待 W1 定）。</summary>
    public List<BuildingSave> buildings = new List<BuildingSave>();

    /// <summary>已点亮的家族树节点 ID（M3；效果靠 FamilyTreeEffect 关联，不在此存效果）。</summary>
    public List<uint> familyTreeNodes = new List<uint>();

    /// <summary>本局派遣队伍（门客 id 列表；出发时写入，零迁移）。</summary>
    public List<uint> runTeam = new List<uint>();

    /// <summary>进入局内的次数（M15 局内计数）。</summary>
    public uint runs;

    /// <summary>累计通关数（M13 里程碑 Type=1 的统计量）。</summary>
    public uint clears;

    /// <summary>累计通关过的场景数。</summary>
    public uint scenesCleared;

    /// <summary>历史最深通关深度（原型期统计；只写不清）。</summary>
    public uint maxClearedDepth = 0;

    /// <summary>库存堆叠（itemId → count）。**不用 Dictionary**（JsonUtility 不支持）。</summary>
    public List<ItemStackSave> items = new List<ItemStackSave>();

    /// <summary>
    /// **W5.1 新增（M4 剧情事件）**：已处置的**一次性**事件 ID 列表（存的是 `EventConfig.Index`）。
    ///
    /// ⚠ **追加字段 = 零迁移**（见本文件顶部）：老存档没有 `firedEvents` 时保留这里的
    /// `new List&lt;uint&gt;()`，即"还没触发过任何事件"，不报错、不需要迁移代码。
    /// 可重复事件（`Repeatable=true`）**不写这里**，它靠运行时不落盘的会话集合去重。
    /// </summary>
    public List<uint> firedEvents = new List<uint>();
}

/// <summary>一个存档文件的根对象（= 落盘的整个 Json）。</summary>
[Serializable]
public class SaveSlotFile
{
    public SaveSlotMeta meta;
    public SavePayload payload;
}

/// <summary>
/// 槽位的**运行时视图**（不落盘、不进 Json）。
///
/// 为什么和落盘结构分开：UI 需要的"这个格子现在该显示什么"是**派生态**
/// （文件存不存在 / 解析成不成功 / 时间是多少），把它塞进落盘结构会让存档
/// 带上大量与玩法无关的字段。
///
/// ⚠ 本类**不含任何 UI 文案**（方案 §决策 4）：空槽显示什么字、时间怎么格式化，
/// 全部由面板决定；数据层只给事实。
/// </summary>
public class SaveSlotInfo
{
    /// <summary>槽位号（1..3）。</summary>
    public int Slot;

    /// <summary>是否视为空（文件不存在，或文件损坏也按空处理）。</summary>
    public bool IsEmpty;

    /// <summary>
    /// 文件存在但解析失败（内容坏了 / 结构不对）。
    /// 语义上仍算空槽（可被覆盖），但**必须允许玩家删除**（给一个恢复手段），
    /// 且覆盖前应二次确认 —— 详见面板的 IsCorrupt 分支。
    /// </summary>
    public bool IsCorrupt;

    /// <summary>最后保存的 UTC ticks；空槽或损坏槽为 0。</summary>
    public long LastSaveUtcTicks;

    /// <summary>存档文件的完整路径（即使文件不存在也给出来，便于排查）。</summary>
    public string FilePath;

    /// <summary>便捷判断：非空且未损坏，才允许"直接读取进入游戏"。</summary>
    public bool HasUsableSave { get { return !IsEmpty && !IsCorrupt; } }
}

/// <summary>
/// 门客存档（**W0 最小占位**：W1 定字段后再往上加 —— 只加字段，零迁移）。
/// ⚠ JsonUtility 限制：只 public 字段、不要属性/DateTime/Dictionary。
/// </summary>
[Serializable]
public class RetainerSave
{
    public uint id;
    public uint quality;
    public int hp;
    public int confidence;
    public bool wounded;
    /// <summary>重伤到期时刻，**UTC ticks**（JsonUtility 不支持 DateTime）。0 = 未重伤。</summary>
    public long woundedUntilUtcTicks;

    /// <summary>模板外键 → `RetainerConfig.Index`（表→存档的外键；0 = 老存档/占位）。</summary>
    public uint protoId;

    /// <summary>重伤剩余秒数（**原型期真正读写的字段**；只按运行中时间递减）。</summary>
    public uint woundedRemainSeconds;
}

/// <summary>建筑存档（**W0 最小占位**：W1 定字段后再往上加）。</summary>
[Serializable]
public class BuildingSave
{
    public uint id;
    public uint buildingType;
    public uint level;
    public bool completed;
    /// <summary>完工时刻，**UTC ticks**。0 = 未在建造。</summary>
    public long finishUtcTicks;

    /// <summary>建造/升级剩余秒数（**原型期真正读写的字段**；只按运行中时间递减）。</summary>
    public uint remainSeconds;
}

/// <summary>库存堆叠（**W1a 新增**）：一种道具占一格，count 是该种的数量。</summary>
[Serializable]
public class ItemStackSave
{
    /// <summary>外键 → `ItemConfig.Index`。</summary>
    public uint itemId;

    /// <summary>该种道具持有的数量（上限 = `ItemConfig.StackLimit`）。</summary>
    public uint count;
}
