using System;
using System.Collections.Generic;

namespace RunMap
{
    /// <summary>
    /// Pass A~D：读 preset → 生成抽象房间图 → 嵌入网格 → 选房间块。
    ///
    /// 为什么顺序不能颠倒（§3.2 P1「拓扑先于几何」）：
    /// 先定"谁连着谁"，再定"落在哪一格"，最后才挑具体块。
    /// 反过来先摆块再连边，会退化成一堆互相打架的回溯。
    /// </summary>
    public static class RunMapPlanner
    {
        /// <summary>一条待定边的端点对（方向在布局完成后才能算出来）。</summary>
        private class Pending
        {
            public ushort From, To;
            public bool IsLayerLink;
        }

        /// <summary>最近一次失败的原因。生成器/工具用它留痕，避免"只报失败不报为什么"。</summary>
        public static string LastFailure;

        /// <summary>规划一张图。返回 null 表示本次尝试失败（调用方按 retryMax 重试）。</summary>
        public static MapLayout Plan(RunMapContext ctx, PresetDef preset, RunMapCatalog cat, string themeKey, RunMapRng rng,
            System.Action<uint> onStage = null)
        {
            LastFailure = null;
            var layout = new MapLayout
            {
                Seed = ctx.SceneSeed,
                SceneIndex = ctx.SceneIndex,
                ThemeKey = themeKey,
                PresetId = preset.Id,
            };

            var pending = new List<Pending>();
            var chain = new List<ushort>();
            ushort nextId = 1;

            // ── Pass A/B：主路 + 支路 ──
            BuildMainPath(layout, preset, rng, chain, pending, ref nextId);
            BuildBranches(layout, preset, rng, chain, pending, ref nextId);
            if (layout.Nodes.Count < preset.TotalMin || layout.Nodes.Count > preset.TotalMax)
            {
                LastFailure = "房间总数 " + layout.Nodes.Count + " 不在 [" + preset.TotalMin + "," + preset.TotalMax + "]";
                return null;
            }

            // 阶段码与 MapLoading 表一致：2 生成房间图 / 3 排布网格 / 4 挑选房间块
            if (onStage != null) onStage(2);

            // ── Pass C：网格布局（含冲突回溯）──
            if (onStage != null) onStage(3);
            if (!Place(layout, chain, pending))
            {
                if (string.IsNullOrEmpty(LastFailure)) LastFailure = "Pass C 网格布局失败（房间 " + layout.Nodes.Count + "）";
                return null;
            }

            // ── Pass D：按方向/层/类型选块 ──
            if (onStage != null) onStage(4);
            if (!Pick(layout, preset, cat, themeKey, rng, pending))
            {
                if (string.IsNullOrEmpty(LastFailure)) LastFailure = "Pass D 选块失败（房间 " + layout.Nodes.Count + "）";
                return null;
            }

            // 边 = 待定边 + 由坐标算出的方向；顺带把高度档填进边里（校验器要用）
            for (int i = 0; i < pending.Count; i++)
            {
                RoomNode a = layout.Find(pending[i].From);
                RoomNode b = layout.Find(pending[i].To);
                SocketDir dir = DirFromTo(a, b);
                if (dir == 0) return null;
                layout.Edges.Add(new RoomEdge
                {
                    From = a.Id,
                    To = b.Id,
                    Dir = dir,
                    Profile = a.Profile,
                    IsLayerLink = pending[i].IsLayerLink,
                });
            }

            return layout;
        }

        // ═════════════════════════ 主路 ═════════════════════════

