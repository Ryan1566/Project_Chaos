using System;
using LocalizationSystem;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 设置面板拼装器。菜单：Tools/UI/生成设置面板
///
/// ══════════════════════ 什么时候该用它 ══════════════════════
/// 这是一个【一次性生成工具】：按设计文档把 SettingPanel.prefab 整块拼出来。
/// 生成完成之后，prefab 才是唯一的事实来源 —— 之后调位置、改字号、加控件都直接在 prefab 上做。
/// ⚠ 重复运行会【覆盖】这个 prefab，手工调整会全部丢失。
/// 只有"想推倒重来"或"想按新的尺寸规范重排一次"时才再运行它。
///
/// ══════════════════════ 它保留了什么、重建了什么 ══════════════════════
/// 保留：根节点上的 Image 背景、CanvasGroup、SettingPanel、UIPanelAnimator，以及既有的 Title("设置")。
/// 删除：两个从主菜单抄来的废弃隐藏按钮（StartBtn / SettingBtn），以及旧的根级 ReturnBtn
///       （它已经挪进底部按钮条，留在原地会变成两个"返回"）。
/// 重建：主选列表 / 副设置列表 / 底部按钮条 / 返回确认浮层。
/// 另外生成 5 个行模板 prefab 到 Assets/Prefabs/UI/SettingRows/。
///
/// ══════════════════════ 行的摆放方式：复制，不是嵌套 ══════════════════════
/// 面板里的行是【普通 GameObject 复制】，不是行模板 prefab 的嵌套实例。这是刻意的权衡：
///   嵌套实例的好处是"改模板能同步到所有行"，但代价是本生成器要处理
///   嵌套 prefab + 预览场景 + 保存前解包（解包会把嵌套一起拆掉）三者的相互影响，
///   而且美术得先理解"哪些属性是覆盖、哪些是继承"才能改对一行。
///   复制体则是所见即所得的普通节点，改哪行就是哪行。
/// 行模板的作用因此是两件事：① 记录每个行类型必须有的子节点结构；
///   ② 以后要加新行时，从 Assets/Prefabs/UI/SettingRows/ 拖一个进来再改。
/// ⚠ 反过来说：改行模板【不会】影响面板里已经摆好的行。想批量重排就重跑本工具。
///
/// ══════════════════════ 子节点命名是硬契约 ══════════════════════
/// 行控件与面板脚本都靠 transform.Find("名字") 找子节点，且【都只查一层】，所以这里生成的名字
/// 必须与脚本里的约定逐字一致。名字清单见 SettingRowBase / SettingPanel 的类注释。
/// 改名字等于改接口：脚本会立刻在 Console 报错（这是特意的，好过静默失效）。
///
/// ══════════════════════ 坐标基准 ══════════════════════
/// 画布 1920×1080、中心为原点、y 向上。锚点全用相对比例写，所以换画布分辨率不用重排，
/// 前提是 CanvasScaler 的参考分辨率仍是 1920×1080。
/// </summary>
public static class SettingPanelBuilder
{
    private const string PanelPrefabPath = "Assets/Resources/UIPanels/SettingPanel.prefab";
    private const string RowTemplateDir = "Assets/Prefabs/UI/SettingRows";

    /// <summary>
    /// 字体：用 Assets/Fonts/MSYH SDF.asset（微软雅黑），【不要】沿用面板标题原来那个
    /// STFANGSO SDF（guid 2ecef2357a9c6564ab0866d7c7c33df9）。
    ///
    /// ⚠ 这不是审美偏好，是 STFANGSO SDF 根本无法显示本面板的文字：
    ///   它的 sourceFontFile 是空的（工程里压根没有 STFANGSO 字体文件），
    ///   所以即便 atlasPopulationMode 是 Dynamic，它也没有源字库可查 ——
    ///   图集里那 693 个字形就是它的全部，永远补不了新字。
    ///   实测它缺这 28 个字：辨视帧限垂直步局水反灵敏触输独占连低盘柄绑弃签页版语言震，
    ///   对应到本面板就是"帧率上限""垂直同步""独占全屏""鼠标灵敏度""输入设备""未绑定"
    ///   这些行整块变方框（TMP 会刷 "character ... was not found" 的警告）。
    ///   MSYH SDF 的源字库是 Assets/Fonts/MSYH.TTC（在工程里），动态图集能按需补字，所以用它。
    ///
    /// 附带影响：MainMenuPanel.prefab 仍在用 STFANGSO SDF，同样有缺字问题，
    /// 只是它的文案正好没踩到那 28 个字。要统一的话是一处改动（见交付说明）。
    /// </summary>
    private const string FontPath = "Assets/Fonts/MSYH SDF.asset";

    // ══════════════════ 尺寸规范 ══════════════════
    private const float RowHeight = 96f;
    private const float RowSpacing = 10f;
    private const float LabelFont = 46f;
    private const float ValueFont = 40f;
    private const float ButtonFont = 38f;
    private const float HintFont = 52f;

    // ══════════════════ 配色 ══════════════════
    // 面板底图是浅色（内置 Background 精灵 + 白色），所以文字用深色
    private static readonly Color TextDark = new Color(0.10f, 0.10f, 0.10f, 1f);
    private static readonly Color TextMuted = new Color(0.35f, 0.35f, 0.35f, 1f);
    private static readonly Color RowBg = new Color(0.93f, 0.93f, 0.93f, 0.85f);
    private static readonly Color ButtonBg = new Color(1f, 1f, 1f, 0.95f);
    private static readonly Color TabIdle = new Color(0.90f, 0.90f, 0.90f, 0.95f);
    private static readonly Color BarBg = new Color(0.78f, 0.78f, 0.78f, 1f);
    private static readonly Color BarFill = new Color(0.35f, 0.62f, 0.85f, 1f);

    /// <summary>主选按钮的节点名，下标 = SettingCategory。</summary>
    private static readonly string[] TabNames = { "Btn_Gameplay", "Btn_Keybind", "Btn_Graphics", "Btn_Audio" };

