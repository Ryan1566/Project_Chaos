using System;
using System.Collections.Generic;

namespace RunMap
{
    /// <summary>
    /// 房间块与原型图名录。
    ///
    /// ⚠ 现在装的是**灰盒名录**（与 Assets/Prefabs/RunMap/Placeholder 下真实存在的 prefab 一一对应），
    /// 不是配置表。等 `MapChunk` / `MapChunkSocket` / `MapPreset` 等表落地后，
    /// 只需再写一个从 Json 装载的 Catalog 实现，规划器/校验器/装配层都不用改
    /// —— 这是把"数据源"和"算法"分开的收益（§12.1）。
    /// </summary>
    public class RunMapCatalog
    {
        private readonly Dictionary<uint, ChunkDef> _byId = new Dictionary<uint, ChunkDef>();
        private readonly List<ChunkDef> _all = new List<ChunkDef>();
        private readonly List<PresetDef> _presets = new List<PresetDef>();
        /// <summary>层 → 主题。**表驱动时由配置表登记**（跨层地图每层的主题不同，必须按层解析）。</summary>
        private readonly Dictionary<MapLayer, string> _themeByLayer = new Dictionary<MapLayer, string>();

        public void RegisterTheme(MapLayer layer, string themeKey)
        {
            if (!string.IsNullOrEmpty(themeKey)) _themeByLayer[layer] = themeKey;
        }

        public List<ChunkDef> All { get { return _all; } }
        public List<PresetDef> Presets { get { return _presets; } }

        public ChunkDef Get(uint id)
        {
            ChunkDef c;
            return _byId.TryGetValue(id, out c) ? c : null;
        }

        public void Add(ChunkDef c)
        {
            if (c == null) return;
            _byId[c.Id] = c;
            _all.Add(c);
        }

        public void AddPreset(PresetDef p) { if (p != null) _presets.Add(p); }

        /// <summary>取候选块池：同主题 + 同层 + 同类型 + 已启用。</summary>
        public List<ChunkDef> Pool(string themeKey, MapLayer layer, ChunkKind kind)
        {
            var list = new List<ChunkDef>();
            for (int i = 0; i < _all.Count; i++)
            {
                ChunkDef c = _all[i];
                if (c.ThemeKey != themeKey) continue;
                if (c.Kind != kind) continue;
                // 层间连接块 layer 填 0（跨层），任何层都能用
                if ((byte)c.Layer != 0 && c.Layer != layer) continue;
                list.Add(c);
            }
            return list;
        }

        /// <summary>
        /// 层 → 主题的规范映射。
        /// **一张跨层的地图里，主题必须按"层"解析，不能按"场景"解析**：
        /// 两层场景的上层是地表主题、下层是地下主题，套同一个主题就会取不到块池。
        /// </summary>
        public string ThemeKeyForLayer(MapLayer layer)
        {
            string key;
            if (_themeByLayer.TryGetValue(layer, out key)) return key;   // 表驱动：以配置表登记的为准
            for (int i = 0; i < ThemeLayers.Length; i++) if (ThemeLayers[i] == layer) return ThemeKeys[i];
            return "Surface";
        }

        public PresetDef GetPreset(int id)
        {
            for (int i = 0; i < _presets.Count; i++) if (_presets[i].Id == id) return _presets[i];
            return null;
        }

        /// <summary>某个空间层可用的第一个已启用单层 preset（按层分配场景时用）。</summary>
        public PresetDef FindSingleLayerPreset(MapLayer layer)
        {
            for (int i = 0; i < _presets.Count; i++)
            {
                PresetDef p = _presets[i];
                if (!p.Enabled || p.Layers.Count != 1) continue;
                if (p.Layers[0].Layer == layer) return p;
            }
            return null;
        }

        /// <summary>
        /// C1：一局 3 个场景合计，三层各至少出现 1 次。
        /// 返回 3 个 preset（顺序 = 场景 1/2/3）。**这里刻意不做随机**：分配规则稳定，
        /// 玩家才能预期"第 1 关是地表、第 3 关是有建筑"。
        /// </summary>
        public List<PresetDef> AllocateRunPresets()
        {
            var result = new List<PresetDef>();
            MapLayer[] order = { MapLayer.Surface, MapLayer.Underground, MapLayer.Building };
            for (int i = 0; i < 3; i++)
            {
                PresetDef p = FindSingleLayerPreset(order[i]);
                if (p == null) p = FindSingleLayerPreset(MapLayer.Surface);
                result.Add(p);
            }
            return result;
        }

