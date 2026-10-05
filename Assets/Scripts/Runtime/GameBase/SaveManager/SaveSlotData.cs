using System;

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
