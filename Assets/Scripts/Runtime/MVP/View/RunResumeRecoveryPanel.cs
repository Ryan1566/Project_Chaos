using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>续档失败时保留存档，玩家可重试或明确确认放弃。</summary>
public sealed class RunResumeRecoveryPanel : BasePanel
{
    private TextMeshProUGUI label;
    private Button retry, abandon, back;
    private string failure;
    private bool confirming;

    public static void Open(string reason)
    {
        if (UIManager.Instance.PushPanel(PanelType.RunResumeRecoveryPanel) is RunResumeRecoveryPanel panel)
            panel.ShowFailure(reason);
    }

    public override void OnEnter()
    {
        if (label == null)
        {
            GetComponent<Image>().color = new Color(0, 0, 0, .8f);
            var card = GameplayUi.Rect(transform, "ResumeRecoveryCard", Vector2.one * .5f, Vector2.one * .5f,
                Vector2.zero, new Vector2(690, 340));
            GameplayUiSkin.Apply(card.gameObject.AddComponent<Image>(), "UI_Panel_Dialog", GameplayUiSkin.DarkPaper);
            var textArea = GameplayUi.Rect(card, "MessageArea", Vector2.up, Vector2.up, new Vector2(22, -22), new Vector2(646, 220));
            label = GameplayUi.Text(textArea, "Message", "", 22);
            label.enableAutoSizing = true;
            label.fontSizeMin = 15;
            label.fontSizeMax = 22;
            retry = GameplayUi.Button(card, "RetryRunResume", "重试", new Vector2(30, -270), new Vector2(140, 46), Retry);
            abandon = GameplayUi.Button(card, "AbandonRunResume", "放弃本局并回村", new Vector2(190, -270), new Vector2(270, 46), Abandon);
            back = GameplayUi.Button(card, "ReturnToSlots", "返回", new Vector2(500, -270), new Vector2(150, 46), HandleBack);
        }
        CursorEnable();
    }

    private void ShowFailure(string reason)
    {
        failure = reason;
        confirming = false;
        label.text = "无法继续未完成的出征\n" + reason + "\n原检查点仍保留。";
        retry.interactable = true;
        abandon.GetComponentInChildren<TextMeshProUGUI>().text = "放弃本局并回村";
        back.GetComponentInChildren<TextMeshProUGUI>().text = "返回";
    }

    private void Retry()
    {
        if (!confirming && !RunSceneFlow.Resume(out string reason)) ShowFailure(reason);
    }

    private void Abandon()
    {
        if (!confirming)
        {
            confirming = true;
            label.text = "确定放弃这次出征？\n按撤离处理，未结算的普通物品不会返回仓库。";
            retry.interactable = false;
            abandon.GetComponentInChildren<TextMeshProUGUI>().text = "确认放弃";
            back.GetComponentInChildren<TextMeshProUGUI>().text = "取消";
            return;
        }
        if (!RunCheckpoint.Abandon(out string reason)) { ShowFailure(reason); return; }
        try { VillageSceneFlow.EnterFromSave(); }
        catch (System.Exception error) { VillageSceneFlow.ReportEntryFailure(error.Message); }
    }

    public override void HandleBack()
    {
        if (confirming) { ShowFailure(failure); return; }
        ClosePanel();
    }
}
