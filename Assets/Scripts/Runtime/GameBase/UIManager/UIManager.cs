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
    /// <summary>缓存的 UI Canvas（避免隐藏后再 Find 找不到 → Console error）。</summary>
    private static Canvas _cachedCanvas;

    private Dictionary<PanelType, BasePanel> PanelDic = new Dictionary<PanelType, BasePanel>();//存储对应ui的字典
    public Stack<BasePanel> panelStack = new Stack<BasePanel>();//存储ui内容的堆栈

    private Transform _canvas;//锁定场景中的画布，方便放置ui

    public void OnInit()//ui的初始化
    {
        _canvas = FindCanvas();

        //初始化时显示底图
        GameObject baseBG = null;
        baseBG = _canvas.Find("Base_BG").gameObject ?? null;
        baseBG.SetActive(true);

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

        //① 缓存优先（Canvas 被隐藏后再 Find 会找不到 → 之前会报 error）
        GameObject canvasObj = _cachedCanvas != null ? _cachedCanvas.gameObject : GameObject.Find("Canvas");

        //② 兜底：包含未激活对象再找一次（进局时 Canvas 被 HideAllPanelsForGameplay 隐藏过）
        if (canvasObj == null)
        {
            Canvas[] all = Resources.FindObjectsOfTypeAll<Canvas>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null || all[i].gameObject.name != "Canvas") continue;
                canvasObj = all[i].gameObject;
                break;
            }
        }

        if (canvasObj != null) _cachedCanvas = canvasObj.GetComponent<Canvas>();   //③ 写入缓存

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

        InputManager.Instance.SetGameplayInputBlocked(this, true);
        if (topPanel != null) topPanel.Hide();//旧面板的退场动画与新面板的进场动画并发进行

        panelStack.Push(panel);//存入到堆栈中，成为堆栈中新的顶部对象

        //必须在 Show 之前：让进场面板渲染在最上层，压住正在退场的旧面板
        panel.transform.SetAsLastSibling();

        panel.Show();//显示新的ui
        RefreshGameplayInputBlock();

        return panel;
    }

    /// <summary>
    /// 关闭当前ui
    /// </summary>
    /// <summary>Only the visible top panel receives one back press, before other shortcut drivers.</summary>
    public bool TryHandleBackNavigation()
    {
        if (panelStack.Count == 0 || RunSceneFlow.IsTransitioning || VillageSceneFlow.IsLoading) return false;
        var top = panelStack.Peek(); var input = InputManager.Instance;
        if (top == null || !top.IsShowing || !top.gameObject.activeInHierarchy
            || input.HasGameplayBlockerOtherThan(this) || !input.WasNavigationPressed(InputManager.ActionMenu)) return false;
        input.ConsumeNavigationPress();
        top.HandleBack();
        return true;
    }

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

        if (panelStack.Count > 0)
        {
            BasePanel panel = panelStack.Peek();
            if (panel != topPanel) panel.Show();
        }
        // A gameplay overlay can cover another gameplay overlay (camp -> run menu).
        // Hide the outside canvas only when the last such overlay closes.
        if (topPanel.ReturnsToGameplayOnClose
            && (panelStack.Count == 0 || !panelStack.Peek().ReturnsToGameplayOnClose)) HideAllPanelsForGameplay();
        RefreshGameplayInputBlock();
    }

    /// <summary>
    /// 实例化对应的ui
    /// </summary>
    /// <param name="panelType"></param>
    /// <summary>
    /// **替换根面板**：把当前堆栈里的所有面板退场并清空堆栈，再把目标面板压成新的根。
    ///
    /// ══════════════ 为什么需要它 ══════════════
    /// `PushPanel` 是"叠加"语义（新的压旧的）。而「主菜单 → 读档 → 局外主界面」是**替换**语义 ——
    /// 只 Push 的话，局外界面底下会留着主菜单/存档面板，玩家按返回会**露出主菜单**，
    /// 而且 `panelStack` 会越堆越深。
    ///
    /// ⚠ `PushPanel` / `PopPanel` 的语义**未改动**：这里只是在它们之上组合出替换语义，
    /// 并且是**幂等**的（目标已是栈顶时 `PushPanel` 会自行提前返回），
    /// 所以即使 `Entry` 的两条路由路径都跑到，也不会压出两层。
    /// </summary>
    public BasePanel SetRootPanel(PanelType panelType)
    {
        //先把现有面板全部退场并清空堆栈。
        //从栈底往栈顶处理：SetAsLastSibling 之后栈顶那份的 sibling 最靠后，
        //退场动画不会被下层挡掉（与 PopPanel 里"退场面板必须在下面那个之上"同一口径）。
        BasePanel[] old = panelStack.ToArray();//[栈顶 ... 栈底]
        panelStack.Clear();

        for (int i = old.Length - 1; i >= 0; i--)
        {
            if (old[i] == null) continue;
            old[i].transform.SetAsLastSibling();
            old[i].Hide();
        }

        //栈已空 → PushPanel 不会误把旧栈顶当成"当前顶部"
        BasePanel panel = PushPanel(panelType);

        if (panel == null)
        {
            ChaosLog.Error($"[UIManager] SetRootPanel({panelType}) 失败：面板没能生成（预制体缺失？）");
        }
        return panel;
    }

    /// <summary>Scene gameplay may be the root without any full-screen panel.</summary>
    public void CloseAllPanels()
    {
        panelStack.Clear();
        foreach (var panel in PanelDic.Values)
            if (panel != null && panel.IsShowing) panel.Hide();
        RefreshGameplayInputBlock();
    }
    public void SetSceneBackgroundVisible(bool visible)
    {
        if (_canvas == null) return;
        var background = _canvas.Find("Base_BG"); if (background != null) background.gameObject.SetActive(visible);
    }

    // ══════════════════════ 局内：隐藏/恢复局外面板（W3.0b）══════════════════════

    /// <summary>进局时被隐藏的面板（按发现顺序，恢复时按原序 SetActive 回来）。</summary>
    private readonly List<PanelType> _hiddenForGameplay = new List<PanelType>();

    /// <summary>进局时被隐藏的 UI Canvas（局外 UI 根，例如主菜单 Canvas）；GM 的 Canvas 不在此列。</summary>
    private readonly List<GameObject> _hiddenCanvases = new List<GameObject>();

    /// <summary>
    /// **进局时隐藏所有已打开的局外面板**（不改 PushPanel/PopPanel 语义，也不动 panelStack，
    /// 只是把面板对象 SetActive(false)；恢复时原样激活回来）。
    /// ⚠ **GM 面板不隐藏** —— 开发期要能用它撤离/调试。
    /// </summary>
    public void HideAllPanelsForGameplay()
    {
        _hiddenForGameplay.Clear();
        foreach (KeyValuePair<PanelType, BasePanel> pair in PanelDic)
        {
            if (pair.Value == null) continue;
            if (pair.Key == PanelType.GMPanel) continue;              //GM 面板保持可用
            if (!pair.Value.gameObject.activeSelf) continue;

            _hiddenForGameplay.Add(pair.Key);
            pair.Value.gameObject.SetActive(false);
        }

        //局外面板挂在这些 Canvas 下：只隐藏面板对象的话 Canvas 的背景/其它元素仍会挡画面，
        //所以把非 GM 的 UI Canvas 一起隐藏（记录后原样恢复）。
        _hiddenCanvases.Clear();
        UnityEngine.Canvas[] canvases = UnityEngine.Object.FindObjectsOfType<UnityEngine.Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            UnityEngine.Canvas canvas = canvases[i];
            if (canvas == null) continue;
            if (canvas.GetComponent<GameplayCanvas>() != null) continue;
            string canvasName = canvas.gameObject.name;
            if (canvasName.Contains("GM")) continue;          //GM 的 Canvas（GMBtn / GM_Canvas）保持可见
            if (!canvas.gameObject.activeSelf) continue;
            _hiddenCanvases.Add(canvas.gameObject);
            canvas.gameObject.SetActive(false);
        }
        RefreshGameplayInputBlock();
        ChaosLog.Info(LogChannel.UI, "[局内] 已隐藏局外 Canvas " + _hiddenCanvases.Count + " 个");

        ChaosLog.Info(LogChannel.UI, "[局内] 已隐藏局外面板 " + _hiddenForGameplay.Count + " 个（GM 面板保持可见）："
            + string.Join(",", _hiddenForGameplay.ConvertAll(t => t.ToString()).ToArray()));
    }

    /// <summary>撤离时恢复进局前打开的面板（按原顺序）。</summary>
    public void RestoreAfterGameplay()
    {
        //先恢复 Canvas，再恢复面板（面板是 Canvas 的子节点）
        for (int i = 0; i < _hiddenCanvases.Count; i++)
        {
            if (_hiddenCanvases[i] != null) _hiddenCanvases[i].SetActive(true);
        }
        _hiddenCanvases.Clear();

        int restored = 0;
        for (int i = 0; i < _hiddenForGameplay.Count; i++)
        {
            BasePanel panel;
            if (!PanelDic.TryGetValue(_hiddenForGameplay[i], out panel) || panel == null) continue;
            panel.gameObject.SetActive(true);
            restored++;
        }

        ChaosLog.Info(LogChannel.UI, "[局外] 已恢复面板 " + restored + " 个："
            + string.Join(",", _hiddenForGameplay.ConvertAll(t => t.ToString()).ToArray()));
        _hiddenForGameplay.Clear();
        RefreshGameplayInputBlock();
    }

    private void RefreshGameplayInputBlock()
    {
        bool blocked = false;
        foreach (var panel in PanelDic.Values)
            if (panel != null && panel.IsShowing && panel.gameObject.activeInHierarchy) { blocked = true; break; }
        InputManager.Instance.SetGameplayInputBlocked(this, blocked);
    }

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
