using System;
using UnityEngine;

namespace Chaos.Gameplay.Items
{
    public enum ItemParentCategory { Important = 1, Recovery = 2, Engineering = 3, Material = 4, Misc = 5 }
    public enum ItemUseEffect { None, Heal, MaximumHealth, AreaDamage }
    [Serializable]
    public sealed class ItemUseProfile
    {
        public uint itemId;
        public ItemParentCategory parentCategory;
        public string subCategory, description;
        public ItemUseEffect effect;
        [Min(0)] public int value;
        [Min(0)] public float radius = 3, duration = 30, cooldown = 1, animationSeconds = .5f;
        public string animationName;
        public bool consume = true;
    }
    /// <summary>Use/animation extension to Excel's ItemConfig. Additional prototype definitions never override Excel IDs.</summary>
    [CreateAssetMenu(menuName = "Chaos/道具使用配置", fileName = "ItemUseCatalog")]
    public sealed class ItemUseCatalog : ScriptableObject
    {
        public ItemUseProfile[] profiles = Array.Empty<ItemUseProfile>();
        public ItemConfig[] additionalDefinitions = Array.Empty<ItemConfig>();
        public const string ResourcePath = "Items/ItemUseCatalog";
        public static ItemUseCatalog Load() => Resources.Load<ItemUseCatalog>(ResourcePath);
        public ItemUseProfile Find(uint id)
        { foreach (var profile in profiles) if (profile != null && profile.itemId == id) return profile; return null; }
    }
}
