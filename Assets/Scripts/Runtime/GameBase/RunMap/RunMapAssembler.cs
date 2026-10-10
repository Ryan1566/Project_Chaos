using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using ChaosDebug;

namespace RunMap
{
    /// <summary>
    /// **Pass E（装配）运行时实现**：把 <see cref="MapLayout"/> 变成场景里的实体。
    ///
    /// 三件事：
    /// ① 按房间格坐标从 `Resources` 取房间块 prefab 并实例化（走 `ResManager`，拿到的就是实例）；
    /// ② **封堵未使用的开口** —— v2 规格让每个房间都带 L/R/U/D 开口，没接东西的洞口必须封，
    ///    否则玩家会从天花板 / 地板掉出地图；
    /// ③ **地形起伏** —— 按"种子 + 房间 ID"确定性地加高台 / 坑 / 柱子 / 台阶，让每个房间长得不一样。
    ///
    /// 瓦片引用取自房间块自带的 <see cref="RunMapChunkTiles"/>（不依赖编辑器 API）。
    /// </summary>
    public static class RunMapAssembler
    {
        /// <summary>最近一次装配里每个房间用了什么地形修饰（留痕 / 校验用）。</summary>
        public static readonly List<string> Decorations = new List<string>();

        /// <summary>装配整张图。返回拼好的根物体；一个房间都没装上时返回 null。</summary>
        public static GameObject Assemble(MapLayout layout, RunMapCatalog cat, Transform parent, string prefabRootOverride = null)
        {
            if (layout == null || cat == null) return null;

            Dictionary<ushort, List<SocketDir>> used = CollectUsed(layout);
            var root = new GameObject("RunMap_" + layout.ThemeKey + "_" + layout.Seed);
            if (parent != null) root.transform.SetParent(parent, false);
            Decorations.Clear();

            int placed = 0, sealedTotal = 0, decorated = 0;

            for (int i = 0; i < layout.Nodes.Count; i++)
            {
                RoomNode n = layout.Nodes[i];
                ChunkDef def = cat.Get(n.ChunkId);
                if (def == null)
                {
                    ChaosLog.Warn(LogChannel.General, "[RunMap] 布局里的块 " + n.ChunkId + " 不在名录里，跳过房间 " + n.Id);
                    continue;
                }

                string path = string.IsNullOrEmpty(prefabRootOverride) ? def.PrefabPath : prefabRootOverride + "/" + TypeName(n.Kind);
                GameObject go = ResManager.Instance.Load<GameObject>(path);   // GameObject 会自动 Instantiate
                if (go == null)
                {
                    ChaosLog.Error(LogChannel.General, "[RunMap] 房间块加载失败：" + path);
                    continue;
                }

                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3(n.CellX * RunMapConst.ChunkW, n.CellY * RunMapConst.ChunkH, 0f);
                go.name = "R" + n.Id + "_" + TypeName(n.Kind) + "_L" + (int)n.Layer;
                placed++;

                RunMapChunkTiles tiles = go.GetComponent<RunMapChunkTiles>();
                Tile fill = tiles != null ? tiles.fill : null;
                Tile top = tiles != null ? tiles.top : null;

                List<SocketDir> dirs = used.ContainsKey(n.Id) ? used[n.Id] : new List<SocketDir>();
                if (fill != null) sealedTotal += SealUnused(go, fill, dirs);

                string deco = Decorate(go, n, layout, fill, top);
                if (deco != "none") { Decorations.Add("R" + n.Id + "(" + n.Kind + ")=" + deco); decorated++; }
            }

            if (placed == 0) { Object.Destroy(root); return null; }

            ChaosLog.Info(LogChannel.General, "[RunMap] 装配完成：房间 " + placed + " 个，封堵 " + sealedTotal +
                " 格，地形修饰 " + decorated + " 处（" + layout.ThemeKey + "，种子 " + layout.Seed + "）");
            return root;
        }

        // ═════════════════════════ 封堵 ═════════════════════════

        /// <summary>
        /// 封堵未使用的开口：把该方向那条边界带上"空的格子"补成实心。
        /// 左右补列 0..1 / W-2..W-1（行 3..H-4），上下补列 19..21（行 0..2 / H-2..H-1）。
        /// **先判空再填**，所以对"本来就封死的墙"和竖井块都是幂等的。
        /// </summary>
        private static int SealUnused(GameObject chunk, Tile fill, List<SocketDir> usedDirs)
        {
            Transform t = chunk.transform.Find("Tilemap_Ground");
            if (t == null) return 0;
            Tilemap tm = t.GetComponent<Tilemap>();
            if (tm == null) return 0;

            int n = 0;
            if (!usedDirs.Contains(SocketDir.Left))
                n += FillBand(tm, fill, 0, 1, 3, RunMapConst.ChunkH - 4);
            if (!usedDirs.Contains(SocketDir.Right))
                n += FillBand(tm, fill, RunMapConst.ChunkW - 2, RunMapConst.ChunkW - 1, 3, RunMapConst.ChunkH - 4);
            if (!usedDirs.Contains(SocketDir.Up))
                n += FillBand(tm, fill, RunMapConst.ShaftColumn - 1, RunMapConst.ShaftColumn + 1, RunMapConst.ChunkH - 2, RunMapConst.ChunkH - 1);
            if (!usedDirs.Contains(SocketDir.Down))
                n += FillBand(tm, fill, RunMapConst.ShaftColumn - 1, RunMapConst.ShaftColumn + 1, 0, 2);

            if (n > 0) tm.RefreshAllTiles();
            return n;
        }

        private static int FillBand(Tilemap tm, Tile fill, int x0, int x1, int y0, int y1)
        {
            int n = 0;
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    if (tm.GetTile(cell) != null) continue;
                    tm.SetTile(cell, fill);
                    n++;
                }
            return n;
        }

