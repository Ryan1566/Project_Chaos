using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using ChaosDebug;

namespace LocalizationSystem
{
    /// <summary>
    /// 本地化管理器 - 负责管理和切换语言
    /// </summary>
    public class LocalizationManager : SingletonMono<LocalizationManager>
    {
        [Header("配置")]
        public LocalizationData localizationData;//本地化数据配置
        public LanguageType defaultLanguage = LanguageType.ChineseSimplified;//默认语言

        [Header("事件")]
        public UnityEvent<LanguageType> OnLanguageChanged;//语言切换事件

        //当前语言
        private LanguageType currentLanguage;
        public LanguageType CurrentLanguage => currentLanguage;

        //缓存的字典，用于快速查找
        private Dictionary<string, string> textCache = new Dictionary<string, string>();
        private bool isInitialized = false;

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private void Initialize()
        {
            if (isInitialized) return;

            //加载保存的语言设置
            string savedLanguage = PlayerPrefs.GetString("Localization_CurrentLanguage", defaultLanguage.ToString());
            if (Enum.TryParse<LanguageType>(savedLanguage, out var language))
            {
                currentLanguage = language;
            }
            else
            {
                currentLanguage = defaultLanguage;
            }

            //构建缓存
            BuildCache();

            isInitialized = true;
        }

        /// <summary>
        /// 构建文本缓存
        /// </summary>
        private void BuildCache()
        {
            textCache.Clear();

            if (localizationData == null)
            {
                ChaosLog.Warn(LogChannel.Localization, "LocalizationData is null!");
                return;
            }

            foreach (var entry in localizationData.entries)
            {
                if (!string.IsNullOrEmpty(entry.key))
                {
                    string text = entry.GetText(currentLanguage);
                    //如果当前语言没有文本，使用英语作为后备
                    if (string.IsNullOrEmpty(text))
                    {
                        text = entry.english;
                    }
                    //如果英语也没有，使用简体中文
                    if (string.IsNullOrEmpty(text))
                    {
                        text = entry.chineseSimplified;
                    }
                    textCache[entry.key] = text ?? entry.key;
                }
            }
        }

        /// <summary>
        /// 切换语言
        /// </summary>
        public void ChangeLanguage(LanguageType language)
        {
            if (currentLanguage == language) return;

            currentLanguage = language;

            //保存语言设置
            PlayerPrefs.SetString("Localization_CurrentLanguage", currentLanguage.ToString());
            PlayerPrefs.Save();

            //重建缓存
            BuildCache();

            //触发事件
            OnLanguageChanged?.Invoke(currentLanguage);

            //更新所有本地化文本
            UpdateAllLocalizedTexts();

            ChaosLog.Info(LogChannel.Localization, $"语言已切换为: {currentLanguage}");
        }

        /// <summary>
        /// 获取本地化文本
        /// </summary>
        public string GetText(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            if (textCache.TryGetValue(key, out string text))
            {
                return text;
            }

            //缓存中没有，尝试从数据中获取
            if (localizationData != null)
            {
                var entry = localizationData.GetEntry(key);
                if (entry != null)
                {
                    text = entry.GetText(currentLanguage);
                    if (string.IsNullOrEmpty(text)) text = entry.english;
                    if (string.IsNullOrEmpty(text)) text = entry.chineseSimplified;
                    textCache[key] = text ?? key;
                    return textCache[key];
                }
            }

            //返回Key作为后备
            return key;
        }

        /// <summary>
        /// 获取本地化文本（带格式化参数）
        /// </summary>
        public string GetText(string key, params object[] args)
        {
            string text = GetText(key);
            try
            {
                return string.Format(text, args);
            }
            catch (FormatException)
            {
                ChaosLog.Warn(LogChannel.Localization, $"Format error for key: {key}, text: {text}");
                return text;
            }
        }

        /// <summary>
        /// 检查是否存在指定Key
        /// </summary>
        public bool HasKey(string key)
        {
            if (localizationData == null) return false;
            return localizationData.ContainsKey(key);
        }

        /// <summary>
        /// 设置本地化数据
        /// </summary>
        public void SetLocalizationData(LocalizationData data)
        {
            localizationData = data;
            BuildCache();
            UpdateAllLocalizedTexts();
        }

        /// <summary>
        /// 更新所有本地化文本组件
        /// </summary>
        private void UpdateAllLocalizedTexts()
        {
            //查找场景中所有LocalizedText组件并更新
            var localizedTexts = FindObjectsOfType<LocalizedText>();
            foreach (var text in localizedTexts)
            {
                text.UpdateText();
            }
        }

        /// <summary>
        /// 获取语言名称（用于显示）
        /// </summary>
        public static string GetLanguageDisplayName(LanguageType language)
        {
            return language switch
            {
                LanguageType.ChineseSimplified => "简体中文",
                //LanguageType.ChineseTraditional => "繁體中文",
                LanguageType.English => "English",
                //LanguageType.Japanese => "日本語",
                //LanguageType.Korean => "???",
                //LanguageType.French => "Fran?ais",
                //LanguageType.German => "Deutsch",
                //LanguageType.Spanish => "Espa?ol",
                //LanguageType.Russian => "Русский",
                //LanguageType.Portuguese => "Português",
                //LanguageType.Italian => "Italiano",
                //LanguageType.Arabic => "???????",
                //LanguageType.Thai => "???",
                //LanguageType.Vietnamese => "Ti?ng Vi?t",
                //LanguageType.Turkish => "Türk?e",
                //LanguageType.Polish => "Polski",
                //LanguageType.Dutch => "Nederlands",
                //LanguageType.Indonesian => "Bahasa Indonesia",
                _ => language.ToString()
            };
        }

        /// <summary>
        /// 获取所有支持的语言
        /// </summary>
        public static LanguageType[] GetAllLanguages()
        {
            return (LanguageType[])Enum.GetValues(typeof(LanguageType));
        }
    }
}
