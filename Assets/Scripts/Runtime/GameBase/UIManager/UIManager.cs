using System.Collections.Generic;
using ChaosDebug;
using UnityEngine;

/// <summary>
/// UI 管理器：维护面板实例字典与显示堆栈。
///
/// 显隐动画是"交叉同时播放"的 —— 打开新面板时旧面板的退场动画与新面板的进场动画并发进行，
/// 退场面板在动画期间保持激活（由 BasePanel.Hide 内部的动画完成回调负责关闭），
/// 所以这里的 Push / Pop 都是同步返回的，不需要协程或回调。
/// </summary>
public class UIManager : SingletonBase<UIManager>
{
    private Dictionary<PanelType, BasePanel> PanelDic = new Dictionary<PanelType, BasePanel>();//存储对应ui的字典
    public Stack<BasePanel> panelStack = new Stack<BasePanel>();//存储ui内容的堆栈

    private Transform _canvas;//锁定场景中的画布，方便放置ui

    public void OnInit()//ui的初始化
    {
        _canvas = FindCanvas();

#if UNITY_EDITOR
        ValidatePanelPrefabs();
#endif
    }

    /// <summary>
    /// 找场景里的画布。按名字查找而不是按 Tag —— 本项目并没有定义 "Canvas" 这个 Tag，
    /// FindGameObjectsWithTag 会直接抛 "Tag: Canvas is not defined."。
    /// </summary>
    private static Transform FindCanvas()
    {
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            ChaosLog.Error("[UIManager] 场景中找不到名为 Canvas 的物体，UI 无法显示");
            return null;
        }
        return canvasObj.transform;
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器期校验：PanelType 的每个成员都要有同名的面板预制体，否则运行时会静默生成失败。
    /// 把"运行时 NRE"变成一条可操作的提示（聚合成一条，避免刷屏）。
    /// </summary>
    private static void ValidatePanelPrefabs()
    {
        List<string> missing = null;

        foreach (PanelType type in System.Enum.GetValues(typeof(PanelType)))
        {
            if (Resources.Load<GameObject>(GlobalPath.res_PanelPath + type) == null)
            {
                if (missing == null) missing = new List<string>();
                missing.Add(type.ToString());
            }
        }

        if (missing != null)
        {
            ChaosLog.Warn($"[UIManager] 以下 PanelType 还没有对应的面板预制体" +
                          $"（Assets/Resources/{GlobalPath.res_PanelPath}）：{string.Join("、", missing)}");
        }
    }
#endif

    /// <summary>
    /// 把ui显示在界面上
    /// </summary>
    /// <param name="panelType"></param>
    public BasePanel PushPanel(PanelType panelType)//ui的显示,显示之后得有个返回值
    {
        BasePanel panel;
        // 缓存里可能躺着一个已被销毁的实例：UIManager 是静态单例（不是 MonoBehaviour），
        // PanelDic 会跨场景存活，而面板是画布的子物体、切场景时会被销毁。
        // Unity 给 UnityEngine.Object 重载了 ==，已销毁的对象比较起来就是 null，
        // 所以要把它当成"没缓存过"重新生成 —— 否则这个 PanelType 会永久打不开，
        // 而且因为不再走 SpawnPanel，连一条错误日志都不会有。
        if (!PanelDic.TryGetValue(panelType, out panel) || panel == null)
        {
            panel = SpawnPanel(panelType);//实例化该ui
        }

        if (panel == null) return null;//生成失败（缺预制体等），SpawnPanel 里已经报过错

        BasePanel topPanel = panelStack.Count > 0 ? panelStack.Peek() : null;//堆栈最顶部的对象

        if (topPanel == panel)
        {
            //重复打开栈顶面板：不退场也不再入栈。
            //不这样挡的话，同一个实例会被压两次，之后 Pop 会让堆栈永久错乱
            panel.transform.SetAsLastSibling();
            return panel;
        }

        // 面板已经在栈里但不在栈顶（典型场景：从下层面板的按钮再次打开它）。
        // 不处理的话同一个实例会被压两次，栈深凭空 +1 —— Pop 一次只消费掉一份，
        // 用户要按两次返回才能退出一个面板，而且栈顶判断从此全是错的。
        // Stack 不支持从中间删除，只能按"顶->底"取出再倒着压回去，保持相对次序。
        if (panelStack.Contains(panel))
        {
            BasePanel[] old = panelStack.ToArray();//[栈顶 ... 栈底]
            panelStack.Clear();
            for (int i = old.Length - 1; i >= 0; i--)
            {
                if (old[i] != panel) panelStack.Push(old[i]);
            }
        }

        if (topPanel != null) topPanel.Hide();//旧面板的退场动画与新面板的进场动画并发进行

        panelStack.Push(panel);//存入到堆栈中，成为堆栈中新的顶部对象

        //必须在 Show 之前：让进场面板渲染在最上层，压住正在退场的旧面板
        //panel.transform.SetAsLastSibling();

        panel.Show();//显示新的ui

        return panel;
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

        //退场面板必须保持在"下面那个面板"之上，否则它的退场动画会被下层的不透明背景完全挡住
        topPanel.transform.SetAsLastSibling();

        topPanel.Hide();//播放退场动画，播完自动关闭

        if (panelStack.Count == 0) return;//修复：原来这里直接 Peek()，栈里只剩一个面板时会抛异常

        BasePanel panel = panelStack.Peek();//获取删除原本顶部对象后新的顶部对象
        if (panel != topPanel) panel.Show();//显示新的ui
    }

    /// <summary>
    /// 实例化对应的ui
    /// </summary>
    /// <param name="panelType"></param>
    public BasePanel SpawnPanel(PanelType panelType)//实例化生成ui
    {
        // 同一类型已经有活着的实例就直接复用。SpawnPanel 是公开的，直接调用它会把
        // PanelDic 里的旧实例顶掉 —— 那个实例会永远留在画布上，既关不掉也再拿不到，
        // 而且它的 Update/事件订阅全都还在跑。要显示面板请用 PushPanel。
        BasePanel cached;
        if (PanelDic.TryGetValue(panelType, out cached) && cached != null) return cached;

        if (_canvas == null)
        {
            _canvas = FindCanvas();
            if (_canvas == null) return null;
        }

        //直接用枚举名拼路径：预制体的文件名与 PanelType 的成员同名。
        //（原来先用 Load 出来的实例名拼路径，而 ResManager.Load 内部会 Instantiate，
        //  实例名带上了 "(Clone)" 后缀，拼出来的路径必然加载失败；而且那次 Load 还白生成了一份实例）
        string path = GlobalPath.res_PanelPath + panelType.ToString();

        GameObject panel = ResManager.Instance.Load<GameObject>(path);//从Resources加载并实例化一个ui对象
        if (panel == null)
        {
            ChaosLog.Error($"[UIManager] 面板预制体缺失：Assets/Resources/{path}.prefab");
            return null;
        }

        BasePanel basePanel = panel.GetComponent<BasePanel>();
        if (basePanel == null)
        {
            ChaosLog.Error($"[UIManager] {path} 上没有挂 BasePanel 组件");
            Object.Destroy(panel);
            return null;
        }

        panel.transform.SetParent(_canvas, false);//加载的ui放置于画布下，并不以世界坐标显示
        panel.transform.SetAsLastSibling();

        basePanel.setUIManager = this;//让加载的ui获取到本脚本

        PanelDic[panelType] = basePanel;//用索引器而不是 Add：重复生成时 Add 会抛 ArgumentException

        return basePanel;//返回ui的内容
    }
}
