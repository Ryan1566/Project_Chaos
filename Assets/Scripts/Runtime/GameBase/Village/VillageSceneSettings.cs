using System;
using LocalizationSystem;
using UnityEngine;

[Serializable]
public sealed class VillageSceneSettings
{
    public uint Id;
    public int widthTiles, groundDepth, workerCount;
    public float playerSpawnX, groundY, workerMoveSpeed, workSeconds, restSeconds, wanderMinSeconds, wanderMaxSeconds;
    public float plotSpacing, firstPlotX;
    public string groundTilePool;
    public static VillageSceneSettings Load()
    {
        var rows = ConfigLoader.Instance.LoadData<VillageSceneConfig>();
        if (rows == null || rows.datas.Count != 1) throw new InvalidOperationException("VillageScene requires one configuration row");
        var p = JsonUtility.FromJson<VillageSceneSettings>(JsonUtility.ToJson(rows.datas[0])); p.Validate(); return p;
    }
    public void Validate()
    {
        foreach (float f in new[] { playerSpawnX, groundY, workerMoveSpeed, workSeconds, restSeconds, wanderMinSeconds, wanderMaxSeconds, plotSpacing, firstPlotX })
            if (float.IsNaN(f) || float.IsInfinity(f)) throw new InvalidOperationException("VillageScene contains non-finite values");
        if (widthTiles < 48 || widthTiles > 512 || widthTiles % 2 != 0 || Mathf.Abs(groundY - Mathf.Round(groundY)) > .001f
            || groundDepth < 2 || groundDepth > 32 || workerCount < 1 || workerCount > 16
            || workerMoveSpeed <= 0 || workSeconds <= 0 || restSeconds <= 0 || wanderMinSeconds <= 0 || wanderMaxSeconds < wanderMinSeconds
            || plotSpacing < 12 || firstPlotX - 6 < -widthTiles * .5f || firstPlotX + 3 * plotSpacing + 6 > widthTiles * .5f
            || Mathf.Abs(playerSpawnX) > widthTiles * .5f - 2 || string.IsNullOrEmpty(groundTilePool))
            throw new InvalidOperationException("VillageScene dimensions, plots or worker budgets are invalid");
    }
}

public static class VillageText
{
    public static string Get(string key, params object[] args)
    {
        var rows = ConfigLoader.Instance.LoadData<VillageTextConfig>();
        var row = rows == null ? null : rows.datas.Find(r => r.key == key);
        if (row == null) throw new InvalidOperationException("VillageText missing key: " + key);
        var manager = LocalizationManager.GetInstance();
        var lang = manager == null ? LanguageType.ChineseSimplified : manager.CurrentLanguage;
        string text = lang == LanguageType.English ? row.enUS : lang == LanguageType.ChineseTraditional ? row.zhTW
            : lang == LanguageType.Japanese ? row.jaJP : lang == LanguageType.Korean ? row.koKR : row.zhCN;
        return args == null || args.Length == 0 ? text : string.Format(text, args);
    }
}
