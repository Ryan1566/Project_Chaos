using System.Collections.Generic;
using UnityEngine;
using ChaosDebug;

namespace RunMap
{
    /// <summary>
    /// 从**配置表**装载房间块 / 预设名录 —— 走项目既有的配置读取链路
    /// （`ConfigLoader.LoadData&lt;T&gt;()` → `ResManager.Load&lt;TextAsset&gt;` → `Resources/Data/Json/Runtime/*.json`）。
    ///
    /// 与 <see cref="RunMapCatalog.CreateGreybox"/> 的关系：
    /// 那个是**写死在代码里的灰盒名录**（保底 / 单测用），这里是**表驱动**（正式用）。
    /// 两者产出同一个 <see cref="RunMapCatalog"/> 模型，所以规划器 / 校验器 / 装配层一行都不用改 ——
    /// 这就是当初把"数据源"和"算法"分开的收益。
    ///
    /// ⚠ 前置条件：`GlobalPath.data_JsonPathToRead` 必须是 `Data/Json/Runtime/`
    /// （原值是 `Json/Runtime/`，与导出路径 `Resources/Data/Json/Runtime/` 错位 → 读回来恒为 null）。
    /// </summary>
    public static class RunMapConfigLoader
    {
        /// <summary>最近一次装载的简报（给了哪些表、各多少行）—— 留痕用，避免"读了个空表还不知道"。</summary>
        public static string LastReport = "";

        private const string BiomeTable = "MapBiomeConfig";
        private const string ChunkTable = "MapChunkConfig";
        private const string SocketTable = "ChunkSocketConfig";
        private const string SlotTable = "ChunkSlotConfig";
        private const string PresetTable = "MapPresetConfig";
        private const string LayerTable = "PresetLayerConfig";

