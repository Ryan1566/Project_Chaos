using System;
using LocalizationSystem;
using UnityEngine;

[Serializable]
public sealed class VillageFacilityText
{
    public string zhCN, enUS, zhTW, jaJP, koKR;
    public string Resolve()
    {
        var manager=LocalizationManager.GetInstance();var language=manager==null?LanguageType.ChineseSimplified:manager.CurrentLanguage;
        string text=language==LanguageType.English?enUS:language==LanguageType.ChineseTraditional?zhTW:language==LanguageType.Japanese?jaJP:language==LanguageType.Korean?koKR:zhCN;
        return string.IsNullOrWhiteSpace(text)?zhCN??enUS??string.Empty:text;
    }
}

[Serializable]
public sealed class VillageFacilityDefinition
{
    public string id;
    public VillageFacilityKind kind;
    public float x, width=6, height=4;
    public Color color=Color.gray;
    public VillageFacilityText name,description;
}

/// <summary>Fixed non-production civic facilities; not a second building economy or inventory.</summary>
[CreateAssetMenu(menuName="Chaos/Village Facilities")]
public sealed class VillageFacilitySettings : ScriptableObject
{
    public VillageFacilityDefinition[] facilities;
    public VillageFacilityText officeTitle,officeBody,tasksLabel,tasksEmptyBody,tradeLabel,taxLabel,backLabel,taxTitle,taxBody;
    public VillageFacilityText warehouseTitle,permanentLabel,missingSession;
    public VillageFacilityText interactHint;
    public static VillageFacilitySettings Load()
    {
        var value=Resources.Load<VillageFacilitySettings>("Village/VillageFacilitySettings");
        if(value==null)throw new InvalidOperationException("缺少Village/VillageFacilitySettings配置");
        return value;
    }
    public VillageFacilityDefinition Get(string id)
    { return facilities==null?null:Array.Find(facilities,f=>f!=null&&f.id==id); }
    public void Validate(VillageSceneSettings village,VillagePlot[] plots)
    {
        if(facilities==null||facilities.Length!=3)throw new InvalidOperationException("村庄需要3个基础设施定义");
        var ids=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);var kinds=new System.Collections.Generic.HashSet<VillageFacilityKind>();
        foreach(var definition in facilities)
        {
            if(definition==null||string.IsNullOrWhiteSpace(definition.id)||!ids.Add(definition.id)||!kinds.Add(definition.kind)
                || definition.kind==VillageFacilityKind.None||!Enum.IsDefined(typeof(VillageFacilityKind),definition.kind)
                ||float.IsNaN(definition.x)||float.IsInfinity(definition.x)||float.IsNaN(definition.width)||float.IsInfinity(definition.width)
                ||float.IsNaN(definition.height)||float.IsInfinity(definition.height)||definition.width<2||definition.height<2
                ||Mathf.Abs(definition.x)+definition.width*.5f>village.widthTiles*.5f-1||definition.name==null||string.IsNullOrWhiteSpace(definition.name.Resolve()))
                throw new InvalidOperationException("村庄基础设施定义、类型或边界非法");
            foreach(var plot in plots)if(Mathf.Abs(plot.Entrance.x-definition.x)<(9+definition.width)*.5f+1-.001f)
                throw new InvalidOperationException("基础设施与现有生产地块重叠："+definition.id);
            foreach(var other in facilities)if(other!=null&&other!=definition&&Mathf.Abs(other.x-definition.x)<(other.width+definition.width)*.5f+1)
                throw new InvalidOperationException("基础设施之间重叠："+definition.id);
        }
    }
}
