using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using RunMap;

namespace RunMapTool.Editor
{
    /// <summary>
    /// 生成器的验证与可视化工具（纯逻辑驱动，不需要开场景）。
    ///
    /// 为什么先做这个而不是先做实例化：
    /// 生成算法的正确性（连通性 / 接口匹配 / 同种子同结果）**只能在大量种子上跑出来**，
    /// 而单张地图"看着对"不能说明问题。先把逻辑跑穿，再把对的图交给 Pass E 去拼 prefab。
    ///
    /// 菜单：Tools/局内地图/3. 校验（多种子）、4. 生成整图预览 PNG
    /// </summary>
    public static class RunMapVerifyTool
    {
        public const string PreviewRoot = "Assets/AI/runmap-art";

        [MenuItem("Tools/局内地图/3. 校验生成器（多种子压测）", false, 103)]
        public static void MenuVerify()
        {
            string report = Verify(300);
            EditorUtility.DisplayDialog("局内地图生成器校验", report, "好");
            Debug.Log("[RunMapVerify] " + report.Replace("\n", " | "));
        }

        [MenuItem("Tools/局内地图/4. 生成整图预览 PNG", false, 104)]
        public static void MenuPreview()
        {
            string report = PreviewSamples();
            EditorUtility.DisplayDialog("局内地图预览", report, "好");
            Debug.Log("[RunMapVerify] " + report.Replace("\n", " | "));
        }

        /// <summary>给自动化调用：每个已启用 preset 跑 seeds 个种子，统计失败与确定性。</summary>
        public static string Verify(int seeds)
        {
            var sb = new StringBuilder();
            RunMapCatalog cat = RunMapCatalog.CreateGreybox();
            int totalOk = 0, totalFail = 0, degraded = 0, detFail = 0, c1Fail = 0;
            var firstErrors = new List<string>();

            for (int p = 0; p < cat.Presets.Count; p++)
            {
                PresetDef preset = cat.Presets[p];
                if (!preset.Enabled) { sb.Append("  · ").Append(preset.Name).Append("：已停用，跳过\n"); continue; }

                string theme = RunMapGenerator.ThemeKeyOf(preset);
                int ok = 0, fail = 0, deg = 0, det = 0;
                var errs = new List<string>();

                for (int i = 0; i < seeds; i++)
                {
                    var ctx = new RunMapContext { RunSeed = (uint)(i * 2654435761u + 12345u), SceneIndex = 1, Era = 1, Depth = 1 };
                    RunMapResult r1 = RunMapGenerator.Generate(ctx, preset, cat, theme);

                    // 确定性：同一上下文再跑一次，签名必须逐字段一致
                    RunMapResult r2 = RunMapGenerator.Generate(ctx, preset, cat, theme);
                    if (r1.Layout == null || r2.Layout == null || r1.Layout.Signature() != r2.Layout.Signature()) det++;

                    if (!r1.Ok || r1.Layout == null) { fail++; if (errs.Count < 3 && r1.Errors.Count > 0) errs.Add(r1.Errors[0]); continue; }
                    if (r1.Degraded) deg++;

                    List<string> v = RunMapValidator.Validate(r1.Layout, preset, cat);
                    if (v.Count > 0) { fail++; if (errs.Count < 3) errs.Add(v[0]); continue; }
                    ok++;
                }

                totalOk += ok; totalFail += fail; degraded += deg; detFail += det;
                sb.Append("  · ").Append(preset.Name).Append("（").Append(theme).Append("）× ").Append(seeds)
                  .Append(" 种子 → 通过 ").Append(ok).Append("，失败 ").Append(fail)
                  .Append("，降级 ").Append(deg).Append("，确定性不一致 ").Append(det).Append('\n');
                for (int e = 0; e < errs.Count; e++) sb.Append("      ⚠ ").Append(errs[e]).Append('\n');
            }

            // 一局 3 场景的三层覆盖（C1）
            int runSeeds = 100;
            for (int i = 0; i < runSeeds; i++)
            {
                List<RunMapResult> run = RunMapGenerator.GenerateRun((uint)(i * 40503u + 7u), 1, 1, cat);
                if (RunMapGenerator.MissingLayers(run).Count > 0) c1Fail++;
            }

            sb.Append("汇总：通过 ").Append(totalOk).Append(" / 失败 ").Append(totalFail)
              .Append(" / 降级 ").Append(degraded).Append(" / 确定性不一致 ").Append(detFail)
              .Append("；一局三层覆盖失败 ").Append(c1Fail).Append(" / ").Append(runSeeds).Append('\n');
            sb.Append(totalFail == 0 && detFail == 0 && c1Fail == 0 ? "✅ 全部通过" : "❌ 有失败项，见上");
            return sb.ToString();
        }