    /// <summary>页面节点名与它该挂的页面脚本，下标 = SettingCategory。顺序必须与 SettingEnums 里的枚举一致。</summary>
    private static readonly string[] PageNames = { "Page_Gameplay", "Page_Keybind", "Page_Graphics", "Page_Audio" };

    private static readonly Type[] PageTypes =
    {
        typeof(GameplaySettingsPage),
        typeof(KeybindSettingsPage),
        typeof(GraphicsSettingsPage),
        typeof(AudioSettingsPage),
    };

    private static TMP_FontAsset _font;
    private static Sprite _uiSprite;
    private static Sprite _bgSprite;
    private static Sprite _checkSprite;

    // ══════════════════ 入口 ══════════════════

    [MenuItem("Tools/UI/生成设置面板")]
    public static void Build()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        _uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        _bgSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        _checkSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");

        if (_font == null)
        {
            Debug.LogError("[SettingPanelBuilder] 找不到 TMP 字体资产：" + FontPath + "，生成中止");
            return;
        }

        if (_font.sourceFontFile == null)
        {
            //没有源字库的动态字体补不了字，界面会出现方框。这里提前拦住，
            //免得生成完了才发现是字体问题（排查方向会跑到 prefab 上去）
            Debug.LogError("[SettingPanelBuilder] " + FontPath +
                " 没有源字库（sourceFontFile 为空），它补不了图集里没有的字，界面会出现方框。生成中止");
            return;
        }

        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
        if (source == null)
        {
            Debug.LogError("[SettingPanelBuilder] 找不到面板预制体：" + PanelPrefabPath);
            return;
        }

        //在预览场景里拼，避免往玩家正开着的场景里塞临时物体（那会把场景标记成"已修改"）
        Scene preview = EditorSceneManager.NewPreviewScene();
        GameObject root = null;

