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
    /// 按键图标图集的构建器。菜单：Tools/图标/重建图标图集
    ///
    /// ══════════════════════ 为什么要打图集 ══════════════════════
    /// 图标是零散的小图。不打图集时每一张都是一个独立纹理：
    ///   · 一个按键界面同时显示 8 个格子 → 至少 8 次 draw call（同图才可能合批）；
    ///   · 每张 64x64 即便压过也有独立的纹理对象与采样器状态；
    ///   · 显存按【张数】线性长，而不是按"一张图集能装多少"长。
    /// 打进图集之后，界面上所有图标来自同一张纹理 → 一次 draw call，
    /// 而且显存变成"图集页数"这一个可算的量。
    ///
    /// ══════════════════════ 白名单，不是整目录 ══════════════════════
    /// 本构建器只把【映射表引用到的】图标放进图集，而不是把 Art/icon 整个目录 436 张全塞进去。
    /// 两个理由：
    ///   1. 库里同一个控制常有 2~4 种画法（_outline / _icon / _alternative / _color_），
    ///      真正会显示的每个控制只有一张，其余都是死重；
    ///   2. 白名单随映射表自动跟着走 —— 以后美术再加图，只有"真的被选中的"才进包，
    ///      占用不会因为"美术多丢了几批图"而无声膨胀。
    ///
    /// ⚠ 白名单必须是【映射表】而不是"当前默认绑定用到的键"。
    /// 玩家能在运行时把任意键绑到任意动作上；只按当前绑定打图集的话，
    /// 玩家改到一个没进包里的键，那一格就会变成空图。
    /// 映射表覆盖的是整个键盘/鼠标/手柄，所以按它打是完整的。
    ///
    /// ══════════════════════ 报告为什么要"真的打一次" ══════════════════════
    /// 图集的实际页数与尺寸只有打包后才知道（Unity 按需分页）。
    /// 所以 Rebuild 里会主动 PackAtlases 一次，再用反射取回生成的页纹理，
    /// 把"几页 / 多大 / 估算显存"如实报出来 —— 而不是拿"图标数 × 猜测"糊弄。
    ///
    /// ══════════════════════ 编辑器里看不出来是正常的 ══════════════════════
    /// 本工程的 EditorSettings.spritePackerMode 是 Disabled（历史状态，本工具不去动它）。
    /// 影响只有一条：编辑器 Play 模式下精灵不绑定到图集，所以 Frame Debugger 里看到的
    /// 仍是一张张独立纹理 —— 而【构建出来的包用的是图集】，因为 IncludeInBuild = true。
    /// 也就是说：这里报告的数字是"进包之后"的占用；想在编辑器里也看到合批效果，
    /// 需要把 Project Settings → Editor → Sprite Atlas 的模式手动改成"一直启用"。
    /// 那是全局设置（会影响所有图集的导入成本），不属于本工具该替人决定的事。
    /// </summary>
    public static class IconAtlasBuilder
    {
        /// <summary>图集资产路径。放在图标目录下，方便和素材放一起看。</summary>
        public const string AtlasPath = IconImportPostprocessor.IconRoot + "/IconAtlas_Input.spriteatlas";

        /// <summary>打包时的单页上限。图标是 64px，2048 一页能装下几百张，够用且不会一次开太大。</summary>
        private const int MaxTextureSize = 2048;

        [MenuItem("Tools/图标/重建图标图集", false, 901)]
        public static void RebuildMenu()
        {
            BuildReport report = Rebuild();
            Debug.Log("[图标图集] " + report.message.Replace("\n", " "));
            EditorUtility.DisplayDialog("重建图标图集", report.message, "好");
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
        /// 重建图集：清空旧的白名单 → 按映射表重新加入 → 写设置 → 强制打包 → 报告。
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

            EnsureFolder();

            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);
            bool created = false;
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, AtlasPath);
                created = true;
            }

            // ── 白名单：先全清再全加 ──
            UnityEngine.Object[] old = SpriteAtlasExtensions.GetPackables(atlas);
            if (old != null && old.Length > 0) SpriteAtlasExtensions.Remove(atlas, old);

            var packables = new List<UnityEngine.Object>();
            for (int i = 0; i < sprites.Count; i++)
            {
                if (sprites[i] != null) packables.Add(sprites[i]);
            }
            SpriteAtlasExtensions.Add(atlas, packables.ToArray());

            ApplySettings(atlas);

            EditorUtility.SetDirty(atlas);
            AssetDatabase.SaveAssets();

            // 图集的页纹理是"打包时"生成的，不主动打一次就拿不到真实尺寸
            PackNow(atlas);

            BuildReport report = Inspect(atlas);
            report.spriteCount = packables.Count;

            var sb = new StringBuilder();
            sb.Append(created ? "已创建图集 " : "已更新图集 ").Append(AtlasPath).Append('\n');
            sb.Append("写入 sprite ").Append(report.spriteCount).Append(" 张");
            if (report.pageCount > 0)
            {
                sb.Append("；打包为 ").Append(report.pageCount).Append(" 页 ")
                  .Append(report.pageWidth).Append('x').Append(report.pageHeight)
                  .Append("，估算显存 ").Append(FormatBytes(report.estimatedBytes));
            }
            else
            {
                sb.Append("；⚠ 没能取到打包后的页信息（图集可能没生效，检查 Project Settings → Editor → Sprite Atlas 模式）");
            }
            sb.Append("\n\n图集设置：单页上限 ").Append(MaxTextureSize)
              .Append("，padding 4、禁止旋转、禁止紧贴打包，Standalone 走 BC7 + crunch。");
            report.message = sb.ToString();
            return report;
        }

        /// <summary>写死每一项设置，不依赖默认值 —— 默认值随 Unity 版本变，而占用不该随版本漂。</summary>
        private static void ApplySettings(SpriteAtlas atlas)
        {
            // ── 打包 ──
            SpriteAtlasPackingSettings packing = SpriteAtlasExtensions.GetPackingSettings(atlas);
            // padding 4：图标自带 8px 透明边距，再加 4 有足够余量防止相邻图在双线性采样下互相渗色
            packing.padding = 4;
            // 禁止旋转：UI 的 Image 按原始朝向铺，图集里转 90 度会让 preserveAspect 算歪
            packing.enableRotation = false;
            // 禁止紧贴打包：Image 的 Sliced / preserveAspect 都按矩形网格算，紧贴网格会改变显示结果
            packing.enableTightPacking = false;
            packing.blockOffset = 1;
            packing.enableAlphaDilation = true;   // 往透明区做一点扩散，缩小时边缘不会渗出黑边
            SpriteAtlasExtensions.SetPackingSettings(atlas, packing);

            // ── 纹理 ──
            SpriteAtlasTextureSettings texture = SpriteAtlasExtensions.GetTextureSettings(atlas);
            texture.readable = false;              // 运行时不需要读像素
            texture.generateMipMaps = false;       // UI 不缩远，开了白吃 33% 显存
            texture.sRGB = true;                   // 图是给颜色管线看的
            texture.filterMode = FilterMode.Bilinear;
            texture.anisoLevel = 1;
            SpriteAtlasExtensions.SetTextureSettings(atlas, texture);

            // ── 平台格式 ──
            // 带 alpha 的 UI 图，桌面平台 BC7 是质量/体积最平衡的一档；
            // 移动端走 ASTC 6x6（4x4 对 64px 图标来说质量过剩）。
            // 其余平台留给 Unity 默认：没有证据说明它们需要特殊对待，不写死。
            SetPlatform(atlas, "Standalone", TextureImporterFormat.BC7, 80, true);
            SetPlatform(atlas, "Android", TextureImporterFormat.ASTC_6x6, 80, false);

            // ── 进包 ──
            // 不打进包的话图集只存在于编辑器（运行时又变回一张张独立纹理），这个开关是必须的
            SpriteAtlasExtensions.SetIncludeInBuild(atlas, true);
            SpriteAtlasExtensions.SetIsVariant(atlas, false);
        }

        private static void SetPlatform(SpriteAtlas atlas, string platform, TextureImporterFormat format,
            int quality, bool crunch)
        {
            TextureImporterPlatformSettings ps = SpriteAtlasExtensions.GetPlatformSettings(atlas, platform);
            ps.name = platform;
            ps.overridden = true;
            ps.maxTextureSize = MaxTextureSize;
            ps.format = format;
            ps.compressionQuality = quality;
            ps.crunchedCompression = crunch;
            SpriteAtlasExtensions.SetPlatformSettings(atlas, ps);
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
                TextureImporterPlatformSettings ps = SpriteAtlasExtensions.GetPlatformSettings(
                    atlas, PlatformName(EditorUserBuildSettings.activeBuildTarget));
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

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(IconImportPostprocessor.IconRoot))
            {
                throw new InvalidOperationException(
                    "图标目录不存在：" + IconImportPostprocessor.IconRoot + "。路径是不是改了？");
            }
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
