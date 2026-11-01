using UnityEngine;
using UnityEngine.UI;

/// <summary>导航暂停页：被设置页覆盖时继续持有暂停状态，真正退栈才恢复原时间倍率。</summary>
public sealed class PausePanel : BasePanel
{
    bool paused;
    float previousScale;
    Button resume,settings;
    public override bool ReturnsToGameplayOnClose=>true;
    void Awake()
    {
        GetComponent<Image>().color=new Color(0,0,0,.7f);
        var card=GameplayUi.Rect(transform,"PauseCard",Vector2.one*.5f,Vector2.one*.5f,Vector2.zero,new Vector2(480,270));
        card.gameObject.AddComponent<Image>().color=new Color(.055f,.075f,.10f,1);
        resume=GameplayUi.Button(card,"Resume","",new Vector2(50,-45),new Vector2(380,64),ClosePanel);
        settings=GameplayUi.Button(card,"Settings","",new Vector2(50,-155),new Vector2(380,64),()=>UIManager.Instance.PushPanel(PanelType.SettingPanel));
    }
    public override void OnEnter()
    {
        if(!paused){previousScale=Time.timeScale;paused=true;}
        Time.timeScale=0;CursorEnable();
        resume.GetComponentInChildren<TMPro.TextMeshProUGUI>().text=OfficeTaskText.Get("resume");
        settings.GetComponentInChildren<TMPro.TextMeshProUGUI>().text=OfficeTaskText.Get("settings");
    }
    public override void OnExit(){if(!UIManager.Instance.panelStack.Contains(this))Restore();}
    void Restore(){if(!paused)return;Time.timeScale=previousScale;paused=false;}
    void OnDestroy(){Restore();}
}