        try
        {
            root = (GameObject)PrefabUtility.InstantiatePrefab(source, preview);
            if (root == null)
            {
                Debug.LogError("[SettingPanelBuilder] 实例化面板预制体失败");
                return;
            }

            //先把实例拆成普通层级再改。两个原因：
            //  1) 不拆就动子节点，增删都会变成"相对原 prefab 的覆盖记录"，存回去容易出现
            //     "删掉的按钮又冒出来"这类覆盖残留；
            //  2) 拆开之后 SaveAsPrefabAsset 写回同一个路径就是明确的覆盖写，
            //     不必担心实例与资产互相指向。
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            StripGeneratedChildren(root);
            RetargetTitleFont(root);

            //只补组件、不触发生命周期：面板脚本是 MonoBehaviour，这里不实例化任何单例
            if (root.GetComponent<CanvasGroup>() == null) root.AddComponent<CanvasGroup>();
            if (root.GetComponent<SettingPanel>() == null) root.AddComponent<SettingPanel>();
            if (root.GetComponent<UIPanelAnimator>() == null) root.AddComponent<UIPanelAnimator>();

            BuildMainList(root.transform);
            RectTransform contentArea = BuildContentArea(root.transform);
            BuildPages(contentArea);
            BuildBottomBar(root.transform);
            BuildConfirmGroup(root.transform);

            BuildRowTemplates();

            PrefabUtility.SaveAsPrefabAsset(root, PanelPrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[SettingPanelBuilder] 已生成 " + PanelPrefabPath + " 与行模板（" + RowTemplateDir + "）");
        }
        catch (Exception e)
        {
            Debug.LogError("[SettingPanelBuilder] 生成失败，prefab 未被修改：" + e);
        }
        finally
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    // ══════════════════ 根节点整理 ══════════════════

    /// <summary>
    /// 清掉所有由本工具负责的节点，让生成变成【可重复执行】的：
    /// 每次跑都是"把这四块连同废弃按钮全删掉，再重建一遍"，而不是在旧结构上再叠一层。
    ///
    /// ⚠ 这里漏删任何一块的后果很隐蔽：prefab 不会报错，而是根节点下出现两套 MainList/ContentArea，
    /// 而 SettingPanel 用 transform.Find 只认第一套 —— 于是面板显示的是上一轮生成的旧结构，
    /// 看起来像"改了代码却没生效"。（第一版就是只删了三个废弃按钮，连跑三次后根下叠了三套。）
    ///
    /// Title 不在删除之列：它是从主菜单沿用下来的标题，不属于本工具重建的部分。
    /// </summary>
    private static void StripGeneratedChildren(GameObject root)
    {
        //前三个是从主菜单面板抄过来的废弃按钮，ReturnBtn 则会以新的形式出现在 BottomBar 里
        string[] generated = { "StartBtn", "SettingBtn", "ReturnBtn", "MainList", "ContentArea", "BottomBar", "ConfirmGroup" };

        foreach (string name in generated)
        {
            //必须循环删干净：transform.Find 只返回【第一个】同名子节点。
            //只删一次的话，重复运行会变成"删掉一套旧的、再补上一套新的"，根下的套数永远不变 ——
            //表面上看生成成功了，实际面板认的是最旧的那一套（SettingPanel 用 Find 只认第一个）。
            //DestroyImmediate 是立即生效的，所以下一轮 Find 拿到的是下一个同名节点，能一路删空。
            Transform dead;
            while ((dead = root.transform.Find(name)) != null)
            {
                UnityEngine.Object.DestroyImmediate(dead.gameObject);
            }
        }
    }

    /// <summary>
    /// 把沿用下来的 Title 也换成本面板用的字体。
    /// 不换的话一个面板里会同时出现仿宋（标题"设置"）和雅黑（其余所有文字），
    /// 而这换起来只是一行 —— 统一比"少改一处"更值得。
    /// 字号、颜色、位置都不动，只换字体资源。
    /// </summary>
    private static void RetargetTitleFont(GameObject root)
    {
        Transform title = root.transform.Find("Title");
        if (title == null)
        {
            Debug.LogWarning("[SettingPanelBuilder] 没找到 Title 节点，标题会保持原字体（可能与本面板其余文字不一致）");
            return;
        }

        var text = title.GetComponent<TMP_Text>();
        if (text == null)
        {
            Debug.LogWarning("[SettingPanelBuilder] Title 上没有 TMP 文本组件，跳过字体替换");
            return;
        }

        text.font = _font;
        text.fontSharedMaterial = _font.material;
    }

    // ══════════════════ 主选列表 ══════════════════

    private static void BuildMainList(Transform parent)
    {
        RectTransform list = NewRect("MainList", parent);
        SetAnchors(list, 0.025f, 0.16f, 0.20f, 0.70f);

        VerticalLayoutGroup layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        for (int i = 0; i < TabNames.Length; i++)
        {
            Button tab = NewButton(TabNames[i], list, SettingsLabels.CategoryName(i), LabelFont, TabIdle);
            LayoutElement le = tab.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 110f;
            le.minHeight = 110f;

            //选中指示物：SettingPanel 按 "Selected" 这个名字找它，切分类时 SetActive
            GameObject marker = AddSelectedMarker(tab);
            marker.SetActive(i == 0);//第一个分类默认是当前页
        }
    }

    // ══════════════════ 副设置列表容器 ══════════════════

    private static RectTransform BuildContentArea(Transform parent)
    {
        RectTransform area = NewRect("ContentArea", parent);
        SetAnchors(area, 0.22f, 0.16f, 0.965f, 0.70f);
        return area;
    }

    private static void BuildPages(RectTransform contentArea)
    {
        BuildGameplayPage(NewPage(contentArea, PageNames[0], PageTypes[0], true, 0f, out _));
        BuildKeybindPage(NewPage(contentArea, PageNames[1], PageTypes[1], true, 0f, out _));
        BuildGraphicsPage(NewPage(contentArea, PageNames[2], PageTypes[2], false, 0f, out _));
        BuildAudioPage(NewPage(contentArea, PageNames[3], PageTypes[3], false, 0f, out _));

        //二级界面也是 ContentArea 下的一页（与本页平级，不能嵌进 Page_Keybind 子树，
        //理由是 FindRow / ValidateRows 会沿子树收集行，嵌进去会被父页重复收一遍）。
        //它默认不激活，由 KeybindSettingsPage 的「更改按键绑定」按钮点亮
        BuildKeybindBindingsPage(contentArea);
    }

    /// <summary>
    /// 一个页面 = 页面脚本 + ScrollRect + Viewport(RectMask2D) + Content(纵向排列)。
    /// 页面脚本必须挂在 Page_X 节点上：SettingPanel 按 ContentArea/Page_X 找它，
    /// 各行的值也要靠它从子树里收集（GetComponentsInChildren）。
    ///
    /// 返回值是【Content】而不是 Page 节点，因为这个返回值就是各页装配函数的 parent ——
    /// 行必须挂在 Content 下，才会被 VerticalLayoutGroup 排版、被 RectMask2D 裁剪、随滚轮滚动。
    /// 挂错到 Page 上照样能被 GetComponentsInChildren 找到（所以绑得上、日志不报错），
    /// 但所有行会堆在页面正中央且溢出可视区 —— 是个只看日志发现不了的错。
    /// 页根节点从 out 参数拿：二级界面的 Header 要挂在页根上（与 Viewport 平级），
    /// 只有它需要，所以不做成返回值。
    ///
    /// headerInset：给页顶留出的像素高度（二级界面放返回按钮与页签用）。
    /// 0 表示不留 —— 四个一级分类页都是 0，行为与本参数引入前完全一致。
    /// </summary>
    private static RectTransform NewPage(RectTransform parent, string nodeName, Type pageType, bool active,
        float headerInset, out RectTransform pageRoot)
    {
        RectTransform page = NewRect(nodeName, parent);
        pageRoot = page;
        Stretch(page, 0f, 0f, 0f, 0f);
        page.gameObject.AddComponent(pageType);

        ScrollRect scroll = page.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;
        scroll.inertia = false;//设置项是逐个调的，惯性滑动会让"停在某一项"变难

        RectTransform viewport = NewRect("Viewport", page);
        Stretch(viewport, 0f, 0f, 0f, headerInset);
        //用 RectMask2D 而不是 Mask：Mask 要一张遮罩图，RectMask2D 只按矩形裁剪，少一个资产、少一次绘制
        viewport.gameObject.AddComponent<RectMask2D>();
        Image viewportImg = viewport.gameObject.AddComponent<Image>();
        viewportImg.color = new Color(1f, 1f, 1f, 0f);//alpha 为 0 仍能接收射线，玩家才能拖动/滚轮
        viewportImg.raycastTarget = true;

        RectTransform content = NewRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = RowSpacing;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport;
        scroll.content = content;

        //只有当前分类的页面默认打开，其余关掉：既避免四页叠在一起看不出来，
        //也符合 SettingPanel 的用法（它每次切页都会先 SetActive 再绑定）
        page.gameObject.SetActive(active);
        return content;
    }

    // ══════════════════ 各页内容 ══════════════════

    private static void BuildGameplayPage(RectTransform content)
    {
        //语言：档位文案由 GameplaySettingsPage 在运行时从 LocalizationManager 取，
        //这里只负责摆一个选择器行，所以生成器不需要知道有哪几种语言。
        SelectorRow(content, "Row_Language", SettingIds.Language, "语言");

        //其余游戏性项还没做，留一句占位说明。
        //它不带 SettingRowBase 组件，所以 SettingsPageBase 的"摆了行却没绑"校验不会对它报警告
        RectTransform row = NewRowRoot(content, "Row_Hint", null, 170f);
        TextMeshProUGUI hint = NewText("Hint", row,
            "其余游戏性设置项将在后续版本中加入\n（屏幕震动、伤害数字等）",
            HintFont, TextAlignmentOptions.Center, TextMuted);
        Stretch(hint.rectTransform, 0f, 0f, 0f, 0f);
    }

    private static void BuildGraphicsPage(RectTransform content)
    {
        SelectorRow(content, "Row_Resolution", SettingIds.Resolution, "分辨率");
        SelectorRow(content, "Row_WindowMode", SettingIds.WindowMode, "视窗模式");
        SelectorRow(content, "Row_FrameRate", SettingIds.FrameRate, "帧率上限");
        ToggleRow(content, "Row_VSync", SettingIds.VSync, "垂直同步");
        SelectorRow(content, "Row_Quality", SettingIds.Quality, "画质等级（预留）");
    }

    private static void BuildAudioPage(RectTransform content)
    {
        SliderRow(content, "Row_MasterVolume", SettingIds.MasterVolume, "全局音量");
        SliderRow(content, "Row_MusicVolume", SettingIds.MusicVolume, "音乐音量");
        SliderRow(content, "Row_SfxVolume", SettingIds.SfxVolume, "音效音量");
        SliderRow(content, "Row_UiVolume", SettingIds.UiVolume, "UI 音量");
    }

    /// <summary>
    /// 按键页（一级界面）。改键与设备选择都不在这里 —— 它们搬进了二级界面
    /// （见 BuildKeybindBindingsPage），这一页只留一个入口 + 四个顺手可改的鼠标项。
    ///
    /// 后面四项全是【鼠标专用】：它们的值只有鼠标（指向）才有意义，手柄方案下改了没作用，
    /// 所以 KeybindSettingsPage 会在手柄方案下把它们整行置灰（保留可见，不是隐藏）。
    /// </summary>
    private static void BuildKeybindPage(RectTransform content)
    {
        ButtonRow(content, "Row_OpenBindings", SettingIds.OpenBindings, "按键绑定", "更改按键绑定");
        ToggleRow(content, "Row_MouseInvertX", SettingIds.MouseInvertX, "鼠标水平反转");
        ToggleRow(content, "Row_MouseInvertY", SettingIds.MouseInvertY, "鼠标垂直反转");
        SliderRow(content, "Row_MouseSensitivity", SettingIds.MouseSensitivity, "鼠标灵敏度");
        SelectorRow(content, "Row_AttackTrigger", SettingIds.AttackTrigger, "攻击触发方式");
    }

    // ══════════════════ 按键绑定二级界面 ══════════════════

    /// <summary>Header 条的像素高度，同时也是 Viewport 的上边让位量。</summary>
    private const float HeaderHeight = 110f;

    /// <summary>
    /// 按键绑定二级界面。页签（键鼠 / 手柄）+ 手柄型号 + 两套按键行 + 按方案恢复默认。
    ///
    /// ══════════ 为什么两套行都在 prefab 里 ══════════
    /// 键鼠三行是双格（键盘格 + 鼠标格，两格共存），手柄三行是单格。
    /// 结构与列宽都不同，所以各摆一套、按页签 SetActive 切换 ——
    /// 比"一套行里藏半个格子"好：每套行都能按自己的列数摆整齐，
    /// 行控件也不必知道"自己现在有没有第二格"。
    /// VerticalLayoutGroup 会自动跳过未激活的行重排，所以不会留下空洞。
    /// </summary>
    private static void BuildKeybindBindingsPage(RectTransform contentArea)
    {
        RectTransform content = NewPage(contentArea, KeybindBindingsPage.PageNodeName,
            typeof(KeybindBindingsPage), false, HeaderHeight, out RectTransform page);

        BuildKeybindBindingsHeader(page);

        //键鼠方案：每行两格
        KeybindPairRow(content, "Row_Move_KM", SettingIds.Move, "移动");
        KeybindPairRow(content, "Row_Attack_KM", SettingIds.Attack, "攻击");
        KeybindPairRow(content, "Row_Jump_KM", SettingIds.Jump, "跳跃");

        //手柄方案：每行一格。默认收起 —— 存档里 inputDevice 的初值是 Keyboard（键鼠方案），
        //生成时就摆成与初值一致的样子，prefab 单看才是自洽的
        //（运行时 RefreshAll 每次都会按当前方案重设一遍，那是纠正玩家的切换，不是这里要依赖的）
        KeybindRow(content, "Row_Move_Pad", SettingIds.MoveGamepad, "移动").gameObject.SetActive(false);
        KeybindRow(content, "Row_Attack_Pad", SettingIds.AttackGamepad, "攻击").gameObject.SetActive(false);
        KeybindRow(content, "Row_Jump_Pad", SettingIds.JumpGamepad, "跳跃").gameObject.SetActive(false);

        ButtonRow(content, "Row_ResetScheme", SettingIds.ResetScheme, "恢复按键默认", "恢复当前方案默认");
    }

    /// <summary>
    /// 二级界面的顶部固定条。它是页根的直接子节点（与 Viewport 平级），
    /// 所以【不随内容滚动】—— 页签与返回按钮在滚动时一直可见。
    ///
    /// 名字是硬契约：KeybindBindingsPage 按 transform.Find 一层层找，
    /// 改名会让返回按钮与页签静默失效（它会在 Console 报错，这是特意的）。
    /// </summary>
    private static void BuildKeybindBindingsHeader(RectTransform page)
    {
        RectTransform header = NewRect("Header", page);
        //顶部定高条：横向铺满、纵向 110px，从上边往下长
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.anchoredPosition = Vector2.zero;
        header.sizeDelta = new Vector2(0f, HeaderHeight);

        //文字用 "< 返回" 而不是箭头 "←"：小于号在现有的选择器 Prev 按钮上已经验证过字体里有，
        //箭头（U+2190）没验证过，缺字会显示成方框
        Button back = NewButton("BackButton", header, "< 返回", ButtonFont, ButtonBg);
        SetAnchors((RectTransform)back.transform, 0f, 0.15f, 0.14f, 0.85f);

        NewSchemeTab("Tab_Keyboard", header, SettingsLabels.SchemeName(0), 0.16f, 0.31f);
        NewSchemeTab("Tab_Gamepad", header, SettingsLabels.SchemeName(1), 0.32f, 0.47f);

        //手柄型号组：整组只在手柄页签下可见，由 KeybindBindingsPage 控制显隐。
        //默认收起（与"默认方案是键鼠"一致）
        RectTransform modelGroup = NewRect("ModelGroup", header);
        SetAnchors(modelGroup, 0.50f, 0.15f, 0.98f, 0.85f);
        modelGroup.gameObject.SetActive(false);

        NewSchemeTab("ModelTab_PS", modelGroup, SettingsLabels.DeviceName(1), 0f, 0.49f);
        NewSchemeTab("ModelTab_Xbox", modelGroup, SettingsLabels.DeviceName(2), 0.51f, 1f);
    }

    /// <summary>
    /// 一个页签 / 型号按钮。与主选列表的分类按钮同一套做法
    /// 【NewButton + 子节点 Selected 作为选中指示物，SetAsFirstSibling 垫在文字下面】，
    /// 因为承载它的页面脚本就是这么找选中态的（见 KeybindBindingsPage.FindMarker）。
    /// </summary>
    private static void NewSchemeTab(string name, Transform parent, string label, float xMin, float xMax)
    {
        Button tab = NewButton(name, parent, label, ButtonFont, TabIdle);
        SetAnchors((RectTransform)tab.transform, xMin, 0.15f, xMax, 0.85f);
        AddSelectedMarker(tab);
    }

    /// <summary>
    /// 给按钮加一层选中指示物（一整块高亮条，而不是只改文字颜色 ——
    /// 以后接手柄导航时更显眼）。必须 SetAsFirstSibling：后加的节点会盖住按钮上的文字。
    /// </summary>
    private static GameObject AddSelectedMarker(Button button)
    {
        RectTransform marker = NewRect("Selected", button.transform);
        Stretch(marker, -6f, -6f, -6f, -6f);

        Image img = marker.gameObject.AddComponent<Image>();
        img.sprite = _uiSprite;
        img.type = Image.Type.Sliced;
        img.color = new Color(1f, 1f, 1f, 0.85f);
        img.raycastTarget = false;

        marker.SetAsFirstSibling();
        marker.gameObject.SetActive(false);
        return marker.gameObject;
    }

    // ══════════════════ 行的装配 ══════════════════

    /// <summary>
    /// 行根节点。给了 id 就顺带铺一层浅灰底（让玩家看得出"这一行"的范围）。
    /// 名字带上设置项名（Row_Resolution 这种）而不是统一叫 "Row"：
    /// SettingsPageBase 的"没绑定的行"警告只打节点名，重名会让那条警告失去指向。
    /// </summary>
    private static RectTransform NewRowRoot(Transform parent, string rowName, string id, float height)
    {
        RectTransform row = NewRect(rowName, parent);

        if (!string.IsNullOrEmpty(id))
        {
            Image bg = row.gameObject.AddComponent<Image>();
            bg.sprite = _uiSprite;
            bg.type = Image.Type.Sliced;
            bg.color = RowBg;
            bg.raycastTarget = true;
        }

        LayoutElement le = row.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
        return row;
    }

    /// <summary>
    /// 行标签。除了摆文字，还会按这一行的 settingId 给它挂上 LocalizedText。
    ///
    /// ══════════════ 为什么要显式传 settingId ══════════════
    /// 不能在这里用 row.GetComponent&lt;SettingRowBase&gt;() 去读 —— 各 Row 方法是在
    /// 【最后一步】才调 SetRowId 把行控件挂上去的，所以此刻组件还不存在。
    /// 调用方本来就持有 id，直接传进来最省事也最不容易错。
    ///
    /// ══════════════ Key 为什么由 settingId 派生 ══════════════
    /// 标签文字是生成时烘焙进 prefab 的字面量，运行时没有任何代码去设置它，
    /// 所以不挂 LocalizedText 的话，切换语言后面板自己的文字永远不变。
    /// 约定：ui_setting_ + settingId 去掉分类前缀。例：
    ///   graphics.resolution  → ui_setting_resolution
    ///   keybind.openBindings → ui_setting_openbindings
    /// 这样加新设置项时不用再手工登记 Key；配置里缺这一条只会回退成显示 Key 本身
    /// （见 LocalizationManager 的取词优先级），不会报错也不会空白。
    /// </summary>
    private static void NewLabel(RectTransform row, string text, float xMax, string settingId)
    {
        TextMeshProUGUI label = NewText("Label", row, text, LabelFont, TextAlignmentOptions.Left, TextDark);
        RectTransform rt = label.rectTransform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(xMax, 1f);
        rt.offsetMin = new Vector2(28f, 0f);
        rt.offsetMax = new Vector2(0f, 0f);

        if (!string.IsNullOrEmpty(settingId))
        {
            LocalizedText lt = label.gameObject.AddComponent<LocalizedText>();
            lt.localizationKey = MakeSettingLabelKey(settingId);
            lt.autoUpdateOnStart = true;
            lt.listenToLanguageChange = true;
        }
    }

    /// <summary>settingId → 标签的本地化 Key（见 NewLabel 的注释）。</summary>
    private static string MakeSettingLabelKey(string settingId)
    {
        int dot = settingId.IndexOf('.');
        string tail = (dot >= 0 && dot + 1 < settingId.Length) ? settingId.Substring(dot + 1) : settingId;
        return "ui_setting_" + tail.ToLowerInvariant();
    }

    private static void SliderRow(RectTransform content, string rowName, string id, string label)
    {
        RectTransform row = NewRowRoot(content, rowName, id, RowHeight);
        NewLabel(row, label, 0.42f, id);

        RectTransform sliderRt = NewRect("Slider", row);
        SetAnchors(sliderRt, 0.44f, 0.25f, 0.86f, 0.75f);

        Slider slider = sliderRt.gameObject.AddComponent<Slider>();
        slider.transition = Selectable.Transition.None;//拉条不该有按钮那套变色反馈，会跟填充色打架
        slider.direction = Slider.Direction.LeftToRight;

        //Fill 必须是滑块量程容器的子物体：Slider 拿 fillRect.parent 当量程容器来算填充比例
        RectTransform track = NewRect("Background", sliderRt);
        Stretch(track, 0f, 0f, 0f, 0f);
        Image trackImg = track.gameObject.AddComponent<Image>();
        trackImg.sprite = _uiSprite;
        trackImg.type = Image.Type.Sliced;
        trackImg.color = BarBg;

        RectTransform fill = NewRect("Fill", track);
        Stretch(fill, 0f, 0f, 0f, 0f);
        Image fillImg = fill.gameObject.AddComponent<Image>();
        fillImg.sprite = _uiSprite;
        fillImg.type = Image.Type.Sliced;
        fillImg.color = BarFill;
        fillImg.raycastTarget = false;

        slider.targetGraphic = trackImg;
        slider.fillRect = fill;
        slider.handleRect = null;//不摆拖动手柄：整条都能拖，少一个要对齐的节点
        //取值域不在这里设：各页的 OnBind 会用 BindSlider 的 min/max 调 Configure，
        //prefab 里写死反而会造成"prefab 显示 0~1、运行起来是 0~100"的困惑
        slider.minValue = 0f;
        slider.maxValue = 1f;

        TextMeshProUGUI value = NewText("ValueText", row, "0", ValueFont, TextAlignmentOptions.Right, TextDark);
        RectTransform vrt = value.rectTransform;
        vrt.anchorMin = new Vector2(0.87f, 0f);
        vrt.anchorMax = new Vector2(1f, 1f);
        vrt.offsetMin = Vector2.zero;
        vrt.offsetMax = new Vector2(-28f, 0f);

        SetRowId(row.gameObject.AddComponent<SettingRow_Slider>(), id);
    }

    private static void SelectorRow(RectTransform content, string rowName, string id, string label)
    {
        RectTransform row = NewRowRoot(content, rowName, id, RowHeight);
        NewLabel(row, label, 0.42f, id);

        Button prev = NewButton("Prev", row, "<", ButtonFont, ButtonBg);
        SetAnchors((RectTransform)prev.transform, 0.44f, 0.22f, 0.52f, 0.78f);

        TextMeshProUGUI value = NewText("ValueText", row, "-", ValueFont, TextAlignmentOptions.Center, TextDark);
        SetAnchors(value.rectTransform, 0.53f, 0f, 0.86f, 1f);

        Button next = NewButton("Next", row, ">", ButtonFont, ButtonBg);
        SetAnchors((RectTransform)next.transform, 0.88f, 0.22f, 0.96f, 0.78f);

        SetRowId(row.gameObject.AddComponent<SettingRow_Selector>(), id);
    }

    private static void ToggleRow(RectTransform content, string rowName, string id, string label)
    {
        RectTransform row = NewRowRoot(content, rowName, id, RowHeight);
        NewLabel(row, label, 0.70f, id);

        RectTransform toggleRt = NewRect("Toggle", row);
        //靠右定尺寸摆放：用比例锚点会把它拉成扁长条，勾选框该是方的
        toggleRt.anchorMin = new Vector2(1f, 0.5f);
        toggleRt.anchorMax = new Vector2(1f, 0.5f);
        toggleRt.pivot = new Vector2(1f, 0.5f);
        toggleRt.anchoredPosition = new Vector2(-28f, 0f);
        toggleRt.sizeDelta = new Vector2(64f, 64f);

        Toggle toggle = toggleRt.gameObject.AddComponent<Toggle>();

        RectTransform box = NewRect("Background", toggleRt);
        Stretch(box, 0f, 0f, 0f, 0f);
        Image boxImg = box.gameObject.AddComponent<Image>();
        boxImg.sprite = _uiSprite;
        boxImg.type = Image.Type.Sliced;
        boxImg.color = ButtonBg;

        RectTransform check = NewRect("Checkmark", box);
        Stretch(check, 10f, 10f, 10f, 10f);
        Image checkImg = check.gameObject.AddComponent<Image>();
        checkImg.sprite = _checkSprite;
        checkImg.color = BarFill;
        checkImg.raycastTarget = false;

        toggle.targetGraphic = boxImg;
        toggle.graphic = checkImg;//Toggle 显隐的是这个对象，接错就变成"勾了没反应"
        toggle.isOn = false;

        SetRowId(row.gameObject.AddComponent<SettingRow_Toggle>(), id);
    }

    /// <summary>
    /// 按钮行。buttonText 是这个按钮上写什么 —— 它不承载值，
    /// 所以"这一行是干什么的"全靠按钮文案说清楚（"更改按键绑定" / "恢复当前方案默认"）。
    /// </summary>
    private static void ButtonRow(RectTransform content, string rowName, string id, string label, string buttonText)
    {
        RectTransform row = NewRowRoot(content, rowName, id, RowHeight);
        NewLabel(row, label, 0.42f, id);

        Button button = NewButton("Button", row, buttonText, ButtonFont, ButtonBg);
        SetAnchors((RectTransform)button.transform, 0.46f, 0.12f, 0.78f, 0.88f);

        SetRowId(row.gameObject.AddComponent<SettingRow_Button>(), id);
    }

    /// <summary>
    /// 按键行（单格）。手柄方案用这个。
    ///
    /// KeyText 必须是【行的直接子节点】，不能塞进 KeyButton 里 ——
    /// SettingRow_Keybind 用 transform.Find("KeyText") 找它，只查一层。
    /// 所以这里是"两个同级节点叠在同一块区域"：下面的 KeyButton 吃点击，
    /// 上面的 KeyText 只显示键名（raycastTarget 已关，不会挡住按钮）。
    /// </summary>
    private static RectTransform KeybindRow(RectTransform content, string rowName, string id, string label)
    {
        RectTransform row = NewRowRoot(content, rowName, id, RowHeight);
        NewLabel(row, label, 0.30f, id);

        //按钮不带自己的文字：键名由 KeyText 显示（它要能被页面动态改写）
        Button key = NewButton("KeyButton", row, null, ButtonFont, ButtonBg);
        SetAnchors((RectTransform)key.transform, 0.32f, 0.12f, 0.66f, 0.88f);

        TextMeshProUGUI keyText = NewText("KeyText", row, "未绑定", ButtonFont,
            TextAlignmentOptions.Center, TextDark);
        //与 KeyButton 完全重合，且在层级里靠后 → 画在按钮上面
        SetAnchors(keyText.rectTransform, 0.32f, 0.12f, 0.66f, 0.88f);

        Button reset = NewButton("ResetButton", row, "重置", ButtonFont, ButtonBg);
        SetAnchors((RectTransform)reset.transform, 0.68f, 0.12f, 0.82f, 0.88f);

        SetRowId(row.gameObject.AddComponent<SettingRow_Keybind>(), id);
        return row;
    }

    /// <summary>
    /// 按键行（双格）。键鼠方案用这个：左格键盘、右格鼠标，两格【共存同时生效】
    /// （点鼠标左键和按 J 都能攻击），所以它们是两个平行的格子，不是"键盘或鼠标"二选一。
    ///
    /// 右侧是个更窄的"重置"——它只重置这一行（两格一起），
    /// 整页的"恢复当前方案默认"是下面单独的一行按钮。
    ///
    /// 每一格同样是"按钮 + 与它完全重合的文字节点"两个同级节点：
    /// 按钮吃点击、文字显示键名（SettingRow_Keybind 只查一层，名字是硬契约）。
    /// </summary>
    private static void KeybindPairRow(RectTransform content, string rowName, string id, string label)
    {
        RectTransform row = NewRowRoot(content, rowName, id, RowHeight);
        NewLabel(row, label, 0.27f, id);

        //左格：键盘
        AddKeySlot(row, "KeyButton", "KeyText", 0.29f, 0.51f);

        //右格：鼠标
        AddKeySlot(row, "MouseButton", "MouseText", 0.53f, 0.75f);

        Button reset = NewButton("ResetButton", row, "重置", ButtonFont, ButtonBg);
        SetAnchors((RectTransform)reset.transform, 0.81f, 0.12f, 0.93f, 0.88f);

        SetRowId(row.gameObject.AddComponent<SettingRow_Keybind>(), id);
    }

    /// <summary>
    /// 按键格里的一对节点：吃点击的按钮 + 显示键名的文字。
    /// 两者锚点完全相同 —— 文字在层级里更靠后，所以画在按钮上面。
    /// </summary>
    private static void AddKeySlot(RectTransform row, string buttonName, string textName,
        float xMin, float xMax)
    {
        Button button = NewButton(buttonName, row, null, ButtonFont, ButtonBg);
        SetAnchors((RectTransform)button.transform, xMin, 0.12f, xMax, 0.88f);

        TextMeshProUGUI text = NewText(textName, row, "未绑定", ButtonFont,
            TextAlignmentOptions.Center, TextDark);
        SetAnchors(text.rectTransform, xMin, 0.12f, xMax, 0.88f);
    }

    /// <summary>给行控件填 settingId。集中在一处，避免二十来个 AddComponent 各自漏填。</summary>
    private static void SetRowId(SettingRowBase row, string id)
    {
        row.settingId = id;
    }

    // ══════════════════ 底部按钮条 ══════════════════

    private static void BuildBottomBar(Transform parent)
    {
        RectTransform bar = NewRect("BottomBar", parent);
        SetAnchors(bar, 0.26f, 0.012f, 0.74f, 0.14f);

        HorizontalLayoutGroup layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 4, 4);
        layout.spacing = 60f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        NewButton("ApplyBtn", bar, "应用", ButtonFont, ButtonBg);
        NewButton("ResetBtn", bar, "恢复默认", ButtonFont, ButtonBg);
        NewButton("ReturnBtn", bar, "返回", ButtonFont, ButtonBg);
    }

