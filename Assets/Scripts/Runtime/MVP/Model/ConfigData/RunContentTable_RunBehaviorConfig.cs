using System;

[Serializable]
public class RunContentTable_RunBehaviorConfig
{
    /// <summary>
    /// 行为行编号
    /// </summary>
    public uint Id;
    /// <summary>
    /// 角色用途
    /// </summary>
    public string role;
    /// <summary>
    /// 移动速度 米每秒
    /// </summary>
    public float moveSpeed;
    /// <summary>
    /// 巡逻速度 米每秒
    /// </summary>
    public float patrolSpeed;
    /// <summary>
    /// 加速度 米每秒平方
    /// </summary>
    public float acceleration;
    /// <summary>
    /// 起跳速度 米每秒
    /// </summary>
    public float jumpForce;
    /// <summary>
    /// 重新寻路间隔 秒
    /// </summary>
    public float repathInterval;
    /// <summary>
    /// 路径点容差 米
    /// </summary>
    public float waypointTolerance;
    /// <summary>
    /// 卡住判定 秒
    /// </summary>
    public float stuckSeconds;
    /// <summary>
    /// 安全恢复阈值 秒
    /// </summary>
    public float recoverySeconds;
    /// <summary>
    /// 锚点搜索半径 米
    /// </summary>
    public int searchRadius;
    /// <summary>
    /// 单次搜索展开节点上限
    /// </summary>
    public int maxExpandedNodes;
    /// <summary>
    /// 发现距离 米
    /// </summary>
    public float detectRange;
    /// <summary>
    /// 丢失距离 米
    /// </summary>
    public float loseRange;
    /// <summary>
    /// 目标记忆 秒
    /// </summary>
    public float memorySeconds;
    /// <summary>
    /// 攻击范围 米
    /// </summary>
    public float attackRange;
    /// <summary>
    /// 攻击前摇 秒
    /// </summary>
    public float attackWindup;
    /// <summary>
    /// 攻击后摇 秒
    /// </summary>
    public float attackRecovery;
    /// <summary>
    /// 攻击起手间隔 秒
    /// </summary>
    public float attackCooldown;
    /// <summary>
    /// 攻击高度容差 米
    /// </summary>
    public float attackHeight;
    /// <summary>
    /// 巡逻等待 秒
    /// </summary>
    public float patrolWait;
    /// <summary>
    /// 返回目标容差 米
    /// </summary>
    public float returnTolerance;
    /// <summary>
    /// 活动区扩展 米
    /// </summary>
    public float leashPadding;
    /// <summary>
    /// 受控角色公共交互距离 米
    /// </summary>
    public float interactionRange;
}
