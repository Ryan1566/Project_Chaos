#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using Chaos.Gameplay.Interaction;
using RunMap;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Chaos.MVP.Model.Gameplay;
using TMPro;

public static class RunContentVerification
{
    public static string Plans(int samples=100)
    {
        var report=new StringBuilder();int count=0;
        for(int stage=1;stage<=3;stage++)
        {
            var p=NaturalMapRuntime.LoadParameters(stage,false);var structures=NaturalMapRuntime.LoadStructures(p);
            for(uint seed=0;seed<samples;seed++)
            {
                var job=new NaturalMapBuild(seed,stage,p,structures,NaturalMapRuntime.CaptureActorProfile());var map=job.Generate();
                if(map==null)throw new Exception(job.Error);
                if(NaturalMapValidation.Validate(map).Count!=0)throw new Exception("invalid plan "+seed);
                if(map.Placements.Count(x=>x.Kind==MapPlacementKind.Npc)!=p.npcCount
                    || map.Placements.Count(x=>x.Kind==MapPlacementKind.Interactive)!=p.interactiveCount
                    || map.Placements.Count(x=>x.Kind==MapPlacementKind.Gatherable)!=p.gatherableCount)throw new Exception("missing content "+stage+"/"+seed);
                count++;
            }
            report.AppendLine("PASS stage "+stage+" plans "+samples+": reachable supported disjoint content inside bounds");
        }
        report.AppendLine("PASS total plans "+count);Write("plans.txt",report.ToString());return report.ToString();
    }
    public static string Configuration()
    {
        var catalog=RunContentCatalog.Load();catalog.Validate();int checks=0;
        foreach(var d in catalog.definitions)
        {
            if(string.IsNullOrEmpty(d.name.enUS)||string.IsNullOrEmpty(d.name.jaJP)||string.IsNullOrEmpty(d.name.koKR)||string.IsNullOrEmpty(d.name.zhTW))throw new Exception("missing locale "+d.id);
            checks++;
        }
        var scene=SceneManager.GetActiveScene();var temp=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(temp);
        try
        {
            var p=NaturalMapRuntime.LoadParameters(1,false);var build=new NaturalMapBuild(17,1,p,NaturalMapRuntime.LoadStructures(p));var plan=build.Generate();
            var go=new GameObject("ContentSnapshotProbe");var target=go.AddComponent<RunContentInteractable>();
            var original=catalog.Get("chest");target.ConfigureContent("test/chest",original,plan);var copy=target.Definition;
            copy.kind=RunContentKind.Npc;copy.rewards[0].itemId=999999;
            if(target.Definition.kind!=RunContentKind.Chest||target.Definition.rewards[0].itemId==999999)throw new Exception("definition reference escaped");checks++;
            target.ConfigureContent("test/chest",original,plan);
            foreach(var mutate in new Action<RunContentDefinition>[] {d=>d.rewards[0].minCount=0,d=>d.rewards[0].itemId=999999,d=>d.kind=(RunContentKind)99,d=>d.rewards[0].maxCount=100,d=>{d.rewards[0].itemId=11;d.rewards[0].maxCount=2;}})
            {
                var bad=JsonUtility.FromJson<RunContentDefinition>(JsonUtility.ToJson(original));mutate(bad);bool failed=false;
                try{RunContentCatalog.ValidateDefinition(bad);}catch(InvalidOperationException){failed=true;}
                if(!failed)throw new Exception("bad definition accepted");checks++;
            }
        }
        finally{SceneManager.SetActiveScene(scene);EditorSceneManager.CloseScene(temp,true);}
        string result="PASS content configuration/snapshot checks "+checks;Write("configuration.txt",result);return result;
    }
    public static string View()
    {
        if(!Application.isPlaying)throw new Exception("Play mode fixture");
        var root=new GameObject("RunContentOwnedViewProbe",typeof(Canvas),typeof(CanvasScaler));
        root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        try
        {
            var view=root.AddComponent<RunContentView>();int intentCount=0,last=-1,closeCount=0;
            view.ClaimRequested+=i=>{intentCount++;last=i;};view.CloseRequested+=()=>closeCount++;
            var state=new RunContentViewState {Title="交互标题",Body="A wrapped content description. "+new string('文',75),Hint="提示",CloseText="Back",
                Choices=Enumerable.Range(0,8).Select(i=>new RunContentChoiceState{Index=7+i*7,Name="Content "+i,Detail="×2",Enabled=true,ButtonText="Claim"}).ToArray()};
            view.Render(state);Canvas.ForceUpdateCanvases();int objects=root.GetComponentsInChildren<Transform>(true).Length;
            for(int i=0;i<40;i++)view.Render(state);Canvas.ForceUpdateCanvases();
            if(intentCount!=0||objects!=root.GetComponentsInChildren<Transform>(true).Length)throw new Exception("render emits or allocates rows");
            var button=root.GetComponentsInChildren<Button>(false).Single(x=>x.name=="Claim7");button.onClick.Invoke();
            if(intentCount!=1||last!=7)throw new Exception("sparse index or duplicate listener");
            root.GetComponentsInChildren<Button>(false).Single(x=>x.name=="Close").onClick.Invoke();if(closeCount!=1)throw new Exception("close intent");
            var card=root.transform.Find("ContentCard").GetComponent<RectTransform>();
            foreach(var name in new[]{"Title","Body","Hint"})
            {
                var label=root.GetComponentsInChildren<TextMeshProUGUI>(false).Single(x=>x.name==name);
                var area=(RectTransform)label.transform.parent;
                if(area.rect.width<=0||area.rect.height<=0||label.rectTransform.rect.width>area.rect.width+.1f)throw new Exception("text area overflow "+name);
            }
            var scroll=root.GetComponentInChildren<ScrollRect>();scroll.verticalNormalizedPosition=.3f;Canvas.ForceUpdateCanvases();view.Render(state);Canvas.ForceUpdateCanvases();
            if(Mathf.Abs(scroll.verticalNormalizedPosition-.3f)>.03f)throw new Exception("render resets scroll");
            view.Render(new RunContentViewState{Title="Empty",CloseText="Back",Choices=Array.Empty<RunContentChoiceState>()});
            if(root.GetComponentsInChildren<Button>(false).Any(x=>x.name.StartsWith("Claim")))throw new Exception("stale rows visible");
            string result="PASS passive view: 40 renders reuse objects, sparse index, single intents, nonzero fitted text, scroll preserved, empty state clears";Write("view.txt",result);return result;
        }
        finally{UnityEngine.Object.Destroy(root);}
    }
    static void Write(string file,string value){Directory.CreateDirectory(Application.dataPath+"/QA/RunContent");File.WriteAllText(Application.dataPath+"/QA/RunContent/"+file,value);}
}
#endif
