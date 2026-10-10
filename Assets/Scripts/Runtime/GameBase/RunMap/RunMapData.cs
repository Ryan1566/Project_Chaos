using System;
using System.Collections.Generic;
using System.Text;

namespace RunMap
{
    // ═══════════════════════════════════════════════════════════════════════
    //  局内地图生成 · 运行时数据模型（纯逻辑，不引用 UnityEngine 场景）
    //  规格依据：Story/02_系统策划/01_局内地图生成系统.md（§4 / §6 / §7 / §16）
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>空间层。⚠ 与概念案的「深度 Depth」是两条不同的轴，不要混用。</summary>
    public enum MapLayer : byte { Underground = 1, Surface = 2, Building = 3, Roof = 4 }

    /// <summary>房间块类型（数字与 §8 MapChunk.chunkType 对照一致）。</summary>
    public enum ChunkKind : byte
    {
        Entry = 1, Corridor = 2, Combat = 3, Elite = 4, Treasure = 5, Shop = 6,
        Event = 7, Camp = 8, LayerLink = 9, BossGate = 10, DeadEnd = 11
    }

    /// <summary>接口方向（数字与 §8 ChunkSocket.dir 对照一致）。</summary>
    public enum SocketDir : byte { Left = 1, Right = 2, Up = 3, Down = 4 }

    public static class RunMapConst
    {
        /// <summary>房间块尺寸（格）。1 格 = 1 瓦片 = 1 世界单位（PPU=64）。</summary>
        public const int ChunkW = 40;
        public const int ChunkH = 24;

        /// <summary>竖直开口的中心列（左右各 1 格，共 3 格宽）。</summary>
        public const int ShaftColumn = 20;

        /// <summary>横向门洞高度（格）。</summary>
        public const int DoorHeight = 4;

        /// <summary>
        /// 3 档「出口地板行」（自底向上、0 起算）。**是地板高度，不是门洞中心**（§4.3 C7）。
        /// 竖直开口（U/D）不用高度档，兼容判据是**同列**（§16.5）。
        /// </summary>
        public static readonly int[] ProfileFloorRow = { 3, 11, 18 };

        public const uint MapVersion = 1;

        public static SocketDir Opposite(SocketDir d)
        {
            switch (d)
            {
                case SocketDir.Left: return SocketDir.Right;
                case SocketDir.Right: return SocketDir.Left;
                case SocketDir.Up: return SocketDir.Down;
                default: return SocketDir.Up;
            }
        }

        public static bool IsVertical(SocketDir d) { return d == SocketDir.Up || d == SocketDir.Down; }

        public static string DirName(SocketDir d)
        {
            switch (d)
            {
                case SocketDir.Left: return "L";
                case SocketDir.Right: return "R";
                case SocketDir.Up: return "U";
                default: return "D";
            }
        }
    }

    // ─────────────────────────── 种子与随机数 ───────────────────────────

    public static class RunMapSeed
    {
        /// <summary>FNV-1a：SceneSeed = FNV1a(RunSeed, sceneIndex)（§5 F4）。</summary>
        public static uint Derive(uint runSeed, int sceneIndex)
        {
            uint h = 2166136261u;
            h = (h ^ (byte)(runSeed & 0xFF)) * 16777619u;
            h = (h ^ (byte)((runSeed >> 8) & 0xFF)) * 16777619u;
            h = (h ^ (byte)((runSeed >> 16) & 0xFF)) * 16777619u;
            h = (h ^ (byte)((runSeed >> 24) & 0xFF)) * 16777619u;
            h = (h ^ (byte)(sceneIndex & 0xFF)) * 16777619u;
            return h;
        }

        /// <summary>重试时换一条种子支线（保证"同种子同结果"仍成立：attempt 也参与派生）。</summary>
        public static uint DeriveAttempt(uint sceneSeed, int attempt)
        {
            uint h = 2166136261u;
            h = (h ^ (byte)(sceneSeed & 0xFF)) * 16777619u;
            h = (h ^ (byte)((sceneSeed >> 8) & 0xFF)) * 16777619u;
            h = (h ^ (byte)((sceneSeed >> 16) & 0xFF)) * 16777619u;
            h = (h ^ (byte)((sceneSeed >> 24) & 0xFF)) * 16777619u;
            h = (h ^ (byte)(attempt & 0xFF)) * 16777619u;
            return h;
        }
    }

