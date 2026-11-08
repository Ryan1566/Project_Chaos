using UnityEngine;
using UnityEngine.UI;

/// <summary>导航暂停页：被设置页覆盖时继续持有暂停状态，真正退栈才恢复原时间倍率。</summary>
public sealed class PausePanel : BasePanel
{
    bool paused;
    float previousScale;
    Button resume,settings,saveExit;
    TMPro.TextMeshProUGUI saveFeedback;
    public override bool ReturnsToGameplayOnClose=>true;
    void Awake()
    {
        GetComponent<Image>().color=new Color(0,0,0,.7f);
        var card=GameplayUi.Rect(transform,"PauseCard",Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(480,360));
        card.gameObject.AddComponent<Image>().color=new Color(.055f,.075f,.10f,1);
        resume=GameplayUi.Button(card,"Resume","",new Vector2(50,-45),new Vector2(380,64),ClosePanel);
        settings=GameplayUi.Button(card,"Settings","",new Vector2(50,-127),new Vector2(380,64),()=>UIManager.Instance.PushPanel(PanelType.SettingPanel));
        saveExit=GameplayUi.Button(card,"SaveRunAndReturn","保存并返回主菜单",new Vector2(50,-209),new Vector2(380,64),SaveAndExit);
        var feedback=GameplayUi.Rect(card,"SaveFeedback",Vector2.up,Vector2.up,new Vector2(24,-281),new Vector2(432,62));
        saveFeedback=GameplayUi.Text(feedback,"Text","",18);
    }
    public override void OnEnter()
    {
        if(!paused){previousScale=Time.timeScale;paused=true;}
        Time.timeScale=0;CursorEnable();
        resume.GetComponentInChildren<TMPro.TextMeshProUGUI>().text=OfficeTaskText.Get("resume");
        settings.GetComponentInChildren<TMPro.TextMeshProUGUI>().text=OfficeTaskText.Get("settings");
        saveExit.gameObject.SetActive(RunCheckpoint.IsPlayable);
        saveFeedback.text="";
    }
    private void SaveAndExit()
    {
        if (!RunCheckpoint.SaveAndReturnToTitle(out string reason)) ShowSaveFailure(reason);
    }
    internal void ShowSaveFailure(string reason) => saveFeedback.text = reason;
    public override void OnExit(){if(!UIManager.Instance.panelStack.Contains(this))Restore();}
    void Restore(){if(!paused)return;Time.timeScale=previousScale;paused=false;}
    void OnDestroy(){Restore();}
}
