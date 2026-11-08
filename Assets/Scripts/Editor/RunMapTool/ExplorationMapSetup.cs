#if UNITY_EDITOR
using System.Linq;
using LocalizationSystem;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>创建实际设置行与输入源动作；可重复执行，不重置用户改键或其他设置。</summary>
public static class ExplorationMapSetup
{
    [MenuItem("Tools/RunMap/配置探索地图攀梯界面")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new System.InvalidOperationException("需要编辑模式");
        const string path="Assets/Resources/UIPanels/SettingPanel.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var rows=root.GetComponentsInChildren<SettingRow_Selector>(true);
            if(!rows.Any(r=>r.settingId==SettingIds.LadderEntryMode))
            {
                var source=rows.Single(r=>r.settingId==SettingIds.ItemWheelMode);
                var clone=Object.Instantiate(source.gameObject,source.transform.parent);
                clone.name="Row_LadderEntryMode";clone.transform.SetSiblingIndex(source.transform.GetSiblingIndex()+1);
                clone.GetComponent<SettingRow_Selector>().settingId=SettingIds.LadderEntryMode;
                foreach(var local in clone.GetComponentsInChildren<LocalizedText>(true))Object.DestroyImmediate(local);
                clone.transform.Find("Label").GetComponent<TextMeshProUGUI>().text="梯子进入方式";
                string[] names={"自动爬梯（上下）","手动爬梯（交互键）"};var options=clone.transform.Find("Options");
                for(int i=0;i<2;i++)options.GetChild(i).GetComponent<TextMeshProUGUI>().text=names[i];
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        const string inputPath="Assets/Resources/Input/ChaosInputActions.inputactions";
        var sourceAsset=AssetDatabase.LoadAssetAtPath<InputActionAsset>(inputPath);
        var input=InputActionAsset.FromJson(sourceAsset.ToJson());
        try
        {
            var map=input.FindActionMap(InputManager.MapName);bool changed=false;
            foreach(var pair in new[]{new[]{InputManager.ActionClimbUp,"w","up"},new[]{InputManager.ActionClimbDown,"s","down"}})
            {
                if(map.FindAction(pair[0],false)!=null)continue;
                var action=map.AddAction(pair[0],InputActionType.Value);action.AddBinding("<Keyboard>/"+pair[1],groups:InputManager.GroupKeyboard);
                action.AddBinding("<Gamepad>/leftStick/"+pair[2],groups:InputManager.GroupGamepad);changed=true;
            }
            if(changed)System.IO.File.WriteAllText(inputPath,input.ToJson());
        }
        finally{Object.DestroyImmediate(input);}
        AssetDatabase.Refresh();
    }
}
#endif
