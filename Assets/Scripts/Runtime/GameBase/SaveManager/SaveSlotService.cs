using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ChaosDebug;
using UnityEngine;

/// <summary>
/// 玩家存档读写的【唯一入口】。固定 3 个档位（1/2/3），永不重排。
///
/// ══════════════ 它和 Recorder 是什么关系 ══════════════
/// **没有关系，两条互不相交的链路**（见 `04_架构/05_存档槽位系统落地方案.md` §4）：
///   · `Recorder`   = Excel「Data 模式」的**导出产物**链路，写 `Application.dataPath`
///                    （打包后只读；且 `ForceSave` 只回写磁盘已存在的键）→ 其重构已排期，**本次不动**。
///   · 本类         = **玩家存档**，写 `Application.persistentDataPath`（打包后仍可写）。
/// 正确范例是 `SettingsManager.cs:46-49`：`Path.Combine(Application.persistentDataPath, ...)`。
/// → **玩家存档不得经过 `Recorder`。**
///
/// ══════════════ 错误处理约定 ══════════════
/// 所有失败**返回 false / 空视图 + `ChaosLog.Error`**，**绝不把异常抛给 UI**
/// （面板只负责显示，不该被 IO 异常打断）。唯一的例外是"槽位号非法"，那是编程错误。
///
/// ══════════════ 空槽 vs 损坏槽（语义必须分清） ══════════════
///   · **空槽** = 文件不存在 → `IsEmpty=true`，**不是错误**，不报 Error。
///   · **损坏槽** = 文件在但解析不了 → 也算空（可被覆盖），但 `IsCorrupt=true`：
///     报 `Error`、**允许玩家删除**（给他一个恢复手段）、覆盖前面板要做二次确认 ——
///     否则玩家会看到"一个看起来是空的档位被直接盖掉"。
/// </summary>
public class SaveSlotService : SingletonBase<SaveSlotService>
{
    /// <summary>存档目录的完整路径：`persistentDataPath/Saves`（Windows 例：`%USERPROFILE%\AppData\LocalLow\DefaultCompany\Project_Chaos\Saves`）。</summary>
    public static string SavesDirectory
    {
        get { return Path.Combine(Application.persistentDataPath, GlobalPath.save_SlotDir); }
    }

    /// <summary>槽位号是否合法（1..GlobalPath.save_SlotCount）。</summary>
    public static bool IsValidSlot(int slot)
    {
        return slot >= 1 && slot <= GlobalPath.save_SlotCount;
    }

    /// <summary>
    /// 取某个槽位的存档文件完整路径。**在内部做 1..3 参数校验**（方案 §决策 8）：
    /// 越界时返回 null 并报 Error，而不是拼出一个野路径让调用方去踩。
    /// </summary>
    public string GetSlotPath(int slot)
    {
        if (!IsValidSlot(slot))
        {
            ChaosLog.Error(LogChannel.Save,
                "槽位号非法：" + slot + "（合法范围 1.." + GlobalPath.save_SlotCount + "）");
            return null;
        }

        string fileName = GlobalPath.save_SlotFilePrefix + slot + ".json";
        return Path.Combine(SavesDirectory, fileName);
    }

    /// <summary>确保存档目录存在（照 `SettingsManager.cs:144-145` 的写法）。目录建不出来就没法写。</summary>
    private static bool EnsureDirectory()
    {
        string dir = SavesDirectory;
        try
        {
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return true;
        }
        catch (Exception e)
        {
            ChaosLog.Error(LogChannel.Save, "创建存档目录失败：" + dir + " → " + e.Message);
            return false;
        }
    }

    // ══════════════════ 读 ══════════════════

    /// <summary>
    /// 读全部档位，**恒定返回 GlobalPath.save_SlotCount（3）项、按槽位号 1..3 顺序**。
    /// 面板直接用这个列表刷 3 个格子；顺序由槽位号决定，与磁盘上的文件顺序无关。
    /// </summary>
    public List<SaveSlotInfo> ReadAllSlots()
    {
        var list = new List<SaveSlotInfo>(GlobalPath.save_SlotCount);
        for (int slot = 1; slot <= GlobalPath.save_SlotCount; slot++)
        {
            list.Add(ReadSlot(slot));
        }

        WarnAboutForeignFiles();
        return list;
    }

