using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;

/// <summary>
/// 读 `EraConfig` / `DepthConfig`（导出生成的类），对外只给"算难度"与"查里程碑"两件事。
///
/// ⚠ **表缺失时不得抛异常**：回退占位值并 Warn（原型阶段允许表还没落地）。
/// ⚠ 不走 `ConfigLoader.LoadData`：它在 Json 不存在时会直接解引用 null（NRE），
///   这里自己 `Resources.Load` + 判空，路径仍取 `GlobalPath.data_JsonPathToRead`（单一真相源）。
/// </summary>
public class EraConfigService : SingletonBase<EraConfigService>
{
    /// <summary>表缺失/越界时的回退基础难度（与 07 文档示例同源）。</summary>
    private const uint FallbackBaseDifficulty = 10;

    private List<EraConfig> _eraRows;
    private List<DepthConfig> _depthRows;
    private bool _loaded;

    public int EraRowCount { get { EnsureLoaded(); return _eraRows == null ? 0 : _eraRows.Count; } }
    public int DepthRowCount { get { EnsureLoaded(); return _depthRows == null ? 0 : _depthRows.Count; } }

    public void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        _eraRows = LoadTable<EraConfig>("Era");
        _depthRows = LoadTable<DepthConfig>("Depth");
    }

    /// <summary>强制重读（GM/导出后热更用）。</summary>
    public void Reload()
    {
        _loaded = false;
        _eraRows = null;
        _depthRows = null;
        EnsureLoaded();
    }

    private static List<T> LoadTable<T>(string tableName) where T : class
    {
        string path = GlobalPath.data_JsonPathToRead + tableName + "Config";
        TextAsset asset = Resources.Load<TextAsset>(path);
        if (asset == null)
        {
            ChaosLog.Warn(LogChannel.Config, "[Era/Depth] 找不到 " + path + "（回退占位值，不抛异常）");
            return null;
        }

        DataList<T> list = JsonUtility.FromJson<DataList<T>>(asset.text);
        if (list == null || list.datas == null || list.datas.Count == 0)
        {
            ChaosLog.Warn(LogChannel.Config, "[Era/Depth] " + path + " 解析为空（回退占位值）");
            return null;
        }
        return list.datas;
    }

    /// <summary>`Era` 表里该时代的基础难度档；查不到回退占位 + Warn。</summary>
    public uint GetBaseDifficulty(uint era)
    {
        EnsureLoaded();
        if (_eraRows != null)
        {
            for (int i = 0; i < _eraRows.Count; i++)
            {
                EraConfig row = _eraRows[i];
                if (row != null && row.Enable && row.Era == era) return row.BaseDifficulty;
            }
        }

        ChaosLog.Warn(LogChannel.Config, "[Era/Depth] 查不到 Era=" + era + " 的启用行，回退占位难度 " + FallbackBaseDifficulty);
        return FallbackBaseDifficulty;
    }

    /// <summary>`Depth` 表里该(时代,深度)的难度增量；查不到回退占位公式 + Warn。</summary>
    public float GetDepthBonus(uint era, uint depth)
    {
        EnsureLoaded();
        if (_depthRows != null)
        {
            for (int i = 0; i < _depthRows.Count; i++)
            {
                DepthConfig row = _depthRows[i];
                if (row != null && row.Enable && row.Era == era && row.Depth == depth) return row.DifficultyDelta;
            }
        }

        float fallback = (depth - 1) * 2f + (era - 1);
        ChaosLog.Warn(LogChannel.Config,
            "[Era/Depth] 查不到 Era=" + era + "/Depth=" + depth + " 的启用行，回退占位增量 " + fallback);
        return fallback;
    }

    /// <summary>最终难度 = `Era.BaseDifficulty + Depth.DifficultyDelta`（**加法**，07 文档 K2）。</summary>
    public float GetFinalDifficulty(uint era, uint depth)
    {
        return GetBaseDifficulty(era) + GetDepthBonus(era, depth);
    }

    /// <summary>该时代的里程碑类型（1=累计通关数 / 2=家族树已点亮节点数；查不到按 1）。</summary>
    public uint GetMilestoneType(uint era)
    {
        EnsureLoaded();
        if (_eraRows != null)
        {
            for (int i = 0; i < _eraRows.Count; i++)
            {
                EraConfig row = _eraRows[i];
                if (row != null && row.Enable && row.Era == era) return row.MilestoneType;
            }
        }
        return 1u;
    }

    /// <summary>该时代进入下一时代的里程碑阈值（查不到按 0 = 不设门槛，并 Warn）。</summary>
    public uint GetMilestoneValue(uint era)
    {
        EnsureLoaded();
        if (_eraRows != null)
        {
            for (int i = 0; i < _eraRows.Count; i++)
            {
                EraConfig row = _eraRows[i];
                if (row != null && row.Enable && row.Era == era) return row.MilestoneValue;
            }
        }

        ChaosLog.Warn(LogChannel.Config, "[Era/Depth] 查不到 Era=" + era + " 的里程碑阈值，按 0 处理");
        return 0u;
    }

    /// <summary>该时代的本地化 key（`era_0X_name`；查不到回退拼串）。</summary>
    public string GetNameKey(uint era)
    {
        EnsureLoaded();
        if (_eraRows != null)
        {
            for (int i = 0; i < _eraRows.Count; i++)
            {
                EraConfig row = _eraRows[i];
                if (row != null && row.Enable && row.Era == era) return row.NameKey;
            }
        }
        return "era_0" + era + "_name";
    }
}
