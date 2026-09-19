using System.Collections.Generic;
using ChaosDebug;
using UnityEditor;
using UnityEngine;

namespace UIPanelAnimatorTool.Editor
{
    /// <summary>
    /// 批量把 UIPanelAnimator 补到面板预制体上。
    ///
    /// 存在的意义：运行时自动补挂出来的组件不会被保存进预制体，它的 Inspector 参数
    /// 永远是 C# 字段的初始值，策划调不了。这个工具就是把预制体从"运行时默认值"变成"可配置"。
    /// 可重复执行，已有组件的会被跳过。
    /// </summary>
    public static class UIPanelAnimatorTool
    {
        private const string PanelPrefabFolder = "Assets/Resources/UIPanels";
        private const string MenuPath = "Tool/UI/为所有面板预制体添加动画组件";

        [MenuItem(MenuPath, false, 100)]
        private static void AddAnimatorToAllPanelPrefabs()
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PanelPrefabFolder });
            if (guids == null || guids.Length == 0)
            {
                EditorUtility.DisplayDialog("面板显隐动画",
                    $"在 {PanelPrefabFolder} 下没有找到任何预制体。", "好");
                return;
            }

            List<string> changed = new List<string>();
            List<string> skipped = new List<string>();

            AssetDatabase.StartAssetEditing();//批量修改期间暂停资源导入，快很多
            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    GameObject root = PrefabUtility.LoadPrefabContents(path);//在隔离场景里打开预制体
                    try
                    {
                        if (root.GetComponent<BasePanel>() == null)
                        {
                            skipped.Add($"{path}（没有 BasePanel 组件）");
                            continue;
                        }

                        if (root.GetComponent<UIPanelAnimator>() != null)
                        {
                            skipped.Add($"{path}（已有动画组件）");
                            continue;
                        }

                        //RequireComponent 会自动把 CanvasGroup 一起加上并保存进预制体
                        root.AddComponent<UIPanelAnimator>();
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        changed.Add(path);
                    }
                    finally
                    {
                        PrefabUtility.UnloadPrefabContents(root);//必须成对，否则隔离场景会泄漏
                    }
                }
            }
            finally
            {
                //必须在 finally 里收尾：中途抛异常会把 AssetDatabase 锁死，症状很难排查
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }

            ChaosLog.Success($"[UI面板工具] 已为 {changed.Count} 个面板预制体添加 UIPanelAnimator，跳过 {skipped.Count} 个");

            for (int i = 0; i < changed.Count; i++) ChaosLog.Info($"[UI面板工具] 已处理：{changed[i]}");
            for (int i = 0; i < skipped.Count; i++) ChaosLog.Info($"[UI面板工具] 跳过：{skipped[i]}");
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateAddAnimatorToAllPanelPrefabs()
        {
            return !EditorApplication.isPlaying && !EditorApplication.isCompiling;
        }
    }
}
