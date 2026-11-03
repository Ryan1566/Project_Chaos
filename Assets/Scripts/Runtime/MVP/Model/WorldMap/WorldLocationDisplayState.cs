using System;
using System.Collections.Generic;

// ══════════════════ 展示契约（全局命名空间，避免集成歧义） ══════════════════

/// <summary>地点面板会向 Presenter 发出的意图类型。View 本身不执行业务。</summary>
public enum WorldLocationUiAction
{
    Select, Official, Family, Build, Harvest, Resolve, Buy, Sell, OpenOffice, CloseDetail
}

/// <summary>一条完整的意图；LocationId / ItemId 视 Action 取用。不可变。</summary>
[Serializable]
public readonly struct WorldLocationUiIntent
{
    public readonly WorldLocationUiAction Action;
    public readonly uint LocationId;
    public readonly uint ItemId;

    public WorldLocationUiIntent(WorldLocationUiAction action, uint locationId = 0u, uint itemId = 0u)
    {
        Action = action;
        LocationId = locationId;
        ItemId = itemId;
    }
}

/// <summary>交易列表中的一行（买价 / 卖价 / 持有量）。</summary>
public sealed class WorldLocationTradeRow
{
    public uint ItemId;
    public uint BuyPrice;
    public uint SellPrice;
    public uint Owned;
    public string Name;
    public bool CanBuy;
    public bool CanSell;
}

/// <summary>一次渲染所需的完整显示状态；View 只读。</summary>
public sealed class WorldLocationsDisplayState
{
    public uint Era;
    public uint SelectedId;
    public WorldLocationIdentity Identity;
    public List<WorldLocationSnapshot> Locations;
    public WorldLocationSnapshot Selected;
    public List<WorldLocationTradeRow> Offers;
    public string Hint;
}

