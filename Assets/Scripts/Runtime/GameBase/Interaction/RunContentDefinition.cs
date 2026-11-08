using System;
using LocalizationSystem;

namespace Chaos.Gameplay.Interaction
{
    public enum RunContentKind { Npc, Chest, Gatherable }

    /// <summary>Authored text only; runtime quantities and claim state never live here.</summary>
    [Serializable]
    public sealed class RunContentText
    {
        public string zhCN, enUS, zhTW, jaJP, koKR;

        public string Resolve()
        {
            var manager = LocalizationManager.GetInstance();
            LanguageType language = manager == null ? LanguageType.ChineseSimplified : manager.CurrentLanguage;
            string value;
            switch (language)
            {
                case LanguageType.English: value = enUS; break;
                case LanguageType.ChineseTraditional: value = zhTW; break;
                case LanguageType.Japanese: value = jaJP; break;
                case LanguageType.Korean: value = koKR; break;
                default: value = zhCN; break;
            }
            if (string.IsNullOrWhiteSpace(value)) value = zhCN;
            if (string.IsNullOrWhiteSpace(value)) value = enUS;
            return value ?? string.Empty;
        }
    }

    [Serializable]
    public sealed class RunContentReward
    {
        public uint itemId, minCount, maxCount;
        public RunContentText label;
    }

    [Serializable]
    public sealed class RunContentDefinition
    {
        public string id;
        public RunContentKind kind;
        public RunContentText name, body;
        public RunContentReward[] rewards;
        public RunNpcQuestDefinition quest;
    }

    /// <summary>医者可选委托；独立开关兼容Unity对内联空对象的序列化。</summary>
    [Serializable]
    public sealed class RunNpcQuestDefinition
    {
        public bool enabled;
        public string id, targetContentId;
        public RunContentText title, description;
        public int requiredCount = 1;
        public uint rewardItemId, rewardCount = 1;
    }

    /// <summary>Read-only view of one configured reward after its stable count has been snapshotted.</summary>
    public sealed class RunContentRewardState
    {
        public int Index { get; private set; }
        public uint ItemId { get; private set; }
        public uint Count { get; private set; }
        public string Label { get; private set; }
        public bool Claimed { get; private set; }

        internal RunContentRewardState(int index, uint itemId, uint count, string label)
        {
            Index = index; ItemId = itemId; Count = count; Label = label;
        }

        internal void MarkClaimed(bool value = true) { Claimed = value; }
    }
}
