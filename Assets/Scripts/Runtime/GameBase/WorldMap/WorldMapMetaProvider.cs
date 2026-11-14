using UnityEngine;

/// <summary>
/// 点位名称和图标的兼容查询接口。位置来自绘图表，业务内容来自地点配置。
/// 完整详情由IWorldLocationMetaProvider按时代及身份提供。
/// </summary>
public interface IWorldMapMetaProvider
{
    /// <summary>
    /// 查 `(kind, type, refId)` 对应的名称与图标。
    /// 查不到（或表还没落地）返回 **false**，此时 <paramref name="name"/> 与 <paramref name="icon"/> 均为 null。
    /// </summary>
    bool TryGetMeta(uint kind, uint type, uint refId, out string name, out Sprite icon);
}

/// <summary>地点详情的时代及身份查询，与旧名称图标接口兼容。</summary>
public interface IWorldLocationMetaProvider : IWorldMapMetaProvider
{
    WorldLocationSnapshot GetLocationMeta(uint era,uint refId,WorldLocationIdentity identity);
}

/// <summary>正式地点表提供名称和图标；缺失PNG时仍可返回名称。</summary>
public class WorldMapMetaProvider : IWorldLocationMetaProvider
{
    public WorldLocationSnapshot GetLocationMeta(uint era,uint refId,WorldLocationIdentity identity)
        =>WorldLocationService.Instance.GetLocation(era,refId,identity);
    public bool TryGetMeta(uint kind, uint type, uint refId, out string name, out Sprite icon)
    {
        name = null;
        icon = null;
        if(kind!=1&&kind!=2)return false;
        uint era=GameSession.Instance.LoadedPayload?.era??1;
        var place=GetLocationMeta(era,refId,WorldLocationIdentity.Family);
        if(place==null)return false;
        name=place.Name;
        if(!string.IsNullOrEmpty(place.IconKey))icon=GameplayUiSkin.Location(place.IconKey);
        return !string.IsNullOrEmpty(name);
    }
}
