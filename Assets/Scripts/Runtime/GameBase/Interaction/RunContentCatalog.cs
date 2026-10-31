using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chaos.Gameplay.Interaction
{
    [CreateAssetMenu(menuName = "Chaos/Run Content Catalog")]
    public sealed class RunContentCatalog : ScriptableObject
    {
        public RunContentDefinition[] definitions;
        public int gatherableCount;

        public static RunContentCatalog Load()
        {
            var catalog = Resources.Load<RunContentCatalog>("Interaction/RunContentCatalog");
            if (catalog == null)
                throw new InvalidOperationException("缺少 Resources/Interaction/RunContentCatalog 配置资产");
            catalog.Validate();
            return catalog;
        }

        public RunContentDefinition Get(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            Validate();
            for (int i = 0; i < definitions.Length; i++)
                if (definitions[i].id == id) return definitions[i];
            return null;
        }

        public void Validate()
        {
            if (definitions == null || definitions.Length == 0)
                throw new InvalidOperationException("局内交互目录没有任何定义");
            if (gatherableCount < 0)
                throw new InvalidOperationException("局内交互采集预算不能为负数");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < definitions.Length; i++)
            {
                var definition = definitions[i];
                ValidateDefinition(definition);
                if (!ids.Add(definition.id))
                    throw new InvalidOperationException("局内交互定义 ID 重复：" + definition.id);
            }
        }

        public static void ValidateDefinition(RunContentDefinition definition)
        {
            if (definition == null) throw new InvalidOperationException("局内交互定义为空");
            if (string.IsNullOrWhiteSpace(definition.id))
                throw new InvalidOperationException("局内交互定义缺少 ID");
            if (definition.kind != RunContentKind.Npc && definition.kind != RunContentKind.Chest
                && definition.kind != RunContentKind.Gatherable)
                throw new InvalidOperationException("局内交互定义类型非法：" + definition.id);
            ValidateText(definition.name, definition.id, "名称");
            ValidateText(definition.body, definition.id, "正文");
            if (definition.rewards == null || definition.rewards.Length == 0)
                throw new InvalidOperationException("局内交互定义没有奖励：" + definition.id);
            if (definition.kind == RunContentKind.Gatherable && definition.rewards.Length != 1)
                throw new InvalidOperationException("采集内容必须且只能配置一项奖励：" + definition.id);
            for (int i = 0; i < definition.rewards.Length; i++)
            {
                var reward = definition.rewards[i];
                if (reward == null)
                    throw new InvalidOperationException("局内交互奖励为空：" + definition.id + "[" + i + "]");
                if (reward.minCount == 0 || reward.maxCount < reward.minCount || reward.maxCount > int.MaxValue)
                    throw new InvalidOperationException("局内交互奖励数量非法：" + definition.id + "[" + i + "]");
                var item = InventoryService.Instance.GetDefinition(reward.itemId);
                if (item == null || !item.Enable || item.RunCategory > 3)
                    throw new InvalidOperationException("局内交互奖励物品未知或不可发放：" + definition.id + "[" + i + "] item=" + reward.itemId);
                uint limit = item.RunCategory == 1 || item.IsUnique ? 1u : Math.Max(1u, item.StackLimit);
                if (reward.maxCount > limit)
                    throw new InvalidOperationException("局内交互奖励超过该物品的持有上限：" + definition.id + "[" + i + "]");
            }
            ValidateKnownKind(definition);
        }

        static void ValidateText(RunContentText text, string id, string field)
        {
            if (text == null || (string.IsNullOrWhiteSpace(text.zhCN) && string.IsNullOrWhiteSpace(text.enUS)))
                throw new InvalidOperationException("局内交互" + field + "缺少可用本地化文本：" + id);
        }

        static void ValidateKnownKind(RunContentDefinition definition)
        {
            RunContentKind expected;
            switch (definition.id)
            {
                case "traveller": expected = RunContentKind.Npc; break;
                case "chest": expected = RunContentKind.Chest; break;
                case "herbs":
                case "ore": expected = RunContentKind.Gatherable; break;
                default: return;
            }
            if (definition.kind != expected)
                throw new InvalidOperationException("局内交互定义类型与 ID 不符：" + definition.id);
        }
    }
}