        private static void BuildMainPath(MapLayout layout, PresetDef preset, RunMapRng rng,
            List<ushort> chain, List<Pending> pending, ref ushort nextId)
        {
            List<LayerSpec> layers = preset.SortedLayers();
            var mainLayers = new List<LayerSpec>();
            for (int i = 0; i < layers.Count; i++) if (layers[i].IsMainPath) mainLayers.Add(layers[i]);
            if (mainLayers.Count == 0 && layers.Count > 0) mainLayers.Add(layers[0]);

            int total = rng.Range(preset.MainPathMin, preset.MainPathMax);
            if (total < 3) total = 3;
            int inner = total - 2;                                  // 扣掉入口与 boss 门

            // 内部节点按层均分（余数给前面的层）
            var quota = new int[mainLayers.Count];
            for (int i = 0; i < quota.Length; i++) quota[i] = inner / mainLayers.Count;
            for (int i = 0; i < inner % mainLayers.Count; i++) quota[i]++;

            RoomNode entry = NewNode(layout, ref nextId, ChunkKind.Entry, mainLayers[0].Layer, true, false);
            layout.EntryId = entry.Id;
            RoomNode prev = entry;
            chain.Add(entry.Id);

            for (int li = 0; li < mainLayers.Count; li++)
            {
                for (int k = 0; k < quota[li]; k++)
                {
                    // 通路与战斗交替：纯战斗房连排会让节奏单调（灰盒阶段先保证结构）
                    ChunkKind kind = (k % 3 == 2) ? ChunkKind.Corridor : ChunkKind.Combat;
                    RoomNode n = NewNode(layout, ref nextId, kind, mainLayers[li].Layer, true, false);
                    pending.Add(new Pending { From = prev.Id, To = n.Id });
                    chain.Add(n.Id);
                    prev = n;
                }

                if (li < mainLayers.Count - 1)
                {
                    RoomNode link = NewNode(layout, ref nextId, ChunkKind.LayerLink, mainLayers[li].Layer, true, true);
                    pending.Add(new Pending { From = prev.Id, To = link.Id, IsLayerLink = true });
                    chain.Add(link.Id);
                    prev = link;
                }
            }

            RoomNode boss = NewNode(layout, ref nextId, ChunkKind.BossGate, mainLayers[mainLayers.Count - 1].Layer, true, false);
            pending.Add(new Pending { From = prev.Id, To = boss.Id });
            chain.Add(boss.Id);
            layout.BossId = boss.Id;
        }

        // ═════════════════════════ 支路 ═════════════════════════

        private static void BuildBranches(MapLayout layout, PresetDef preset, RunMapRng rng,
            List<ushort> chain, List<Pending> pending, ref ushort nextId)
        {
            // ⚠ BranchMin/BranchMax 是**支路房间总数（含收尾的死路奖励房）**，不是"分叉条数"。
            // 只有这样：房间总数 = 主路房间数 + 支路房间数，才和 MapPreset.totalMin/totalMax 对得上。
            int branchRooms = rng.Range(preset.BranchMin, preset.BranchMax);
            if (branchRooms < 2) return;

            // 每条支路 = 1 个死路奖励房 ⇒ 条数至少覆盖配置的宝藏房数，且每条至少 2 间
            int wantBranches = Math.Max(1, preset.SpecialTreasure);
            int branches = Math.Min(wantBranches, Math.Max(1, branchRooms / 2));

            // 只从主路的中段挂支路：入口和 boss 门是端点，挂上去会变成"进门就分叉"
            var hosts = new List<RoomNode>();
            for (int i = 1; i < chain.Count - 1; i++)
            {
                RoomNode n = layout.Find(chain[i]);
                if (n != null && !n.IsLayerLink) hosts.Add(n);
            }
            if (hosts.Count == 0) return;

            int left = branchRooms;
            for (int b = 0; b < branches; b++)
            {
                int remainBranches = branches - b;
                int rooms = Math.Max(2, left / remainBranches);
                if (rooms > left) rooms = left;
                left -= rooms;
                int middles = rooms - 1;

                RoomNode host = hosts[rng.Range(hosts.Count)];
                RoomNode prev = host;
                for (int k = 0; k < middles; k++)
                {
                    RoomNode n = NewNode(layout, ref nextId, rng.Chance(60) ? ChunkKind.Combat : ChunkKind.Corridor,
                        host.Layer, false, false);
                    pending.Add(new Pending { From = prev.Id, To = n.Id });
                    prev = n;
                }
                RoomNode dead = NewNode(layout, ref nextId, ChunkKind.DeadEnd, host.Layer, false, false);
                pending.Add(new Pending { From = prev.Id, To = dead.Id });
            }
        }

        private static RoomNode NewNode(MapLayout layout, ref ushort nextId, ChunkKind kind, MapLayer layer, bool main, bool link)
        {
            var n = new RoomNode
            {
                Id = nextId++,
                Kind = kind,
                Layer = layer,
                IsMainPath = main,
                IsLayerLink = link,
            };
            layout.Nodes.Add(n);
            return n;
        }

        // ═════════════════════════ 网格布局 ═════════════════════════

