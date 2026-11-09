using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace ArtPipeline.Editor
{
    /// <summary>
    /// 用 Unity 的 Sprite Atlas V2 维护按键和世界地点图标。
    /// 按键收录整份映射表引用，以覆盖玩家改键；地点收录专用目录的 Sprite。
    /// </summary>
    public static class IconAtlasBuilder
    {
        /// <summary>图集资产路径。放在图标目录下，方便和素材放一起看。</summary>
        public const string AtlasPath = IconImportPostprocessor.IconRoot + "/IconAtlas_Input.spriteatlasv2";
        public const string WorldLocationAtlasPath = "Assets/Art/PNG/GameWorld_Map/WorldLocationIcons.spriteatlasv2";
        private const string WorldLocationIconRoot = "Assets/Resources/UI/WorldLocations";

        /// <summary>打包时的单页上限。图标是 64px，2048 一页能装下几百张，够用且不会一次开太大。</summary>
        private const int MaxTextureSize = 2048;

        [MenuItem("Tools/图标/重建图标图集", false, 901)]
        public static void RebuildMenu()
        {
            BuildReport report = Rebuild();
            Debug.Log("[图标图集] " + report.message.Replace("\n", " "));
            EditorUtility.DisplayDialog("重建图标图集", report.message, "好");
        }

        [MenuItem("Tools/图标/重建世界地点图集", false, 902)]
        public static void RebuildWorldLocationMenu()
        {
            BuildReport report = RebuildWorldLocations();
            Debug.Log("[图标图集] " + report.message.Replace("\n", " "));
            EditorUtility.DisplayDialog("重建世界地点图集", report.message, "好");
        }

        public static BuildReport RebuildWorldLocations()
        {
            if (!AssetDatabase.IsValidFolder(WorldLocationIconRoot))
                throw new InvalidOperationException("找不到世界地点图标目录：" + WorldLocationIconRoot);

            var sprites = new List<Sprite>();
            var names = new HashSet<string>(StringComparer.Ordinal);
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { WorldLocationIconRoot });
            Array.Sort(guids, StringComparer.Ordinal);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                    throw new InvalidOperationException("世界地点贴图缺少 Sprite：" + path);
                if (!names.Add(sprite.name))
                    throw new InvalidOperationException("世界地点图标重名：" + sprite.name);
                sprites.Add(sprite);
            }
            return RebuildAtlas(WorldLocationAtlasPath, sprites);
        }

        public struct BuildReport
        {
            /// <summary>写进图集的 sprite 数（= 映射表引用到且资产有效的数量）。</summary>
            public int spriteCount;
            /// <summary>打包后的页数。0 表示没能取到页信息（图集没生效或反射失败）。</summary>
            public int pageCount;
            public int pageWidth;
            public int pageHeight;
            /// <summary>按压缩格式估算的显存占用（字节）。</summary>
            public long estimatedBytes;
            public string message;
        }

        // ══════════════════════ 构建 ══════════════════════

        /// <summary>按当前生效的映射表重建图集。表不存在时如实报错，不做任何资产改动。</summary>
        public static BuildReport Rebuild()
        {
            KeyIconMap map = KeyIconMap.Load();
            if (map == null)
            {
                return new BuildReport
                {
                    message = "找不到映射表（Resources/" + GlobalPath.res_KeyIconMapPath + "）。\n" +
                              "先建表并自动匹配图标，再回来重建图集 —— 图集的白名单就是这张表。"
                };
            }
            return Rebuild(map);
        }

        /// <summary>
        /// 按映射表同步白名单，并报告目标平台的打包结果。
        ///
        /// 每次【整表重建】而不是增量追加：映射表可能换了画风（Filled → Outline），
        /// 增量加的话旧画风的图会留在图集里越攒越多，占用只增不减。
        /// </summary>
        public static BuildReport Rebuild(KeyIconMap map)
        {
            if (map == null) return new BuildReport { message = "映射表为空，未执行。" };

            List<Sprite> sprites = map.CollectSprites();
            if (sprites.Count == 0)
            {
                return new BuildReport
                {
                    message = "映射表里没有任何图标引用，图集未改动。\n" +
                              "先在「按键图标映射」窗口里做一次自动匹配。"
                };
            }

            return RebuildAtlas(AtlasPath, sprites);
        }

        private static BuildReport RebuildAtlas(string path, List<Sprite> sprites)
        {
            var wanted = new HashSet<UnityEngine.Object>(sprites);
            wanted.Remove(null);
            if (wanted.Count == 0)
                return new BuildReport { message = "没有可收录的 Sprite，图集未更新。" };

            string folder = path.Substring(0, path.LastIndexOf('/'));
            if (!AssetDatabase.IsValidFolder(folder))
                throw new InvalidOperationException("图集目录不存在：" + folder);

            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            bool created = atlas == null;
            var previous = created ? Array.Empty<UnityEngine.Object>()
                : SpriteAtlasExtensions.GetPackables(atlas) ?? Array.Empty<UnityEngine.Object>();
            if (created && AssetImporter.GetAtPath(path) != null)
                throw new InvalidOperationException("现有图集无法读取：" + path);

            bool changed = created || previous.Length != wanted.Count || !wanted.SetEquals(previous);
            if (changed)
            {
                // V2 的可编辑清单保存在源资产，导入后的 SpriteAtlas 负责运行时使用。
                var source = created ? new SpriteAtlasAsset() : SpriteAtlasAsset.Load(path);
                if (source == null)
                    throw new InvalidOperationException("图集须为可编辑的 V2 主图集：" + path);
                try
                {
                    if (source.isVariant)
                        throw new InvalidOperationException("重建工具需要 V2 主图集：" + path);
                    if (previous.Length > 0) source.Remove(previous);
                    var packables = new List<UnityEngine.Object>(wanted.Count);
                    foreach (var sprite in sprites)
                        if (wanted.Remove(sprite)) packables.Add(sprite);
                    source.Add(packables.ToArray());
                    SpriteAtlasAsset.Save(source, path);
                }
                finally { UnityEngine.Object.DestroyImmediate(source); }
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }

            if (!(AssetImporter.GetAtPath(path) is SpriteAtlasImporter importer))
                throw new InvalidOperationException("无法读取 V2 图集导入设置：" + path);
            if (created) ApplySettings(importer);
            if (created || !importer.includeInBuild)
            {
                importer.includeInBuild = true;
                importer.SaveAndReimport();
            }
            atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas == null)
                throw new InvalidOperationException("图集导入失败：" + path);

            // 构建模式可能只在目标包中生成图集页。
            PackNow(atlas);

            BuildReport report = Inspect(atlas);

            var sb = new StringBuilder();
            sb.Append(created ? "已创建图集 " : changed ? "已更新图集 " : "已检查图集 ").Append(path).Append('\n');
            sb.Append("收录 Sprite ").Append(report.spriteCount).Append(" 张");
            if (report.pageCount > 0)
            {
                sb.Append("；打包为 ").Append(report.pageCount).Append(" 页 ")
                  .Append(report.pageWidth).Append('x').Append(report.pageHeight)
                  .Append("，估算显存 ").Append(FormatBytes(report.estimatedBytes));
            }
            else
            {
                sb.Append("；当前编辑器没有提供打包页预览，请用目标平台构建确认尺寸");
            }
            report.message = sb.ToString();
            return report;
        }

        /// <summary>新图集按 UI 贴图初始化，重建现有图集时沿用其导入参数。</summary>
        private static void ApplySettings(SpriteAtlasImporter importer)
        {
            // ── 打包 ──
            SpriteAtlasPackingSettings packing = importer.packingSettings;
            // padding 4：图标自带 8px 透明边距，再加 4 有足够余量防止相邻图在双线性采样下互相渗色
            packing.padding = 4;
            // 禁止旋转：UI 的 Image 按原始朝向铺，图集里转 90 度会让 preserveAspect 算歪
            packing.enableRotation = false;
            // 禁止紧贴打包：Image 的 Sliced / preserveAspect 都按矩形网格算，紧贴网格会改变显示结果
            packing.enableTightPacking = false;
            packing.blockOffset = 1;
            packing.enableAlphaDilation = true;   // 往透明区做一点扩散，缩小时边缘不会渗出黑边
            importer.packingSettings = packing;

            // ── 纹理 ──
            SpriteAtlasTextureSettings texture = importer.textureSettings;
            texture.readable = false;              // 运行时不需要读像素
            texture.generateMipMaps = false;       // UI 不缩远，开了白吃 33% 显存
            texture.sRGB = true;                   // 图是给颜色管线看的
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 1;
            importer.textureSettings = texture;

            // ── 平台格式 ──
            // 带 alpha 的 UI 图，桌面平台 BC7 是质量/体积最平衡的一档；
            // 移动端走 ASTC 6x6（4x4 对 64px 图标来说质量过剩）。
            // 其余平台留给 Unity 默认：没有证据说明它们需要特殊对待，不写死。
            SetPlatform(importer, "Standalone", TextureImporterFormat.BC7);
            SetPlatform(importer, "Android", TextureImporterFormat.ASTC_6x6);
        }

        private static void SetPlatform(SpriteAtlasImporter importer, string platform, TextureImporterFormat format)
        {
            TextureImporterPlatformSettings ps = importer.GetPlatformSettings(platform);
            ps.name = platform;
            ps.overridden = true;
            ps.maxTextureSize = MaxTextureSize;
            ps.format = format;
            ps.compressionQuality = 80;
            ps.crunchedCompression = false;
            importer.SetPlatformSettings(ps);
        }

        /// <summary>强制打一次包。异常不当失败 —— 打不出来时报告里会显示"没取到页信息"。</summary>
        private static void PackNow(SpriteAtlas atlas)
        {
            try
            {
                SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget, false);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[图标图集] 主动打包失败，页信息可能不准：" + e.Message);
            }
        }

        // ══════════════════════ 只读检查 ══════════════════════

        /// <summary>
        /// 读现有图集的打包结果，不做任何改动。用于报告与"只想知道占用"的场合。
        ///
        /// ══════════════ 估算显存为什么不能看预览纹理的格式 ══════════════
        /// 编辑器里拿到的页纹理是【未压缩的预览图】（RGBA32），直接按它算会得到 4 MB，
        /// 而真正进包的是平台压缩格式（Standalone 这里是 BC7，1 字节/像素）——
        /// 差 4 倍，会把一个"其实很省"的方案报成"很占显存"，人就会去优化一个不存在的问题。
        /// 所以按【图集在目标平台上的设置】算，拿不到才退回预览格式。
        ///
        /// crunch 只压构建体积，不压显存 —— 这里的数字是显存，别把两者混起来看。
        /// </summary>
        public static BuildReport Inspect(SpriteAtlas atlas)
        {
            var report = new BuildReport { message = "" };
            if (atlas == null) return report;

            UnityEngine.Object[] packables = SpriteAtlasExtensions.GetPackables(atlas);
            report.spriteCount = packables != null ? packables.Length : 0;

            Texture2D[] pages = GetPackedPages(atlas);
            if (pages == null || pages.Length == 0) return report;

            TextureImporterFormat platformFormat = GetPlatformFormat(atlas);

            report.pageCount = pages.Length;
            long bytes = 0;
            for (int i = 0; i < pages.Length; i++)
            {
                Texture2D p = pages[i];
                if (p == null) continue;
                if (p.width > report.pageWidth) report.pageWidth = p.width;
                if (p.height > report.pageHeight) report.pageHeight = p.height;
                bytes += EstimateBytes(p.width, p.height,
                    platformFormat == TextureImporterFormat.Automatic ? p.format : ToTextureFormat(platformFormat));
            }
            report.estimatedBytes = bytes;
            return report;
        }

        /// <summary>当前构建目标对应的平台设置名。</summary>
        private static string PlatformName(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneLinux64: return "Standalone";
                case BuildTarget.Android: return "Android";
                case BuildTarget.iOS: return "iPhone";
                case BuildTarget.WebGL: return "WebGL";
                default: return "Standalone";
            }
        }

        private static TextureImporterFormat GetPlatformFormat(SpriteAtlas atlas)
        {
            try
            {
                var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(atlas)) as SpriteAtlasImporter;
                TextureImporterPlatformSettings ps = importer?.GetPlatformSettings(PlatformName(EditorUserBuildSettings.activeBuildTarget));
                if (ps != null && ps.overridden) return ps.format;
            }
            catch
            {
                // 取不到就退回预览格式：报告只是估算，不值得为它抛异常
            }
            return TextureImporterFormat.Automatic;
        }

        /// <summary>把图集上的格式枚举映射成"给 EstimateBytes 看的纹理格式"。只关心每像素几个字节。</summary>
        private static TextureFormat ToTextureFormat(TextureImporterFormat f)
        {
            switch (f)
            {
                case TextureImporterFormat.BC7: return TextureFormat.BC7;
                case TextureImporterFormat.DXT1: return TextureFormat.DXT1;
                case TextureImporterFormat.DXT1Crunched: return TextureFormat.DXT1;
                case TextureImporterFormat.DXT5: return TextureFormat.DXT5;
                case TextureImporterFormat.DXT5Crunched: return TextureFormat.DXT5;
                case TextureImporterFormat.RGB24: return TextureFormat.RGB24;
                case TextureImporterFormat.RGBA32: return TextureFormat.RGBA32;
                case TextureImporterFormat.RGBAHalf: return TextureFormat.RGBAHalf;
                default:
                    // ASTC 系列与其余压缩格式：按名字判断，见 EstimateBytes
                    return TextureFormat.RGBA32;
            }
        }

        /// <summary>
        /// 按格式估算每页占多少显存。压缩率按官方块大小算：
        /// DXT1/BC1 = 0.5 字节/像素、DXT5/BC7 = 1、ASTC 4x4 = 1、ASTC 8x8 = 0.25，未压缩 = 每通道 1 字节。
        /// </summary>
        private static long EstimateBytes(int width, int height, TextureFormat format)
        {
            double pixels = (double)width * height;
            string name = format.ToString();

            if (name.StartsWith("ASTC_4x4", StringComparison.Ordinal)) return (long)(pixels * 1.0);
            if (name.StartsWith("ASTC_5x5", StringComparison.Ordinal)) return (long)(pixels * 0.64);
            if (name.StartsWith("ASTC_6x6", StringComparison.Ordinal)) return (long)(pixels * 0.44);
            if (name.StartsWith("ASTC_8x8", StringComparison.Ordinal)) return (long)(pixels * 0.25);
            if (name.StartsWith("ASTC", StringComparison.Ordinal)) return (long)(pixels * 1.0);

            switch (format)
            {
                case TextureFormat.DXT1: return (long)(pixels * 0.5);
                case TextureFormat.DXT5:
                case TextureFormat.BC7: return (long)pixels;
                case TextureFormat.RGB24: return (long)(pixels * 3);
                case TextureFormat.RGBAHalf: return (long)(pixels * 8);
                default: return (long)(pixels * 4);
            }
        }

        /// <summary>
        /// 取打包后的页纹理。公开 API 里没有这个入口（只有打包器内部持有），
        /// 所以走反射 —— 与工程里 Spine 的 AtlasUtilities 用的是同一个口子。
        /// 取不到时返回 null，调用方必须容忍（报告里会写明没取到）。
        /// </summary>
        private static Texture2D[] GetPackedPages(SpriteAtlas atlas)
        {
            try
            {
                Type ext = typeof(SpriteAtlasExtensions);
                MethodInfo m = ext.GetMethod("GetPreviewTextures",
                    BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (m == null) return null;

                object result = m.Invoke(null, new object[] { atlas });
                return result as Texture2D[];
            }
            catch
            {
                return null;
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024 * 1024) return (bytes / 1024f / 1024f).ToString("0.00") + " MB";
            if (bytes >= 1024) return (bytes / 1024f).ToString("0.0") + " KB";
            return bytes + " B";
        }

        // ══════════════════════ 给测试用的入口 ══════════════════════

        /// <summary>把报告压成一行，供测试断言与日志。</summary>
        public static string DescribeForTest(BuildReport r)
        {
            return "sprites=" + r.spriteCount + " pages=" + r.pageCount +
                   " maxPage=" + r.pageWidth + "x" + r.pageHeight +
                   " bytes=" + r.estimatedBytes;
        }
    }
}
