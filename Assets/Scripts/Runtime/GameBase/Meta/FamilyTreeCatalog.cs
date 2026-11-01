using System;
using System.Collections.Generic;
using System.Linq;
using LocalizationSystem;
using UnityEngine;

/// <summary>配置与图结构的只读目录，验证完成后才允许解锁。</summary>
public sealed class FamilyTreeCatalog
{
    public readonly List<MetaSkillTreeTable_FamilyTreeConfig> Nodes;
    public readonly List<MetaSkillTreeTable_FamilyTreeBoardConfig> Boards;
    public readonly List<MetaSkillTreeTable_FamilyTreeEffectConfig> Links;
    public readonly Dictionary<uint, EffectTable_EffectConfig> Effects;
    public readonly Dictionary<uint, BuildingTable_BuildingConfig> Buildings;
    private readonly Dictionary<string, MetaSkillTreeTable_FamilyTreeTextConfig> _texts = new Dictionary<string, MetaSkillTreeTable_FamilyTreeTextConfig>();
    public FamilyTreeCatalog()
    {
        Nodes = Read<MetaSkillTreeTable_FamilyTreeConfig>(); Boards = Read<MetaSkillTreeTable_FamilyTreeBoardConfig>();
        Links = Read<MetaSkillTreeTable_FamilyTreeEffectConfig>(); Effects = Unique(Read<EffectTable_EffectConfig>(), x => x.Index, "效果");
        foreach (var text in Read<MetaSkillTreeTable_FamilyTreeTextConfig>())
            if (text == null || string.IsNullOrEmpty(text.Key) || _texts.ContainsKey(text.Key)) throw new InvalidOperationException("家族树文案缺少key或重复");
            else _texts.Add(text.Key, text);
        Buildings = new Dictionary<uint, BuildingTable_BuildingConfig>();
        foreach (var building in Read<BuildingTable_BuildingConfig>())
            if (building != null && building.Enable && building.Level == 1) Buildings.Add(building.BuildingId, building);
        Validate();
    }
    private static List<T> Read<T>() { return ConfigLoader.Instance.LoadData<T>().datas; }
    private static Dictionary<uint,T> Unique<T>(IEnumerable<T> values, Func<T,uint> key, string label) where T : class
    {
        var result = new Dictionary<uint,T>();
        foreach (var value in values)
            if (value == null || key(value) == 0 || result.ContainsKey(key(value))) throw new InvalidOperationException("家族树" + label + "ID为空或重复");
            else result.Add(key(value), value);
        return result;
    }
    public string Text(string key)
    {
        var manager = LocalizationManager.GetInstance();
        if (!string.IsNullOrEmpty(key) && manager != null && manager.HasKey(key)) return manager.GetText(key);
        if (_texts.TryGetValue(key ?? "", out var text))
            return manager == null || manager.CurrentLanguage == LanguageType.ChineseSimplified || string.IsNullOrEmpty(text.English) ? text.ChineseSimplified : text.English;
        return "未配置名称";
    }
    public MetaSkillTreeTable_FamilyTreeConfig Node(uint id) { return Nodes.Find(x => x.Index == id); }
    public IEnumerable<MetaSkillTreeTable_FamilyTreeEffectConfig> EffectsOf(uint id) { return Links.Where(x => x.Enable && x.NodeId == id); }
    public void Validate()
    {
        var nodes = Unique(Nodes, x => x.Index, "节点"); var boards = Unique(Boards, x => x.Index, "板块"); Unique(Links, x => x.Index, "映射");
        foreach (var node in Nodes.Where(x => x.Enable))
        {
            if (!boards.TryGetValue(node.Board, out var board) || !board.Enable) throw new InvalidOperationException("节点所属板块不存在：" + node.Index);
            if (node.CostType < 1 || node.CostType > 4 || !Finite(node.LayoutX) || !Finite(node.LayoutY)) throw new InvalidOperationException("节点费用或坐标无效：" + node.Index);
            if (!_texts.ContainsKey(node.NameKey ?? "") || !_texts.ContainsKey(node.DescriptionKey ?? "") || !_texts.ContainsKey(node.BranchKey ?? "")) throw new InvalidOperationException("节点文案缺失：" + node.Index);
            var visited = new HashSet<uint>(); var cursor = node;
            while (cursor != null)
            {
                if (!visited.Add(cursor.Index)) throw new InvalidOperationException("家族树前置形成循环：" + node.Index);
                if (cursor.PrereqNodeId == 0) break;
                if (!nodes.TryGetValue(cursor.PrereqNodeId, out var parent) || !parent.Enable || parent.Board != node.Board) throw new InvalidOperationException("家族树前置缺失、禁用或跨板块：" + cursor.Index);
                cursor = parent;
            }
            if (!EffectsOf(node.Index).Any()) throw new InvalidOperationException("节点没有启用效果：" + node.Index);
            foreach (var other in Nodes.Where(x => x.Enable && x.Board == node.Board && x.Index != node.Index))
                if (Mathf.Abs(node.LayoutX-other.LayoutX) < 172 && Mathf.Abs(node.LayoutY-other.LayoutY) < 148) throw new InvalidOperationException("节点布局重叠：" + node.Index + "/" + other.Index);
        }
        foreach (var link in Links.Where(x => x.Enable))
        {
            if (!nodes.ContainsKey(link.NodeId) || !Effects.TryGetValue(link.EffectId, out var effect) || !effect.Enable || !Finite(link.Weight) || link.Weight <= 0) throw new InvalidOperationException("家族树效果映射无效：" + link.Index);
            bool supported = effect.EffectType == 2 || effect.EffectType == 3 || effect.EffectType == 4 && effect.Target >= 1 && effect.Target <= 4
                || effect.EffectType == 5 && effect.Value >= 1 || (effect.EffectType == 6 || effect.EffectType == 7) && Buildings.ContainsKey(effect.Target);
            if (!supported || !Finite(effect.Value)) throw new InvalidOperationException("效果尚无实际消费入口：" + effect.Index);
        }
    }
    private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
}