        private static bool Place(MapLayout layout, List<ushort> chain, List<Pending> pending)
        {
            var used = new HashSet<long>();

            // ① 主路：先横向铺，遇到层间连接件就上抬一层（§4.4 C10.6：连接件与其前后节点同列）
            int curX = 0, curY = 0;
            bool prevWasLink = false, any = false;
            for (int i = 0; i < chain.Count; i++)
            {
                RoomNode n = layout.Find(chain[i]);
                if (n == null) return false;

                if (n.IsLayerLink)
                {
                    curY += 1;                                      // 连接件站在前驱正上方
                    prevWasLink = true;
                }
                else
                {
                    // ⚠ 上层第一个房间要再抬一层：连接件自己占了一格，停在同列 ≠ 留在同一格。
                    // 只"不前进 x"是不够的 —— 那样会和连接件落在同一格里（这里踩过一次）。
                    if (prevWasLink) { curY += 1; prevWasLink = false; }
                    else if (any) curX += 1;
                }

                if (!Occupy(used, n, curX, curY))
                {
                    LastFailure = "主路房 " + n.Id + " 落在 (" + curX + "," + curY + ") 已被占";
                    return false;
                }
                any = true;
            }

            // ② 支路：优先挂在主路节点正上方 / 正下方（竖直开口），再横向延伸
            var nodes = new List<RoomNode>(layout.Nodes);
            for (int i = 0; i < pending.Count; i++)
            {
                RoomNode a = layout.Find(pending[i].From);
                RoomNode b = layout.Find(pending[i].To);
                if (b == null || b.IsMainPath) continue;            // 只处理支路里的节点
                if (b.CellY != 0 || b.CellX != 0) continue;         // 已布局
                if (a == null) return false;

                if (!PlaceBranchNode(layout, used, a, b, pending[i]))
                {
                    LastFailure = "支路房 " + b.Id + "(" + b.Kind + ") 放不下：父房 " + a.Id + " 在 (" + a.CellX + "," + a.CellY + ")";
                    return false;
                }
            }
            return true;
        }

        /// <summary>把支路节点 b 放到 a 的某个相邻格上（优先上/下，其次左右）。</summary>
        private static bool PlaceBranchNode(MapLayout layout, HashSet<long> used, RoomNode a, RoomNode b, Pending edge)
        {
            int[][] dirs = { new[] { 0, 1 }, new[] { 0, -1 }, new[] { 1, 0 }, new[] { -1, 0 } };
            // 已经放在某个格子上的支路节点：后续节点必须从它出发
            for (int d = 0; d < dirs.Length; d++)
            {
                int x = a.CellX + dirs[d][0];
                int y = a.CellY + dirs[d][1];
                if (!Occupy(used, b, x, y)) continue;
                // 水平连接要求 b 有朝向 a 的侧门
                SocketDir dir = DirFromTo(a, b);
                if (RunMapConst.IsVertical(dir) && d > 1) { /* 竖直落位优先 */ }
                if (!RunMapConst.IsVertical(dir))
                {
                    // 侧向：块必须有对应侧门（灰盒里 DeadEnd 只有左侧门）
                    if (dir == SocketDir.Right && !HasKindSide(b.Kind, SocketDir.Left)) { Release(used, b); continue; }
                    if (dir == SocketDir.Left && !HasKindSide(b.Kind, SocketDir.Right)) { Release(used, b); continue; }
                }
                return true;
            }
            return false;
        }

        private static bool HasKindSide(ChunkKind kind, SocketDir side)
        {
            // 灰盒形状约束：入口只有右门；死路只有左门（进入方向的反向可返回）
            if (kind == ChunkKind.Entry) return side == SocketDir.Right || side == SocketDir.Up;
            if (kind == ChunkKind.DeadEnd) return side == SocketDir.Left || side == SocketDir.Up || side == SocketDir.Down;
            if (kind == ChunkKind.LayerLink) return side == SocketDir.Up || side == SocketDir.Down;
            if (kind == ChunkKind.BossGate) return side == SocketDir.Left;
            return true; // Corridor / Combat 四向都有
        }

        private static bool Occupy(HashSet<long> used, RoomNode n, int x, int y)
        {
            long key = Key(x, y);
            if (used.Contains(key)) return false;
            used.Add(key);
            n.CellX = x;
            n.CellY = y;
            return true;
        }

        private static void Release(HashSet<long> used, RoomNode n)
        {
            used.Remove(Key(n.CellX, n.CellY));
            n.CellX = 0;
            n.CellY = 0;
        }

        private static long Key(int x, int y) { return ((long)x << 32) ^ (uint)y; }

        private static SocketDir DirFromTo(RoomNode a, RoomNode b)
        {
            if (b.CellX > a.CellX) return SocketDir.Right;
            if (b.CellX < a.CellX) return SocketDir.Left;
            if (b.CellY > a.CellY) return SocketDir.Up;
            if (b.CellY < a.CellY) return SocketDir.Down;
            return 0;
        }