        /// <summary>单独压测一个 preset（含"已停用但想试跑"的多层预设）。</summary>
        public static string VerifyPreset(int presetId, int seeds, bool forceEnable)
        {
            RunMapCatalog cat = RunMapCatalog.CreateGreybox();
            PresetDef preset = cat.GetPreset(presetId);
            if (preset == null) return "找不到 preset " + presetId;
            if (forceEnable) preset.Enabled = true;
            string theme = RunMapGenerator.ThemeKeyOf(preset);

            int ok = 0, fail = 0, deg = 0, det = 0;
            var errs = new List<string>();
            for (int i = 0; i < seeds; i++)
            {
                var ctx = new RunMapContext { RunSeed = (uint)(i * 2654435761u + 999u), SceneIndex = 1, Era = 2, Depth = 2 };
                RunMapResult r1 = RunMapGenerator.Generate(ctx, preset, cat, theme);
                RunMapResult r2 = RunMapGenerator.Generate(ctx, preset, cat, theme);
                if (r1.Layout == null || r2.Layout == null || r1.Layout.Signature() != r2.Layout.Signature()) det++;
                if (!r1.Ok || r1.Layout == null) { fail++; if (errs.Count < 5 && r1.Errors.Count > 0) errs.Add(r1.Errors[0]); continue; }
                if (r1.Degraded) deg++;
                List<string> v = RunMapValidator.Validate(r1.Layout, preset, cat);
                if (v.Count > 0) { fail++; if (errs.Count < 5) errs.Add(string.Join(" | ", v.ToArray())); continue; }
                ok++;
                if (i == 0)
                {
                    var sb0 = new StringBuilder();
                    sb0.Append("      · 样例：层数=").Append(CountLayers(r1.Layout)).Append("，房间=").Append(r1.Layout.Nodes.Count)
                       .Append("，边=").Append(r1.Layout.Edges.Count).Append("，连接件=").Append(CountLinks(r1.Layout));
                    errs.Add(sb0.ToString());
                }
            }
            return "preset " + presetId + "（" + preset.Name + "，" + theme + "）× " + seeds + " 种子 → 通过 " + ok +
                   "，失败 " + fail + "，降级 " + deg + "，确定性不一致 " + det + "\n" +
                   string.Join("\n", errs.ToArray());
        }

        private static int CountLayers(MapLayout l)
        {
            var s = new List<MapLayer>();
            for (int i = 0; i < l.Nodes.Count; i++) if (!s.Contains(l.Nodes[i].Layer)) s.Add(l.Nodes[i].Layer);
            return s.Count;
        }

        private static int CountLinks(MapLayout l)
        {
            int n = 0;
            for (int i = 0; i < l.Nodes.Count; i++) if (l.Nodes[i].IsLayerLink) n++;
            return n;
        }

