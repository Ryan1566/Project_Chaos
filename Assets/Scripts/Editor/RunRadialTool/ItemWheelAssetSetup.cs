#if UNITY_EDITOR
using System;
using System.Linq;
using Chaos.Gameplay.Items;
using LocalizationSystem;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>Explicit, idempotent authoring utility. Never opens/saves the user's scene.</summary>
public static class ItemWheelAssetSetup
{
    [MenuItem("Tools/道具/建立轮盘配置和设置行")]
    public static void Run()
    {
        const string catalogPath = "Assets/Resources/Items/ItemUseCatalog.asset";
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Items")) AssetDatabase.CreateFolder("Assets/Resources", "Items");
        var catalog = AssetDatabase.LoadAssetAtPath<ItemUseCatalog>(catalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<ItemUseCatalog>();
            catalog.profiles = new[] {
                new ItemUseProfile { itemId = 1, parentCategory = ItemParentCategory.Recovery, subCategory = "草药", description = "恢复当前角色35点生命；满生命或倒地时不能使用。", effect = ItemUseEffect.Heal, value = 35 },
                new ItemUseProfile { itemId = 5, parentCategory = ItemParentCategory.Recovery, subCategory = "增益食物", description = "30秒内生命上限增加20，不自动恢复生命；重复使用刷新持续时间。", effect = ItemUseEffect.MaximumHealth, value = 20, duration = 30 },
                new ItemUseProfile { itemId = 6, parentCategory = ItemParentCategory.Recovery, subCategory = "伤药", description = "恢复当前角色60点生命；满生命或倒地时不能使用。", effect = ItemUseEffect.Heal, value = 60 },
                new ItemUseProfile { itemId = 10, parentCategory = ItemParentCategory.Engineering, subCategory = "爆破器具", description = "对自身3格范围内的局内敌人造成20点伤害；范围内无敌人时不能使用。", effect = ItemUseEffect.AreaDamage, value = 20, radius = 3 },
                new ItemUseProfile { itemId = 11, parentCategory = ItemParentCategory.Important, subCategory = "信物", description = "永久归属当前存档的唯一信物。暂无主动效果。", effect = ItemUseEffect.None, consume = false }
            };
            catalog.additionalDefinitions = new[] {
                new ItemConfig { Index = 10, NameKey = "震响罐", Category = 5, RunCategory = 2, StackLimit = 99, Enable = true, BuyPrice = 10, SellPrice = 5 },
                new ItemConfig { Index = 11, NameKey = "行军信物", Category = 5, RunCategory = 1, IsUnique = true, StackLimit = 1, Enable = true }
            };
            AssetDatabase.CreateAsset(catalog, catalogPath);
        }
        var translations = AssetDatabase.LoadAssetAtPath<LocalizationData>("Assets/Data/Localization/Sub_LD/SettingLocalizationConfig.asset");
        var entries = new[] { new LocalizationEntry { key = SettingKeys.ItemWheelOptions[0], chineseSimplified = "点按弹出", chineseTraditional = "點按彈出", english = "Tap to toggle" },
            new LocalizationEntry { key = SettingKeys.ItemWheelOptions[1], chineseSimplified = "长按显示", chineseTraditional = "長按顯示", english = "Hold to show" } };
        foreach (var entry in entries) if (translations.GetEntry(entry.key) == null) translations.entries.Add(entry);
        EditorUtility.SetDirty(translations); AssetDatabase.SaveAssetIfDirty(translations);
        var merged = LocalizationSystem.Editor.LocalizationMerger.Merge(false);
        if (!merged.Success) throw new InvalidOperationException("本地化主表合并失败");
        const string path = "Assets/Resources/UIPanels/SettingPanel.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var rows = root.GetComponentsInChildren<SettingRow_Selector>(true);
            if (!rows.Any(r => r.settingId == SettingIds.ItemWheelMode))
            {
                var source = rows.Single(r => r.settingId == SettingIds.Language);
                var clone = UnityEngine.Object.Instantiate(source.gameObject, source.transform.parent);
                clone.name = "Row_ItemWheelMode";
                clone.GetComponent<SettingRow_Selector>().settingId = SettingIds.ItemWheelMode;
                var label = clone.transform.Find("Label");
                foreach (var local in label.GetComponents<LocalizedText>()) UnityEngine.Object.DestroyImmediate(local);
                label.GetComponent<TextMeshProUGUI>().text = "快捷轮盘唤出方式";
                var options = clone.transform.Find("Options");
                for (int i = options.childCount - 1; i >= 2; i--) UnityEngine.Object.DestroyImmediate(options.GetChild(i).gameObject);
                string[] names = { "点按弹出", "长按显示" };
                for (int i = 0; i < 2; i++)
                {
                    var option = options.GetChild(i);
                    option.GetComponent<TextMeshProUGUI>().text = names[i];
                    var local = option.GetComponent<LocalizedText>();
                    if (local != null) { local.localizationKey = SettingKeys.ItemWheelOptions[i]; local.autoUpdateOnStart = false; }
                    option.gameObject.SetActive(i == 0);
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssetIfDirty(catalog); InventoryService.Instance.Reload();
    }
}
#endif
