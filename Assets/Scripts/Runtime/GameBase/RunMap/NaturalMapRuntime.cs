using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RunMap
{
    /// <summary>Runtime boundary: immutable configuration snapshots in; a validated plan out.</summary>
    public static class NaturalMapRuntime
    {
        public static NaturalMapPlan Current { get; private set; }
        public static bool IsReady { get; private set; }
        public static NaturalMapParameters DebugOverrides { get; private set; }
        public static uint? PreviewSeed { get; private set; }
        public static string LastError { get; private set; }
        public static readonly List<IMapPopulationPass> PopulationPasses = new List<IMapPopulationPass>();
        public static MapTraversalProfile ActorProfile { get; private set; } = new MapTraversalProfile();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        { Current = null; IsReady = false; DebugOverrides = null; PreviewSeed = null; LastError = null; PopulationPasses.Clear(); }

        public static void Clear() { Current = null; IsReady = false; }
        public static void Fail(string reason) { Clear(); LastError = reason; }
        public static void Publish(NaturalMapPlan plan) { Current = plan; IsReady = plan != null; LastError = null; }

        public static void ConfigurePreview(uint? seed, int width, int height)
        {
            var settings = LoadParameters(1, false);
            settings.widthTiles = width; settings.heightTiles = height;
            var errors = settings.Validate(CaptureActorProfile());
            if (errors.Count > 0) throw new ArgumentException(string.Join("；", errors));
            DebugOverrides = settings; PreviewSeed = seed;
        }
        public static void ClearPreview() { DebugOverrides = null; PreviewSeed = null; }
        public static uint CreateRunSeed() { return MapRandom.Hash(Guid.NewGuid().ToString("N")); }

        public static NaturalMapParameters LoadParameters(int sceneIndex, bool applyPreview = true)
        {
            uint preset = sceneIndex == 1 ? 11u : sceneIndex == 2 ? 10u : 12u;
            var rows = ConfigLoader.Instance.LoadData<MapGenerationConfig>();
            var row = rows == null ? null : rows.datas.Find(r => r.enable && r.presetId == preset);
            if (row == null) throw new InvalidOperationException("MapGeneration缺少预设" + preset + "的启用参数");
            var p = JsonUtility.FromJson<NaturalMapParameters>(JsonUtility.ToJson(row));
            if (applyPreview && DebugOverrides != null)
            { p.widthTiles = DebugOverrides.widthTiles; p.heightTiles = DebugOverrides.heightTiles; }
            return p;
        }

        public static MapTraversalProfile CaptureActorProfile()
        {
            var profile = new MapTraversalProfile();
            var player = Resources.Load<GameObject>("Player/Player");
            if (player == null) throw new InvalidOperationException("缺少Player/Player角色资源");
            var move = player.GetComponent<PlayerLocomotion>(); var body = player.GetComponent<Rigidbody2D>();
            var collider = player.GetComponent<CapsuleCollider2D>();
            if (move == null || body == null || collider == null || body.gravityScale <= 0) throw new InvalidOperationException("角色通行能力配置不完整");
            float gravity = Mathf.Abs(Physics2D.gravity.y * body.gravityScale);
            profile.MaxJumpHeight = Mathf.FloorToInt(move.jumpForce * move.jumpForce / (2f * gravity));
            profile.MaxJumpDistance = Mathf.Max(1, Mathf.FloorToInt(move.moveSpeed * move.jumpForce * 2f / gravity));
            profile.Height = Mathf.CeilToInt(collider.size.y * player.transform.localScale.y);
            profile.BodyWidth = collider.size.x * player.transform.localScale.x;
            profile.MaxDrop = 3;
            var ally = Resources.Load<GameObject>("Player/Ally");
            if (ally != null)
            {
                var ai = ally.GetComponent<AllyController>(); var ab = ally.GetComponent<Rigidbody2D>();
                var ac = ally.GetComponent<CapsuleCollider2D>();
                if (ab != null && ab.gravityScale > 0)
                {
                    float ag = Mathf.Abs(Physics2D.gravity.y * ab.gravityScale);
                    float jump = ai == null ? AllyController.DefaultJumpForce : ai.jumpForce;
                    float speed = ai == null ? AllyController.DefaultMoveSpeed : ai.moveSpeed;
                    profile.MaxJumpHeight = Mathf.Min(profile.MaxJumpHeight, Mathf.FloorToInt(jump * jump / (2f * ag)));
                    profile.MaxJumpDistance = Mathf.Min(profile.MaxJumpDistance, Mathf.FloorToInt(speed * jump * 2f / ag));
                }
                if (ac != null) { profile.Height = Mathf.Max(profile.Height, Mathf.CeilToInt(ac.size.y * ally.transform.localScale.y)); profile.BodyWidth = Mathf.Max(profile.BodyWidth, ac.size.x * ally.transform.localScale.x); }
            }
            ActorProfile = profile;
            return profile;
        }

        public static List<MapStructureTemplate> LoadStructures(NaturalMapParameters p)
        {
            var cat = RunMapConfigLoader.LoadFromConfigs();
            if (cat == null) throw new InvalidOperationException("地图结构配置表读取失败");
            var result = new List<MapStructureTemplate>();
            foreach (var def in cat.All)
            {
                if (def.ThemeKey != p.buildingPool || (def.Kind != ChunkKind.Combat && def.Kind != ChunkKind.Corridor)) continue;
                var prefab = Resources.Load<GameObject>(def.PrefabPath);
                if (prefab == null) throw new InvalidOperationException("结构资源不存在：" + def.PrefabPath);
                var t = new MapStructureTemplate { Id = def.Id, ResourcePath = def.PrefabPath,
                    Width = RunMapConst.ChunkW, Height = RunMapConst.ChunkH, Weight = def.Weight, MaxCount = def.Unique ? 1 : def.MaxCount,
                    Solid = new bool[RunMapConst.ChunkW * RunMapConst.ChunkH] };
                var footprintNode = prefab.transform.Find("CameraBounds");
                var footprint = footprintNode == null ? null : footprintNode.GetComponent<BoxCollider2D>();
                if (footprint != null)
                {
                    t.Width = Mathf.RoundToInt(footprint.size.x); t.Height = Mathf.RoundToInt(footprint.size.y);
                    if (t.Width < 12 || t.Height < 12 || t.Width > p.widthTiles || t.Height > p.heightTiles)
                        throw new InvalidOperationException("手工结构声明占地不合法：" + def.PrefabPath);
                    t.Solid = new bool[t.Width * t.Height];
                }
                int profile = def.ProfileOf(SocketDir.Left);
                if (profile >= 0 && profile < RunMapConst.ProfileFloorRow.Length) t.EntranceFloor = RunMapConst.ProfileFloorRow[profile];
                t.ShaftColumn = t.Width / 2;
                foreach (var map in prefab.GetComponentsInChildren<Tilemap>(true))
                {
                    if (map.name != "Tilemap_Ground" && map.name != "Tilemap_Deco") continue;
                    foreach (var cell in map.cellBounds.allPositionsWithin)
                    {
                        if (map.GetTile(cell) == null || map.GetColliderType(cell) == Tile.ColliderType.None) continue;
                        if (cell.x < 0 || cell.y < 0 || cell.x >= t.Width || cell.y >= t.Height)
                            throw new InvalidOperationException("手工结构瓦片超出声明占地：" + def.PrefabPath);
                        t.Solid[cell.y * t.Width + cell.x] = true;
                    }
                }
                result.Add(t);
            }
            result.Sort((a, b) => a.Id.CompareTo(b.Id));
            return result;
        }

        public static MapLayout LegacyMetadata(NaturalMapPlan plan)
        {
            var layout = new MapLayout { Seed = RunMapSeed.Derive(plan.RunSeed, plan.SceneIndex), SceneIndex = plan.SceneIndex,
                PresetId = (int)plan.Parameters.presetId, ThemeKey = plan.SceneIndex == 1 ? "Surface" : plan.SceneIndex == 2 ? "Underground" : "Building",
                Version = 2, EntryId = plan.EntryRegionId, BossId = plan.ExitRegionId, Attempts = plan.Attempt, Degraded = plan.Degraded };
            foreach (var r in plan.Regions)
                layout.Nodes.Add(new RoomNode { Id = r.Id, Layer = r.Space == MapSpaceKind.Underground ? MapLayer.Underground : r.Space == MapSpaceKind.Building ? MapLayer.Building : MapLayer.Surface,
                    Kind = r.IsEntry ? ChunkKind.Entry : ChunkKind.Combat, IsMainPath = r.Required });
            foreach (var p in plan.Placements) layout.Slots.Add(new SpawnSlot { RoomId = p.RegionId, Index = layout.Slots.Count + 1, SlotType = (int)p.Kind, X = p.Position.x, Y = p.Position.y });
            return layout;
        }
    }
}
