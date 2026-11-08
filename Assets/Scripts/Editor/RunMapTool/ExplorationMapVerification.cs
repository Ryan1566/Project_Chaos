#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RunMap;
using UnityEditor;
using UnityEngine;

/// <summary>在编辑器分批抽样，保留旧生成器基线，避免长时间阻塞编辑器。</summary>
public static class ExplorationMapVerification
{
    public static string Status { get; private set; } = "idle";
    static int stage, sample, count;
    static NaturalMapParameters parameters;
    static List<MapStructureTemplate> structures;
    static MapTraversalProfile actor;
    static readonly List<int> enemyCounts = new List<int>();
    static readonly List<float> lengths = new List<float>();
    static readonly List<double> milliseconds = new List<double>();
    static readonly List<string> report = new List<string>();
    static int degraded, failed;
    static int totalFailures;
    static bool baseline;
    static readonly HashSet<string> geometry = new HashSet<string>();
    static readonly HashSet<ExplorationLayout> layouts = new HashSet<ExplorationLayout>();
    public static readonly int[] EnemyBudgets = new int[3];
    public static void StartBaseline(int samples = 1000)
    {
        if (EditorApplication.isPlaying || Status == "running") throw new InvalidOperationException("需要空闲编辑模式");
        baseline=true; totalFailures=0; count = samples; stage = 1; report.Clear(); LoadStage(); Status = "running";
        EditorApplication.update += Tick;
    }
    public static void StartV3(int samples=1000)
    {
        if(EditorApplication.isPlaying||Status=="running")throw new InvalidOperationException("需要空闲编辑模式");
        baseline=false;totalFailures=0;count=samples;stage=1;report.Clear();LoadStage();Status="running";EditorApplication.update+=Tick;
    }
    static void LoadStage()
    {
        sample = degraded = failed = 0; enemyCounts.Clear(); lengths.Clear(); milliseconds.Clear();
        parameters = NaturalMapRuntime.LoadParameters(stage, false);
        parameters.generationVersion = baseline?2:3;
        geometry.Clear();layouts.Clear();
        structures = NaturalMapRuntime.LoadStructures(parameters); actor = NaturalMapRuntime.CaptureActorProfile();
    }
    static void Tick()
    {
        var budget = Stopwatch.StartNew();
        try
        {
            do
            {
                uint seed = unchecked(0x9E3779B9u * (uint)(sample + 1));
                var clock = Stopwatch.StartNew();
                var build = new NaturalMapBuild(seed, stage, parameters, structures, actor);
                var map = build.Generate(); clock.Stop();
                if (map == null) {failed++;if(failed<=3)report.Add("FAIL stage="+stage+" seed="+seed+" "+build.Error);}
                else
                {
                    if (map.Degraded) degraded++;
                    enemyCounts.Add(map.Placements.Count(p => p.Kind == MapPlacementKind.Enemy));
                    lengths.Add(map.MainRouteLength); milliseconds.Add(clock.Elapsed.TotalMilliseconds);
                    if(!baseline)
                    {
                        var validation=ExplorationMapBuild.Validate(map);
                        if(validation.Count>0)throw new Exception(string.Join(";",validation));
                        uint forward=2166136261,reverse=2166136261;
                        for(int y=0;y<map.Height;y++)for(int x=0;x<map.Width;x++)
                        {forward=unchecked((forward^(map.IsSolid(x,y)?1u:0u))*16777619);reverse=unchecked((reverse^(map.IsSolid(map.Width-1-x,y)?1u:0u))*16777619);}
                        geometry.Add(Math.Min(forward,reverse).ToString("X8"));layouts.Add(map.Layout);
                    }
                }
                sample++;
            } while (sample < count && budget.ElapsedMilliseconds < 25);
            if (sample < count) return;
            enemyCounts.Sort(); lengths.Sort(); milliseconds.Sort();
            if(enemyCounts.Count==0)throw new Exception("该预设全部生成失败");
            int median = enemyCounts[enemyCounts.Count / 2]; if(baseline)EnemyBudgets[stage - 1] = median;
            report.Add($"stage={stage} samples={count} failed={failed} degraded={degraded} enemiesMedian={median} enemiesRange={enemyCounts[0]}..{enemyCounts[enemyCounts.Count-1]} routeRange={lengths[0]:F1}..{lengths[lengths.Count-1]:F1} generationP50Ms={milliseconds[milliseconds.Count/2]:F2} generationP95Ms={milliseconds[(int)(milliseconds.Count*.95)]:F2}");
            Directory.CreateDirectory(Application.dataPath + "/QA/ExplorationMap");
            if(!baseline)report.Add("geometryUnique="+geometry.Count+"/"+count+" excludingMirror/seed/decor layouts="+string.Join(",",layouts));
            File.WriteAllLines(Application.dataPath + "/QA/ExplorationMap/"+(baseline?"v2-baseline.txt":"v3-samples.txt"), report);
            totalFailures+=failed;
            if (++stage <= 3) LoadStage();
            else { Status = totalFailures==0?"passed":"failed samples="+totalFailures; EditorApplication.update -= Tick; }
        }
        catch (Exception e) { Status = "failed: " + e.Message; EditorApplication.update -= Tick; UnityEngine.Debug.LogException(e); }
    }
    public static string Progress => Status + " stage=" + stage + " sample=" + sample + "/" + count;
}
#endif
