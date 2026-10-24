using System;
using LocalizationSystem;

namespace Chaos.Gameplay.Interaction
{
    public static class RunInteractionText
    {
        public static string Get(string key, params object[] args)
        {
            var rows = ConfigLoader.Instance.LoadData<RunInteractionConfig>();
            var row = rows == null ? null : rows.datas.Find(r => r.key == key);
            if (row == null) throw new InvalidOperationException("RunInteraction missing text: " + key);
            var manager = LocalizationManager.GetInstance();
            var language = manager == null ? LanguageType.ChineseSimplified : manager.CurrentLanguage;
            string text = language == LanguageType.English ? row.enUS : language == LanguageType.ChineseTraditional ? row.zhTW
                : language == LanguageType.Japanese ? row.jaJP : language == LanguageType.Korean ? row.koKR : row.zhCN;
            return args == null || args.Length == 0 ? text : string.Format(text, args);
        }
    }
}