    // ══════════════════ 返回确认浮层 ══════════════════

    /// <summary>对话框的像素尺寸。只有 Box（背板）用它，里面的控件用 InDialog 换算锚点。</summary>
    private const float DialogWidth = 900f;
    private const float DialogHeight = 400f;

    /// <summary>参考分辨率，InDialog 的换算基准；与 CanvasScaler 保持一致。</summary>
    private const float CanvasWidth = 1920f;
    private const float CanvasHeight = 1080f;

    /// <summary>
    /// 返回确认浮层。
    ///
    /// ⚠ 结构与别处不同：ConfirmText 与三个按钮必须是 ConfirmGroup 的【直接子节点】。
    ///   SettingPanel.FindConfirmGroup 是一层查找（ConfirmGroup/ConfirmApplyBtn ...），
    ///   套一层 Box 的话它会一条条报"找不到 ConfirmApplyBtn"，然后返回时既不询问也不提示。
    ///   第一版就是把按钮摆在 Box 里，运行时连报三条 ERROR。
    ///   所以这里 Box 只当【背板】（一个居中 900×400 的白底图），控件全是它的兄弟节点、
    ///   靠 InDialog 摆到与背板重合的位置上，且在层级里排在 Box 之后 → 画在背板上面。
    /// </summary>
    private static void BuildConfirmGroup(Transform parent)
    {
        RectTransform group = NewRect("ConfirmGroup", parent);
        Stretch(group, 0f, 0f, 0f, 0f);

        Image dim = group.gameObject.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);
        //遮罩必须挡射线：否则浮层弹出时玩家还能点到底下的设置项
        dim.raycastTarget = true;

