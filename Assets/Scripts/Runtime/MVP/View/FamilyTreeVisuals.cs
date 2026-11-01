using UnityEngine;

/// <summary>技能树共用表现素材，可替换美术而不改规则。</summary>
public sealed class FamilyTreeVisuals : ScriptableObject
{
    public Sprite SectionNormal, SectionSelected, NodeLocked, NodeReady, NodeUnlocked, NodeMilestone, DetailBackground;
}