        /// <summary>每个主题各出一张整图预览（含边与接口开口示意）。</summary>
        public static string PreviewSamples()
        {
            RunMapCatalog cat = RunMapCatalog.CreateGreybox();
            EnsureFolder(PreviewRoot);
            var sb = new StringBuilder();
            string[] themes = { "Surface", "Underground", "Building" };
            for (int t = 0; t < themes.Length; t++)
            {
                MapLayer layer = t == 0 ? MapLayer.Surface : (t == 1 ? MapLayer.Underground : MapLayer.Building);
                PresetDef preset = cat.FindSingleLayerPreset(layer);
                var ctx = new RunMapContext { RunSeed = 20261010u, SceneIndex = 1, Era = 1, Depth = 1 };
                RunMapResult r = RunMapGenerator.Generate(ctx, preset, cat, themes[t]);
                if (r.Layout == null) { sb.Append("  · ").Append(themes[t]).Append("：生成失败\n"); continue; }
                string path = PreviewRoot + "/map_" + themes[t] + ".png";
                RenderLayout(r.Layout, cat, path);
                sb.Append("  · ").Append(themes[t]).Append("：房间 ").Append(r.Layout.Nodes.Count)
                  .Append("，边 ").Append(r.Layout.Edges.Count)
                  .Append("，标记点 ").Append(r.Layout.Slots.Count)
                  .Append(r.Degraded ? "（降级）" : "").Append(" → ").Append(path).Append('\n');
            }
            // 跨层示例：地表 → 地下，中间一个竖井连接件
            PresetDef p20 = cat.GetPreset(20);
            var ctx2 = new RunMapContext { RunSeed = 20261010u, SceneIndex = 2, Era = 2, Depth = 2 };
            RunMapResult r2 = RunMapGenerator.Generate(ctx2, p20, cat, RunMapGenerator.ThemeKeyOf(p20));
            if (r2.Layout != null)
            {
                string path2 = PreviewRoot + "/map_TwoLayer.png";
                RenderLayout(r2.Layout, cat, path2);
                sb.Append("  · 双层（地表→地下）：房间 ").Append(r2.Layout.Nodes.Count)
                  .Append("，边 ").Append(r2.Layout.Edges.Count)
                  .Append("，连接件 ").Append(CountLinks(r2.Layout))
                  .Append(r2.Degraded ? "（降级）" : "").Append(" → ").Append(path2).Append('\n');
            }

            AssetDatabase.Refresh();
            return "整图预览：\n" + sb;
        }

        // ═════════════════════════ 示意渲染 ═════════════════════════

