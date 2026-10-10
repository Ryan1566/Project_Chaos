using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RunMapTool.Editor
{
    /// <summary>
    /// 局内地图「灰盒（占位）资产」管线 —— 路线 A 的可执行实现。
    /// 设计依据：Story/02_系统策划/01_局内地图生成系统.md §4.3（房间块与接口规范）。
    ///
    /// ══════════════════ 为什么要有这个工具 ══════════════════
    /// 地图生成需要三层美术产物：① 瓦片 ② **房间块 prefab** ③ 主题套系。
    /// 现成素材包只能解决 ①，解决不了 ②（房间块才是工量大头），题材上也对不上战国—秦汉。
    /// 所以先用【程序化灰盒】把 ② 这条瓶颈打通：
    ///   · 瓦片由代码画（可复现、零许可证风险、改配色改一行）；
    ///   · 房间块由代码按 §4.3 的接口规范拼装（含 Socket_* / SpawnSlot_* / CameraBounds / RoomMarker）；
    ///   · 生成模块当天就能对着灰盒跑通 A1~A13，美术零阻塞。
    ///
    /// ══════════════════ 为什么生成器在 Unity 侧（C#）而不是 Pillow/SVG ══════════════════
    /// 1) 本机沙箱**写不了** Assets/TileMap、Assets/Art、Assets/Prefabs（见 Story/README.md §5.9），
    ///    只有 Unity 进程能落盘；把生成器放在 Unity 里，省掉"Python 出图 → 复制 → 改导入设置"三段式。
    /// 2) 本机没有 inkscape / ImageMagick / resvg / cairosvg，SVG 光栅化没有可靠工具链（实测）。
    ///    用 C# 直接画 64×64 的小图，零外部依赖。
    /// 3) 生成器与资产在同一进程，导入设置（PPU=64 / Point / 不压缩）在同一次调用里写死，
    ///    不会出现"图对了、设置错了"的错配。
    ///
    /// ⚠ 本工具只产出**占位资产**，放在 *Placeholder* 目录下，**不是正式美术**。
    ///    正式房间块由美术手作，但必须遵守同一套 §4.3 规格。
    ///
    /// 菜单：Tools/局内地图/*
    /// </summary>
    public static class RunMapPlaceholderPipeline
    {
        // ══════════════════ 路径与规格常量（改这里就够，别散落） ══════════════════

        /// <summary>瓦片资产根目录（与既有 Assets/TileMap/Tile 并列，互不干扰）。</summary>
        public const string TileRoot = "Assets/TileMap/Placeholder";

        /// <summary>占位房间块 prefab 根目录。</summary>
        // ⚠ 必须在 **Resources** 下：运行时 Pass E 走 ResManager/Resources.Load 取 prefab，
        //   不在 Resources 里的 prefab 在 Player 包里根本找不到（编辑器里能 AssetDatabase 找到，容易自欺）。
        public const string PrefabRoot = "Assets/Resources/Prefabs/RunMap/Placeholder";

        /// <summary>预览图落点（Assets/AI 是代理工作区，不进正式资产目录）。</summary>
        public const string PreviewRoot = "Assets/AI/runmap-art";

        /// <summary>瓦片像素尺寸。**必须与既有 Grass_* 瓦片一致（64）**，否则和 PPU 对不上、块尺寸会错位。</summary>
        public const int TileSize = 64;

        /// <summary>房间块尺寸（格）。1 格 = 1 瓦片 = 1 世界单位（PPU=64）。</summary>
        public const int ChunkW = 40;
        public const int ChunkH = 24;

        /// <summary>
        /// 出口地板行（自底向上，0 起算）——即 §4.3 C7「3 档接口高度」的精确化。
        /// 开口 = 该行起往上 4 格。块高 24 时：3 / 11 / 18。
        /// ⚠ 这是**地板高度**，不是门洞中心高度（门洞中心 = floorRow + 2）。
        /// </summary>
        public static readonly int[] ProfileFloorRow = { 3, 11, 18 };

        /// <summary>门洞高度（格）。</summary>
        public const int DoorHeight = 4;

        /// <summary>层间竖井的中心列（Stair 块上下开口对齐用）。</summary>
        public const int ShaftColumn = 20;

        // ══════════════════ 主题（三套空间层配色，一眼可辨） ══════════════════

        private class Theme
        {
            public string Key;    // 目录/文件名用
            public string Label;  // 中文名（注释与报告用）
            public int Layer;     // MapLayer：1 地下 / 2 地表 / 3 地表建筑
            public Color32 Fill;  // 实心
            public Color32 Top;   // 顶面（草/砖顶/岩顶）
            public Color32 Beam;  // 平台/立柱
            public Color32 Deco;  // 背景装饰层（无碰撞）
        }

        private static readonly Theme[] Themes =
        {
            new Theme
            {
                Key = "Underground", Label = "地下", Layer = 1,
                Fill = new Color32(0x3B, 0x42, 0x50, 0xFF),
                Top  = new Color32(0x59, 0x63, 0x7A, 0xFF),
                Beam = new Color32(0x7A, 0x5C, 0x3A, 0xFF),
                Deco = new Color32(0x23, 0x28, 0x33, 0xFF),
            },
            new Theme
            {
                Key = "Surface", Label = "地表", Layer = 2,
                Fill = new Color32(0x5C, 0x45, 0x2C, 0xFF),
                Top  = new Color32(0x4E, 0x7A, 0x38, 0xFF),
                Beam = new Color32(0x8A, 0x6A, 0x44, 0xFF),
                Deco = new Color32(0x2F, 0x3A, 0x26, 0xFF),
            },
            new Theme
            {
                Key = "Building", Label = "地表建筑", Layer = 3,
                Fill = new Color32(0x7A, 0x46, 0x32, 0xFF),
                Top  = new Color32(0x9C, 0x6B, 0x4A, 0xFF),
                Beam = new Color32(0xA8, 0x87, 0x4F, 0xFF),
                Deco = new Color32(0x3B, 0x24, 0x1C, 0xFF),
            },
        };

        // ══════════════════ 瓦片种类 ══════════════════

        /// <summary>瓦片种类。后三种是"自动拼接"用的端件：顶面左右端 + 圆底。</summary>
        private enum TileKind { Fill, Top, Beam, Deco, TopL, TopR, Rounded }

        private static TileKind[] AllKinds = { TileKind.Fill, TileKind.Top, TileKind.Beam, TileKind.Deco, TileKind.TopL, TileKind.TopR, TileKind.Rounded };

        // ══════════════════ 房间块类型与布局 ══════════════════

        /// <summary>房间块类型（与 §8 MapChunk.chunkType 的数字对照保持一致）。</summary>
        public enum ChunkType
        {
            Entry = 1,
            Corridor = 2,
            Combat = 3,
            DeadEnd = 11,
            LayerLinkStair = 9,
            BossGate = 10,
        }

        private class SocketSpec
        {
            public string Name;   // Socket_L / Socket_R / Socket_U / Socket_D
            public float X, Y;
            public int Dir;       // 1 左 / 2 右 / 3 上 / 4 下（与 §8 ChunkSocket.dir 同义）
            public int Profile;   // 0/1/2 高度档
            public int Type;      // 1 普通 / 2 跨层 / 3 boss 门
        }

        private class SlotSpec
        {
            public float X, Y;
            public int SlotType;  // 与 §8 ChunkSlot.slotType 同义：1 敌人 2 精英 3 宝箱 5 事件 6 扎营点
        }

        /// <summary>一张房间块的"几何草图"：地面网格 + 装饰网格 + 接口 + 标记点。prefab 与预览图共用它，避免两边画出不一样的东西。</summary>
        private class ChunkLayout
        {
            public string TypeName;
            public ChunkType Type;
            public char[,] Ground = new char[ChunkW, ChunkH]; // '.' 空 / '#' 实心 / '^' 顶面 / '=' 平台
            public char[,] Deco = new char[ChunkW, ChunkH];   // '.' 空 / 'o' 背景
            public List<SocketSpec> Sockets = new List<SocketSpec>();
            public List<SlotSpec> Slots = new List<SlotSpec>();
            public int Profile; // 水平出口统一用这一档
        }

        // ═══════════════════════════════════════════════════════════
        //  菜单入口
        // ═══════════════════════════════════════════════════════════

        [MenuItem("Tools/局内地图/0. 一键生成（瓦片 + 房间块 + 预览图）", false, 100)]
        public static void MenuGenerateAll()
        {
            string report = RunAll();
            EditorUtility.DisplayDialog("局内地图占位资产", report, "好");
            Debug.Log("[RunMapPlaceholder] " + report.Replace("\n", " | "));
        }

        [MenuItem("Tools/局内地图/1. 只生成占位瓦片", false, 101)]
        public static void MenuGenerateTilesOnly()
        {
            string report = GenerateTiles();
            EditorUtility.DisplayDialog("占位瓦片", report, "好");
            Debug.Log("[RunMapPlaceholder] " + report.Replace("\n", " | "));
        }

        [MenuItem("Tools/局内地图/2. 只生成占位房间块 Prefab", false, 102)]
        public static void MenuGenerateChunksOnly()
        {
            string report = GenerateChunks();
            EditorUtility.DisplayDialog("占位房间块", report, "好");
            Debug.Log("[RunMapPlaceholder] " + report.Replace("\n", " | "));
        }

        /// <summary>给自动化/代理调用：不打弹窗，直接返回报告。</summary>
        public static string RunAll()
        {
            var sb = new StringBuilder();
            sb.Append("[1/2] 瓦片\n").Append(GenerateTiles()).Append("\n");
            sb.Append("[2/2] 房间块\n").Append(GenerateChunks());
            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════
        //  1) 瓦片：画 PNG → 设导入设置 → 建 Tile 资产
        // ═══════════════════════════════════════════════════════════

        public static string GenerateTiles()
        {
            var sb = new StringBuilder();
            int made = 0, reused = 0;

            for (int t = 0; t < Themes.Length; t++)
            {
                Theme th = Themes[t];
                string dir = TileRoot + "/" + th.Key;
                EnsureFolder(dir);

                for (int k = 0; k < AllKinds.Length; k++)
                {
                    TileKind kind = AllKinds[k];
                    string baseName = "Tile_" + KindName(kind);
                    string pngPath = dir + "/" + baseName + ".png";
                    string tilePath = dir + "/" + baseName + ".asset";

                    Texture2D tex = PaintTile(kind, th);
                    byte[] png = tex.EncodeToPNG();
                    UnityEngine.Object.DestroyImmediate(tex);

                    bool existed = File.Exists(pngPath);
                    File.WriteAllBytes(pngPath, png);            // 覆盖写，GUID 不变
                    AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);

                    ApplyTileImportSettings(pngPath, TileSize);  // 见下方"为什么逐张写导入设置"

                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
                    if (sprite == null)
                    {
                        sb.Append("⚠ 无法把 ").Append(pngPath).Append(" 读成 Sprite（导入设置没生效？）\n");
                        continue;
                    }

                    Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                    if (tile == null)
                    {
                        tile = ScriptableObject.CreateInstance<Tile>();
                        tile.sprite = sprite;
                        tile.colliderType = kind == TileKind.Deco ? Tile.ColliderType.None : Tile.ColliderType.Grid;
                        tile.color = Color.white;
                        AssetDatabase.CreateAsset(tile, tilePath);
                        made++;
                    }
                    else
                    {
                        // 已存在 → 只更新引用，保留 GUID（避免引用它的 prefab 断链）
                        tile.sprite = sprite;
                        tile.colliderType = kind == TileKind.Deco ? Tile.ColliderType.None : Tile.ColliderType.Grid;
                        EditorUtility.SetDirty(tile);
                        reused++;
                    }
                }
                sb.Append("  · 主题 ").Append(th.Label).Append("（layer=").Append(th.Layer).Append("）→ ")
                  .Append(AllKinds.Length).Append(" 张瓦片 @ ").Append(dir).Append('\n');
            }

            AssetDatabase.SaveAssets();
            sb.Append("瓦片小结：新建 ").Append(made).Append(" 个 Tile 资产，复用更新 ").Append(reused).Append(" 个。");
            return sb.ToString();
        }

        private static string KindName(TileKind k)
        {
            switch (k)
            {
                case TileKind.Fill: return "Fill";
                case TileKind.Top: return "Top";
                case TileKind.Beam: return "Beam";
                case TileKind.TopL: return "TopL";
                case TileKind.TopR: return "TopR";
                case TileKind.Rounded: return "Rounded";
                default: return "Deco";
            }
        }

        /// <summary>
        /// 为什么逐张写导入设置，而不是靠 AssetPostprocessor：
        /// 本工具**自己产出**这些图，就在产出点把口径写死最不容易错配；
        /// 且后处理器是"别人图落地时"的兜底（见 ArtPipeline/IconImportPostprocessor），
        /// 两者职责不同 —— 这里不动后处理器，避免误伤 Art/ 下的其它图。
        /// 口径与既有 Grass_* 瓦片实测值一致：PPU=64、Point、不开 mipmap。
        /// </summary>
        private static void ApplyTileImportSettings(string pngPath, int ppu)
        {
            TextureImporter importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;          // = 瓦片像素数 → 1 瓦片 = 1 世界单位
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.filterMode = FilterMode.Point;      // 像素风：绝不能被双线性糊掉
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 128;               // 64px 的图给 128 上限，挡住"误放大图"
            // 不压缩：占位图是纯色块，压缩只会引入色块边缘杂色（正式美术再按平台定）
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = false;

            importer.SaveAndReimport();
        }

        // ═══════════════════════════════════════════════════════════
        //  2) 房间块：按 §4.3 规格拼 prefab
        // ═══════════════════════════════════════════════════════════

        public static string GenerateChunks(string prefabRootOverride = null, string tileRootOverride = null)
        {
            string prefabRoot = prefabRootOverride != null ? prefabRootOverride : PrefabRoot;
            string tileRoot = tileRootOverride != null ? tileRootOverride : TileRoot;
            var sb = new StringBuilder();
            ChunkType[] types = { ChunkType.Entry, ChunkType.Corridor, ChunkType.Combat, ChunkType.DeadEnd, ChunkType.LayerLinkStair, ChunkType.BossGate };
            int prefabs = 0;

            for (int t = 0; t < Themes.Length; t++)
            {
                Theme th = Themes[t];
                string dir = prefabRoot + "/" + th.Key;
                EnsureFolder(dir);

                for (int i = 0; i < types.Length; i++)
                {
                    ChunkLayout layout = BuildLayout(types[i]);
                    string goName = "Chunk_" + th.Key + "_" + layout.TypeName;
                    string prefabPath = dir + "/" + goName + ".prefab";

                    GameObject root = BuildChunkGameObject(goName, layout, th, tileRoot);
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    UnityEngine.Object.DestroyImmediate(root);
                    prefabs++;

                    // 预览图：把同一份 layout 画成 PNG，人（和代理）可以直接看图验收
                    RenderPreview(layout, th);
                }
                sb.Append("  · 主题 ").Append(th.Label).Append(" → ").Append(types.Length).Append(" 个 prefab @ ").Append(dir).Append('\n');
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            sb.Append("房间块小结：").Append(prefabs).Append(" 个 prefab（").Append(Themes.Length).Append(" 主题 × ").Append(types.Length).Append(" 类型）→ ").Append(prefabRoot).Append("，预览图 @ ").Append(PreviewRoot).Append("。");
            return sb.ToString();
        }

        /// <summary>
        /// 房间块几何草图。**这是 §4.3 规格的可执行版本**：
        /// 块 = 40×24 格；地板 0~2 行（第 2 行是顶面）；天花板 22~23 行；左右墙各 2 格厚；
        /// 出口 = 墙上挖一个 4 格高的洞，洞底 = 该档地板行。
        /// </summary>
        private static ChunkLayout BuildLayout(ChunkType type)
        {
            var L = new ChunkLayout();
            L.Type = type;

            // ① 地板与天花板（所有类型都一样：有房才有边界）
            for (int x = 0; x < ChunkW; x++)
            {
                L.Ground[x, 0] = '#';
                L.Ground[x, 1] = '#';
                L.Ground[x, 2] = '^';           // 顶面用亮色瓦片，一眼能看出"哪面能站"
                L.Ground[x, ChunkH - 1] = '#';
                L.Ground[x, ChunkH - 2] = '#';
            }

            // ② 背景装饰层：把室内铺满（无碰撞），让房间"封闭感"出来
            for (int x = 2; x < ChunkW - 2; x++)
                for (int y = 3; y < ChunkH - 2; y++)
                    L.Deco[x, y] = 'o';

            // ③ 左右墙 + 出口
            int profile = 0;
            switch (type)
            {
                case ChunkType.Entry: profile = 0; L.TypeName = "Entry"; break;
                case ChunkType.Corridor: profile = 0; L.TypeName = "Corridor"; break;
                case ChunkType.Combat: profile = 0; L.TypeName = "Combat"; break;
                case ChunkType.DeadEnd: profile = 0; L.TypeName = "DeadEnd"; break;
                case ChunkType.BossGate: profile = 1; L.TypeName = "BossGate"; break;
                case ChunkType.LayerLinkStair: profile = 0; L.TypeName = "LayerLink_Stair"; break;
            }
            L.Profile = profile;

            bool stair = type == ChunkType.LayerLinkStair;

            if (stair)
            {
                // ── 层间竖井：不砌左右墙，只留中央竖井 + 两侧实心 ──
                for (int x = 0; x < ChunkW; x++)
                    for (int y = 3; y < ChunkH - 2; y++)
                        L.Ground[x, y] = '#';
                for (int y = 0; y < ChunkH; y++)
                {
                    L.Ground[ShaftColumn - 1, y] = '.';
                    L.Ground[ShaftColumn, y] = '.';
                    L.Ground[ShaftColumn + 1, y] = '.';
                }
                // 井内交错平台：给"往上爬"一点结构，也验证平台瓦片
                for (int i = 0; i < 6; i++)
                {
                    int row = 3 + i * 3;
                    int col = (i % 2 == 0) ? ShaftColumn - 1 : ShaftColumn;
                    if (row < ChunkH - 2) L.Ground[col, row] = '=';
                }
                // 上下接口：必须同列对齐（§4.4 C10.6）
                L.Sockets.Add(new SocketSpec { Name = "Socket_D", X = ShaftColumn + 0.5f, Y = 0.5f, Dir = 4, Profile = 0, Type = 2 });
                // 竖直接口用**列**匹配、不用高度档（规格 v2：U/D 的 Profile 恒为 0，兼容判据 = 同列）
                L.Sockets.Add(new SocketSpec { Name = "Socket_U", X = ShaftColumn + 0.5f, Y = ChunkH - 0.5f, Dir = 3, Profile = 0, Type = 2 });
                L.Slots.Add(new SlotSpec { X = ShaftColumn + 0.5f, Y = 12f, SlotType = 1 });
                MarkEdges(L);
                return L;
            }

            // 非竖井：左右墙（2 格厚）+ 按需要开洞
            for (int y = 3; y < ChunkH - 2; y++)
            {
                L.Ground[0, y] = '#';
                L.Ground[1, y] = '#';
                L.Ground[ChunkW - 1, y] = '#';
                L.Ground[ChunkW - 2, y] = '#';
            }

            // Entry 只朝右开；DeadEnd / BossGate 只朝左开；Corridor / Combat 左右都开
            bool openL = type != ChunkType.Entry;
            bool openR = type == ChunkType.Entry || type == ChunkType.Corridor || type == ChunkType.Combat;

            int floorRow = ProfileFloorRow[profile];
            // BossGate 的**入口门**必须落在地面层（档 0）：玩家从地面走进来，再爬台阶上到 boss 门（档 1）。
            // 否则会拼出"入口挂在半空、一进门就得飞"的图。
            int leftFloorRow = ProfileFloorRow[type == ChunkType.BossGate ? 0 : profile];
            if (openL) CarveDoor(L, true, leftFloorRow);
            if (openR) CarveDoor(L, false, leftFloorRow);

            // ── 统一竖直开口（规格 v2）──
            // 为什么每个房间都带 U/D 开口：只有左右门时，"支路"只能和主路挤在同一条横排上，
            // 2D 网格里必然重叠 —— 竖直开口是支路与跨层能成立的前提。
            // 未使用的开口由运行时**封堵**（§16.6 Pass E），所以 prefab 一律带开口、不在这里判用途。
            CarveVertical(L, true, type != ChunkType.Entry);   // Entry 是最下面的起点房，不需要向下开口
            L.Sockets.Add(new SocketSpec { Name = "Socket_U", X = ShaftColumn + 0.5f, Y = ChunkH - 0.5f, Dir = 3, Profile = 0, Type = 1 });
            if (type != ChunkType.Entry)
                L.Sockets.Add(new SocketSpec { Name = "Socket_D", X = ShaftColumn + 0.5f, Y = 0.5f, Dir = 4, Profile = 0, Type = 1 });

            // ④ 各类型的室内内容
            switch (type)
            {
                case ChunkType.Entry:
                    L.Slots.Add(new SlotSpec { X = 5.5f, Y = floorRow + 0.5f, SlotType = 5 });  // 起点/事件位
                    break;

                case ChunkType.Corridor:
                    for (int y = 3; y <= 7; y++) { L.Ground[14, y] = '#'; L.Ground[25, y] = '#'; }
                    // x 避开 ShaftColumn：竖直开口就开在 19~21 列，标记点落在洞口上没有意义
                    L.Slots.Add(new SlotSpec { X = 12f, Y = floorRow + 0.5f, SlotType = 1 });
                    break;

                case ChunkType.Combat:
                    // ⚠ 平台不能压到 19..21 列：那是**竖直开口的通道**，跨层与支路都要从这里过。
                    //    （之前这里放的是 x=18..23，直接把通道横切了一刀 —— 是不变量检查抓出来的。）
                    AddPlatform(L, 6, 8, 5);
                    AddPlatform(L, 25, 8, 5);
                    AddPlatform(L, 32, 13, 4);
                    L.Slots.Add(new SlotSpec { X = 10f, Y = 3.5f, SlotType = 1 });
                    L.Slots.Add(new SlotSpec { X = 20f, Y = 12.5f, SlotType = 1 });
                    L.Slots.Add(new SlotSpec { X = 33f, Y = 8.5f, SlotType = 2 });  // 精英位
                    break;

                case ChunkType.DeadEnd:
                    for (int y = 3; y <= 4; y++) L.Ground[30, y] = '#';
                    L.Slots.Add(new SlotSpec { X = 31.5f, Y = 5.5f, SlotType = 3 });  // 宝箱位
                    break;

                case ChunkType.BossGate:
                    // Boss 门：右侧两列高墙 + 门前留空（真实 boss 房由 M8 接管）
                    for (int y = 3; y < ChunkH - 2; y++) { L.Ground[33, y] = '#'; L.Ground[34, y] = '#'; }
                    AddStairs(L, 3, floorRow);
                    L.Slots.Add(new SlotSpec { X = 28f, Y = floorRow + 0.5f, SlotType = 2 });  // boss/精英位
                    break;
            }

            // ⑤ 出口位置：门洞中心（floorRow + 2）
            //    命名只有 4 个方向名 —— 同一侧只会有一个 socket（BossGate 的右侧是"boss 门"，
            //    Type=3 表示它不是可通行的普通通道，而是通往下一场景的门）。
            if (openL)
                L.Sockets.Add(new SocketSpec { Name = "Socket_L", X = 0.5f, Y = leftFloorRow + 2f, Dir = 1, Profile = type == ChunkType.BossGate ? 0 : profile, Type = 1 });
            if (openR)
                L.Sockets.Add(new SocketSpec { Name = "Socket_R", X = ChunkW - 0.5f, Y = floorRow + 2f, Dir = 2, Profile = profile, Type = 1 });
            else if (type == ChunkType.BossGate)
                L.Sockets.Add(new SocketSpec { Name = "Socket_R", X = ChunkW - 0.5f, Y = floorRow + 2f, Dir = 2, Profile = profile, Type = 3 });

            MarkEdges(L);
            return L;
        }

        /// <summary>在天花板 / 地板上挖一个 3 格宽的竖直开口（列 = ShaftColumn-1..+1），供 U/D 接口使用。</summary>
        private static void CarveVertical(ChunkLayout L, bool up, bool down)
        {
            for (int x = ShaftColumn - 1; x <= ShaftColumn + 1; x++)
            {
                if (x < 0 || x >= ChunkW) continue;
                if (up) { L.Ground[x, ChunkH - 1] = '.'; L.Ground[x, ChunkH - 2] = '.'; }
                if (down) { L.Ground[x, 0] = '.'; L.Ground[x, 1] = '.'; L.Ground[x, 2] = '.'; }
            }
        }

        /// <summary>在左右墙上挖一个门洞：洞底 = floorRow，洞高 = DoorHeight（挖穿 2 格厚的墙）。</summary>
        private static void CarveDoor(ChunkLayout L, bool left, int floorRow)
        {
            int c0 = left ? 0 : ChunkW - 2;
            for (int dx = 0; dx < 2; dx++)
                for (int y = floorRow; y < floorRow + DoorHeight; y++)
                    if (y >= 0 && y < ChunkH) L.Ground[c0 + dx, y] = '.';
        }

        /// <summary>铺一段平台（1 格厚的实心台面，用 Beam 瓦片）。</summary>
        private static void AddPlatform(ChunkLayout L, int x0, int row, int len)
        {
            for (int i = 0; i < len; i++)
            {
                int x = x0 + i;
                if (x > 1 && x < ChunkW - 2 && row > 2 && row < ChunkH - 2) L.Ground[x, row] = '=';
            }
        }

        /// <summary>从基础地板爬到目标楼层的一组台阶（出口在地板行 &gt; 3 时必须给）。</summary>
        private static void AddStairs(ChunkLayout L, int baseRow, int targetRow)
        {
            int steps = 3;
            for (int i = 1; i <= steps; i++)
            {
                int row = baseRow + Mathf.RoundToInt((targetRow - baseRow) * i / (float)(steps + 1));
                AddPlatform(L, 2 + (i - 1) * 6, row, 5);   // 台阶同样避开 19..21 列
            }
        }

        private static GameObject BuildChunkGameObject(string goName, ChunkLayout layout, Theme th, string tileRoot)
        {
            var root = new GameObject(goName);
            var grid = root.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            Tile fill = LoadTile(tileRoot, th.Key, TileKind.Fill);
            Tile top = LoadTile(tileRoot, th.Key, TileKind.Top);
            Tile beam = LoadTile(tileRoot, th.Key, TileKind.Beam);
            Tile deco = LoadTile(tileRoot, th.Key, TileKind.Deco);
            Tile topL = LoadTile(tileRoot, th.Key, TileKind.TopL);
            Tile topR = LoadTile(tileRoot, th.Key, TileKind.TopR);
            Tile rounded = LoadTile(tileRoot, th.Key, TileKind.Rounded);

            // ── 装饰层（无碰撞，排在下层） ──
            var decoGo = new GameObject("Tilemap_Deco");
            decoGo.transform.SetParent(root.transform, false);
            var decoTm = decoGo.AddComponent<Tilemap>();
            var decoRr = decoGo.AddComponent<TilemapRenderer>();
            decoRr.sortingOrder = -10;
            Paint(decoTm, layout.Deco, deco, null);

            // ── 地面层（带碰撞） ──
            var groundGo = new GameObject("Tilemap_Ground");
            groundGo.transform.SetParent(root.transform, false);
            var groundTm = groundGo.AddComponent<Tilemap>();
            var groundRr = groundGo.AddComponent<TilemapRenderer>();
            groundRr.sortingOrder = 0;
            Paint(groundTm, layout.Ground, fill, top, beam, topL, topR, rounded);

            // 碰撞三件套：TilemapCollider2D + Static Rigidbody2D + CompositeCollider2D
            // （Composite 必须与 Rigidbody2D 同物体；用 Polygons 而不是 Outlines —— 平台游戏里
            //   Outlines 的线性碰撞体在高速下落时容易穿地）
            var rb = groundGo.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            var comp = groundGo.AddComponent<CompositeCollider2D>();
            comp.geometryType = CompositeCollider2D.GeometryType.Polygons;
            var tmCol = groundGo.AddComponent<TilemapCollider2D>();
            tmCol.usedByComposite = true;

            // ── 接口空物体（程序按名字取，名字即契约） ──
            for (int i = 0; i < layout.Sockets.Count; i++)
            {
                SocketSpec s = layout.Sockets[i];
                var go = new GameObject(s.Name);
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3(s.X, s.Y, 0f);
            }

            // ── 标记点空物体 ──
            for (int i = 0; i < layout.Slots.Count; i++)
            {
                SlotSpec s = layout.Slots[i];
                var go = new GameObject("SpawnSlot_" + (i + 1).ToString("00"));
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = new Vector3(s.X, s.Y, 0f);
            }

            // ── 相机边界（§9 U4） ──
            var camGo = new GameObject("CameraBounds");
            camGo.transform.SetParent(root.transform, false);
            camGo.transform.localPosition = new Vector3(ChunkW * 0.5f, ChunkH * 0.5f, 0f);
            var camCol = camGo.AddComponent<BoxCollider2D>();
            camCol.isTrigger = true;
            camCol.size = new Vector2(ChunkW, ChunkH);

            // ── 房间根标记（程序索引房间用） ──
            var marker = new GameObject("RoomMarker");
            marker.transform.SetParent(root.transform, false);

            // ── 把本块用的瓦片直接挂在块上 ──
            // 运行时装配要补瓦片（封堵开口 / 加地形），但运行时不能 AssetDatabase 取资产；
            // 让块自带引用，既不重复资产也不怕以后换皮。
            RunMap.RunMapChunkTiles tiles = root.AddComponent<RunMap.RunMapChunkTiles>();
            tiles.fill = fill;
            tiles.top = top;

            return root;
        }

        private static void Paint(Tilemap tm, char[,] grid, Tile fill, Tile top = null, Tile beam = null,
            Tile topL = null, Tile topR = null, Tile rounded = null)
        {
            tm.ClearAllTiles();
            for (int x = 0; x < ChunkW; x++)
                for (int y = 0; y < ChunkH; y++)
                {
                    char c = grid[x, y];
                    if (c == '.') continue;
                    Tile tile = null;
                    if (c == '#') tile = fill;
                    else if (c == '^') tile = (top != null ? top : fill);
                    else if (c == '=') tile = (beam != null ? beam : fill);
                    else if (c == 'o') tile = fill;
                    else if (c == '(') tile = (topL != null ? topL : (top != null ? top : fill));
                    else if (c == ')') tile = (topR != null ? topR : (top != null ? top : fill));
                    else if (c == 'v') tile = (rounded != null ? rounded : fill);
                    if (tile != null) tm.SetTile(new Vector3Int(x, y, 0), tile);
                }
        }

        private static Tile LoadTile(string tileRoot, string themeKey, TileKind kind)
        {
            string p = tileRoot + "/" + themeKey + "/Tile_" + KindName(kind) + ".asset";
            Tile t = AssetDatabase.LoadAssetAtPath<Tile>(p);
            if (t == null) Debug.LogError("[RunMapPlaceholder] 缺瓦片资产：" + p + "（先跑菜单 1）");
            return t;
        }

        // ═══════════════════════════════════════════════════════════
        //  画图（纯代码，无外部依赖）
        // ═══════════════════════════════════════════════════════════

        private static Texture2D PaintTile(TileKind kind, Theme th)
        {
            var tex = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
            var px = new Color32[TileSize * TileSize];

            Color32 body = th.Fill;
            if (kind == TileKind.Deco) body = th.Deco;
            if (kind == TileKind.Beam) body = th.Beam;

            for (int y = 0; y < TileSize; y++)
                for (int x = 0; x < TileSize; x++)
                {
                    Color32 c = body;

                    // 噪点：让相邻瓦片之间有可辨识的纹理，避免"一整块死色"
                    int h = Hash(x, y);
                    if (h % 17 == 0) c = Shift(body, -10);
                    else if (h % 23 == 0) c = Shift(body, 8);

                    // 1px 暗边框：占位图**故意保留网格感** —— 调生成时能直接数格子
                    if (kind != TileKind.Deco && (x == 0 || y == 0 || x == TileSize - 1 || y == TileSize - 1))
                        c = Shift(body, -26);

                    // 顶面瓦片（含左右端件）：上 10px 亮色 + 下 1px 阴影线
                    if (kind == TileKind.Top || kind == TileKind.TopL || kind == TileKind.TopR)
                    {
                        if (y >= TileSize - 10) c = (Hash(x, y) % 7 == 0) ? Shift(th.Top, 12) : th.Top;
                        else if (y == TileSize - 11) c = Shift(th.Top, -30);
                    }

                    // 平台瓦片：上 12px 亮色，其余暗一档，明显区别于实心块
                    if (kind == TileKind.Beam)
                        c = (y >= TileSize - 12) ? Shift(th.Beam, 18) : Shift(th.Beam, -18);

                    // 端件：靠外那一侧压一条亮边，一眼能看出"这是切口"
                    if (kind == TileKind.TopL && x <= 3) c = Shift(th.Top, 26);
                    if (kind == TileKind.TopR && x >= TileSize - 4) c = Shift(th.Top, 26);
                    // 圆底：底部 6px 压暗，表示"下面是空的"
                    if (kind == TileKind.Rounded && y <= 5) c = Shift(body, -22);

                    px[y * TileSize + x] = c;
                }

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        private static int Hash(int x, int y)
        {
            unchecked
            {
                int h = x * 73856093 ^ y * 19349663;
                return h & 0x7FFFFFFF;
            }
        }

        private static Color32 Shift(Color32 c, int delta)
        {
            return new Color32(
                (byte)Mathf.Clamp(c.r + delta, 0, 255),
                (byte)Mathf.Clamp(c.g + delta, 0, 255),
                (byte)Mathf.Clamp(c.b + delta, 0, 255),
                c.a);
        }

        /// <summary>把同一份 layout 画成 8px/格 的预览 PNG，人和代理都能直接看图验收。</summary>
        private static void RenderPreview(ChunkLayout layout, Theme th)
        {
            const int scale = 8;
            EnsureFolder(PreviewRoot);
            var tex = new Texture2D(ChunkW * scale, ChunkH * scale, TextureFormat.RGBA32, false);
            var px = new Color32[tex.width * tex.height];
            Color32 bg = new Color32(0x12, 0x12, 0x14, 0xFF);

            for (int y = 0; y < tex.height; y++)
                for (int x = 0; x < tex.width; x++)
                    px[y * tex.width + x] = bg;

            for (int gy = 0; gy < ChunkH; gy++)
                for (int gx = 0; gx < ChunkW; gx++)
                {
                    char c = layout.Ground[gx, gy];
                    char d = layout.Deco[gx, gy];
                    Color32 col = bg;
                    if (c == '#') col = th.Fill;
                    else if (c == '^') col = th.Top;
                    else if (c == '=') col = th.Beam;
                    else if (d == 'o') col = Shift(th.Deco, 12);
                    for (int sy = 0; sy < scale; sy++)
                        for (int sx = 0; sx < scale; sx++)
                        {
                            int pxX = gx * scale + sx;
                            int pxY = gy * scale + sy;
                            px[pxY * tex.width + pxX] = col;
                        }
                }

            // 接口位置用亮色十字标出来（预览图专用，不进 prefab）
            for (int i = 0; i < layout.Sockets.Count; i++)
            {
                SocketSpec s = layout.Sockets[i];
                Mark(px, tex.width, tex.height, Mathf.RoundToInt(s.X * scale), Mathf.RoundToInt(s.Y * scale), new Color32(0xFF, 0xD0, 0x40, 0xFF), 5);
            }
            for (int i = 0; i < layout.Slots.Count; i++)
            {
                SlotSpec s = layout.Slots[i];
                Mark(px, tex.width, tex.height, Mathf.RoundToInt(s.X * scale), Mathf.RoundToInt(s.Y * scale), new Color32(0x40, 0xE0, 0xFF, 0xFF), 3);
            }

            tex.SetPixels32(px);
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            File.WriteAllBytes(PreviewRoot + "/preview_" + th.Key + "_" + layout.TypeName + ".png", png);
        }

        private static void Mark(Color32[] px, int w, int h, int cx, int cy, Color32 col, int radius)
        {
            for (int dy = -radius; dy <= radius; dy++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (x < 0 || y < 0 || x >= w || y >= h) continue;
                    if (dx == 0 || dy == 0) px[y * w + x] = col;
                }
        }

        // ═══════════════════════════════════════════════════════════
        //  工具
        // ═══════════════════════════════════════════════════════════

        // ═══════════════════════════════════════════════════════════
        //  Kenney 皮肤（CC0）：把下载好的原图导入成 Tile 资产
        //  许可与来源记录在 Assets/AI/runmap-art/KENNEY_LICENSES.md
        // ═══════════════════════════════════════════════════════════

        public const string KenneySourceRoot = "Assets/AI/runmap-art/kenney";
        public const string KenneyTileRoot = "Assets/TileMap/Kenney";
        public const string KenneyPrefabRoot = "Assets/Resources/Prefabs/RunMap/Kenney";

        /// <summary>Kenney Platformer Art 的瓦片像素尺寸（70×70）。PPU 取它 → 1 瓦片 = 1 世界单位。</summary>
        public const int KenneyTilePixels = 70;

        /// <summary>
        /// 主题 × 瓦片种类 → Kenney 原图名（实测挑选，不是随手取）：
        ///   · Top 用 `XMid` —— 草 / 石 / 砖的"顶面"；
        ///   · Fill 用 `XCenter` —— 纯材质内部（地表用 `grassCenter` 泥土本体，**不用** `dirtCenter` 那层淡蓝底纹）；
        ///   · Beam 用 `XHalfMid` —— 半高台面：上半格有图、下半格透明，正好贴格子顶边，和 Grid 碰撞"站在格顶"一致；
        ///   · Deco 用 `XCenter` + `Tile.color` 染暗（不额外占一张图）。
        /// </summary>
        private static readonly string[,] KenneyFiles =
        {
            { "stoneCenter",  "stoneMid",  "stoneHalfMid",  "stoneCenter",  "stoneLeft",  "stoneRight",  "stoneCenter_rounded"  },  // Themes[0] 地下
            { "grassCenter",  "grassMid",  "grassHalfMid",  "dirtCenter",   "grassLeft",  "grassRight",  "grassCenter_rounded"  },  // Themes[1] 地表
            { "castleCenter", "castleMid", "castleHalfMid", "castleCenter", "castleLeft", "castleRight", "castleCenter_rounded" },  // Themes[2] 地表建筑
        };

        [MenuItem("Tools/局内地图/5. 导入 Kenney 皮肤瓦片（CC0）", false, 105)]
        public static void MenuImportKenney()
        {
            string report = ImportKenneySkin();
            EditorUtility.DisplayDialog("Kenney 皮肤", report, "好");
            Debug.Log("[RunMapPlaceholder] " + report.Replace("\n", " | "));
        }

        [MenuItem("Tools/局内地图/7. 生成 Kenney RuleTile（自动拼接用）", false, 107)]
        public static void MenuGenerateKenneyChunks()
        {
            string report = ImportKenneySkin() + "\n" + GenerateChunks(KenneyPrefabRoot, KenneyTileRoot);
            EditorUtility.DisplayDialog("Kenney 房间块", report, "好");
            Debug.Log("[RunMapPlaceholder] " + report.Replace("\n", " | "));
        }

        /// <summary>把 Kenney 原图复制成 Tile 资产（PNG 统一命名 Tile_{Kind}.png，沿用灰盒那 4 种）。幂等。</summary>
        public static string ImportKenneySkin()
        {
            var sb = new StringBuilder();
            int ok = 0;
            for (int t = 0; t < Themes.Length; t++)
            {
                Theme th = Themes[t];
                string dir = KenneyTileRoot + "/" + th.Key;
                EnsureFolder(dir);

                for (int k = 0; k < AllKinds.Length; k++)
                {
                    TileKind kind = AllKinds[k];
                    string src = KenneySourceRoot + "/" + KenneyFiles[t, k] + ".png";
                    if (!File.Exists(src)) { sb.Append("⚠ 缺原图 ").Append(src).Append('\n'); continue; }

                    string pngPath = dir + "/Tile_" + KindName(kind) + ".png";
                    File.Copy(src, pngPath, true);
                    AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);
                    ApplyTileImportSettings(pngPath, KenneyTilePixels);

                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
                    if (sprite == null) { sb.Append("⚠ 导入失败 ").Append(pngPath).Append('\n'); continue; }

                    string tilePath = dir + "/Tile_" + KindName(kind) + ".asset";
                    Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                    Color decoTint = new Color(0.42f, 0.45f, 0.58f, 1f);
                    if (tile == null)
                    {
                        tile = ScriptableObject.CreateInstance<Tile>();
                        tile.sprite = sprite;
                        tile.colliderType = kind == TileKind.Deco ? Tile.ColliderType.None : Tile.ColliderType.Grid;
                        tile.color = kind == TileKind.Deco ? decoTint : Color.white;
                        AssetDatabase.CreateAsset(tile, tilePath);
                    }
                    else
                    {
                        tile.sprite = sprite;
                        tile.color = kind == TileKind.Deco ? decoTint : Color.white;
                        EditorUtility.SetDirty(tile);
                    }
                    ok++;
                }
                sb.Append("  · ").Append(th.Label).Append(" → ").Append(dir).Append('\n');
            }
            AssetDatabase.SaveAssets();
            sb.Append("Kenney 皮肤瓦片：").Append(ok).Append(" 张（PPU=").Append(KenneyTilePixels).Append("，Point）。");
            return sb.ToString();
        }

        /// <summary>
        /// 自动拼接的**最小必要集**：把实心格按"哪一侧露空"细分成端件。
        ///
        /// 为什么只做这三样：Kenney 这套不是 47 格 blob 集，硬做全自动必然出怪。
        /// 而我们的房间是规整方块，真正会露空的只有**竖直开口（竖井口）周围那一圈**：
        ///   · 顶面行的左右端 → TopL / TopR（圆角）
        ///   · 下方露空的实心格 → Rounded（圆底，覆盖地板底沿与天花板下沿）
        /// </summary>
        private static void MarkEdges(ChunkLayout L)
        {
            var src = (char[,])L.Ground.Clone();
            for (int y = 0; y < ChunkH; y++)
                for (int x = 0; x < ChunkW; x++)
                {
                    char c = src[x, y];
                    if (c != '#' && c != '^') continue;
                    bool openL = x == 0 || src[x - 1, y] == '.';
                    bool openR = x == ChunkW - 1 || src[x + 1, y] == '.';
                    bool openB = y == 0 || src[x, y - 1] == '.';
                    if (c == '^' && openL && !openR) { L.Ground[x, y] = '('; continue; }
                    if (c == '^' && openR && !openL) { L.Ground[x, y] = ')'; continue; }
                    if (openB) L.Ground[x, y] = 'v';
                }
        }

        // ═══════════════════════════════════════════════════════════
        //  RuleTile（2d-extras）：给手作房间 / 调色板刷地形用
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 每主题一张 RuleTile。规则集刻意只有 5 条 —— 这套瓦片不是 blob 集：
        ///   ① 上露空 + 左露空 → 顶面左端　② 上露空 + 右露空 → 顶面右端
        ///   ③ 上露空 → 顶面　　　　　　　④ 下露空 → 圆底　　⑤ 其余 → 实心
        /// 邻居顺序 = RuleTile 默认的 8 邻域（不含中心）：左上,上,右上,左,右,左下,下,右下。
        ///
        /// ⚠ 生成器**不依赖** RuleTile：房间块仍由 MarkEdges 直接选端件（更可控）。
        /// RuleTile 是留给"手作房间块 / 后续不规则地形"的工具。
        /// </summary>
        public static string BuildRuleTiles()
        {
            var sb = new StringBuilder();
            // ⚠ 本工程这版 2d-extras 的 Neighbor 是**常量类**（This = 1 / NotThis = 2），**没有 DontCare**：
            // RuleMatch 的 default 分支直接 return true，所以"不关心"就是 0。
            int O = RuleTile.TilingRuleOutput.Neighbor.NotThis;
            const int A = 0;

            for (int t = 0; t < Themes.Length; t++)
            {
                Theme th = Themes[t];
                string dir = KenneyTileRoot + "/" + th.Key;
                EnsureFolder(dir);
                string path = dir + "/RuleTile_" + th.Key + ".asset";

                Sprite fill = LoadSprite(dir, TileKind.Fill);
                Sprite top = LoadSprite(dir, TileKind.Top);
                Sprite topL = LoadSprite(dir, TileKind.TopL);
                Sprite topR = LoadSprite(dir, TileKind.TopR);
                Sprite rounded = LoadSprite(dir, TileKind.Rounded);
                if (fill == null || top == null || topL == null || topR == null || rounded == null)
                {
                    sb.Append("⚠ ").Append(th.Label).Append("：端件还没导入（先跑菜单 5）\n");
                    continue;
                }

                RuleTile rt = AssetDatabase.LoadAssetAtPath<RuleTile>(path);
                if (rt == null)
                {
                    rt = ScriptableObject.CreateInstance<RuleTile>();
                    AssetDatabase.CreateAsset(rt, path);
                }

                rt.m_DefaultSprite = fill;
                rt.m_DefaultColliderType = Tile.ColliderType.Grid;
                rt.m_TilingRules = new List<RuleTile.TilingRule>
                {
                    MakeRule(new[] { A, O, A, O, A, A, A, A }, topL),
                    MakeRule(new[] { A, O, A, A, O, A, A, A }, topR),
                    MakeRule(new[] { A, O, A, A, A, A, A, A }, top),
                    MakeRule(new[] { A, A, A, A, A, A, O, A }, rounded),
                    MakeRule(new[] { A, A, A, A, A, A, A, A }, fill),
                };
                EditorUtility.SetDirty(rt);
                sb.Append("  · ").Append(th.Label).Append(" → ").Append(path).Append('\n');
            }
            AssetDatabase.SaveAssets();
            sb.Append("RuleTile：").Append(Themes.Length).Append(" 张（每主题 5 条规则）。");
            return sb.ToString();
        }

        private static RuleTile.TilingRule MakeRule(int[] neighbors, Sprite sprite)
        {
            var r = new RuleTile.TilingRule();
            r.m_Neighbors = new List<int>(neighbors);
            r.m_Sprites = new[] { sprite };
            r.m_Output = RuleTile.TilingRuleOutput.OutputSprite.Single;
            r.m_ColliderType = Tile.ColliderType.Grid;
            return r;
        }

        private static Sprite LoadSprite(string dir, TileKind kind)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(dir + "/Tile_" + KindName(kind) + ".png");
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            string parent = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            string leaf = Path.GetFileName(assetPath);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
