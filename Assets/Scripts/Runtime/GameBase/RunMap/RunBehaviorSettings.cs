using System;
using UnityEngine;

namespace RunMap
{
    /// <summary>Validated, per-actor snapshot of the authored behavior row.</summary>
    [Serializable]
    public sealed class RunBehaviorSettings
    {
        public uint Id;
        public string role;
        public float moveSpeed, patrolSpeed, acceleration, jumpForce, repathInterval, waypointTolerance;
        public float stuckSeconds, recoverySeconds, detectRange, loseRange, memorySeconds;
        public int searchRadius, maxExpandedNodes;
        public float attackRange, attackWindup, attackRecovery, attackCooldown, attackHeight;
        public float patrolWait, returnTolerance, leashPadding, interactionRange;

        public static RunBehaviorSettings Load(string role)
        {
            var rows = ConfigLoader.Instance.LoadData<RunBehaviorConfig>();
            var row = rows == null ? null : rows.datas.Find(r => r.role == role);
            if (row == null) throw new InvalidOperationException("RunBehavior missing role: " + role);
            var result = JsonUtility.FromJson<RunBehaviorSettings>(JsonUtility.ToJson(row));
            result.Validate(); return result;
        }
        public void Validate()
        {
            float[] positive = { moveSpeed, patrolSpeed, acceleration, jumpForce, repathInterval, waypointTolerance,
                stuckSeconds, recoverySeconds, detectRange, loseRange, memorySeconds, attackRange, attackWindup,
                attackRecovery, attackCooldown, attackHeight, patrolWait, returnTolerance, interactionRange };
            foreach (float value in positive)
                if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0)
                    throw new InvalidOperationException("RunBehavior requires finite positive values: " + role);
            if (searchRadius < 1 || searchRadius > 32 || maxExpandedNodes < 32 || maxExpandedNodes > 32768
                || recoverySeconds < stuckSeconds || loseRange < detectRange || leashPadding < 0
                || float.IsNaN(leashPadding) || float.IsInfinity(leashPadding)
                || attackWindup + attackRecovery > attackCooldown || waypointTolerance > .45f)
                throw new InvalidOperationException("RunBehavior incompatible navigation/combat budgets: " + role);
        }
    }
}
