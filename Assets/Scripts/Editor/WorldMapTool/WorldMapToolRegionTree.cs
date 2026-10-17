using System.Collections.Generic;

/// <summary>树节点种类（三级：时期 → 势力 → 区域）。</summary>
public enum WorldMapTreeKind
{
    Stage,
    Faction,
    Region,
}

/// <summary>三级树的一个节点。**纯数据，不含绘制**。</summary>
public class WorldMapTreeNode
{
    public WorldMapTreeKind kind;
    public uint stage;
    public uint factionId;
    public uint regionId;
    public string color;
    public int vertexCount;
    /// <summary>区域节点专用：在 `GroupRegions` 列表里的下标（选中/删除/移动都靠它）。</summary>
    public int sourceIndex = -1;
    public readonly List<WorldMapTreeNode> children = new List<WorldMapTreeNode>();
}

/// <summary>按展开状态拍平后的**可见行**（一次 OnGUI 只算一次 → Layout/Repaint 元素数一致）。</summary>
public class WorldMapTreeRow
{
    public WorldMapTreeNode node;
    public int depth;
    public bool expandable;
    public bool expanded;
    public string key;
}

/// <summary>拖拽中的节点（**只在同一窗口内拖放**，不跨窗口）。</summary>
public class WorldMapDragNode
{
    public WorldMapTreeKind kind;
    public uint stage;
    public uint factionId;
    public uint regionId;
    public int sourceIndex = -1;
    public string label;
}

/// <summary>「把区域移到另一个势力下」的意图（**延后**到 Layout 之前才执行）。</summary>
public class WorldMapMoveRegionIntent
{
    public int sourceIndex = -1;
    public uint targetStage;
    public uint targetFactionId;
}

/// <summary>「同一势力内调整区域顺序」的意图（改完按视觉顺序重编 RegionId = 1..N）。</summary>
public class WorldMapReorderIntent
{
    public uint stage;
    public uint factionId;
    public int sourceIndex = -1;
    public int targetIndex = -1;
}

/// <summary>构建三级树：时期 1..5 **全部列出**（空时期也占一行，方便看到"（无）"）。</summary>
public static class WorldMapRegionTree
{
    public static string StageKey(uint stage) { return "S" + stage; }
    public static string FactionKey(uint stage, uint factionId) { return "S" + stage + "F" + factionId; }

    public static List<WorldMapTreeNode> Build(List<WorldMapFactionRegion> regions, uint minStage, uint maxStage)
    {
        var stages = new List<WorldMapTreeNode>();

        for (uint s = minStage; s <= maxStage; s++)
        {
            var stageNode = new WorldMapTreeNode { kind = WorldMapTreeKind.Stage, stage = s };

            var factionIds = new List<uint>();
            for (int i = 0; i < regions.Count; i++)
            {
                if (regions[i].stage != s) continue;
                if (!factionIds.Contains(regions[i].factionId)) factionIds.Add(regions[i].factionId);
            }
            factionIds.Sort();

            for (int f = 0; f < factionIds.Count; f++)
            {
                uint fid = factionIds[f];
                var factionNode = new WorldMapTreeNode { kind = WorldMapTreeKind.Faction, stage = s, factionId = fid };
                stageNode.children.Add(factionNode);

                for (int i = 0; i < regions.Count; i++)
                {
                    if (regions[i].stage != s || regions[i].factionId != fid) continue;

                    var regionNode = new WorldMapTreeNode
                    {
                        kind = WorldMapTreeKind.Region, stage = s, factionId = fid,
                        regionId = regions[i].regionId, color = regions[i].color,
                        vertexCount = regions[i].vertices.Count, sourceIndex = i,
                    };
                    factionNode.children.Add(regionNode);
                    if (factionNode.color == null) factionNode.color = regions[i].color;   //势力取第一块的颜色
                }
            }

            stages.Add(stageNode);
        }
        return stages;
    }
}