        RectTransform box = NewRect("Box", group);
        box.anchorMin = new Vector2(0.5f, 0.5f);
        box.anchorMax = new Vector2(0.5f, 0.5f);
        box.pivot = new Vector2(0.5f, 0.5f);
        box.anchoredPosition = Vector2.zero;
        box.sizeDelta = new Vector2(DialogWidth, DialogHeight);

        Image boxImg = box.gameObject.AddComponent<Image>();
        boxImg.sprite = _bgSprite;
        boxImg.type = Image.Type.Sliced;
        boxImg.color = Color.white;

        //三个按钮的名字是 SettingPanel 的约定（它按名字 AddListener）；ConfirmText 只是给人看的
        TextMeshProUGUI text = NewText("ConfirmText", group, "有未应用的设置改动，要保存吗？",
            HintFont, TextAlignmentOptions.Center, TextDark);
        InDialog(text.rectTransform, 0.06f, 0.55f, 0.94f, 0.90f);

        ConfirmButton("ConfirmApplyBtn", group, "应用并退出", 0.05f, 0.33f);
        ConfirmButton("ConfirmDiscardBtn", group, "放弃改动", 0.36f, 0.64f);
        ConfirmButton("ConfirmCancelBtn", group, "取消", 0.67f, 0.95f);

