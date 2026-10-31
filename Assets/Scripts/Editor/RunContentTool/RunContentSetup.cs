#if UNITY_EDITOR
using Chaos.Gameplay.Interaction;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RunContentSetup
{
    static RunContentText T(string cn, string en, string tw, string jp, string kr) =>
        new RunContentText { zhCN = cn, enUS = en, zhTW = tw, jaJP = jp, koKR = kr };
    static RunContentReward R(uint id, uint min, uint max) => new RunContentReward { itemId = id, minCount = min, maxCount = max };
    public static string Apply()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Edit mode only");
        const string directory = "Assets/Resources/Interaction";
        if (!AssetDatabase.IsValidFolder(directory)) AssetDatabase.CreateFolder("Assets/Resources", "Interaction");
        var catalog = AssetDatabase.LoadAssetAtPath<RunContentCatalog>(directory + "/RunContentCatalog.asset");
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<RunContentCatalog>(); catalog.gatherableCount = 2;
            catalog.definitions = new[] {
                new RunContentDefinition { id="traveller",kind=RunContentKind.Npc,
                    name=T("行脚医者","Travelling Healer","行腳醫者","旅の医師","떠돌이 의원"),
                    body=T("乱世行路不易。我还有些补给，你可以从中选一份。药材可用于疗伤，干粮能暂时提高生命上限。保重！", "These roads are dangerous. Take one of my supplies: medicine for wounds, or rations for a temporary health boost. Travel safely!", "亂世行路不易。我還有些補給，你可以從中選一份。藥材可用於療傷，乾糧能暫時提高生命上限。保重！", "危険な旅路だ。薬か携行食を一つ選んでくれ。薬は治療に、携行食は一時的な体力強化に使える。気をつけて。", "험한 길이군요. 약재나 식량 중 하나를 가져가세요. 약재는 치료에, 식량은 일시적인 최대 체력 증가에 쓰입니다. 조심하세요!"), rewards=new[]{R(1,1,1),R(5,1,1)} },
                new RunContentDefinition { id="chest",kind=RunContentKind.Chest,
                    name=T("遗留补给箱","Abandoned Supply Chest","遺留補給箱","残された補給箱","남겨진 보급 상자"),
                    body=T("箱中留有药材、矿料和干粮。可以分别领取；背包装不下时，未领取的物品会留在箱中。", "Medicine, ore and rations remain inside. Claim each separately; unclaimed items stay here if your bag is full.", "箱中留有藥材、礦料和乾糧。可以分別領取；背包裝不下時，未領取的物品會留在箱中。", "薬、鉱石、携行食が入っている。一つずつ受け取れる。鞄が満杯なら未取得の品は箱に残る。", "약재, 광석, 식량이 들어 있습니다. 따로 받을 수 있으며 가방이 가득 차면 남은 물품은 상자에 보관됩니다."), rewards=new[]{R(1,1,2),R(7,2,4),R(5,1,1)} },
                new RunContentDefinition { id="herbs",kind=RunContentKind.Gatherable,
                    name=T("野生药草","Wild Herbs","野生藥草","野生の薬草","야생 약초"),
                    body=T("可采集少量药材。采集成功后本处药草耗尽，背包装不下时可以稍后再来。", "Gather a little medicine. This patch is exhausted after collection. Return later if your bag is full.", "可採集少量藥材。採集成功後本處藥草耗盡，背包裝不下時可以稍後再來。", "少量の薬を採取できる。成功すると採り尽くされる。鞄が満杯なら後で戻ろう。", "소량의 약재를 채집할 수 있습니다. 채집하면 고갈됩니다. 가방이 가득 차면 나중에 돌아오세요."), rewards=new[]{R(1,1,3)} },
                new RunContentDefinition { id="ore",kind=RunContentKind.Gatherable,
                    name=T("露出矿料","Exposed Ore","露出礦料","露出した鉱石","드러난 광석"),
                    body=T("可拾取用于局外养成的矿料。采集后放入局内背包，需要通过原有结算带出。", "Collect material for village development. It goes into your run bag and must be selected for extraction at settlement.", "可拾取用於局外養成的礦料。採集後放入局內背包，需要通過原有結算帶出。", "村の発展に使う素材を拾える。探索鞄に入り、精算時に持ち帰る必要がある。", "마을 성장에 쓰는 재료를 모읍니다. 원정 가방에 들어가며 정산 때 반출해야 합니다."), rewards=new[]{R(7,2,4)} }
            };
            AssetDatabase.CreateAsset(catalog, directory + "/RunContentCatalog.asset");
        }
        var visuals = AssetDatabase.LoadAssetAtPath<RunContentVisualSettings>(directory + "/RunContentVisualSettings.asset");
        if (visuals == null)
        {
            visuals = ScriptableObject.CreateInstance<RunContentVisualSettings>();
            var source = Resources.Load<GameObject>("Player/Ally").GetComponentInChildren<SkeletonAnimation>(true);
            visuals.npcSkeleton = source.skeletonDataAsset; visuals.npcSkin = source.initialSkinName;
            visuals.closeText=T("返回","Back","返回","戻る","돌아가기");
            visuals.talkText=T("交谈","Talk","交談","話す","대화");
            visuals.chooseText=T("选择补给","Choose","選擇補給","選ぶ","보급 선택");
            visuals.claimText=T("领取","Claim","領取","受け取る","받기");
            visuals.harvestText=T("采集","Gather","採集","採取","채집");
            visuals.claimedText=T("已领取","Claimed","已領取","取得済み","수령 완료");
            visuals.completedText=T("已结束","Unavailable","已結束","終了","종료");
            visuals.receivedText=T("物品已加入局内背包。","Added to your run bag.","物品已加入局內背包。","探索鞄に入れた。","원정 가방에 추가했습니다.");
            visuals.npcHint=T("每位医者提供一份补给，选择后不能更换。","Choose one supply from this healer.","每位醫者提供一份補給，選擇後不能更換。","この医師から補給を一つ選べる。","의원마다 보급품 하나를 선택할 수 있습니다.");
            visuals.lootHint=T("未领内容仅在本图保留；切图不自动带出，请及时整理背包。","Unclaimed contents remain on this map only. Claim them before moving on.","未領內容僅在本圖保留；切圖不自動帶出，請及時整理背包。","未取得の品はこの地図にのみ残る。移動前に鞄を整理して受け取ろう。","미수령 물품은 현재 지도에만 남습니다. 다음 지도로 가기 전에 받으세요.");
            AssetDatabase.CreateAsset(visuals, directory + "/RunContentVisualSettings.asset");
        }
        const string prefabPath="Assets/Resources/UIPanels/RunContentPanel.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)==null)
        {
            var original=SceneManager.GetActiveScene(); var temporary=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            SceneManager.SetActiveScene(temporary);
            try
            {
                var go=new GameObject("RunContentPanel",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(UIPanelAnimator),typeof(RunContentPanel));
                var rect=go.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                go.GetComponent<Image>().color=new Color(0,0,0,.72f); PrefabUtility.SaveAsPrefabAsset(go,prefabPath);
            }
            finally {SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(temporary,true);}
        }
        AssetDatabase.SaveAssets();return "Content catalog, replaceable Spine settings and formal panel prefab ready";
    }
}
#endif