        /// <summary>装载。任一张表读不到都返回 null（调用方决定降级到灰盒名录，见 §10 E12）。</summary>
        public static RunMapCatalog LoadFromConfigs()
        {
            Biomes.Clear();          //W5.2：每次装载都重建地形登记（表改了就跟着变，§12 A5）
            BiomeByPreset.Clear();
            var cat = new RunMapCatalog();

            var biomes = Read<MapBiomeConfig>(BiomeTable);
            var chunks = Read<MapChunkConfig>(ChunkTable);
            var sockets = Read<ChunkSocketConfig>(SocketTable);
            var slots = Read<ChunkSlotConfig>(SlotTable);
            var presets = Read<MapPresetConfig>(PresetTable);
            var layers = Read<PresetLayerConfig>(LayerTable);

            if (biomes == null || chunks == null || presets == null)
            {
                LastReport = "配置表读取失败（" + BiomeTable + " / " + ChunkTable + " / " + PresetTable + "）";
                return null;
            }

            int nBiome = 0, nChunk = 0, nSock = 0, nSlot = 0, nPreset = 0, nLayer = 0;

            // ① 主题：同时登记"层 → 主题"的映射（跨层地图按层取主题靠它）
            for (int i = 0; i < biomes.datas.Count; i++)
            {
                MapBiomeConfig b = biomes.datas[i];
                if (!b.enable) continue;
                cat.RegisterTheme((MapLayer)b.layer, b.themeKey);
                RegisterBiome(b);   //W5.2：地形倍率 / 掉落组（只读进运行时结构，不参与生成）
                nBiome++;
            }

            // ② 房间块
            for (int i = 0; i < chunks.datas.Count; i++)
            {
                MapChunkConfig c = chunks.datas[i];
                if (!c.enable) continue;
                cat.Add(new ChunkDef
                {
                    Id = c.Id,
                    Kind = (ChunkKind)c.kind,
                    Layer = (MapLayer)c.layer,
                    ThemeKey = c.themeKey,
                    PrefabPath = c.prefabPath,
                    Weight = (int)c.weight,
                    MaxCount = (int)c.maxCount,
                    Unique = c.isUnique,
                });
                nChunk++;
            }

            // ③ 接口 / 标记点：按 chunkId 挂回各自的块
            if (sockets != null)
            {
                for (int i = 0; i < sockets.datas.Count; i++)
                {
                    ChunkSocketConfig s = sockets.datas[i];
                    ChunkDef d = cat.Get(s.chunkId);
                    if (d == null) continue;
                    d.Sockets.Add(new SocketDef
                    {
                        Dir = (SocketDir)s.dir,
                        Profile = (int)s.heightProfile,
                        Type = (int)s.socketType,
                        X = s.posX,
                        Y = s.posY,
                    });
                    nSock++;
                }
            }
            if (slots != null)
            {
                for (int i = 0; i < slots.datas.Count; i++)
                {
                    ChunkSlotConfig s = slots.datas[i];
                    ChunkDef d = cat.Get(s.chunkId);
                    if (d == null) continue;
                    d.Slots.Add(new SlotDef { SlotType = (int)s.slotType, X = s.posX, Y = s.posY });
                    nSlot++;
                }
            }

            // ④ 预设 + 层序
            var byPresetId = new Dictionary<uint, PresetDef>();
            for (int i = 0; i < presets.datas.Count; i++)
            {
                MapPresetConfig p = presets.datas[i];
                if (!p.enable) continue;
                var def = new PresetDef
                {
                    Id = (int)p.Id,
                    BiomeId = p.biomeId,
                    Name = p.nameCN,
                    Enabled = true,
                    MainPathMin = (int)p.mainPathMin,
                    MainPathMax = (int)p.mainPathMax,
                    BranchMin = (int)p.branchMin,
                    BranchMax = (int)p.branchMax,
                    TotalMin = (int)p.totalMin,
                    TotalMax = (int)p.totalMax,
                    SpecialTreasure = (int)p.specialTreasure,
                    SpecialCamp = (int)p.specialCamp,
                    RetryMax = (int)p.retryMax,
                };
                cat.AddPreset(def);
                BiomeByPreset[p.Id] = p.biomeId;   //W5.2：场景 → 地形 走 MapPreset.biomeId
                byPresetId[p.Id] = def;
                nPreset++;
            }
            if (layers != null)
            {
                for (int i = 0; i < layers.datas.Count; i++)
                {
                    PresetLayerConfig l = layers.datas[i];
                    PresetDef def;
                    if (!byPresetId.TryGetValue(l.presetId, out def)) continue;
                    def.Layers.Add(new LayerSpec
                    {
                        Layer = (MapLayer)l.layer,
                        Order = (int)l.layerOrder,
                        IsMainPath = l.isMainPath,
                        LinkFromOrder = (int)l.linkFromLayerOrder,
                        AllowDirect = l.allowDirect,
                        LinkMin = (int)l.linkMin,
                        LinkMax = (int)l.linkMax,
                    });
                    nLayer++;
                }
            }

            LastReport = "主题 " + nBiome + " / 房间块 " + nChunk + " / 接口 " + nSock +
                         " / 标记点 " + nSlot + " / 预设 " + nPreset + " / 层序 " + nLayer;
            return cat;
        }

        // ══════════════════════════ 地形（W5.2 / M14） ══════════════════════════
        //
        // 口径（Lead 已裁决）：`MapBiome_地图主题表` 末尾加 3 列
        // `EnemyHpMul`(float) / `EnemyDmgMul`(float) / `DropGroupBase`(uint)；
        // 「场景 → 地形」不新建映射，走**已存在**的 `MapPreset.biomeId`。
        // 这里只做**读取**：把表读成运行时 `BiomeInfo`，生成逻辑一行不改。

        /// <summary>`MapBiome` 行（Id → 地形）。每次 `LoadFromConfigs` 重建。</summary>
        private static readonly Dictionary<uint, BiomeInfo> Biomes = new Dictionary<uint, BiomeInfo>();

        /// <summary>`MapPreset.Id` → `biomeId`。每次 `LoadFromConfigs` 重建。</summary>
        private static readonly Dictionary<uint, uint> BiomeByPreset = new Dictionary<uint, uint>();

        /// <summary>`preset.biomeId` 悬空时的兜底地形（§9 E2：视为地表）。</summary>
        public const uint DefaultBiomeId = 2u;

        /// <summary>按 Id 取地形；表里没有则返回 null。</summary>
        public static BiomeInfo GetBiome(uint biomeId)
        {
            BiomeInfo info;
            return Biomes.TryGetValue(biomeId, out info) ? info : null;
        }

