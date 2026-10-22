using System;
using TMPro;

namespace LocalizationSystem
{
    /// <summary>Runtime UI text with a readable fallback while a localization key is being authored.</summary>
    public static class GameplayText
    {
        public static string Text(string key, string fallback, params object[] args)
        {
            LocalizationManager manager = LocalizationManager.GetInstance();
            string template = manager != null && manager.HasKey(key) ? manager.GetText(key) : fallback;
            if (args == null || args.Length == 0) return template ?? string.Empty;
            try { return string.Format(template ?? string.Empty, args); }
            catch (FormatException) { return string.Format(fallback ?? string.Empty, args); }
        }

        public static string ItemName(uint itemId)
        {
            return ItemName(InventoryService.Instance.GetDefinition(itemId), itemId);
        }

        public static string ItemName(ItemConfig definition, uint missingId = 0)
        {
            if (definition == null) return Text("ui_item_unknown", "道具 #{0}", missingId);
            string key = definition.NameKey;
            if (string.IsNullOrEmpty(key)) return Text("ui_item_unknown", "道具 #{0}", definition.Index);
            // Legacy rows may store literal names. New rows resolve through their configured NameKey.
            return Text(key, key);
        }

        public static void Bind(TextMeshProUGUI label, string key, string fallback, params string[] args)
        {
            if (label == null) return;
            label.text = Text(key, fallback, args);
            LocalizationManager manager = LocalizationManager.GetInstance();
            if (manager == null || !manager.HasKey(key)) return;
            LocalizedText localized = label.GetComponent<LocalizedText>();
            if (localized == null) localized = label.gameObject.AddComponent<LocalizedText>();
            localized.autoUpdateOnStart = false;
            localized.SetKey(key, args);
        }
    }
}