        // ═══════════════════════════════════════════════════════════════
        //  灰盒名录：与 RunMapPlaceholderPipeline 产出的 18 个 prefab 严格对应
        // ═══════════════════════════════════════════════════════════════

        private static readonly string[] ThemeKeys = { "Underground", "Surface", "Building" };
        private static readonly MapLayer[] ThemeLayers = { MapLayer.Underground, MapLayer.Surface, MapLayer.Building };
        private static readonly string[] ThemeLabels = { "地下", "地表", "地表建筑" };

        public static RunMapCatalog CreateGreybox()
        {
            var cat = new RunMapCatalog();

            for (int t = 0; t < ThemeKeys.Length; t++)
            {
                string key = ThemeKeys[t];
                MapLayer layer = ThemeLayers[t];
                int baseId = 1000 + t * 100;

                cat.Add(Make(baseId + (int)ChunkKind.Entry, layer, key, ChunkKind.Entry, "Entry",
                    new[] { SocketDir.Right, SocketDir.Up }, false, false));
                cat.Add(Make(baseId + (int)ChunkKind.Corridor, layer, key, ChunkKind.Corridor, "Corridor",
                    new[] { SocketDir.Left, SocketDir.Right, SocketDir.Up, SocketDir.Down }, false, false));
                cat.Add(Make(baseId + (int)ChunkKind.Combat, layer, key, ChunkKind.Combat, "Combat",
                    new[] { SocketDir.Left, SocketDir.Right, SocketDir.Up, SocketDir.Down }, false, false));
                cat.Add(Make(baseId + (int)ChunkKind.DeadEnd, layer, key, ChunkKind.DeadEnd, "DeadEnd",
                    new[] { SocketDir.Left, SocketDir.Up, SocketDir.Down }, false, false));
                cat.Add(Make(baseId + (int)ChunkKind.LayerLink, layer, key, ChunkKind.LayerLink, "LayerLink_Stair",
                    new[] { SocketDir.Up, SocketDir.Down }, false, true));
                cat.Add(Make(baseId + (int)ChunkKind.BossGate, layer, key, ChunkKind.BossGate, "BossGate",
                    new[] { SocketDir.Left, SocketDir.Up, SocketDir.Down }, true, false));
            }

            // ── 预设 ──
            // 单层：MVP 主用。一局 3 个场景分别落在地表 / 地下 / 地表建筑（满足 C1 的三层齐全）。
            for (int t = 0; t < ThemeLayers.Length; t++)
            {
                var p = new PresetDef
                {
                    Id = 10 + t,
                    BiomeId = (uint)(t + 1),
                    Name = "Greybox_Single_" + ThemeKeys[t],
                    MainPathMin = 6, MainPathMax = 8,
                    BranchMin = 4, BranchMax = 7,
                    TotalMin = 10, TotalMax = 15,
                    SpecialTreasure = 2, SpecialCamp = 1,
                    RetryMax = 30,
                };
                p.Layers.Add(new LayerSpec
                {
                    Layer = ThemeLayers[t], Order = 1, RoomMin = 4, RoomMax = 8,
                    IsMainPath = true, LinkFromOrder = 0, AllowDirect = true, LinkMin = 0, LinkMax = 0
                });
                cat.AddPreset(p);
            }

            // 双层（地表 → 地下）：v2 的"四向开口"规格让跨层真正成立 ——
            // 每个房间自带 U/D 开口，层间连接件（竖井）站在前驱正上方，
            // 上层第一个房间再抬一层且**与竖井同列**（§4.4 C10.6）。
            // 已实测：100 种子 × 2 层全通过（C10.4/C10.5/C10.6/C10.7 都被覆盖到）。
            // ⚠ 代价：未使用的开口需要运行时封堵（§16.6 Pass E），否则玩家能从没接东西的洞口掉出地图。
            var two = new PresetDef
            {
                Id = 20,
                BiomeId = 1,
                Name = "Greybox_TwoLayer_Surface_Underground",
                Enabled = true,
                MainPathMin = 7, MainPathMax = 9,
                BranchMin = 4, BranchMax = 8,
                TotalMin = 12, TotalMax = 18,
                SpecialTreasure = 2, SpecialCamp = 1,
                RetryMax = 40,
            };
            two.Layers.Add(new LayerSpec { Layer = MapLayer.Surface, Order = 1, RoomMin = 3, RoomMax = 6, IsMainPath = true, LinkFromOrder = 0, LinkMin = 1, LinkMax = 2 });
            two.Layers.Add(new LayerSpec { Layer = MapLayer.Underground, Order = 2, RoomMin = 3, RoomMax = 6, IsMainPath = true, LinkFromOrder = 1, AllowDirect = true, LinkMin = 1, LinkMax = 2 });
            cat.AddPreset(two);

            return cat;
        }

