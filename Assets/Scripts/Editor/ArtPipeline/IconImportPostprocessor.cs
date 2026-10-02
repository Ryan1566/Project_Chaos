using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ArtPipeline.Editor
{
    /// <summary>
    /// <c>Art/icon</c> 下所有按键图标的导入设置策略。菜单：Tools/图标/应用图标导入设置
    ///
    /// ══════════════════════ 为什么要有这一层 ══════════════════════
    /// 图标是美术按批拖进来的，每一批的导入设置都会带着 Unity 的默认值。
    /// 而默认值对 UI 图标【每一项都是错的或浪费的】：
    ///   · textureType 默认 Default —— 这样的纹理根本赋不进 Image.sprite，功能直接不工作；
    ///   · 默认开 mipmap —— UI 图标不会缩远，白白多吃 33% 显存，还让边缘发糊；
    ///   · 默认不开 alphaIsTransparency —— 透明边缘会被当成"真的有这个颜色"，出现暗边；
    ///   · 默认 NPOT 垫到 2 的幂 —— 以后来一张 48x48 的图会被悄悄垫成 64x64；
    ///   · 默认 Point 过滤 —— 64px 的图缩到 ~50px 显示会出硬锯齿；
    ///   · 默认 maxTextureSize 2048 —— 一张 64px 的图允许 2048，等于把"放错图"的后果放大 1024 倍。
    ///
    /// 手工逐张改是不可持续的：图标已经有 436 张，而且会长期增加。
    /// 所以把口径写成 AssetPostprocessor —— 它在新图落地的【那一刻】就生效，
    /// 人不需要记得任何事，也就不会漏。
    ///
    /// ══════════════ 已有的图怎么办 ══════════════
    /// 后处理器只在"资产被导入"时跑。已经躺在工程里的 436 张不会自己重导，
    /// 所以另给一个菜单项（见 ApplyToAll）强制重扫一遍。
    ///
    /// ══════════════ 只管 Art/icon，别碰别人的东西 ══════════════
    /// 工程里还有 Spine 的 AssetPostprocessor（它按自己的规则处理贴图）。
    /// 不命中本目录时【立刻返回】，不做任何写操作，两边互不干扰。
    /// </summary>
    public class IconImportPostprocessor : AssetPostprocessor
    {
        /// <summary>按键图标所在目录（工程内路径，Assets/ 开头）。工具与图集构建共用它。</summary>
        public const string IconRoot = "Assets/Art/icon";

        /// <summary>
        /// 走脚本路径时置 true，跳过确认对话框。
        /// 与本地化那几个工具用同一套约定：无人值守的批量操作不该弹窗卡住（See LanguageFontMapWindow.SilentMode）。
        /// </summary>
        public static bool SilentMode;

        /// <summary>
        /// 导入前的最后一道闸口。只认 Art/icon 下的贴图，其余一律放行。
        ///
        /// 用 assetPath.StartsWith 而不是 Contains：Contains("Art/icon") 会命中
        /// "Assets/Something/Art/icon_x/..." 这类无关目录，那种误伤很难查（表现是别的贴图被改了设置）。
        /// </summary>
        private void OnPreprocessTexture()
        {
            if (string.IsNullOrEmpty(assetPath)) return;
            if (!assetPath.StartsWith(IconRoot + "/", StringComparison.OrdinalIgnoreCase)) return;

            TextureImporter importer = assetImporter as TextureImporter;
            if (importer == null) return;

            ApplyPolicy(importer);
        }

        /// <summary>把图标导入口径写进一个 importer。菜单项与后处理器共用它，口径只有一份。</summary>
        public static void ApplyPolicy(TextureImporter importer)
        {
            if (importer == null) return;

            // ══ 类型：必须是 Sprite，否则 Image.sprite 根本接不上 ══
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.spriteBorder = Vector4.zero;

            // FullRect 而不是 Tight：Image 的 preserveAspect 与 Sliced 都按矩形算，
            // Tight 给的紧贴网格会让"按比例缩放"的结果和直觉不同；而且图集关了紧贴打包，
            // 这里保持矩形最省心。
            // ⚠ spriteMeshType 不在 TextureImporter 上，只能走 TextureImporterSettings 这条口子
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            // ══ 尺寸：图标是 UI，不缩远 ══
            importer.mipmapEnabled = false;
            // 64px 的源图给 128 的上限：留一点 2x 资产的余地，同时挡住"误放一张 2048 大图"这种事故
            importer.maxTextureSize = 128;
            // 不去垫成 2 的幂：图标都是 64x64，但将来出现 48x48 也不该被垫到 64x64 白吃显存
            importer.npotScale = TextureImporterNPOTScale.None;

            // ══ 采样 ══
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.anisoLevel = 1;

            // ══ 颜色 ══
            importer.sRGBTexture = true;
            // 不开的话透明区域里的颜色会被当成真实颜色参与过滤，边缘出现暗边/亮边
            importer.alphaIsTransparency = true;

            // ══ 压缩 ══
            // CompressedHQ：带 alpha 的纹理在桌面平台会走 BC7，质量与体积都比默认的 DXT5 划算。
            // crunch 进一步压构建体积（代价是导入变慢，而图标只在导入时付这一次）。
            // ⚠ 这些值只在【图集没接住它】时才生效 —— 进了 SpriteAtlas 之后格式由图集说了算，
            //    所以图集的压缩设置才是重点（见 IconAtlasBuilder）。
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.crunchedCompression = true;
            importer.compressionQuality = 80;

            // 运行时不需要读像素（图标只是显示），保持不可读能省一份内存副本
            importer.isReadable = false;
        }

        // ══════════════════ 给已有的图补一遍 ══════════════════

        [MenuItem("Tools/图标/应用图标导入设置", false, 900)]
        public static void ApplyToAll()
        {
            if (!SilentMode)
            {
                bool go = EditorUtility.DisplayDialog(
                    "应用图标导入设置",
                    "将按统一口径重新导入 " + IconRoot + " 下的全部图标。\n\n" +
                    "会把 textureType 改成 Sprite、关掉 mipmap、打开 alphaIsTransparency、\n" +
                    "把 maxTextureSize 收到 128、开启 crunch 压缩。\n\n" +
                    "图标数量较多时这一步会花几秒到几十秒。",
                    "开始", "取消");
                if (!go) return;
            }

            string report = ApplyToAllInternal();
            if (!SilentMode) EditorUtility.DisplayDialog("应用图标导入设置", report, "好");
            Debug.Log("[图标导入] " + report.Replace("\n", " "));
        }

        /// <summary>强制重扫图标目录。返回给人和给测试看的一份报告。</summary>
        public static string ApplyToAllInternal()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { IconRoot });
            if (guids.Length == 0)
            {
                return "在 " + IconRoot + " 下没找到任何贴图。目录路径是不是改了？";
            }

            // ══════════════ 为什么整目录一次导入，而不是逐张 SaveAndReimport ══════════════
            // 逐张 SaveAndReimport 每张都要走一遍"写 importer → 导入 → 落盘"，
            // 436 张要几十秒；而 ImportAsset(目录, ImportRecursive | ForceUpdate) 一次就能
            // 让后处理器在每张图上跑一遍。ForceUpdate 是关键 ——
            // 没有它，内容没变的资产会被跳过，后处理器根本不会被调用（看起来"点了没反应"）。
            //
            // ⚠ 不能用 AssetDatabase.StartAssetEditing 包住：那个 API 会把导入推迟，
            // 而后处理器恰恰要在导入时起作用（而且 StartAssetEditing 期间不许 SaveAssets）。
            AssetDatabase.ImportAsset(IconRoot,
                ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);

            // 复核一遍，而不是假定"导入过了就对了"：口径写错时这里会立刻暴露
            int total = 0, sprite = 0, mipOn = 0, notTransparent = 0;
            var samples = new List<string>();
            string[] afterGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { IconRoot });
            for (int i = 0; i < afterGuids.Length; i++)
            {
                string p = AssetDatabase.GUIDToAssetPath(afterGuids[i]);
                TextureImporter ti = AssetImporter.GetAtPath(p) as TextureImporter;
                if (ti == null) continue;

                total++;
                if (ti.textureType == TextureImporterType.Sprite) sprite++;
                if (ti.mipmapEnabled) mipOn++;
                if (!ti.alphaIsTransparency) notTransparent++;

                if (samples.Count < 3 && ti.textureType != TextureImporterType.Sprite) samples.Add(p);
            }

            var sb = new StringBuilder();
            sb.Append("重扫 ").Append(IconRoot).Append("：共 ").Append(total).Append(" 张，");
            sb.Append("已是 Sprite ").Append(sprite).Append(" 张");
            if (mipOn > 0) sb.Append("；⚠ 仍开着 mipmap 的 ").Append(mipOn).Append(" 张");
            if (notTransparent > 0) sb.Append("；⚠ 未开 alphaIsTransparency 的 ").Append(notTransparent).Append(" 张");
            if (sprite < total)
            {
                sb.Append("\n没变成 Sprite 的前几张：");
                for (int i = 0; i < samples.Count; i++) sb.Append("\n  · ").Append(samples[i]);
            }
            return sb.ToString();
        }
    }
}
