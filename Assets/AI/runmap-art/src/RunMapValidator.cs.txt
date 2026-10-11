using System;
using System.Collections.Generic;

namespace RunMap
{
    /// <summary>
    /// §4.4 的 C10.1~C10.10：**生成后 100% 校验**。
    /// 原则：校验不过的图绝不交给玩家 —— 校验器只报告问题，重试/降级由 RunMapGenerator 决定。
    /// 纯逻辑，不依赖场景，所以能对着上千个种子跑。
    /// </summary>
    public static class RunMapValidator
    {
        public static List<string> Validate(MapLayout layout, PresetDef preset, RunMapCatalog cat)
        {
            var errs = new List<string>();
            if (layout == null) { errs.Add("layout=null"); return errs; }
            if (layout.Nodes.Count == 0) { errs.Add("C10.1 没有房间"); return errs; }

            var adj = BuildAdjacency(layout);

            // ── C10.1 全图连通 ──
            int reached = BfsCount(adj, layout.EntryId, null);
            if (reached != layout.Nodes.Count)
                errs.Add("C10.1 连通性不足：" + reached + "/" + layout.Nodes.Count);

            // ── C10.2 入口→boss 主路长度 ──
            int hops = BfsDistance(adj, layout.EntryId, layout.BossId);
            if (hops < 0) errs.Add("C10.2 入口到 boss 门不可达");
            else
            {
                int rooms = hops + 1;
                if (rooms < preset.MainPathMin || rooms > preset.MainPathMax)
                    errs.Add("C10.2 主路房间数 " + rooms + " 不在 [" + preset.MainPathMin + "," + preset.MainPathMax + "]");
            }

            // ── 占用唯一 ──
            var cells = new HashSet<long>();
            for (int i = 0; i < layout.Nodes.Count; i++)
            {
                RoomNode n = layout.Nodes[i];
                long k = ((long)n.CellX << 32) ^ (uint)n.CellY;
                if (!cells.Add(k)) errs.Add("房间格重叠：(" + n.CellX + "," + n.CellY + ")");
            }

            // ── C10.3 接口匹配 / C10.7 层直连 ──
            for (int i = 0; i < layout.Edges.Count; i++)
            {
                RoomEdge e = layout.Edges[i];
                RoomNode a = layout.Find(e.From);
                RoomNode b = layout.Find(e.To);
                if (a == null || b == null) { errs.Add("边端点缺失 " + e.From + ">" + e.To); continue; }

                int layerDelta = Math.Abs((int)a.Layer - (int)b.Layer);
                if (layerDelta > 1) errs.Add("C10.7 跨了 " + layerDelta + " 层：" + a.Id + ">" + b.Id);
                if ((a.Layer == MapLayer.Underground && b.Layer == MapLayer.Building) ||
                    (a.Layer == MapLayer.Building && b.Layer == MapLayer.Underground))
                    errs.Add("C10.7 地下直连地表建筑：" + a.Id + ">" + b.Id);

                SocketDir dir = DirFromTo(a, b);
                if (dir == 0) { errs.Add("C10.3 两端不共边 " + a.Id + ">" + b.Id); continue; }
                if (dir != e.Dir) errs.Add("C10.3 方向记录不符 " + a.Id + ">" + b.Id + " 记 " + e.Dir + " 实为 " + dir);

                if (!RunMapConst.IsVertical(dir))
                {
                    // 横向：两端块必须有对应侧门，且高度档一致
                    if (!ChunkHasDir(cat, a, dir)) errs.Add("C10.3 " + a.Id + " 的块没有 " + dir + " 门");
                    if (!ChunkHasDir(cat, b, RunMapConst.Opposite(dir))) errs.Add("C10.3 " + b.Id + " 的块没有 " + RunMapConst.Opposite(dir) + " 门");
                    int pa = ChunkProfile(cat, a, dir), pb = ChunkProfile(cat, b, RunMapConst.Opposite(dir));
                    if (pa >= 0 && pb >= 0 && pa != pb)
                        errs.Add("C10.3 高度档不一致 " + a.Id + "(" + pa + ") vs " + b.Id + "(" + pb + ")");
                }
                else
                {
                    // 竖直：靠**同列**匹配（§16.5），不是高度档
                    if (a.CellX != b.CellX)
                        errs.Add("C10.3 竖直连接不同列 " + a.Id + "(" + a.CellX + ") vs " + b.Id + "(" + b.CellX + ")");
                    if (!ChunkHasDir(cat, a, dir)) errs.Add("C10.3 " + a.Id + " 的块没有 " + dir + " 开口");
                    if (!ChunkHasDir(cat, b, RunMapConst.Opposite(dir))) errs.Add("C10.3 " + b.Id + " 的块没有 " + RunMapConst.Opposite(dir) + " 开口");
                }
            }

            // ── C10.4 每层内部连通 ──
            var layers = new List<MapLayer>();
            for (int i = 0; i < layout.Nodes.Count; i++) if (!layers.Contains(layout.Nodes[i].Layer)) layers.Add(layout.Nodes[i].Layer);
            for (int li = 0; li < layers.Count; li++)
            {
                MapLayer layer = layers[li];
                ushort first = 0; int count = 0;
                for (int i = 0; i < layout.Nodes.Count; i++)
                    if (layout.Nodes[i].Layer == layer) { if (first == 0) first = layout.Nodes[i].Id; count++; }
                int got = BfsCount(adj, first, n => layout.Find(n).Layer == layer);
                if (got != count) errs.Add("C10.4 层 " + (int)layer + " 内有 " + (count - got) + " 个房间不连通");
            }

            // ── C10.5 层间连接数 + C10.6 同列对齐 ──
            List<LayerSpec> specs = preset.SortedLayers();
            for (int si = 0; si < specs.Count; si++)
            {
                LayerSpec to = specs[si];
                if (to.LinkFromOrder <= 0) continue;
                LayerSpec from = FindByOrder(specs, to.LinkFromOrder);
                if (from == null) continue;
                int links = 0;
                for (int i = 0; i < layout.Nodes.Count; i++)
                {
                    RoomNode n = layout.Nodes[i];
                    if (!n.IsLayerLink) continue;
                    if (n.Layer == from.Layer) links++;
                }
                if (links < to.LinkMin || links > to.LinkMax)
                    errs.Add("C10.5 层 " + from.Order + "→" + to.Order + " 连接件 " + links + " 个，不在 [" + to.LinkMin + "," + to.LinkMax + "]");

                // C10.6：连接件的上下相邻节点必须与它同列
                for (int i = 0; i < layout.Nodes.Count; i++)
                {
                    RoomNode link = layout.Nodes[i];
                    if (!link.IsLayerLink) continue;
                    List<ushort> nb = adj.ContainsKey(link.Id) ? adj[link.Id] : new List<ushort>();
                    for (int k = 0; k < nb.Count; k++)
                    {
                        RoomNode other = layout.Find(nb[k]);
                        if (other == null) continue;
                        if (other.CellX != link.CellX)
                            errs.Add("C10.6 连接件 " + link.Id + " 与相邻房 " + other.Id + " 不同列（" + link.CellX + " vs " + other.CellX + "）");
                    }
                }
            }

            // ── C10.8 最小可离开性 / C10.9 boss 是唯一终点 / C10.10 去掉 boss 仍连通 ──
            for (int i = 0; i < layout.Nodes.Count; i++)
            {
                RoomNode n = layout.Nodes[i];
                int deg = adj.ContainsKey(n.Id) ? adj[n.Id].Count : 0;
                if (deg == 0) errs.Add("C10.8 房间 " + n.Id + " 没有任何接口");
                if (n.Kind == ChunkKind.BossGate && deg != 1) errs.Add("C10.9 boss 门度数应为 1，实际 " + deg);
                if (n.Id != layout.BossId && deg == 1 && n.Kind != ChunkKind.DeadEnd && n.Id != layout.EntryId)
                    errs.Add("C10.8 房间 " + n.Id + "（" + n.Kind + "）是单口叶节点，既不是死路奖励也不是端点");
            }
            int withoutBoss = BfsCount(adj, layout.EntryId, id => id != layout.BossId);
            if (withoutBoss != layout.Nodes.Count - 1)
                errs.Add("C10.10 去掉 boss 门后有房间不连通：" + withoutBoss + "/" + (layout.Nodes.Count - 1));

            // ── C6 特殊房数量（灰盒里"死路奖励房"就是宝藏房）──
            int deadEnds = 0;
            for (int i = 0; i < layout.Nodes.Count; i++) if (layout.Nodes[i].Kind == ChunkKind.DeadEnd) deadEnds++;
            if (deadEnds < preset.SpecialTreasure)
                errs.Add("C6 死路奖励房 " + deadEnds + " 少于配置的 " + preset.SpecialTreasure);

            // ── 块使用约束（S5）──
            foreach (KeyValuePair<uint, int> kv in layout.ChunkUseCount)
            {
                ChunkDef d = cat.Get(kv.Key);
                if (d == null) { errs.Add("用了不存在的块 " + kv.Key); continue; }
                if (d.Unique && kv.Value > 1) errs.Add("唯一块 " + kv.Key + " 出现 " + kv.Value + " 次");
                if (kv.Value > d.MaxCount) errs.Add("块 " + kv.Key + " 超过 maxCount " + kv.Value + "/" + d.MaxCount);
            }

            return errs;
        }

