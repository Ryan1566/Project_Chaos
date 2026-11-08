using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunMap
{
    public enum ExplorationLayout { DoubleLoop, LayeredBranches, Convergence }
    public enum ExplorationRole { Entry, Junction, Landmark, Encounter, Objective, DeadEnd }

    [Serializable]
    public sealed class MapLadder
    {
        public string StableId;
        // 两端均为角色脚底落脚点；梯身位于空洞中，不关闭地形碰撞。
        public Vector2Int Bottom, Top;
        public ushort RegionId;
        public float Speed = 3.5f;
    }
    public sealed class ExplorationPlace
    {
        public ushort Id;
        public string StableId, Composition;
        public Vector2Int Anchor;
        public RectInt Bounds;
        public MapSpaceKind Space;
        public ExplorationRole Role;
        public bool MustBeReachable = true, HasEncounter, CountsForCompletion;
    }
    public sealed class ExplorationLink
    {
        public ushort From, To;
        public bool IsLoop, UsesLadder;
        public readonly List<Vector2Int> Path = new List<Vector2Int>();
    }
    public sealed partial class NaturalMapPlan
    {
        public int Version = 2;
        public ExplorationLayout Layout;
        public readonly List<ExplorationPlace> Places = new List<ExplorationPlace>();
        public readonly List<ExplorationLink> Links = new List<ExplorationLink>();
        public readonly List<MapLadder> Ladders = new List<MapLadder>();
        public readonly HashSet<ushort> ObjectiveRegionIds = new HashSet<ushort>();
        public ushort[] RegionCells;
        public byte[] SpaceCells;
        public Vector2Int EntryCell, GoalCell;
        public bool IsObjectiveRegion(ushort id) => Version < 3 || ObjectiveRegionIds.Contains(id);
        public ushort RegionAt(int x, int y)
        {
            if (!Inside(x, y)) return 0;
            if (RegionCells != null) return RegionCells[Index(x, y)];
            foreach (var region in Regions) if (region.Bounds.Contains(new Vector2(x + .5f, y + .05f))) return region.Id;
            return 0;
        }
    }
}