    /// <summary>
    /// xorshift32。**自己实现、自己持有状态**，不用 UnityEngine.Random：
    /// 全局随机源会被其它系统踩，且破坏"同种子同结果"。
    /// </summary>
    public class RunMapRng
    {
        private uint _s;
        public RunMapRng(uint seed) { _s = seed == 0 ? 0x9E3779B9u : seed; }
        public uint NextUInt()
        {
            _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5;
            return _s;
        }
        /// <summary>[0, maxExclusive)。maxExclusive ≤ 0 时返回 0。</summary>
        public int Range(int maxExclusive) { return maxExclusive <= 0 ? 0 : (int)(NextUInt() % (uint)maxExclusive); }
        /// <summary>闭区间 [min, max]。</summary>
        public int Range(int min, int max) { return max <= min ? min : min + Range(max - min + 1); }
        public bool Chance(int percent) { return Range(100) < percent; }
    }

    // ─────────────────────────── 房间块定义（配置侧） ───────────────────────────

    [Serializable]
    public class SocketDef
    {
        public SocketDir Dir;
        /// <summary>横向接口的高度档；竖直接口恒为 0（用列匹配）。</summary>
        public int Profile;
        /// <summary>1 普通通道 / 2 跨层通道 / 3 boss 门。</summary>
        public int Type;
        public float X, Y;
    }

    [Serializable]
    public class SlotDef
    {
        /// <summary>与 §8 ChunkSlot.slotType 同义。</summary>
        public int SlotType;
        public float X, Y;
    }

    [Serializable]
    public class ChunkDef
    {
        public uint Id;
        public ChunkKind Kind;
        public MapLayer Layer;
        public string ThemeKey;
        /// <summary>Resources 相对路径（不含扩展名）。</summary>
        public string PrefabPath;
        public int Weight = 10;
        public int MaxCount = 99;
        public bool Unique;
        public List<SocketDef> Sockets = new List<SocketDef>();
        public List<SlotDef> Slots = new List<SlotDef>();

        /// <summary>是否存在某个方向的接口。</summary>
        public bool HasDir(SocketDir d)
        {
            for (int i = 0; i < Sockets.Count; i++) if (Sockets[i].Dir == d) return true;
            return false;
        }

        /// <summary>横向接口的高度档（-1 = 没有该方向的接口）。</summary>
        public int ProfileOf(SocketDir d)
        {
            for (int i = 0; i < Sockets.Count; i++) if (Sockets[i].Dir == d) return Sockets[i].Profile;
            return -1;
        }
    }

    // ─────────────────────────── 原型图预设（配置侧） ───────────────────────────

    [Serializable]
    public class LayerSpec
    {
        public MapLayer Layer;
        /// <summary>层顺序，从 1 开始连续递增。</summary>
        public int Order;
        public int RoomMin, RoomMax;
        /// <summary>主路是否必须经过本层。</summary>
        public bool IsMainPath = true;
        /// <summary>与第几层相连（0 = 本层为首层）。</summary>
        public int LinkFromOrder;
        /// <summary>是否允许直连（false ⇒ 必须经中间层；地表建筑必须对非地表层填 false）。</summary>
        public bool AllowDirect = true;
        public int LinkMin = 1, LinkMax = 2;
    }

    [Serializable]
    public class PresetDef
    {
        public int Id;
        public uint BiomeId;
        public string Name;
        /// <summary>是否启用。未启用的 preset 不会被抽取（例如多层的块库还没补齐时）。</summary>
        public bool Enabled = true;
        public List<LayerSpec> Layers = new List<LayerSpec>();
        public int MainPathMin = 6, MainPathMax = 9;
        public int BranchMin = 4, BranchMax = 8;
        public int TotalMin = 10, TotalMax = 17;
        public int SpecialTreasure = 2, SpecialShop = 0, SpecialEvent = 0, SpecialCamp = 1;
        public int RetryMax = 30;

        public List<LayerSpec> SortedLayers()
        {
            var list = new List<LayerSpec>(Layers);
            list.Sort((a, b) => a.Order.CompareTo(b.Order));
            return list;
        }
    }

    // ─────────────────────────── 生成结果（运行时） ───────────────────────────