    /// <summary>读单个档位的视图。文件不存在 = 空槽（不是错误）；解析失败 = 损坏槽（报 Error、按空处理）。</summary>
    public SaveSlotInfo ReadSlot(int slot)
    {
        var info = new SaveSlotInfo
        {
            Slot = slot,
            IsEmpty = true,
            IsCorrupt = false,
            LastSaveUtcTicks = 0,
        };

        string path = GetSlotPath(slot);
        info.FilePath = path;
        if (path == null) return info;//槽位号非法，GetSlotPath 已报错

        if (!File.Exists(path)) return info;//空槽：正常状态，不报错

        string json;
        try
        {
            json = File.ReadAllText(path, Encoding.UTF8);
        }
        catch (Exception e)
        {
            ChaosLog.Error(LogChannel.Save,
                "槽 " + slot + " 存档读取失败（按空槽处理，可删除）：" + path + " → " + e.Message);
            info.IsCorrupt = true;
            return info;
        }

        SaveSlotFile parsed;
        try
        {
            parsed = JsonUtility.FromJson<SaveSlotFile>(json);
        }
        catch (Exception e)
        {
            //JsonUtility 对结构性错误会抛（例如 {"meta":是}）——这正是"损坏槽"要兜住的情况
            ChaosLog.Error(LogChannel.Save,
                "槽 " + slot + " 存档损坏：Json 解析失败（按空槽处理，可删除）：" + path + " → " + e.Message);
            info.IsCorrupt = true;
            return info;
        }

        if (parsed == null || parsed.meta == null)
        {
            ChaosLog.Error(LogChannel.Save,
                "槽 " + slot + " 存档损坏：缺少 meta 节点（按空槽处理，可删除）：" + path);
            info.IsCorrupt = true;
            return info;
        }

        info.IsEmpty = false;
        info.LastSaveUtcTicks = parsed.meta.lastSaveUtcTicks;

        //payload 缺失只算"老存档"（JsonUtility 会补默认构造），不判损坏：
        //这保证了"只加字段、零迁移"的纪律真的成立
        if (parsed.payload == null)
        {
            ChaosLog.Warn(LogChannel.Save,
                "槽 " + slot + " 存档没有 payload 节点，按默认 payload 处理（老存档）：" + path);
        }

        return info;
    }

    /// <summary>读取一个档位的完整内容。成功返回 true；空槽/损坏/IO 失败都返回 false 并留日志。</summary>
    public bool TryLoad(int slot, out SaveSlotFile file)
    {
        file = null;

        string path = GetSlotPath(slot);
        if (path == null) return false;

        if (!File.Exists(path))
        {
            ChaosLog.Warn(LogChannel.Save, "槽 " + slot + " 是空档位，没有可读取的存档：" + path);
            return false;
        }

        string json;
        try
        {
            json = File.ReadAllText(path, Encoding.UTF8);
        }
        catch (Exception e)
        {
            ChaosLog.Error(LogChannel.Save, "槽 " + slot + " 存档读取失败：" + path + " → " + e.Message);
            return false;
        }

        SaveSlotFile parsed;
        try
        {
            parsed = JsonUtility.FromJson<SaveSlotFile>(json);
        }
        catch (Exception e)
        {
            ChaosLog.Error(LogChannel.Save, "槽 " + slot + " 存档解析失败（已损坏）：" + path + " → " + e.Message);
            return false;
        }

        if (parsed == null || parsed.meta == null)
        {
            ChaosLog.Error(LogChannel.Save, "槽 " + slot + " 存档解析结果为空或缺少 meta（已损坏）：" + path);
            return false;
        }

        if (parsed.payload == null) parsed.payload = new SavePayload();

        file = parsed;
        ChaosLog.Info(LogChannel.Save,
            "槽 " + slot + " 已读取：slotIndex=" + parsed.meta.slotIndex +
            " version=" + parsed.meta.version +
            " lastSaveUtcTicks=" + parsed.meta.lastSaveUtcTicks);
        return true;
    }

    // ══════════════════ 写 ══════════════════

    /// <summary>
    /// 把一份 payload 写进指定槽位（覆盖写）。时间戳由本方法统一打（`DateTime.UtcNow.Ticks`），
    /// 调用方不要自己传时间 —— 统一出处才能保证"文件里的时间"与"写盘时刻"一致。
    /// 失败返回 false + Error；调用方（面板）在失败时**不得切场景**。
    /// </summary>
    public bool Save(int slot, SavePayload payload)
    {
        if (!IsValidSlot(slot))
        {
            ChaosLog.Error(LogChannel.Save,
                "槽位号非法，拒绝写入：" + slot + "（合法范围 1.." + GlobalPath.save_SlotCount + "）");
            return false;
        }

        string path = GetSlotPath(slot);
        if (path == null) return false;

        if (!EnsureDirectory()) return false;

        SavePayload body = payload ?? new SavePayload();
        if (body.version <= 0) body.version = SavePayload.CurrentVersion;

        var file = new SaveSlotFile
        {
            meta = new SaveSlotMeta
            {
                slotIndex = slot,
                //UTC ticks —— 用 long 而不是 DateTime，因为 JsonUtility 不支持 DateTime（见 SaveSlotData 注释）
                lastSaveUtcTicks = DateTime.UtcNow.Ticks,
                version = SavePayload.CurrentVersion,
            },
            payload = body,
        };

        try
        {
            string json = JsonUtility.ToJson(file, true);
            File.WriteAllText(path, json, new UTF8Encoding(false));
            ChaosLog.Info(LogChannel.Save, "槽 " + slot + " 已写入：" + path);
            return true;
        }
        catch (Exception e)
        {
            ChaosLog.Error(LogChannel.Save, "槽 " + slot + " 写入失败：" + path + " → " + e.Message);
            return false;
        }
    }