        private static LayerSpec FindByOrder(List<LayerSpec> specs, int order)
        {
            for (int i = 0; i < specs.Count; i++) if (specs[i].Order == order) return specs[i];
            return null;
        }

        private static bool ChunkHasDir(RunMapCatalog cat, RoomNode n, SocketDir dir)
        {
            ChunkDef d = cat.Get(n.ChunkId);
            return d != null && d.HasDir(dir);
        }

        private static int ChunkProfile(RunMapCatalog cat, RoomNode n, SocketDir dir)
        {
            ChunkDef d = cat.Get(n.ChunkId);
            return d == null ? -1 : d.ProfileOf(dir);
        }

        private static SocketDir DirFromTo(RoomNode a, RoomNode b)
        {
            if (b.CellX > a.CellX) return SocketDir.Right;
            if (b.CellX < a.CellX) return SocketDir.Left;
            if (b.CellY > a.CellY) return SocketDir.Up;
            if (b.CellY < a.CellY) return SocketDir.Down;
            return 0;
        }

        private static Dictionary<ushort, List<ushort>> BuildAdjacency(MapLayout layout)
        {
            var adj = new Dictionary<ushort, List<ushort>>();
            for (int i = 0; i < layout.Nodes.Count; i++) adj[layout.Nodes[i].Id] = new List<ushort>();
            for (int i = 0; i < layout.Edges.Count; i++)
            {
                RoomEdge e = layout.Edges[i];
                if (!adj.ContainsKey(e.From)) adj[e.From] = new List<ushort>();
                if (!adj.ContainsKey(e.To)) adj[e.To] = new List<ushort>();
                adj[e.From].Add(e.To);
                adj[e.To].Add(e.From);
            }
            return adj;
        }

