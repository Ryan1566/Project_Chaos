using System.Collections.Generic;
using UnityEngine;
using ChaosDebug;
using LocalizationSystem;

namespace RunMap
{
    /// <summary>
    /// 读条期"地图生成阶段"文案。
    ///
    /// **刻意不走游戏自己的本地化系统**（用户 2026-10-10 指定）：
    /// 阶段文案就放在 `MapLoading_地图生成读条表` 里，一列一种语言
    /// （`descCN / descTW / descEN / descJP / descKR`），运行时**按当前设置里的语言**取对应列。
    ///
    /// 当前语言取自 `SettingsManager.Instance.Applied.language`（= 设置面板「游戏性 → 语言」那一行写进去的
    /// `LanguageType` 整数值），**不依赖场景里有没有 LocalizationManager**
    /// —— 读条界面可能比本地化管理器先就绪，取设置值最稳。
    ///
    /// 为什么用"按列取"而不是"按 key 查本地化表"：
    /// ① 阶段文案是**系统文案**、由策划与地图生成表一起维护，放一张表里改起来最省事；
    /// ② 将来某语言还没翻译时，可以只留空那一列，回退到简中，而不是整条 key 缺失。
    /// </summary>
    public static class RunMapLoadingText
    {
        /// <summary>阶段码（与 MapLoading 表的 stage 列一致）。</summary>
        public const uint StagePrepare = 1;
        public const uint StageGraph = 2;
        public const uint StageLayout = 3;
        public const uint StagePickChunk = 4;
        public const uint StageInstantiate = 5;
        public const uint StageSealAndPopulate = 6;
        public const uint StageValidate = 7;
        public const uint StageReady = 8;

        private static List<MapLoadingConfig> _rows;
        private static bool _loadTried;

        /// <summary>当前设置里的语言。取不到就按简中。</summary>
        public static LanguageType CurrentLanguage
        {
            get
            {
                try
                {
                    SettingsData d = SettingsManager.Instance != null ? SettingsManager.Instance.Applied : null;
                    if (d != null) return (LanguageType)d.language;
                }
                catch { /* 设置系统还没起来：按简中 */ }
                return LanguageType.ChineseSimplified;
            }
        }

        /// <summary>取某个阶段的文案；表里没有该阶段时返回空串（调用方可不显示）。</summary>
        public static string ForStage(uint stage)
        {
            return ForStage(stage, CurrentLanguage);
        }

        public static string ForStage(uint stage, LanguageType lang)
        {
            List<MapLoadingConfig> rows = Rows();
            if (rows == null) return "";
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].stage == stage) return Pick(rows[i], lang);
            return "";
        }

        /// <summary>按语言取列。某语言为空时**回退简中**（避免"翻译还没来"变成空白读条）。</summary>
        public static string Pick(MapLoadingConfig row, LanguageType lang)
        {
            if (row == null) return "";
            string s;
            switch (lang)
            {
                case LanguageType.ChineseTraditional: s = row.descTW; break;
                case LanguageType.English: s = row.descEN; break;
                case LanguageType.Japanese: s = row.descJP; break;
                case LanguageType.Korean: s = row.descKR; break;
                default: s = row.descCN; break;
            }
            if (string.IsNullOrEmpty(s)) s = row.descCN;
            return s ?? "";
        }

        /// <summary>按 sort 升序列出全部阶段文案（读条界面可以按它轮播/逐条显示）。</summary>
        public static List<string> AllStages(LanguageType lang)
        {
            var result = new List<string>();
            List<MapLoadingConfig> rows = Rows();
            if (rows == null) return result;
            var copy = new List<MapLoadingConfig>(rows);
            copy.Sort((a, b) => a.sort.CompareTo(b.sort));
            for (int i = 0; i < copy.Count; i++) result.Add(Pick(copy[i], lang));
            return result;
        }

        private static List<MapLoadingConfig> Rows()
        {
            if (_loadTried) return _rows;
            _loadTried = true;
            DataList<MapLoadingConfig> list = ConfigLoader.Instance.LoadData<MapLoadingConfig>();
            if (list == null || list.datas == null || list.datas.Count == 0)
            {
                ChaosLog.Warn(LogChannel.Config, "[RunMap] 读条文案表为空或读不到：MapLoadingConfig");
                _rows = null;
                return null;
            }
            _rows = list.datas;
            return _rows;
        }
    }
}