        /// <summary>
        /// 把整张图按"房间块 = 40×24 格"拼起来画成 PNG。
        /// 每格 3px；房间按层配色，用到的接口画成缺口，边画成连线，入口/ boss 描边区分。
        /// 这张图是给人看的**结构示意图**，不是瓦片拼出来的实景。
        /// </summary>
        public static void RenderLayout(MapLayout layout, RunMapCatalog cat, string path)
        {
            const int cell = 3;
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            for (int i = 0; i < layout.Nodes.Count; i++)
            {
                RoomNode n = layout.Nodes[i];
                if (n.CellX < minX) minX = n.CellX;
                if (n.CellX > maxX) maxX = n.CellX;
                if (n.CellY < minY) minY = n.CellY;
                if (n.CellY > maxY) maxY = n.CellY;
            }
            int cols = maxX - minX + 1, rows = maxY - minY + 1;
            int w = cols * RunMapConst.ChunkW * cell;
            int h = rows * RunMapConst.ChunkH * cell;
            if (w <= 0 || h <= 0 || w > 8192 || h > 8192) return;

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            Color32 bg = new Color32(0x11, 0x11, 0x14, 0xFF);
            for (int i = 0; i < px.Length; i++) px[i] = bg;

            for (int i = 0; i < layout.Nodes.Count; i++)
            {
                RoomNode n = layout.Nodes[i];
                int ox = (n.CellX - minX) * RunMapConst.ChunkW * cell;
                int oy = (n.CellY - minY) * RunMapConst.ChunkH * cell;
                Color32 body = LayerColor(n.Layer);
                Color32 edge = (n.Id == layout.EntryId) ? new Color32(0x40, 0xE0, 0xFF, 0xFF)
                             : (n.Id == layout.BossId) ? new Color32(0xFF, 0x40, 0xC0, 0xFF)
                             : new Color32(0x20, 0x20, 0x28, 0xFF);
                if (n.IsLayerLink) { body = new Color32(0x55, 0x50, 0x40, 0xFF); edge = new Color32(0xE0, 0xC0, 0x40, 0xFF); }

                for (int y = 0; y < RunMapConst.ChunkH * cell; y++)
                    for (int x = 0; x < RunMapConst.ChunkW * cell; x++)
                    {
                        bool border = x < 2 || y < 2 || x >= RunMapConst.ChunkW * cell - 2 || y >= RunMapConst.ChunkH * cell - 2;
                        // 房间内部画暗一档，边线亮一档；这样"哪是房间、哪是缝"一眼能分
                        Color32 c = border ? edge : body;
                        if (!border && ((x / cell) % 8 == 0 || (y / cell) % 8 == 0)) c = Shift(body, -8);
                        int idx = (oy + y) * w + (ox + x);
                        if (idx >= 0 && idx < px.Length) px[idx] = c;
                    }
            }

            // 用到的接口：在房间边界上涂一条亮线（横向按档位定位，竖向按列定位）
            for (int i = 0; i < layout.Edges.Count; i++)
            {
                RoomEdge e = layout.Edges[i];
                RoomNode a = layout.Find(e.From);
                if (a == null) continue;
                int ox = (a.CellX - minX) * RunMapConst.ChunkW * cell;
                int oy = (a.CellY - minY) * RunMapConst.ChunkH * cell;
                int row = RunMapConst.ProfileFloorRow[Mathf.Clamp(e.Profile, 0, 2)];
                int x0 = 0, y0 = 0, x1 = 0, y1 = 0;
                if (e.Dir == SocketDir.Right) { x0 = x1 = (RunMapConst.ChunkW - 1) * cell; y0 = (row + 2) * cell; y1 = y0 + RunMapConst.DoorHeight * cell; }
                else if (e.Dir == SocketDir.Left) { x0 = x1 = 2; y0 = (row + 2) * cell; y1 = y0 + RunMapConst.DoorHeight * cell; }
                else if (e.Dir == SocketDir.Up) { y0 = y1 = (RunMapConst.ChunkH - 1) * cell; x0 = (RunMapConst.ShaftColumn - 1) * cell; x1 = (RunMapConst.ShaftColumn + 2) * cell; }
                else { y0 = y1 = 3; x0 = (RunMapConst.ShaftColumn - 1) * cell; x1 = (RunMapConst.ShaftColumn + 2) * cell; }

                for (int t = 0; t <= Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0)); t++)
                {
                    int x = x0 + (int)Mathf.Sign(x1 - x0) * t;
                    int y = y0 + (int)Mathf.Sign(y1 - y0) * t;
                    for (int d = -1; d <= 1; d++)
                    {
                        int idx = (oy + y) * w + (ox + x + (y0 == y1 ? 0 : d));
                        if (idx >= 0 && idx < px.Length) px[idx] = new Color32(0xFF, 0xF0, 0x60, 0xFF);
                    }
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            File.WriteAllBytes(path, png);
        }

        private static Color32 LayerColor(MapLayer layer)
        {
            switch (layer)
            {
                case MapLayer.Underground: return new Color32(0x2E, 0x38, 0x4E, 0xFF);
                case MapLayer.Building: return new Color32(0x4A, 0x2A, 0x22, 0xFF);
                default: return new Color32(0x2A, 0x3E, 0x28, 0xFF);
            }
        }

        private static Color32 Shift(Color32 c, int d)
        {
            return new Color32((byte)Mathf.Clamp(c.r + d, 0, 255), (byte)Mathf.Clamp(c.g + d, 0, 255), (byte)Mathf.Clamp(c.b + d, 0, 255), c.a);
        }

        private static void EnsureFolder(string p)
        {
            if (AssetDatabase.IsValidFolder(p)) return;
            string parent = Path.GetDirectoryName(p).Replace('\\', '/');
            string leaf = Path.GetFileName(p);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
