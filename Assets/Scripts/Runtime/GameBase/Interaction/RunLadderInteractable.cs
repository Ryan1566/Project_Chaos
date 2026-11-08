using System;
using RunMap;
using UnityEngine;

namespace Chaos.Gameplay.Interaction
{
    /// <summary>
    /// 手动模式的梯子交互点（由 Root 在带 trigger 的梯子实例上挂载并 ConfigureLadder）。
    ///
    /// 交互点不取梯子中点：高梯的中点会超出交互距离，玩家永远按不到 E。
    /// 未攀爬时返回离 actor 脚底最近的入口端点；正在爬这根梯子时返回 actor 脚底，
    /// 这样高梯中段也能稳定选中“离开梯子”。自动模式不注册为可交互，不抢 NPC/出口。
    /// </summary>
    public sealed class RunLadderInteractable : RunInteractable
    {
        private MapLadder ladder;
        private NaturalMapPlan plan;
        private GameObject selectionActor;

        public MapLadder Ladder { get { return ladder; } }

        public override string DisplayName { get { return "梯子"; } }

        public override string ActionLabel
        {
            get
            {
                LadderClimber climber = SelectionClimber;
                return climber != null && climber.IsAttachedTo(ladder) ? "离开梯子" : "攀爬";
            }
        }

        public override Vector2 InteractionPoint
        {
            get
            {
                if (ladder == null) return transform.position;

                GameObject actor = selectionActor != null ? selectionActor : PlayerSpawner.CurrentPlayer;
                Vector2 feet = actor == null ? (Vector2)transform.position : FeetOf(actor);
                LadderClimber climber = SelectionClimber;
                if (climber != null && climber.IsAttachedTo(ladder)) return feet;
                return NearestEntrance(feet);
            }
        }

        /// <summary>Root 创建梯子实例后调用；stableId 统一加前缀，避免和地图内容 ID 撞车。</summary>
        public void ConfigureLadder(MapLadder value, NaturalMapPlan owner)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (owner == null || owner.Ladders == null || !Contains(owner, value))
                throw new ArgumentException("Ladder must belong to the supplied natural map plan", nameof(value));

            ladder = value;
            plan = owner;

            string id = string.IsNullOrEmpty(value.StableId)
                ? "ladder:" + value.RegionId + ":" + value.Bottom.x + ":" + value.Bottom.y + ":" + value.Top.x + ":" + value.Top.y
                : "ladder:" + value.StableId;
            Configure(id);
        }

        public override bool CanInteract(GameObject actor, out string reason)
        {
            reason = string.Empty;
            selectionActor = actor;

            if (ladder == null || plan == null)
            {
                reason = "梯子尚未配置";
                return false;
            }
            if (actor == null || PlayerSpawner.CurrentPlayer != actor)
            {
                reason = RunInteractionText.Get("Inactive");
                return false;
            }
            if (!RunInteractionService.CanControl(actor))
            {
                reason = RunInteractionText.Get("Inactive");
                return false;
            }
            if (!NaturalMapRuntime.IsReady || !ReferenceEquals(NaturalMapRuntime.Current, plan) || !Contains(plan, ladder))
            {
                reason = "地图已切换";
                return false;
            }

            LadderClimber climber = actor.GetComponent<LadderClimber>();
            if (climber == null || !climber.enabled)
            {
                reason = "角色攀梯组件不可用";
                return false;
            }

            // 攀爬中只允许当前梯子优先退出；其他交互（NPC/出口）仍按原排序。
            if (climber.IsClimbing)
            {
                if (!climber.IsAttachedTo(ladder))
                {
                    reason = "正在攀爬其他梯子";
                    return false;
                }
                if (LadderEntryMode != ManualMode)
                {
                    reason = "自动爬梯模式无需交互";
                    return false;
                }
                return true;
            }

            if (LadderEntryMode != ManualMode)
            {
                reason = "自动爬梯模式无需交互";
                return false;
            }

            Vector2 entrance = NearestEntrance(FeetOf(actor));
            if ((entrance - FeetOf(actor)).sqrMagnitude > LadderClimber.AttachRange * LadderClimber.AttachRange)
            {
                reason = RunInteractionText.Get("OutOfRange");
                return false;
            }
            return true;
        }

        protected override bool Perform(GameObject actor, out string result)
        {
            LadderClimber climber = actor == null ? null : actor.GetComponent<LadderClimber>();
            if (climber == null)
            {
                result = "角色缺少攀梯组件";
                return false;
            }

            // 同一个 RunLadderInteractable 负责进入/退出；E 不会在同一帧再被别的交互消费。
            if (climber.IsAttachedTo(ladder))
            {
                climber.Detach(false);
                result = "已离开梯子";
                return true;
            }
            if (!climber.TryAttach(ladder))
            {
                result = "无法攀爬该梯子";
                return false;
            }
            result = "开始攀爬";
            return true;
        }

        internal bool Matches(MapLadder value)
        {
            return value != null && ReferenceEquals(value, ladder);
        }

        private LadderClimber SelectionClimber
        {
            get
            {
                GameObject actor = selectionActor != null ? selectionActor : PlayerSpawner.CurrentPlayer;
                return actor == null ? null : actor.GetComponent<LadderClimber>();
            }
        }

        private const int ManualMode = 1;

        /// <summary>0 = 自动上下，1 = 手动 E；旧配置/未初始化时按 0 处理。</summary>
        private static int LadderEntryMode
        {
            get
            {
                SettingsManager manager = SettingsManager.Instance;
                if (manager == null || manager.Applied == null) return 0;
                return manager.Applied.ladderEntryMode == ManualMode ? ManualMode : 0;
            }
        }

        private static bool Contains(NaturalMapPlan owner, MapLadder value)
        {
            for (int i = 0; i < owner.Ladders.Count; i++)
                if (ReferenceEquals(owner.Ladders[i], value)) return true;
            return false;
        }

        private Vector2 NearestEntrance(Vector2 feet)
        {
            Vector2 bottom = FootOf(ladder.Bottom);
            Vector2 top = FootOf(ladder.Top);
            return (feet - bottom).sqrMagnitude <= (feet - top).sqrMagnitude ? bottom : top;
        }

        private static Vector2 FootOf(Vector2Int cell)
        {
            return new Vector2(cell.x + 0.5f, cell.y + 0.03f);
        }

        private static Vector2 FeetOf(GameObject actor)
        {
            Collider2D collider = actor.GetComponent<Collider2D>();
            if (collider == null) return actor.transform.position;
            Bounds bounds = collider.bounds;
            return new Vector2(bounds.center.x, bounds.min.y);
        }
    }
}