        // ═════════════════════════ 选块（Pass D） ═════════════════════════

        private static bool Pick(MapLayout layout, PresetDef preset, RunMapCatalog cat, string themeKey,
            RunMapRng rng, List<Pending> pending)
        {
            // 每个节点需要的方向集合（由待定边推出来）
            var need = new Dictionary<ushort, List<SocketDir>>();
            for (int i = 0; i < pending.Count; i++)
            {
                RoomNode a = layout.Find(pending[i].From);
                RoomNode b = layout.Find(pending[i].To);
                SocketDir dir = DirFromTo(a, b);
                if (dir == 0) return false;
                Add(need, a.Id, dir);
                Add(need, b.Id, RunMapConst.Opposite(dir));
            }

            for (int i = 0; i < layout.Nodes.Count; i++)
            {
                RoomNode n = layout.Nodes[i];
                List<SocketDir> dirs = need.ContainsKey(n.Id) ? need[n.Id] : new List<SocketDir>();
                List<ChunkDef> pool = cat.Pool(themeKey, n.Layer, n.Kind);
                // 跨层地图：本层在当前主题下没有内容时，退到该层的规范主题（层 → 主题映射）
                if (pool.Count == 0) pool = cat.Pool(cat.ThemeKeyForLayer(n.Layer), n.Layer, n.Kind);
                var ok = new List<ChunkDef>();
                for (int c = 0; c < pool.Count; c++)
                {
                    ChunkDef d = pool[c];
                    if (d.Unique && layout.ChunkUseCount.ContainsKey(d.Id)) continue;
                    int usedCount;
                    if (layout.ChunkUseCount.TryGetValue(d.Id, out usedCount) && usedCount >= d.MaxCount) continue;
                    bool fits = true;
                    for (int k = 0; k < dirs.Count; k++) if (!d.HasDir(dirs[k])) { fits = false; break; }
                    if (fits) ok.Add(d);
                }
                if (ok.Count == 0) return false;

                // 加权抽取（块池只有一个时等价于直接选中）
                int totalW = 0;
                for (int c = 0; c < ok.Count; c++) totalW += Math.Max(1, ok[c].Weight);
                int pick = rng.Range(totalW), acc = 0;
                ChunkDef chosen = ok[ok.Count - 1];
                for (int c = 0; c < ok.Count; c++)
                {
                    acc += Math.Max(1, ok[c].Weight);
                    if (pick < acc) { chosen = ok[c]; break; }
                }

                n.ChunkId = chosen.Id;
                // 横向门的高度档以块为准（竖直开口不看档位）
                for (int k = 0; k < dirs.Count; k++)
                {
                    if (RunMapConst.IsVertical(dirs[k])) continue;
                    int p = chosen.ProfileOf(dirs[k]);
                    if (p >= 0) n.Profile = p;
                }
                int cnt;
                layout.ChunkUseCount.TryGetValue(chosen.Id, out cnt);
                layout.ChunkUseCount[chosen.Id] = cnt + 1;
            }
            return true;
        }

        private static void Add(Dictionary<ushort, List<SocketDir>> map, ushort id, SocketDir dir)
        {
            List<SocketDir> list;
            if (!map.TryGetValue(id, out list)) { list = new List<SocketDir>(); map[id] = list; }
            if (!list.Contains(dir)) list.Add(dir);
        }

        // ═════════════════════════ 内容填充（Pass F，位置部分） ═════════════════════════

        /// <summary>
        /// 把每个房间块的标记点转成世界坐标（本系统**只给位置**，敌人/掉落由 M8/M9/M12 填）。
        /// </summary>
        public static void EmitSlots(MapLayout layout, RunMapCatalog cat)
        {
            layout.Slots.Clear();
            for (int i = 0; i < layout.Nodes.Count; i++)
            {
                RoomNode n = layout.Nodes[i];
                ChunkDef def = cat.Get(n.ChunkId);
                if (def == null) continue;
                for (int s = 0; s < def.Slots.Count; s++)
                {
                    layout.Slots.Add(new SpawnSlot
                    {
                        RoomId = n.Id,
                        Index = s + 1,
                        SlotType = def.Slots[s].SlotType,
                        X = n.CellX * RunMapConst.ChunkW + def.Slots[s].X,
                        Y = n.CellY * RunMapConst.ChunkH + def.Slots[s].Y,
                    });
                }
            }
        }
    }
}
