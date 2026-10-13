using UnityEngine;

/// <summary>
/// 点位**元数据（名称 / 图标）**查询接口。
///
/// ══════════════ 为什么要有这层接口 ══════════════
/// 世界观地图表 (`WorldMap`) **只存"在哪、什么类别、指向谁"**，不存名称与图标 ——
/// 那是**地点表 / 兴趣点表**的职责（见 `03_地点表与兴趣点表_接口约定.md`）。
/// 那两张表**本次尚未落地**，所以现在用一个恒返回 false 的占位实现，
/// 面板据此退化成"只显示 `(Kind, Type, RefId)` 三元组"。
///
/// 等表落地后：**只需换一个实现**（或在这里加分支），`WorldMapPanel` 一行都不用改。
/// </summary>
public interface IWorldMapMetaProvider
{
    /// <summary>
    /// 查 `(kind, type, refId)` 对应的名称与图标。
    /// 查不到（或表还没落地）返回 **false**，此时 <paramref name="name"/> 与 <paramref name="icon"/> 均为 null。
    /// </summary>
    bool TryGetMeta(uint kind, uint type, uint refId, out string name, out Sprite icon);
}

/// <summary>
/// **MVP 占位实现**：地点表 / 兴趣点表尚未落地 → **恒返回 false**。
/// 于是面板只会显示三元组，不会有图标资产依赖（也就不会因为缺图标而报错）。
/// </summary>
public class WorldMapMetaProvider : IWorldMapMetaProvider
{
    public bool TryGetMeta(uint kind, uint type, uint refId, out string name, out Sprite icon)
    {
        name = null;
        icon = null;
        return false;
    }
}