    /// <summary>
    /// 新建一个档位 = 写入一份**全新的空 payload**。
    ///
    /// ⚠ 刻意实现为 `Save(slot, new SavePayload())` 的**便捷包装**，而不是另写一条写盘路径 ——
    /// 否则两条路径会各自演化（方案 §决策 8 的具体意见）。
    /// </summary>
    public bool CreateNew(int slot)
    {
        return Save(slot, new SavePayload());
    }

    /// <summary>删除档位对应的文件。文件本就不存在时视为成功（幂等）。</summary>
    public bool Delete(int slot)
    {
        if (!IsValidSlot(slot))
        {
            ChaosLog.Error(LogChannel.Save,
                "槽位号非法，拒绝删除：" + slot + "（合法范围 1.." + GlobalPath.save_SlotCount + "）");
            return false;
        }

        string path = GetSlotPath(slot);
        if (path == null) return false;

        try
        {
            if (!File.Exists(path))
            {
                ChaosLog.Info(LogChannel.Save, "槽 " + slot + " 本就是空的，无需删除：" + path);
                return true;
            }

            File.Delete(path);
            //存档目录里的 .meta 之类不归我们管；这里只删存档文件本身
            ChaosLog.Info(LogChannel.Save, "槽 " + slot + " 已删除：" + path);
            return true;
        }
        catch (Exception e)
        {
            ChaosLog.Error(LogChannel.Save, "槽 " + slot + " 删除失败：" + path + " → " + e.Message);
            return false;
        }
    }

    // ══════════════════ 选中策略 ══════════════════

    /// <summary>
    /// 找出第一个**确实有存档文件**的档位（有效存档或损坏存档都算"有文件"）。
    ///
    /// ⚠ 为什么损坏槽也算"有文件"而不是被跳过：方案 R8 要求**损坏槽必须可被删除**
    /// （给玩家一个恢复手段）。而"可删除"的前提是它能被选中（删除按钮作用于选中槽位），
    /// 所以它必须参与默认选中。语义上它仍 `IsEmpty=true`（不可读取、可被覆盖），
    /// 只是"占着这个槽位"。
    ///
    /// 全空时返回 1（方案 §决策 6：全空 → 槽 1，且删除按钮禁用）。
    /// </summary>
    public int FindFirstNonEmptySlot()
    {
        for (int slot = 1; slot <= GlobalPath.save_SlotCount; slot++)
        {
            if (HasSaveFile(ReadSlot(slot))) return slot;
        }
        return 1;
    }

    /// <summary>
    /// 该槽位上是否存在存档文件（**有效存档或损坏存档都算**）。
    ///
    /// 用途：决定删除按钮是否可用、以及默认选中谁。
    ///   · 有效存档 → true（可读取、可删除）
    ///   · 损坏存档 → true（不可读取，但**必须可删除**，R8）
    ///   · 完全空槽 → false（没什么可删）
    /// </summary>
    public static bool HasSaveFile(SaveSlotInfo info)
    {
        if (info == null) return false;
        return !info.IsEmpty || info.IsCorrupt;
    }

    // ══════════════════ 目录卫生 ══════════════════

    /// <summary>
    /// 只认 `Slot{1..3}.json`；目录里其它 `.json` 文件名**警告并忽略**（方案 §决策 5）。
    /// 存在的意义：玩家/别的工具往目录里塞了文件时，日志里能看出"我们忽略了一个东西"，
    /// 而不是让存档凭空少一格。
    /// </summary>
    private static void WarnAboutForeignFiles()
    {
        string dir = SavesDirectory;
        if (!Directory.Exists(dir)) return;

        try
        {
            string[] files = Directory.GetFiles(dir, "*.json");
            for (int i = 0; i < files.Length; i++)
            {
                string name = Path.GetFileNameWithoutExtension(files[i]);
                if (IsKnownSlotFileName(name)) continue;

                ChaosLog.Warn(LogChannel.Save,
                    "存档目录中发现无法识别的文件，已忽略：" + Path.GetFileName(files[i]) +
                    "（本系统只认 " + GlobalPath.save_SlotFilePrefix + "1.." +
                    GlobalPath.save_SlotFilePrefix + GlobalPath.save_SlotCount + ".json）");
            }
        }
        catch (Exception e)
        {
            ChaosLog.Warn(LogChannel.Save, "扫描存档目录失败：" + dir + " → " + e.Message);
        }
    }

    /// <summary>不带扩展名的文件名是否是本系统认识的槽位文件（Slot1 / Slot2 / Slot3）。</summary>
    private static bool IsKnownSlotFileName(string nameWithoutExtension)
    {
        if (string.IsNullOrEmpty(nameWithoutExtension)) return false;
        if (!nameWithoutExtension.StartsWith(GlobalPath.save_SlotFilePrefix, StringComparison.Ordinal)) return false;

        string digits = nameWithoutExtension.Substring(GlobalPath.save_SlotFilePrefix.Length);
        int slot;
        return int.TryParse(digits, out slot) && IsValidSlot(slot);
    }
}