        private static ChunkDef Make(int id, MapLayer layer, string themeKey, ChunkKind kind, string typeName,
            SocketDir[] dirs, bool bossDoor, bool layerLink)
        {
            var c = new ChunkDef
            {
                Id = (uint)id,
                Kind = kind,
                Layer = layer,
                ThemeKey = themeKey,
                PrefabPath = "Prefabs/RunMap/Placeholder/" + themeKey + "/Chunk_" + themeKey + "_" + typeName,
                Weight = 10,
                MaxCount = 99,
                Unique = false,
            };

            for (int i = 0; i < dirs.Length; i++)
            {
                SocketDir d = dirs[i];
                bool vertical = RunMapConst.IsVertical(d);
                // 横向门统一落在档 0；BossGate 那条"boss 门"是循环之后单独补的
                int profile = 0;
                c.Sockets.Add(new SocketDef
                {
                    Dir = d,
                    Profile = profile,
                    Type = layerLink ? 2 : 1,
                    X = d == SocketDir.Left ? 0.5f : (d == SocketDir.Right ? RunMapConst.ChunkW - 0.5f : RunMapConst.ShaftColumn + 0.5f),
                    Y = vertical ? (d == SocketDir.Up ? RunMapConst.ChunkH - 0.5f : 0.5f)
                                 : RunMapConst.ProfileFloorRow[profile] + 2f,
                });
            }

            // boss 门：右侧是"通往下一场景的门"，不是可通行通道（Type=3，prefab 里没有挖洞）
            if (bossDoor)
            {
                c.Sockets.Add(new SocketDef
                {
                    Dir = SocketDir.Right, Profile = 1, Type = 3,
                    X = RunMapConst.ChunkW - 0.5f, Y = RunMapConst.ProfileFloorRow[1] + 2f
                });
            }
            // 标记点：坐标与 RunMapPlaceholderPipeline 生成的 SpawnSlot_* 一一对齐。
            // Pass F（EmitSlots）做的就是把这些局部坐标换算成整图世界坐标，再交给 M8/M9 去填内容。
            switch (kind)
            {
                case ChunkKind.Entry:
                    c.Slots.Add(new SlotDef { SlotType = 5, X = 5.5f, Y = 3.5f });
                    break;
                case ChunkKind.Corridor:
                    c.Slots.Add(new SlotDef { SlotType = 1, X = 12f, Y = 3.5f });
                    break;
                case ChunkKind.Combat:
                    c.Slots.Add(new SlotDef { SlotType = 1, X = 10f, Y = 3.5f });
                    c.Slots.Add(new SlotDef { SlotType = 1, X = 20f, Y = 12.5f });
                    c.Slots.Add(new SlotDef { SlotType = 2, X = 33f, Y = 8.5f });
                    break;
                case ChunkKind.DeadEnd:
                    c.Slots.Add(new SlotDef { SlotType = 3, X = 31.5f, Y = 5.5f });
                    break;
                case ChunkKind.LayerLink:
                    c.Slots.Add(new SlotDef { SlotType = 1, X = RunMapConst.ShaftColumn + 0.5f, Y = 12f });
                    break;
                case ChunkKind.BossGate:
                    c.Slots.Add(new SlotDef { SlotType = 2, X = 28f, Y = RunMapConst.ProfileFloorRow[1] + 0.5f });
                    break;
            }

            return c;
        }
    }
}