    [Serializable]
    public class RoomNode
    {
        public ushort Id;
        public MapLayer Layer;
        public ChunkKind Kind;
        /// <summary>房间格坐标（格）。</summary>
        public int CellX, CellY;
        public bool IsMainPath;
        public bool IsLayerLink;
        /// <summary>选中的房间块 ID（0 = 还没选）。</summary>
        public uint ChunkId;
        /// <summary>本房间横向门洞的高度档（竖直开口不用它）。</summary>
        public int Profile;
    }

    [Serializable]
    public class RoomEdge
    {
        public ushort From, To;
        /// <summary>From 一侧的出向（To 一侧必为其反向）。</summary>
        public SocketDir Dir;
        public int Profile;
        public bool IsLayerLink;
    }

    [Serializable]
    public class SpawnSlot
    {
        public ushort RoomId;
        public int Index;
        public int SlotType;
        /// <summary>世界坐标（房间块根在原点，1 格 = 1 单位）。</summary>
        public float X, Y;
    }

    [Serializable]
    public class MapLayout
    {
        public uint Seed;
        public int SceneIndex;
        public uint Version = RunMapConst.MapVersion;
        public string ThemeKey;
        public int PresetId;
        /// <summary>是否走了保底布局（§10 E12）。</summary>
        public bool Degraded;
        public int Attempts;
        public ushort EntryId;
        public ushort BossId;
        public List<RoomNode> Nodes = new List<RoomNode>();
        public List<RoomEdge> Edges = new List<RoomEdge>();
        public List<SpawnSlot> Slots = new List<SpawnSlot>();
        /// <summary>本图用到过的房间块 ID → 次数（校验用）。</summary>
        public Dictionary<uint, int> ChunkUseCount = new Dictionary<uint, int>();

        public RoomNode Find(ushort id)
        {
            for (int i = 0; i < Nodes.Count; i++) if (Nodes[i].Id == id) return Nodes[i];
            return null;
        }

        public bool IsOccupied(int cellX, int cellY)
        {
            for (int i = 0; i < Nodes.Count; i++)
                if (Nodes[i].CellX == cellX && Nodes[i].CellY == cellY) return true;
            return false;
        }

        /// <summary>稳定签名：用于「同种子两次生成逐字段一致」的比对（§13 A1）。</summary>
        public string Signature()
        {
            var sb = new StringBuilder();
            sb.Append("seed=").Append(Seed).Append(";theme=").Append(ThemeKey)
              .Append(";preset=").Append(PresetId).Append(";entry=").Append(EntryId)
              .Append(";boss=").Append(BossId).Append(";degraded=").Append(Degraded ? 1 : 0).Append('\n');
            var nodes = new List<RoomNode>(Nodes);
            nodes.Sort((a, b) => a.Id.CompareTo(b.Id));
            for (int i = 0; i < nodes.Count; i++)
            {
                RoomNode n = nodes[i];
                sb.Append('N').Append(n.Id).Append(':').Append((int)n.Layer).Append('/').Append((int)n.Kind)
                  .Append('/').Append(n.CellX).Append(',').Append(n.CellY)
                  .Append('/').Append(n.IsMainPath ? 1 : 0).Append('/').Append(n.IsLayerLink ? 1 : 0)
                  .Append('/').Append(n.ChunkId).Append('/').Append(n.Profile).Append('\n');
            }
            var edges = new List<RoomEdge>(Edges);
            edges.Sort((a, b) => (a.From != b.From) ? a.From.CompareTo(b.From)
                : (a.To != b.To ? a.To.CompareTo(b.To) : ((int)a.Dir).CompareTo((int)b.Dir)));
            for (int i = 0; i < edges.Count; i++)
            {
                RoomEdge e = edges[i];
                sb.Append('E').Append(e.From).Append('>').Append(e.To).Append(':')
                  .Append((int)e.Dir).Append('/').Append(e.Profile).Append('/').Append(e.IsLayerLink ? 1 : 0).Append('\n');
            }
            return sb.ToString();
        }
    }

    public class RunMapContext
    {
        public uint RunSeed;
        public int SceneIndex;   // 1..3
        public int Era;          // 时代/难度（1..5），后续接难度叠加
        public int Depth;        // 深度轴（1..时代序号）

        public uint SceneSeed { get { return RunMapSeed.Derive(RunSeed, SceneIndex); } }
    }
}