        group.gameObject.SetActive(false);
    }

    private static void ConfirmButton(string name, RectTransform group, string label, float xMin, float xMax)
    {
        Button button = NewButton(name, group, label, ButtonFont, ButtonBg);
        InDialog((RectTransform)button.transform, xMin, 0.10f, xMax, 0.42f);
    }

    /// <summary>
    /// 把一个"按对话框内部比例"写的锚点换算到全屏 ConfirmGroup 的锚点上。
    /// 存在的意义是让人按 0~1 的对话框坐标思考，而不用手算 12 个 0.29xx 这样的魔法数字
    /// （第一版就是硬算的，改了对话框尺寸全得重算）。
    /// </summary>
    private static void InDialog(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
    {
        rt.anchorMin = DialogToGroup(xMin, yMin);
        rt.anchorMax = DialogToGroup(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>对话框内 0~1 坐标 → ConfirmGroup 全屏 0~1 坐标。中心对齐，按像素比缩放。</summary>
    private static Vector2 DialogToGroup(float x, float y)
    {
        return new Vector2(
            0.5f + (x - 0.5f) * (DialogWidth / CanvasWidth),
            0.5f + (y - 0.5f) * (DialogHeight / CanvasHeight));
    }

    // ══════════════════ 行模板 prefab ══════════════════

    /// <summary>
    /// 生成 5 个行模板。它们不进 Resources：面板里的行已经是复制体，
    /// 运行时不需要加载这些模板，放进 Resources 只会白白打进包体。
    /// （UIManager.ValidatePanelPrefabs 会遍历 PanelType 校验 UIPanels/{枚举名}，
    ///   所以 Resources 下只需要 SettingPanel 本身。）
    ///
    /// 模板里的 settingId 填成"类型名"这样的占位值而不是空串：空串会触发
    /// SettingRowBase.OnValidate 的警告（每次打开工程刷一遍），占位值则只会在
    /// "有人把它拖进页面却忘了改成真 id"时由页面的校验报出来，正好是它该报的地方。
    /// </summary>
    private static void BuildRowTemplates()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI")) AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        if (!AssetDatabase.IsValidFolder(RowTemplateDir)) AssetDatabase.CreateFolder("Assets/Prefabs/UI", "SettingRows");

        SaveTemplate("SettingRow_Slider", r => SliderRow(r, "SettingRow_Slider", "SettingRow_Slider", "标签"));
        SaveTemplate("SettingRow_Selector", r => SelectorRow(r, "SettingRow_Selector", "SettingRow_Selector", "标签"));
        SaveTemplate("SettingRow_Toggle", r => ToggleRow(r, "SettingRow_Toggle", "SettingRow_Toggle", "标签"));
        SaveTemplate("SettingRow_Button", r => ButtonRow(r, "SettingRow_Button", "SettingRow_Button", "标签", "点击"));
        //按键行的模板给【双格】版的：它是这个行类型的超集（单格版就是少了 MouseButton/MouseText），
        //模板的用途是"记录这一行必须有哪些子节点"，超集才记得全
        SaveTemplate("SettingRow_Keybind", r => KeybindPairRow(r, "SettingRow_Keybind", "SettingRow_Keybind", "标签"));
    }

    private static void SaveTemplate(string fileName, Action<RectTransform> build)
    {
        Scene preview = EditorSceneManager.NewPreviewScene();
        //父节点是必需的：行的宽度由父容器给（LayoutGroup 撑满），
        //不套一层就存出来的模板在资源视图里是个宽度为 0 的怪东西，拖进页面也会先错一下
        RectTransform holder = NewRect(fileName, null);
        try
        {
            SceneManager.MoveGameObjectToScene(holder.gameObject, preview);
            holder.sizeDelta = new Vector2(1400f, RowHeight);

            build(holder);

            PrefabUtility.SaveAsPrefabAsset(holder.gameObject, RowTemplateDir + "/" + fileName + ".prefab");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(holder.gameObject);
            EditorSceneManager.ClosePreviewScene(preview);
        }
    }

    // ══════════════════ 基础构件 ══════════════════

    private static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        if (parent != null) rt.SetParent(parent, false);
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        return rt;
    }

    /// <summary>锚点按比例给定，四周不留边。</summary>
    private static void SetAnchors(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
    {
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>撑满父容器，四边各留一段像素边距。</summary>
    private static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }

    private static TextMeshProUGUI NewText(string name, Transform parent, string content,
        float fontSize, TextAlignmentOptions align, Color color)
    {
        RectTransform rt = NewRect(name, parent);
        TextMeshProUGUI text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = _font;
        text.fontSharedMaterial = _font.material;//显式给材质：只设 font 在部分版本里不跟着换材质
        text.text = content;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = align;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;//文字一律不吃射线，免得挡住它下面的按钮或拉条
        return text;
    }

    /// <summary>切片精灵 + Button + 居中文案。label 传 null 就只要一个空壳按钮（按键行用）。</summary>
    private static Button NewButton(string name, Transform parent, string label, float fontSize, Color color)
    {
        RectTransform rt = NewRect(name, parent);

        Image image = rt.gameObject.AddComponent<Image>();
        image.sprite = _uiSprite;
        image.type = Image.Type.Sliced;
        image.color = color;

        Button button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;//显式指定：靠自动关联会随版本变化

        if (label != null)
        {
            TextMeshProUGUI text = NewText("Label", rt, label, fontSize, TextAlignmentOptions.Center, TextDark);
            Stretch(text.rectTransform, 6f, 0f, 6f, 0f);
        }

        return button;
    }
}
