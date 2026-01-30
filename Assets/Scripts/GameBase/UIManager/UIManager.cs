using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : SingletonBase<UIManager>
{
    private Dictionary<PanelType, BasePanel> PanelDic = new Dictionary<PanelType, BasePanel>();//存储对应ui的字典
    public Stack<BasePanel> panelStack = new Stack<BasePanel>();//存储ui内容的堆栈

    private Transform _canvas;//锁定场景中的画布，方便放置ui

    //public MainPanel mainPanel;
    //public PlayerStatusPanel playerStatusPanel;
    //public InteractingPromptPanel interactingPromptPanel;//可以调用PromptPanel资源
    //public DialogPanel dialogManager;
    //public QuestPanel questPanel;
    //public GainQuestPanel gainQuestPanel;
    //public ProgressPanel progressPanel;
    //public SavingLoadingPanel slPanel;

    public void OnInit()//ui的初始化
    {
        _canvas = GameObject.Find("Canvas").transform;
        //mainPanel = PushPanel(PanelType.MainPanel) as MainPanel;//生成主菜单
        //ShowMsg("正在初始化", false);
    }

    /// <summary>
    /// 把ui显示在界面上
    /// </summary>
    /// <param name="panelType"></param>
    public BasePanel PushPanel(PanelType panelType)//ui的显示,显示之后得有个返回值
    {
        if (PanelDic.TryGetValue(panelType, out BasePanel panel))//根据ui类型获取ui的内容，如果获取到
        {
            if (panelStack.Count > 0) //堆栈不为空
            {
                BasePanel topPanel = panelStack.Peek();//声明一个变量用于记录堆栈最顶部的对象 堆栈方法Peek可以返回堆栈顶部的对象，但不移除它
                topPanel.OnExit();//顶部ui显示着就关闭
            }
            panelStack.Push(panel);//存入到堆栈中，成为堆栈中新的顶部对象
            panel.OnEnter();//显示新的ui
            return panel;
        }
        else//如果没有获取到对应的ui
        {
            BasePanel panel1 = SpawnPanel(panelType);//实例化该ui
            if (panelStack.Count > 0)//同上
            {
                BasePanel topPanel = panelStack.Peek();//同上
                topPanel.OnExit();
            }
            panelStack.Push(panel1);//同上
            panel1.OnEnter();
            return panel1;
        }
    }

    /// <summary>
    /// 关闭当前ui
    /// </summary>
    public void PopPanel()//移除最上层的ui，因为是动态管理ui，所以移除相当于关闭
    {
        if (panelStack.Count == 0)//堆栈为空，即不存在ui
        {
            return;
        }

        BasePanel topPanel = panelStack.Pop();//Pop()方法用于从栈中返回顶部的对象，并删除它，即从堆栈中移除该ui
        topPanel.OnExit();//将ui本身进行隐藏

        BasePanel panel = panelStack.Peek();//获取删除原本顶部对象后新的顶部对象
        panel.OnEnter();//显示新的ui
    }

    /// <summary>
    /// 实例化对应的ui
    /// </summary>
    /// <param name="panelType"></param>
    public BasePanel SpawnPanel(PanelType panelType)//实例化生成ui
    {
        if (_canvas == null)
        {
            _canvas = GameObject.FindGameObjectsWithTag(nameof(Canvas))[0].transform;
        }
        //GameObject g = Resources.Load<GameObject>("Panel/" + panelType.ToString());
        GameObject g = ResManager.Instance.Load<GameObject>(GlobalPath.res_PanelPath + panelType.ToString());

        string path = GlobalPath.res_PanelPath + g.gameObject.name;
        //GameObject panel = GameObject.Instantiate(Resources.Load(path)) as GameObject;
        GameObject panel = ResManager.Instance.Load<GameObject>(path);//从Resources加载并实例化一个ui对象，并强制转化为GameObject对象
        PanelDic.Add(panelType, panel.GetComponent<BasePanel>());//添加加载的ui到字典中
        panel.transform.SetParent(_canvas, false);//加载的ui放置于画布下，并不以世界坐标显示

        panel.GetComponent<BasePanel>().setUIManager = this;//让加载的ui获取到本脚本

        return panel.GetComponent<BasePanel>();//返回ui的内容
                                               //return null;
    }
}
