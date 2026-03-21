using System;
using System.Collections.Generic;
using UnityEngine;

namespace LocalizationSystem
{
    /// <summary>
    /// 支持的语言类型
    /// </summary>
    public enum LanguageType
    {
        ChineseSimplified,//简体中文
        //ChineseTraditional,//繁体中文
        English,//英语
        //Japanese,//日语
        //Korean,//韩语
        //French,//法语
        //German,//德语
        //Spanish,//西班牙语
        //Russian,//俄语
        //Portuguese,//葡萄牙语
        //Italian,//意大利语
        //Arabic,//阿拉伯语
        //Thai,//泰语
        //Vietnamese,//越南语
        //Turkish,//土耳其语
        //Polish,//波兰语
        //Dutch,//荷兰语
        //Indonesian,//印尼语
    }

    /// <summary>
    /// 单条本地化数据
    /// </summary>
    [Serializable]
    public class LocalizationEntry
    {
        public string key;//唯一标识键
        [TextArea(2, 5)]
        public string description;//描述/备注（用于说明用途）

        //各语言文本
        public string chineseSimplified;
        //public string chineseTraditional;
        public string english;
        //public string japanese;
        //public string korean;
        //public string french;
        //public string german;
        //public string spanish;
        //public string russian;
        //public string portuguese;
        //public string italian;
        //public string arabic;
        //public string thai;
        //public string vietnamese;
        //public string turkish;
        //public string polish;
        //public string dutch;
        //public string indonesian;

        /// <summary>
        /// 获取指定语言的文本
        /// </summary>
        public string GetText(LanguageType language)
        {
            return language switch
            {
                LanguageType.ChineseSimplified => chineseSimplified,
                //LanguageType.ChineseTraditional => chineseTraditional,
                LanguageType.English => english,
                //LanguageType.Japanese => japanese,
                //LanguageType.Korean => korean,
                //LanguageType.French => french,
                //LanguageType.German => german,
                //LanguageType.Spanish => spanish,
                //LanguageType.Russian => russian,
                //LanguageType.Portuguese => portuguese,
                //LanguageType.Italian => italian,
                //LanguageType.Arabic => arabic,
                //LanguageType.Thai => thai,
                //LanguageType.Vietnamese => vietnamese,
                //LanguageType.Turkish => turkish,
                //LanguageType.Polish => polish,
                //LanguageType.Dutch => dutch,
                //LanguageType.Indonesian => indonesian,
                _ => chineseSimplified
            };
        }

        /// <summary>
        /// 设置指定语言的文本
        /// </summary>
        public void SetText(LanguageType language, string text)
        {
            switch (language)
            {
                case LanguageType.ChineseSimplified:
                    chineseSimplified = text;
                    break;
                //case LanguageType.ChineseTraditional:
                //    chineseTraditional = text;
                //    break;
                case LanguageType.English:
                    english = text;
                    break;
                //case LanguageType.Japanese:
                //    japanese = text;
                //    break;
                //case LanguageType.Korean:
                //    korean = text;
                //    break;
                //case LanguageType.French:
                //    french = text;
                //    break;
                //case LanguageType.German:
                //    german = text;
                //    break;
                //case LanguageType.Spanish:
                //    spanish = text;
                //    break;
                //case LanguageType.Russian:
                //    russian = text;
                //    break;
                //case LanguageType.Portuguese:
                //    portuguese = text;
                //    break;
                //case LanguageType.Italian:
                //    italian = text;
                //    break;
                //case LanguageType.Arabic:
                //    arabic = text;
                //    break;
                //case LanguageType.Thai:
                //    thai = text;
                //    break;
                //case LanguageType.Vietnamese:
                //    vietnamese = text;
                //    break;
                //case LanguageType.Turkish:
                //    turkish = text;
                //    break;
                //case LanguageType.Polish:
                //    polish = text;
                //    break;
                //case LanguageType.Dutch:
                //    dutch = text;
                //    break;
                //case LanguageType.Indonesian:
                //    indonesian = text;
                //    break;
            }
        }
    }

    /// <summary>
    /// 本地化数据配置（可创建为ScriptableObject）
    /// </summary>
    [CreateAssetMenu(fileName = "LocalizationConfig", menuName = "Localization/Localization Config")]
    public class LocalizationData : ScriptableObject
    {
        public string configName;//配置名称
        [TextArea(2, 3)]
        public string description;//配置描述

        public List<LocalizationEntry> entries = new List<LocalizationEntry>();//本地化条目列表

        /// <summary>
        /// 根据Key获取条目
        /// </summary>
        public LocalizationEntry GetEntry(string key)
        {
            return entries.Find(e => e.key == key);
        }

        /// <summary>
        /// 检查Key是否已存在
        /// </summary>
        public bool ContainsKey(string key)
        {
            return entries.Exists(e => e.key == key);
        }

        /// <summary>
        /// 添加新条目
        /// </summary>
        public void AddEntry(LocalizationEntry entry)
        {
            if (!ContainsKey(entry.key))
            {
                entries.Add(entry);
            }
        }

        /// <summary>
        /// 移除条目
        /// </summary>
        public void RemoveEntry(string key)
        {
            entries.RemoveAll(e => e.key == key);
        }

        /// <summary>
        /// 获取所有Keys
        /// </summary>
        public List<string> GetAllKeys()
        {
            List<string> keys = new List<string>();
            foreach (var entry in entries)
            {
                keys.Add(entry.key);
            }
            return keys;
        }
    }
}
