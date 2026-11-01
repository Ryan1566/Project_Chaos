using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Adds only the three civic anchors; leaves saved terrain and production plots intact.</summary>
public static class VillageFacilitiesSetup
{
    [MenuItem("Tools/Village/Install Civic Facilities")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before installing facilities");
        const string assetPath="Assets/Resources/Village/VillageFacilitySettings.asset";
        if(!AssetDatabase.IsValidFolder("Assets/Resources/Village"))AssetDatabase.CreateFolder("Assets/Resources","Village");
        var settings=AssetDatabase.LoadAssetAtPath<VillageFacilitySettings>(assetPath);
        if(settings==null)
        {
            settings=ScriptableObject.CreateInstance<VillageFacilitySettings>();
            settings.facilities=new[] {
                Definition("office",VillageFacilityKind.Office,-39,new Color(.62f,.52f,.35f),Text("办事单位","Administrative Office","辦事單位","執務所","집무소")),
                Definition("family",VillageFacilityKind.FamilyMansion,0,new Color(.55f,.4f,.32f),Text("家族府邸","Family Residence","家族府邸","一族の屋敷","가문 저택")),
                Definition("warehouse",VillageFacilityKind.Warehouse,39,new Color(.42f,.51f,.4f),Text("仓库","Warehouse","倉庫","倉庫","창고")) };
            settings.officeTitle=settings.facilities[0].name;
            settings.officeBody=Text("在此处理任务与事件、进行交易，查看税收说明。任务与事件沿用现有流程；税收规则尚未开放。","Handle existing tasks and events, trade, and view tax information. Tax rules are not available yet.","在此處理任務與事件、進行交易，查看稅收說明。任務與事件沿用現有流程；稅收規則尚未開放。","任務と出来事、取引、徴税の案内を扱います。徴税制度はまだ利用できません。","기존 임무와 사건, 거래 및 세금 안내를 이용합니다. 세금 제도는 아직 이용할 수 없습니다.");
            settings.tasksLabel=Text("任务与事件","Tasks & Events","任務與事件","任務と出来事","임무와 사건");
            settings.tradeLabel=Text("交易","Trade","交易","取引","거래");
            settings.taxLabel=Text("税收说明","Tax Information","稅收說明","徴税の案内","세금 안내");
            settings.backLabel=Text("返回","Back","返回","戻る","뒤로");
            settings.taxTitle=settings.taxLabel;
            settings.taxBody=Text("税收制度将在后续设计。本页仅说明预留用途，不征税、不发放收益，也不改变资源。","Tax rules will be designed later. This information page collects no taxes, grants no income and changes no resources.","稅收制度將在後續設計。本頁僅說明預留用途，不徵稅、不發放收益，也不改變資源。","徴税制度は今後設計されます。この案内では徴税や収入の付与、資源の変更は行いません。","세금 제도는 추후 설계됩니다. 이 안내에서는 세금을 걷거나 수입을 지급하거나 자원을 변경하지 않습니다.");
            settings.warehouseTitle=settings.facilities[2].name;
            settings.permanentLabel=Text("永久收藏","Permanent Collection","永久收藏","永久所持","영구 소장");
            settings.missingSession=Text("尚未载入存档","No save loaded","尚未載入存檔","セーブ未読込","저장 데이터 없음");
            settings.interactHint=Text("{0}　按 {1} 进入","{0}   Press {1} to enter","{0}　按 {1} 進入","{0}　{1} で入る","{0}   {1} 키로 입장");
            AssetDatabase.CreateAsset(settings,assetPath);AssetDatabase.SaveAssets();
        }
        if(settings.tasksEmptyBody==null||string.IsNullOrWhiteSpace(settings.tasksEmptyBody.zhCN))
        {
            settings.tasksEmptyBody=Text("当前没有待处理事件。事件仍按既有条件与返乡流程出现；新的任务规则将在后续设计。","There are no pending events. Events keep their existing conditions and return-to-village flow; new task rules will be designed later.","目前沒有待處理事件。事件仍按既有條件與返鄉流程出現；新的任務規則將在後續設計。","現在、未処理の出来事はありません。既存の条件と帰還時の流れを使用します。新しい任務制度は今後設計されます。","현재 처리할 사건이 없습니다. 사건은 기존 조건과 귀환 절차를 따르며, 새로운 임무 규칙은 추후 설계됩니다.");
            EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
        }
        InstallAnchors(settings);
        const string prefabPath="Assets/Resources/UIPanels/VillageOfficePanel.prefab";
        if(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)==null)
        {
            var original=SceneManager.GetActiveScene();var temporary=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(temporary);
                var panel=new GameObject("VillageOfficePanel",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(UIPanelAnimator),typeof(VillageOfficePanel));
                var rect=(RectTransform)panel.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
                PrefabUtility.SaveAsPrefabAsset(panel,prefabPath);
            }
            finally {if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(temporary,true);}
        }
    }
    public static void InstallAnchors(VillageFacilitySettings settings)
    {
        var original=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath("Assets/Scenes/MainScene.unity");
        bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity",OpenSceneMode.Additive);
        try
        {
            if(scene.isDirty)throw new InvalidOperationException("MainScene has unsaved edits; review them before installing facilities");
            var root=scene.GetRootGameObjects().Select(g=>g.GetComponent<VillageSceneBootstrap>()).Single(g=>g!=null);
            var village=VillageSceneSettings.Load();settings.Validate(village,root.GetComponentsInChildren<VillagePlot>(true));
            foreach(var definition in settings.facilities)
            {
                var anchor=root.GetComponentsInChildren<VillageFacility>(true).SingleOrDefault(f=>f.Id==definition.id);
                if(anchor==null){var go=new GameObject("VillageFacility_"+definition.id);go.transform.SetParent(root.transform,false);anchor=go.AddComponent<VillageFacility>();}
                anchor.Id=definition.id;anchor.Kind=definition.kind;anchor.transform.position=new Vector3(definition.x,village.groundY+.03f,0);
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        finally {if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
    static VillageFacilityDefinition Definition(string id,VillageFacilityKind kind,float x,Color color,VillageFacilityText name)
    {return new VillageFacilityDefinition {id=id,kind=kind,x=x,width=6,height=4,color=color,name=name,description=name};}
    static VillageFacilityText Text(string cn,string en,string tw,string ja,string ko)
    {return new VillageFacilityText {zhCN=cn,enUS=en,zhTW=tw,jaJP=ja,koKR=ko};}
}
