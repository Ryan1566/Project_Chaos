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

        internal void MarkClaimed() { Claimed = true; }
    }
}