        /// <summary>预设 → 地形 Id（`MapPreset.biomeId`；没登记则 0）。</summary>
        public static uint GetBiomeIdForPreset(uint presetId)
        {
            uint id;
            return BiomeByPreset.TryGetValue(presetId, out id) ? id : 0u;
        }

        /// <summary>
        /// **当前场景的地形**（W5.2 唯一入口）：
        /// `RunMapBootstrap.CurrentLayout.PresetId` → `MapPreset.biomeId` → `BiomeInfo`。
        ///
        /// 读不到时（不在局内 / 表没装载 / biomeId 悬空 / 灰盒名录）按 §9 E2 退回「地表」并 Warn，
        /// **绝不返回 null** —— 调用方拿到的倍率至少是 1（E1/E4：不能出现 0 血 0 伤）。
        /// </summary>
        public static BiomeInfo GetCurrentBiome()
        {
            MapLayout layout = RunMapBootstrap.CurrentLayout;
            if (layout == null) return FallbackBiome("当前没有地图布局（不在局内？）");

            uint biomeId = GetBiomeIdForPreset((uint)layout.PresetId);
            if (biomeId == 0u)
                return FallbackBiome("预设 " + layout.PresetId + " 没登记 biomeId（表未装载 / 走了灰盒名录）");

            BiomeInfo info = GetBiome(biomeId);
            if (info != null) return info;

            return FallbackBiome("presetId=" + layout.PresetId + " → biomeId=" + biomeId + " 在 MapBiome 表里不存在");
        }

        /// <summary>兜底地形：地表、倍率 1、掉落组 1（§9 E1 / E2）。</summary>
        private static BiomeInfo FallbackBiome(string why)
        {
            ChaosLog.Warn(LogChannel.Config, "[地形] 兜底地表（" + why + "）：HP倍率=1 伤害倍率=1 掉落组=1");
            return new BiomeInfo
            {
                Id = DefaultBiomeId,
                ThemeKey = "Surface",
                NameCN = "地表",
                Layer = 2u,
                EnemyHpMul = 1f,
                EnemyDmgMul = 1f,
                DropGroupBase = 1u,
            };
        }

        /// <summary>把一行 `MapBiome` 读成 `BiomeInfo`（§9 E1：倍率 ≤0 / 掉落组 0 一律兜底 1 + Warn）。</summary>
        private static void RegisterBiome(MapBiomeConfig b)
        {
            var info = new BiomeInfo
            {
                Id = b.Id,
                ThemeKey = b.themeKey,
                NameCN = b.nameCN,
                Layer = b.layer,
                EnemyHpMul = b.EnemyHpMul,
                EnemyDmgMul = b.EnemyDmgMul,
                DropGroupBase = b.DropGroupBase,
            };

            if (info.EnemyHpMul <= 0f)
            {
                ChaosLog.Warn(LogChannel.Config, "[地形] biomeId=" + info.Id + " 的 EnemyHpMul=" + info.EnemyHpMul + "（≤0）→ 按 1 处理");
                info.EnemyHpMul = 1f;
            }
            if (info.EnemyDmgMul <= 0f)
            {
                ChaosLog.Warn(LogChannel.Config, "[地形] biomeId=" + info.Id + " 的 EnemyDmgMul=" + info.EnemyDmgMul + "（≤0）→ 按 1 处理");
                info.EnemyDmgMul = 1f;
            }
            if (info.DropGroupBase == 0u)
            {
                ChaosLog.Warn(LogChannel.Config, "[地形] biomeId=" + info.Id + " 的 DropGroupBase=0 → 按 1（普通组）处理");
                info.DropGroupBase = 1u;
            }

            Biomes[info.Id] = info;
        }

        private static DataList<T> Read<T>(string tableName) where T : class
        {
            DataList<T> list = ConfigLoader.Instance.LoadData<T>();
            if (list == null || list.datas == null || list.datas.Count == 0)
            {
                ChaosLog.Warn(LogChannel.Config, "[RunMap] 配置表为空或读不到：" + tableName);
                return null;
            }
            return list;
        }
    }
}