        /// <summary>从 start 出发能到达的节点数；filter 为 null 表示不过滤。</summary>
        private static int BfsCount(Dictionary<ushort, List<ushort>> adj, ushort start, Func<ushort, bool> filter)
        {
            var seen = new HashSet<ushort>();
            var q = new Queue<ushort>();
            if (!adj.ContainsKey(start)) return 0;
            seen.Add(start); q.Enqueue(start);
            while (q.Count > 0)
            {
                ushort cur = q.Dequeue();
                List<ushort> nb = adj[cur];
                for (int i = 0; i < nb.Count; i++)
                {
                    ushort nxt = nb[i];
                    if (seen.Contains(nxt)) continue;
                    if (filter != null && !filter(nxt)) continue;
                    seen.Add(nxt); q.Enqueue(nxt);
                }
            }
            return seen.Count;
        }

        /// <summary>最短跳数；不可达返回 -1。</summary>
        private static int BfsDistance(Dictionary<ushort, List<ushort>> adj, ushort from, ushort to)
        {
            if (from == to) return 0;
            var dist = new Dictionary<ushort, int>();
            var q = new Queue<ushort>();
            dist[from] = 0; q.Enqueue(from);
            while (q.Count > 0)
            {
                ushort cur = q.Dequeue();
                List<ushort> nb;
                if (!adj.TryGetValue(cur, out nb)) continue;
                for (int i = 0; i < nb.Count; i++)
                {
                    if (dist.ContainsKey(nb[i])) continue;
                    dist[nb[i]] = dist[cur] + 1;
                    if (nb[i] == to) return dist[nb[i]];
                    q.Enqueue(nb[i]);
                }
            }
            return -1;
        }
    }
}