        // ═════════════════════════ 地形起伏 ═════════════════════════

        /// <summary>
        /// 按"场景种子 + 房间 ID"确定性地加地形：高台 / 坑 / 柱子 / 台阶。
        ///
        /// **硬约束**（破了就会搞坏连通性或堵门）：
        ///   · 外侧 4 列不动（门口通道）；
        ///   · 竖井列 18..22 不动（上下开口通道，跨层与支路都从这里过）；
        ///   · 坑最多挖掉第 1、2 行，**第 0 行永远留底**；
        ///   · 高台 / 柱子 ≤ 4 格高。
        /// 只作用于 Combat / Corridor：入口、死路、竖井、boss 门的形状有语义，不该被随机改。
        /// </summary>
        private static string Decorate(GameObject chunk, RoomNode node, MapLayout layout, Tile fill, Tile top)
        {
            if (fill == null) return "none";
            if (node.Kind != ChunkKind.Combat && node.Kind != ChunkKind.Corridor) return "none";

            Transform t = chunk.transform.Find("Tilemap_Ground");
            if (t == null) return "none";
            Tilemap tm = t.GetComponent<Tilemap>();
            if (tm == null) return "none";

            const int Margin = 4;
            int lo = Margin, hi = RunMapConst.ChunkW - Margin - 1;
            var rng = new RunMapRng(layout.Seed * 31u + node.Id * 2654435761u + 7u);
            int pick = rng.Range(4);
            int n = 0;

            switch (pick)
            {
                case 0: // 高台
                {
                    int w = rng.Range(6, 11);
                    int h = rng.Range(2, 4);
                    int x0 = rng.Range(lo, Mathf.Max(lo, hi - w));
                    if (Blocked(x0, w)) return "none";
                    for (int x = x0; x < x0 + w; x++)
                        for (int y = 3; y <= 2 + h; y++)
                        {
                            tm.SetTile(new Vector3Int(x, y, 0), y == 2 + h && top != null ? top : fill);
                            n++;
                        }
                    tm.RefreshAllTiles();
                    return "terrace@x" + x0 + "w" + w + "h" + h;
                }
                case 1: // 坑（第 0 行留底）
                {
                    int w = rng.Range(5, 9);
                    int x0 = rng.Range(lo, Mathf.Max(lo, hi - w));
                    if (Blocked(x0, w)) return "none";
                    for (int x = x0; x < x0 + w; x++)
                        for (int y = 1; y <= 2; y++)
                        {
                            if (tm.GetTile(new Vector3Int(x, y, 0)) == null) continue;
                            tm.SetTile(new Vector3Int(x, y, 0), null);
                            n++;
                        }
                    tm.RefreshAllTiles();
                    return "pit@x" + x0 + "w" + w;
                }
                case 2: // 柱子
                {
                    int count = rng.Range(2, 3);
                    var xs = new List<int>();
                    for (int i = 0; i < count; i++)
                    {
                        int x = rng.Range(lo, hi);
                        if (Blocked(x, 1) || xs.Contains(x) || xs.Contains(x - 1) || xs.Contains(x + 1)) continue;
                        int h = rng.Range(3, 5);
                        for (int y = 3; y <= 2 + h; y++) { tm.SetTile(new Vector3Int(x, y, 0), fill); n++; }
                        xs.Add(x);
                    }
                    if (n == 0) return "none";
                    tm.RefreshAllTiles();
                    return "pillars×" + xs.Count;
                }
                default: // 台阶
                {
                    int x0 = rng.Range(lo, Mathf.Max(lo, hi - 16));
                    if (Blocked(x0, 16)) return "none";
                    for (int s = 0; s < 4; s++)
                        for (int k = 0; k <= s; k++)
                        {
                            int x = x0 + s * 4 + k;
                            int y = 3 + k;
                            tm.SetTile(new Vector3Int(x, y - 1, 0), k == s && top != null ? top : fill);
                            n++;
                        }
                    tm.RefreshAllTiles();
                    return "steps@x" + x0;
                }
            }
        }

        /// <summary>这段列是否压到竖井通道（压到就别放地形）。</summary>
        private static bool Blocked(int x0, int w)
        {
            int a = x0, b = x0 + w - 1;
            return !(b < RunMapConst.ShaftColumn - 2 || a > RunMapConst.ShaftColumn + 2);
        }

        // ═════════════════════════ 工具 ═════════════════════════

        /// <summary>每个房间实际用到的接口方向（由边推出来）。</summary>
        public static Dictionary<ushort, List<SocketDir>> CollectUsed(MapLayout layout)
        {
            var used = new Dictionary<ushort, List<SocketDir>>();
            for (int i = 0; i < layout.Edges.Count; i++)
            {
                RoomEdge e = layout.Edges[i];
                Add(used, e.From, e.Dir);
                Add(used, e.To, RunMapConst.Opposite(e.Dir));
            }
            return used;
        }

        private static void Add(Dictionary<ushort, List<SocketDir>> map, ushort id, SocketDir dir)
        {
            List<SocketDir> list;
            if (!map.TryGetValue(id, out list)) { list = new List<SocketDir>(); map[id] = list; }
            if (!list.Contains(dir)) list.Add(dir);
        }

        public static string TypeName(ChunkKind kind)
        {
            switch (kind)
            {
                case ChunkKind.Entry: return "Entry";
                case ChunkKind.Corridor: return "Corridor";
                case ChunkKind.Combat: return "Combat";
                case ChunkKind.DeadEnd: return "DeadEnd";
                case ChunkKind.LayerLink: return "LayerLink_Stair";
                case ChunkKind.BossGate: return "BossGate";
                default: return kind.ToString();
            }
        }
    }
}
