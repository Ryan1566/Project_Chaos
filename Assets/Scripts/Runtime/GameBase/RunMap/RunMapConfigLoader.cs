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
