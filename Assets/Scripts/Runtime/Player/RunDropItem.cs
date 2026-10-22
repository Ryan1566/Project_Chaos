using UnityEngine;
using ChaosDebug;

/// <summary>地上的可捡掉落物：玩家碰到 → 重试入包，成功销毁，失败保留。</summary>
public class RunDropItem : MonoBehaviour
{
    public uint itemId;
    public uint count;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (other.GetComponentInParent<PlayerLocomotion>() == null) return;   //只认受控玩家

        string reason;
        if (RunInventory.AddItem(itemId, count, out reason))
        {
            ChaosLog.Info(LogChannel.Config, "[局内库存] 捡起掉落物 " + name + "（物品 " + itemId + " ×" + count + "）");
            Destroy(gameObject);
        }
        else
        {
            ChaosLog.Warn(LogChannel.Config, "[局内库存] 捡起失败：" + reason + "（掉落物保留）");
        }
    }
}
