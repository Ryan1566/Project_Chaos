using LocalizationSystem;
using UnityEngine;

namespace Chaos.Gameplay.Items
{
    /// <summary>All five item families share this contract. Quantity always comes from the bag, never from a shared definition.</summary>
    public abstract class ItemBase
    {
        public ItemConfig Definition { get; }
        public ItemUseProfile Profile { get; }
        public uint Id => Definition.Index;
        public string Name => GameplayText.ItemName(Definition);
        public string IconKey => Definition.IconKey;
        public Sprite Icon => string.IsNullOrEmpty(IconKey) ? null : Resources.Load<Sprite>(IconKey);
        public string Description => Profile?.description ?? "此道具暂无局内主动效果。";
        public string SubCategory => Profile?.subCategory ?? "未细分";
        public ItemParentCategory ParentCategory => Definition.RunCategory == 1 ? ItemParentCategory.Important
            : Definition.RunCategory == 2 ? ItemParentCategory.Engineering : Definition.RunCategory == 3 ? ItemParentCategory.Recovery
            : Profile != null && Profile.parentCategory != 0 ? Profile.parentCategory
            : Definition.Category == 2 ? ItemParentCategory.Material : ItemParentCategory.Misc;
        public uint HeldQuantity => RunInventory.GetCount(Id);
        public bool IsUnique => ParentCategory == ItemParentCategory.Important || Definition.IsUnique;
        public bool TryUse(RunBagSection section, int index, out string reason) => RunItemUseService.TryUse(section, index, Id, out reason);
        public bool Consumes => ParentCategory != ItemParentCategory.Important && Profile != null && Profile.consume;
        protected ItemBase(ItemConfig definition, ItemUseProfile profile) { Definition = definition; Profile = profile; }
        public abstract bool CanUse(PlayerHealth actor, out string reason);
        internal abstract void Apply(PlayerHealth actor);
        public virtual void PlayUseAnimation(PlayerHealth actor)
        {
            var animation = actor.GetComponent<ItemUseAnimation>();
            if (animation == null) animation = actor.gameObject.AddComponent<ItemUseAnimation>();
            animation.Play(this);
        }
        public static ItemBase Create(uint id)
        {
            var def = InventoryService.Instance.GetDefinition(id);
            if (def == null || !def.Enable) return null;
            var profile = ItemUseCatalog.Load()?.Find(id);
            return new ConfiguredItem(def, profile);
        }
        sealed class ConfiguredItem : ItemBase
        {
            public ConfiguredItem(ItemConfig d, ItemUseProfile p) : base(d, p) { }
            public override bool CanUse(PlayerHealth actor, out string reason)
            {
                reason = null;
                if (actor == null || actor.IsDowned) reason = "当前角色无法使用道具";
                else if (Profile == null || Profile.effect == ItemUseEffect.None) reason = "此道具没有主动使用效果";
                else if (!System.Enum.IsDefined(typeof(ItemUseEffect), Profile.effect) || Profile.value <= 0 || !Finite(Profile.cooldown) || !Finite(Profile.animationSeconds)
                    || (Profile.effect == ItemUseEffect.MaximumHealth && (!Finite(Profile.duration) || Profile.duration <= 0))
                    || (Profile.effect == ItemUseEffect.AreaDamage && (!Finite(Profile.radius) || Profile.radius <= 0))) reason = "道具效果配置无效";
                else if (Profile.effect == ItemUseEffect.Heal && actor.Hp >= actor.maxHp) reason = "生命已满";
                else if (Profile.effect == ItemUseEffect.AreaDamage && !RunItemUseService.HasEnemyInRange(actor.transform.position, Profile.radius)) reason = "附近没有可命中的敌人";
                return reason == null;
            }
            static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v) && v >= 0;
            internal override void Apply(PlayerHealth actor)
            {
                switch (Profile.effect)
                {
                    case ItemUseEffect.Heal: actor.RestoreHealth(Profile.value); break;
                    case ItemUseEffect.MaximumHealth: RunItemUseService.BoostHealth(actor, Profile.value, Profile.duration); break;
                    case ItemUseEffect.AreaDamage: RunItemUseService.DamageArea(actor.transform.position, Profile.radius, Profile.value); break;
                }
            }
        }
    }
}
