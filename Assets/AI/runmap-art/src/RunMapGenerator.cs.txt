using System;
using System.Collections.Generic;

namespace RunMap
{
    public class RunMapResult
    {
        public bool Ok;
        public bool Degraded;
        public int Attempts;
        public MapLayout Layout;
        public List<string> Errors = new List<string>();
    }

    /// <summary>
    /// 编排：重试 → 校验 → 降级（§6.1 状态机 / §10 E12）。
    ///
    /// 语义：**永远返回一张能玩的图**。校验不通过就换种子重试；重试用尽就退到保底布局，
    /// 并把降级事实记在 `Degraded` 上（§9 U6：玩家无感，只在日志留 Warn）。
    /// </summary>
    public static class RunMapGenerator
    {
        public const int FallbackInnerRooms = 5;

        public static RunMapResult Generate(RunMapContext ctx, PresetDef preset, RunMapCatalog cat, string themeKey,
            System.Action<uint> onStage = null)
        {
            var result = new RunMapResult();
            if (preset == null)
            {
                result.Errors.Add("preset 为空 → 直接保底（§10 E2）");
                return Fallback(ctx, null, cat, themeKey, result.Errors);
            }

            int tries = Math.Max(1, preset.RetryMax);
            for (int attempt = 1; attempt <= tries; attempt++)
            {
                var rng = new RunMapRng(RunMapSeed.DeriveAttempt(ctx.SceneSeed, attempt));
                MapLayout layout = RunMapPlanner.Plan(ctx, preset, cat, themeKey, rng, onStage);
                if (layout == null)
                {
                    result.Errors.Add("attempt " + attempt + "：规划阶段失败（无可用块 / 布局冲突）");
                    continue;
                }
                List<string> errs = RunMapValidator.Validate(layout, preset, cat);
                if (errs.Count == 0)
                {
                    layout.Attempts = attempt;
                    RunMapPlanner.EmitSlots(layout, cat);
                    if (onStage != null) onStage(7);          // 数据级校验通过（阶段码 7 = 校验）
                    result.Ok = true;
                    result.Layout = layout;
                    result.Attempts = attempt;
                    return result;
                }
                result.Errors = errs;
            }

            return Fallback(ctx, preset, cat, themeKey, result.Errors);
        }

        /// <summary>
        /// 保底布局：一条链式主路，全部使用"四向通用块"，不吃任何随机。
        /// 它可能不满足房间数区间（§13 A5 允许这种情况，但必须记 Warn），但**一定连通、一定有 boss 门**。
        /// </summary>
        public static RunMapResult Fallback(RunMapContext ctx, PresetDef preset, RunMapCatalog cat, string themeKey, List<string> why)
        {
            var layout = new MapLayout
            {
                Seed = ctx.SceneSeed,
                SceneIndex = ctx.SceneIndex,
                ThemeKey = themeKey,
                PresetId = preset != null ? preset.Id : 0,
                Degraded = true,
            };

            MapLayer layer = MapLayer.Surface;
            if (preset != null && preset.Layers.Count > 0) layer = preset.Layers[0].Layer;
            else if (themeKey == "Underground") layer = MapLayer.Underground;
            else if (themeKey == "Building") layer = MapLayer.Building;

            int inner = FallbackInnerRooms;
            if (preset != null) inner = Math.Max(2, preset.MainPathMin - 2);

            RoomNode prev = null;
            for (int i = 0; i < inner + 2; i++)
            {
                ChunkKind kind = (i == 0) ? ChunkKind.Entry : (i == inner + 1 ? ChunkKind.BossGate : ChunkKind.Combat);
                var n = new RoomNode
                {
                    Id = (ushort)(i + 1),
                    Kind = kind,
                    Layer = layer,
                    IsMainPath = true,
                    CellX = i,
                    CellY = 0,
                };
                layout.Nodes.Add(n);
                if (i == 0) layout.EntryId = n.Id;
                if (i == inner + 1) layout.BossId = n.Id;
                if (prev != null)
                {
                    layout.Edges.Add(new RoomEdge
                    {
                        From = prev.Id, To = n.Id, Dir = SocketDir.Right, Profile = 0, IsLayerLink = false
                    });
                }
                prev = n;
            }

            // 选块：只挑四条"一定存在"的块（灰盒名录里必然有）
            layout.ChunkUseCount.Clear();
            for (int i = 0; i < layout.Nodes.Count; i++)
            {
                RoomNode n = layout.Nodes[i];
                List<ChunkDef> pool = cat.Pool(themeKey, n.Layer, n.Kind);
                if (pool.Count == 0) pool = cat.Pool(cat.ThemeKeyForLayer(n.Layer), n.Layer, n.Kind);
                if (pool.Count == 0) pool = cat.Pool(themeKey, n.Layer, ChunkKind.Combat);
                if (pool.Count == 0) continue;
                n.ChunkId = pool[0].Id;
                int c; layout.ChunkUseCount.TryGetValue(n.ChunkId, out c);
                layout.ChunkUseCount[n.ChunkId] = c + 1;
            }

            RunMapPlanner.EmitSlots(layout, cat);

            var result = new RunMapResult
            {
                Ok = true,
                Degraded = true,
                Attempts = 0,
                Layout = layout,
            };
            result.Errors.Add("⚠ 已降级到保底布局（可玩，但结构简化）。原因：" +
                (why != null && why.Count > 0 ? why[0] : "未知"));
            return result;
        }

        /// <summary>
        /// 一局 3 个场景。C1（一局内三层各至少出现一次）由 RunMapCatalog.AllocateRunPresets 保证。
        /// </summary>
        public static List<RunMapResult> GenerateRun(uint runSeed, int era, int depth, RunMapCatalog cat)
        {
            var list = new List<RunMapResult>();
            List<PresetDef> presets = cat.AllocateRunPresets();
            for (int i = 0; i < 3; i++)
            {
                var ctx = new RunMapContext { RunSeed = runSeed, SceneIndex = i + 1, Era = era, Depth = depth };
                PresetDef p = (i < presets.Count) ? presets[i] : null;
                string theme = ThemeKeyOf(p);
                list.Add(Generate(ctx, p, cat, theme));
            }
            return list;
        }

        public static string ThemeKeyOf(PresetDef p)
        {
            if (p == null || p.Layers.Count == 0) return "Surface";
            switch (p.Layers[0].Layer)
            {
                case MapLayer.Underground: return "Underground";
                case MapLayer.Building: return "Building";
                default: return "Surface";
            }
        }

        /// <summary>一局的三层覆盖检查（C1）。返回缺失的层（空 = 通过）。</summary>
        public static List<MapLayer> MissingLayers(List<RunMapResult> run)
        {
            var covered = new List<MapLayer>();
            for (int i = 0; i < run.Count; i++)
            {
                if (run[i] == null || run[i].Layout == null) continue;
                for (int k = 0; k < run[i].Layout.Nodes.Count; k++)
                {
                    MapLayer l = run[i].Layout.Nodes[k].Layer;
                    if (!covered.Contains(l)) covered.Add(l);
                }
            }
            var missing = new List<MapLayer>();
            MapLayer[] need = { MapLayer.Underground, MapLayer.Surface, MapLayer.Building };
            for (int i = 0; i < need.Length; i++) if (!covered.Contains(need[i])) missing.Add(need[i]);
            return missing;
        }
    }
}
