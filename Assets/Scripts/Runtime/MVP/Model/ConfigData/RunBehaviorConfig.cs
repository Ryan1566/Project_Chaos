using System;	

[Serializable]	
public class RunBehaviorConfig
{
	public uint Id;//行为行编号
	public string role;//角色用途
	public float moveSpeed;//移动速度 米每秒
	public float patrolSpeed;//巡逻速度 米每秒
	public float acceleration;//加速度 米每秒平方
	public float jumpForce;//起跳速度 米每秒
	public float repathInterval;//重新寻路间隔 秒
	public float waypointTolerance;//路径点容差 米
	public float stuckSeconds;//卡住判定 秒
	public float recoverySeconds;//安全恢复阈值 秒
	public int searchRadius;//锚点搜索半径 米
	public int maxExpandedNodes;//单次搜索展开节点上限
	public float detectRange;//发现距离 米
	public float loseRange;//丢失距离 米
	public float memorySeconds;//目标记忆 秒
	public float attackRange;//攻击范围 米
	public float attackWindup;//攻击前摇 秒
	public float attackRecovery;//攻击后摇 秒
	public float attackCooldown;//攻击起手间隔 秒
	public float attackHeight;//攻击高度容差 米
	public float patrolWait;//巡逻等待 秒
	public float returnTolerance;//返回目标容差 米
	public float leashPadding;//活动区扩展 米
	public float interactionRange;//受控角色公共交互距离 米
}

