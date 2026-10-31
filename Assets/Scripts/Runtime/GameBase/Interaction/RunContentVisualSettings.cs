using Spine.Unity;
using UnityEngine;

namespace Chaos.Gameplay.Interaction
{
    /// <summary>Replace placeholder visuals here without changing content identity or reward rules.</summary>
    [CreateAssetMenu(menuName = "Chaos/Run Content Visuals")]
    public sealed class RunContentVisualSettings : ScriptableObject
    {
        public SkeletonDataAsset npcSkeleton;
        public string npcSkin;
        public string npcIdle = "Idle_Normal";
        public float npcHeight = 1.8f;
        public Sprite npcPortrait, chestClosed, chestOpened, herbs, ore, depleted;
        public RunContentText closeText, talkText, chooseText, claimText, harvestText, claimedText, receivedText, npcHint, lootHint, completedText;
        public static RunContentVisualSettings Load()
        {
            var value = Resources.Load<RunContentVisualSettings>("Interaction/RunContentVisualSettings");
            if (value == null || value.npcSkeleton == null || value.npcHeight <= 0 || float.IsNaN(value.npcHeight) || float.IsInfinity(value.npcHeight))
                throw new System.InvalidOperationException("局内交互表现配置缺失或非法");
            return value;
        }
    }
}
