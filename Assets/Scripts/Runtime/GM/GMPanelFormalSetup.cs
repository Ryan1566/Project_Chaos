using UnityEngine;

public partial class GMPanel
{
    private void BuildFormalSetupPage(Transform page)
    {
        AddHint(page, "正式玩法请从主菜单选择存档，再使用局外管理与派遣界面。此页只补齐当前会话的测试条件，不自动出征。配置会随手动或自动保存写入当前档位。");
        AddActionButton(page, "补齐测试条件（4门客 / 四类资源各至少500）", () =>
        {
            if (!GameSession.Instance.HasSession)
            {
                Debug.LogWarning("[测试环境] 请先从正式存档界面新建或读取存档。");
                return;
            }
            foreach (InventoryService.ResourceKind kind in System.Enum.GetValues(typeof(InventoryService.ResourceKind)))
            {
                uint current = InventoryService.Instance.GetResource(kind);
                if (current < 500) InventoryService.Instance.AddResource(kind, 500 - current);
            }
            while (RetainerService.Instance.Count < 4)
                if (RetainerService.Instance.RecruitRandom() == null) break;
            Debug.Log("[测试环境] 已补齐当前会话条件；关闭GM后从正式界面管理、派遣。配置遵循当前会话的手动与自动保存规则。");
        });
        AddActionButton(page, "快进建造与伤病30秒", () =>
        {
            RetainerService.Instance.Tick(30);
            VillageService.Instance.Tick(30);
            Debug.Log("[测试环境] 建造与伤病已快进30秒。");
        });
    }
}
