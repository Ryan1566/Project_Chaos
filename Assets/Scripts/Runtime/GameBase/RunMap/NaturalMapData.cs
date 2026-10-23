using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RunMap
{
    // Authored data lives in MapGeneration.xlsx. This model is the generator's private snapshot.
    [Serializable]
    public sealed class NaturalMapParameters
    {
        public uint Id = 1, presetId = 11;
        public bool enable = true;
        public int widthTiles = 320, heightTiles = 144, edgeMargin = 8;
        public int surfaceBaseY = 80, skyClearance = 32, undergroundMinY = 8;
        public int surfaceAmplitude = 8, surfaceStepLength = 12, maxStepHeight = 1;
        public int routeHeadroom = 4, platformMinWidth = 6, maxGapWidth = 2, maxDropHeight = 3;
        public int regionWidthMin = 40, regionWidthMax = 64;
        public int branchCountMin = 1, branchCountMax = 3, branchLengthMin = 20, branchLengthMax = 60;
        public int loopChance = 60, caveDepth = 24, caveHeight = 8, caveWidth = 8, chamberRadius = 8;
        public int caveEntranceMin = 2, caveEntranceMax = 3, caveSmoothPasses = 1;
        public int buildingCountMin = 1, buildingCountMax = 2, buildingMinSpacing = 16;
        public string buildingPool = "Building";
        public int enemiesPerRegion = 2, npcCount = 1, propCount = 3, interactiveCount = 1, decorationCount = 24;
        public int contentMinSpacing = 3, entrySafetyRadius = 8;
        public int retryMax = 6, repairMax = 8, maxTileCells = 262144, maxContentObjects = 256;
        public int workColumnsPerFrame = 16;
        public float frameBudgetMs = 5f;
        public float mainRouteMinLength = 200f, mainRouteMaxLength = 600f;
        public int startOnRightChance = 50;

        public NaturalMapParameters Copy() { return (NaturalMapParameters)MemberwiseClone(); }
        public string Fingerprint { get { return MapRandom.Hash(JsonUtility.ToJson(this)).ToString("X8"); } }
        public List<string> Validate(MapTraversalProfile actor)
        {
            var e = new List<string>();
            if (!enable) e.Add("地图参数未启用");
            if (widthTiles < 160 || heightTiles < 96 || widthTiles > 2048 || heightTiles > 1024
                || (long)widthTiles * heightTiles > maxTileCells || maxTileCells > 2097152)
                e.Add("地图尺寸不合法或超过格子预算（最小160×96，最大2048×1024）");
            if (edgeMargin < 4 || edgeMargin * 2 + regionWidthMin * 2 > widthTiles) e.Add("边缘留白与区域宽度无法放入地图");
            if (widthTiles < edgeMargin * 2 + entrySafetyRadius * 2 + (caveDepth + surfaceAmplitude + 8) * 2 + regionWidthMin + 12)
                e.Add("地图宽度不足以容纳洞口坡道与地下主通路");
            if (surfaceAmplitude < 0 || skyClearance < 24 || surfaceBaseY + surfaceAmplitude + skyClearance > heightTiles)
                e.Add("地表起伏或天空净空超出高度");
            if (undergroundMinY < 3 || caveDepth < caveHeight + 4 || surfaceBaseY - surfaceAmplitude - caveDepth - chamberRadius < undergroundMinY)
                e.Add("地下埋深、洞室或最低高度冲突");
            if (maxStepHeight < 1 || maxStepHeight > actor.MaxJumpHeight || maxDropHeight < 1 || maxDropHeight > actor.MaxDrop
                || routeHeadroom < actor.Height || maxGapWidth < 0 || maxGapWidth >= actor.MaxJumpDistance)
                e.Add("地形参数超出角色通行能力");
            if (surfaceStepLength < platformMinWidth || platformMinWidth < 3 || regionWidthMin < 20 || regionWidthMax < regionWidthMin)
                e.Add("平台、起伏步长或区域宽度不合法");
            if (branchCountMin < 0 || branchCountMax < branchCountMin || branchCountMax > 16
                || branchLengthMin < 8 || branchLengthMax < branchLengthMin || branchLengthMax > widthTiles
                || caveEntranceMin < 2 || caveEntranceMax < caveEntranceMin || caveEntranceMax > 6
                || caveHeight < routeHeadroom || caveWidth < routeHeadroom || chamberRadius < 3 || caveSmoothPasses < 0 || caveSmoothPasses > 8)
                e.Add("洞穴或支路参数不合法");
            if (loopChance < 0 || loopChance > 100 || buildingCountMin < 0 || buildingCountMax < buildingCountMin
                || buildingCountMax > 12 || buildingMinSpacing < 0 || string.IsNullOrEmpty(buildingPool)) e.Add("回环或结构参数不合法");
            if (enemiesPerRegion < 1 || enemiesPerRegion > 20 || npcCount < 0 || propCount < 0 || interactiveCount < 0
                || decorationCount < 0 || maxContentObjects < 1 || maxContentObjects > 4096 || contentMinSpacing < 1
                || entrySafetyRadius < 6 || entrySafetyRadius * 4 >= widthTiles) e.Add("内容预算或入口安全参数不合法");
            if (retryMax < 1 || retryMax > 30 || repairMax < 0 || repairMax > 32 || workColumnsPerFrame < 1
                || workColumnsPerFrame > 128 || float.IsNaN(frameBudgetMs) || frameBudgetMs < 1 || frameBudgetMs > 30)
                e.Add("生成工作预算不合法");
            if (startOnRightChance < 0 || startOnRightChance > 100 || float.IsNaN(mainRouteMinLength)
                || float.IsNaN(mainRouteMaxLength) || float.IsInfinity(mainRouteMinLength) || float.IsInfinity(mainRouteMaxLength)
                || mainRouteMinLength < 1 || mainRouteMaxLength < mainRouteMinLength) e.Add("主路长度或方向权重不合法");
            return e;
        }
    }

    public enum MapSpaceKind : byte { Outdoor, Underground, Building }
    public enum MapPlacementKind : byte { Player, Ally, Npc, Enemy, Interactive, Prop, Decoration, Exit }
    [Flags]
    public enum MapCellTags
    {
        None = 0, Air = 1, Solid = 2, Supported = 4, Reachable = 8, ProtectedRoute = 16,
        EntrySafety = 32, Outdoor = 64, Underground = 128, Building = 256, Reserved = 512
    }

    public sealed class MapTraversalProfile
    {
        public int Height = 2, MaxJumpHeight = 2, MaxJumpDistance = 4, MaxDrop = 3;
        public float BodyWidth = 0.7f;
    }

    public static class MapRandom
    {
        public static uint Hash(string key)
        {
            uint h = 2166136261;
            foreach (byte b in Encoding.UTF8.GetBytes(key ?? "")) h = unchecked((h ^ b) * 16777619);
            return h;
        }
        public static uint Seed(uint runSeed, int stage, string stream, int attempt = 0)
        {
            uint h = Hash("natural-v2/" + stream);
            foreach (uint value in new[] { runSeed, (uint)stage, (uint)attempt })
                for (int i = 0; i < 4; i++) h = unchecked((h ^ (byte)(value >> (i * 8))) * 16777619);
            return h;
        }
        public static RunMapRng Stream(uint seed, int stage, string key, int attempt = 0)
        { return new RunMapRng(Seed(seed, stage, key, attempt)); }
    }

    public sealed class EncounterRegion
    {
        public ushort Id;
        public string StableId;
        public MapSpaceKind Space;
        public Rect Bounds;
        public bool IsEntry, Required;
    }

    public sealed class RegionConnection
    {
        public ushort From, To;
        public Vector2 Position;
        public bool IsLoop;
    }

    public sealed class MapPlacement
    {
        public string StableId;
        public MapPlacementKind Kind;
        public ushort RegionId;
        // Feet/support anchor, not the centre of a character collider.
        public Vector2 Position;
        public RectInt Footprint;
        public bool Solid;
        public int Facing = 1;
        public int ClearanceHeight;
        public Vector2 SupportNormal = Vector2.up;
    }

    public sealed class MapStructureTemplate
    {
        public uint Id;
        public string ResourcePath;
        public int Width, Height, Weight = 10, MaxCount = 99;
        public bool[] Solid;
        public int EntranceFloor = 3, ShaftColumn = 20;
    }

    public sealed class MapStructure
    {
        public string StableId;
        public MapStructureTemplate Template;
        public RectInt Bounds;
        public bool MirrorX;
    }

    public sealed class NaturalMapPlan
    {
        public const int GeneratorVersion = 2;
        public NaturalMapParameters Parameters;
        public MapTraversalProfile Actor;
        public uint RunSeed;
        public int SceneIndex, Attempt, Repairs;
        public float MainRouteLength;
        public bool StartsOnRight;
        public bool Degraded;
        public string ConfigFingerprint;
        public int Width { get { return Parameters.widthTiles; } }
        public int Height { get { return Parameters.heightTiles; } }
        public RectInt Bounds { get { return new RectInt(0, 0, Width, Height); } }
        public bool[] Solid, Protected, Reachable;
        public int[] SurfaceY, CaveFloor;
        public readonly List<EncounterRegion> Regions = new List<EncounterRegion>();
        public readonly List<RegionConnection> Connections = new List<RegionConnection>();
        public readonly List<MapStructure> Structures = new List<MapStructure>();
        public readonly List<MapPlacement> Placements = new List<MapPlacement>();
        public readonly List<Vector2Int> Entrances = new List<Vector2Int>();
        public readonly List<string> Diagnostics = new List<string>();
        public readonly List<Vector2Int> StandingCells = new List<Vector2Int>();
        public int[] RouteParents;
        public ushort EntryRegionId = 1, ExitRegionId;
        public int Index(int x, int y) { return y * Width + x; }
        public bool Inside(int x, int y) { return x >= 0 && y >= 0 && x < Width && y < Height; }
        public bool IsSolid(int x, int y) { return !Inside(x, y) || Solid[Index(x, y)]; }
        public bool CanStand(int x, int y)
        {
            if (!Inside(x, y + Actor.Height - 1) || y < 1 || !IsSolid(x, y - 1)) return false;
            for (int h = 0; h < Actor.Height; h++) if (IsSolid(x, y + h)) return false;
            return true;
        }
        public EncounterRegion FindRegion(ushort id) { return Regions.Find(r => r.Id == id); }
        public List<Vector2Int> RouteTo(Vector2Int cell)
        {
            var result = new List<Vector2Int>();
            if (!Inside(cell.x, cell.y) || !Reachable[Index(cell.x, cell.y)] || RouteParents == null) return result;
            for (int index = Index(cell.x, cell.y), budget = Solid.Length; index >= 0 && budget-- > 0; index = RouteParents[index])
                result.Add(new Vector2Int(index % Width, index / Width));
            result.Reverse(); return result;
        }
        public MapPlacement FindPlacement(MapPlacementKind kind, int index = 0)
        {
            foreach (var p in Placements) if (p.Kind == kind && index-- == 0) return p;
            return null;
        }
        public MapSpaceKind SpaceAt(int x, int y)
        {
            foreach (var b in Structures) if (b.Bounds.Contains(new Vector2Int(x, y))) return MapSpaceKind.Building;
            return y < SurfaceY[Mathf.Clamp(x, 0, Width - 1)] - 3 ? MapSpaceKind.Underground : MapSpaceKind.Outdoor;
        }
        public MapCellTags TagsAt(int x, int y)
        {
            if (!Inside(x, y)) return MapCellTags.Solid;
            MapCellTags tags = IsSolid(x, y) ? MapCellTags.Solid : MapCellTags.Air;
            if (CanStand(x, y)) tags |= MapCellTags.Supported;
            if (Reachable[Index(x, y)]) tags |= MapCellTags.Reachable;
            if (Protected[Index(x, y)]) tags |= MapCellTags.ProtectedRoute;
            var entry = FindPlacement(MapPlacementKind.Player);
            if (entry != null && Vector2.Distance(entry.Position, new Vector2(x + .5f, y + .05f)) < Parameters.entrySafetyRadius) tags |= MapCellTags.EntrySafety;
            var space = SpaceAt(x, y);
            tags |= space == MapSpaceKind.Building ? MapCellTags.Building : space == MapSpaceKind.Underground ? MapCellTags.Underground : MapCellTags.Outdoor;
            if (Placements.Exists(p => p.Footprint.Contains(new Vector2Int(x, y)))) tags |= MapCellTags.Reserved;
            return tags;
        }
        public string Signature(bool includeSeed = true, bool includeDecorations = true)
        {
            var s = new StringBuilder();
            if (includeSeed) s.Append(RunSeed).Append('/').Append(SceneIndex).Append('/').Append(ConfigFingerprint);
            s.Append('|').Append(Width).Append('x').Append(Height).Append('|');
            uint h = 2166136261;
            for (int i = 0; i < Solid.Length; i++) h = unchecked((h ^ (Solid[i] ? 1u : 0u)) * 16777619);
            s.Append(h.ToString("X8"));
            foreach (var r in Regions) s.Append('|').Append(r.StableId).Append(':').Append((int)r.Space);
            foreach (var p in Placements)
            {
                if (!includeDecorations && p.Kind == MapPlacementKind.Decoration) continue;
                s.Append('|').Append(p.StableId).Append('@').Append(p.Footprint.x).Append(',').Append(p.Footprint.y);
            }
            return s.ToString();
        }
        public string Report { get { return "v2 seed=" + RunSeed + " stage=" + SceneIndex + " config=" + ConfigFingerprint
            + " bounds=" + Width + "x" + Height + " attempt=" + Attempt + " repairs=" + Repairs + " degraded=" + Degraded
            + " regions=" + Regions.Count + " structures=" + Structures.Count + " placements=" + Placements.Count
            + " route=" + MainRouteLength.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) + " direction=" + (StartsOnRight ? "left" : "right")
            + (Diagnostics.Count > 0 ? " | " + string.Join(" | ", Diagnostics) : ""); } }
    }

    public interface IMapPopulationPass
    {
        string StreamKey { get; }
        int Priority { get; }
        void BuildPlan(MapPlacementContext context, RunMapRng random);
    }

    public sealed class MapPlacementRequest
    {
        public string StableId;
        public MapPlacementKind Kind;
        public ushort RegionId;
        public Vector2Int Cell;
        public int Width = 1, Height = 2;
        public bool Solid;
        public bool AllowProtected;
        public MapCellTags RequiredTags, ForbiddenTags;
    }

    public sealed class MapPlacementContext
    {
        public NaturalMapPlan Plan { get; private set; }
        public MapPlacementContext(NaturalMapPlan plan) { Plan = plan; }
        public List<Vector2Int> Candidates(ushort regionId = 0, bool solid = false,
            MapCellTags required = MapCellTags.None, MapCellTags forbidden = MapCellTags.None)
        {
            var result = new List<Vector2Int>();
            var region = Plan.FindRegion(regionId);
            if (regionId != 0 && region == null) return result;
            foreach (var cell in Plan.StandingCells)
                if (Plan.Reachable[Plan.Index(cell.x, cell.y)] && Plan.CanStand(cell.x, cell.y)
                    && (region == null || region.Bounds.Contains(new Vector2(cell.x + .5f, cell.y + .05f)))
                    && (!solid || !Plan.Protected[Plan.Index(cell.x, cell.y)]))
                {
                    if (required != MapCellTags.None || forbidden != MapCellTags.None)
                    { var tags = Plan.TagsAt(cell.x, cell.y); if ((tags & required) != required || (tags & forbidden) != 0) continue; }
                    result.Add(cell);
                }
            return result;
        }
        public bool TryReserve(MapPlacementRequest request, out MapPlacement placement)
        {
            placement = null;
            if (string.IsNullOrEmpty(request.StableId) || Plan.Placements.Count >= Plan.Parameters.maxContentObjects
                || Plan.Placements.Exists(p => p.StableId == request.StableId)
                || (request.RegionId != 0 && Plan.FindRegion(request.RegionId) == null)) return false;
            var box = new RectInt(request.Cell.x, request.Cell.y, request.Width, request.Height);
            if (box.width < 1 || box.height < 1 || !Plan.Bounds.Contains(box.min) || !Plan.Bounds.Contains(box.max - Vector2Int.one)) return false;
            if (request.RequiredTags != MapCellTags.None || request.ForbiddenTags != MapCellTags.None)
            { var tags = Plan.TagsAt(box.x, box.y); if ((tags & request.RequiredTags) != request.RequiredTags || (tags & request.ForbiddenTags) != 0) return false; }
            for (int x = box.xMin; x < box.xMax; x++)
            {
                if (!Plan.IsSolid(x, box.yMin - 1)) return false;
                if (request.Solid)
                    for (int y = box.yMax; y < box.yMax + Plan.Actor.Height; y++) if (Plan.IsSolid(x, y)) return false;
                for (int y = box.yMin; y < box.yMax; y++)
                    if (Plan.IsSolid(x, y) || (request.Solid && Plan.Protected[Plan.Index(x, y)])
                        || (!request.AllowProtected && Plan.Protected[Plan.Index(x, y)])) return false;
            }
            if (!Plan.Reachable[Plan.Index(box.x, box.y)]) return false;
            var entry = Plan.FindPlacement(MapPlacementKind.Player);
            if (entry != null && request.Kind != MapPlacementKind.Ally && request.Kind != MapPlacementKind.Exit
                && Vector2.Distance(entry.Position, new Vector2(box.x + .5f, box.y)) < Plan.Parameters.entrySafetyRadius) return false;
            foreach (var p in Plan.Placements)
            {
                if (box.Overlaps(p.Footprint)) return false;
                if (Vector2.Distance(p.Position, new Vector2(box.x + .5f, box.y + .05f)) < Plan.Parameters.contentMinSpacing) return false;
            }
            placement = new MapPlacement { StableId = request.StableId, Kind = request.Kind, RegionId = request.RegionId,
                Position = new Vector2(box.x + box.width * .5f, box.y + .05f), Footprint = box, Solid = request.Solid };
            while (Plan.Inside(box.x, box.y + placement.ClearanceHeight) && !Plan.IsSolid(box.x, box.y + placement.ClearanceHeight)) placement.ClearanceHeight++;
            Plan.Placements.Add(placement);
            if (request.Solid)
                for (int x = box.xMin; x < box.xMax; x++) for (int y = box.yMin; y < box.yMax; y++) Plan.Solid[Plan.Index(x, y)] = true;
            return true;
        }
    }
}
