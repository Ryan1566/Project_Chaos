using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RunMap
{
    /// <summary>先生成地点连接图，再雕刻连续地形；所有随机流以稳定用途分离。</summary>
    public sealed class ExplorationMapBuild
    {
        readonly uint seed;
        readonly int stage;
        readonly NaturalMapParameters settings;
        readonly MapTraversalProfile actor;
        readonly IList<IMapPopulationPass> extensions;
        readonly bool recovery;
        public NaturalMapPlan Plan { get; private set; }
        public NaturalMapPlan LastAttempt { get; private set; }
        public string Error { get; private set; }
        public ExplorationMapBuild(uint seed, int stage, NaturalMapParameters settings, MapTraversalProfile actor, IList<IMapPopulationPass> extensions,bool recovery=false)
        { this.seed = seed; this.stage = stage; this.settings = settings.Copy(); this.actor = actor; this.extensions = extensions; this.recovery=recovery; }
        RunMapRng Stream(NaturalMapPlan m, string purpose) => MapRandom.Stream(m.Degraded?17u:seed, stage, "exploration-v3/" + purpose, m.Degraded?0:m.Attempt - 1);
        public IEnumerable<string> Steps()
        {
            var errors = settings.Validate(actor);
            if(settings.compositions==null||settings.compositions.Length<4)errors.Add("探索地图缺少四种有效场景组合");
            if (stage < 1 || stage > 3 || settings.enemyBudget < 1 || settings.placeMin < 8 || settings.placeMax > 12
                || settings.placeMax < settings.placeMin || settings.ladderMin < 0 || settings.ladderMax < settings.ladderMin || settings.ladderMax > 2)
                errors.Add("探索地图的场景、地点、敌人或梯子预算非法");
            if (errors.Count > 0) { Error = string.Join("；", errors); yield break; }
            for (int attempt = 0; attempt <= (recovery?0:settings.retryMax); attempt++)
            {
                if(!recovery&&attempt==settings.retryMax)
                {
                    string lastError=Error;
                    var safe=new ExplorationMapBuild(settings.fallbackLayoutSeed,stage,settings,actor,extensions,true);
                    foreach(var step in safe.Steps())yield return step;
                    LastAttempt=safe.LastAttempt;
                    if(safe.Plan!=null)
                    {
                        Plan=safe.Plan;Plan.RunSeed=seed;Plan.Attempt=attempt+1;Plan.Degraded=true;
                        Plan.Diagnostics.Add("有界探索保底 layoutSeed="+settings.fallbackLayoutSeed+"；前次原因："+lastError);
                        Error=null;yield break;
                    }
                    Error="有界探索保底失败："+safe.Error;yield break;
                }
                var m = new NaturalMapPlan { Version = 3, Parameters = settings.Copy(), Actor = actor, RunSeed = seed, SceneIndex = stage,
                    Attempt = attempt + 1, Degraded = false, ConfigFingerprint = MapRandom.Hash(settings.Fingerprint+"/"+JsonUtility.ToJson(actor)).ToString("X8") };
                LastAttempt = m;
                int size = m.Width * m.Height;
                m.Solid = new bool[size]; m.Protected = new bool[size]; m.Reachable = new bool[size];
                m.RegionCells = new ushort[size]; m.SpaceCells = new byte[size]; m.SurfaceY = new int[m.Width]; m.CaveFloor = new int[m.Width];
                yield return "规划区域与边界";
                Layout(m);
                yield return "生成连续地表";
                foreach (var unused in Terrain(m)) yield return null;
                yield return "雕刻地下与连接通路";
                Connections(m);
                yield return "嵌入手工结构";
                CompositionGeometry(m);
                Ladders(m);
                // 端点和角色初始位置先预留，再做真实站立/跳跃校验。
                PlaceActorAnchors(m);
                yield return "校验角色通行与生成锚点";
                foreach (var unused in NaturalMapBuild.NavigationSteps(m)) yield return null;
                for(int repair=0;repair<settings.repairMax&&m.Places.Any(p=>!m.Reachable[m.Index(p.Anchor.x,p.Anchor.y)]);repair++)
                {
                    if(!RepairFrontier(m))break;
                    m.Repairs++;
                    foreach(var unused in NaturalMapBuild.NavigationSteps(m))yield return null;
                }
                if (m.Places.Any(p => !m.Reachable[m.Index(p.Anchor.x, p.Anchor.y)]))
                { Error = "地点缺少实际可达路径"; continue; }
                var route = m.RouteTo(m.GoalCell);
                for (int i = 1; i < route.Count; i++) m.MainRouteLength += Vector2.Distance(route[i - 1], route[i]);
                yield return "规划NPC、交互物与杂物";
                if (!Populate(m)) { Error = "探索内容无法满足数量、支撑或共享占用预算"; continue; }
                foreach(var unused in NaturalMapBuild.NavigationSteps(m))yield return null;
                var context = new MapPlacementContext(m);
                if (extensions != null)
                    foreach (var pass in extensions.OrderBy(p => p.Priority).ThenBy(p => p.StreamKey, StringComparer.Ordinal))
                    { pass.BuildPlan(context, Stream(m, "population/" + pass.StreamKey)); yield return null; }
                var invalid = Validate(m);
                if (invalid.Count > 0) { Error = string.Join("；", invalid); continue; }
                if (m.Degraded) m.Diagnostics.Add("已使用有界探索布局保底；前次原因：" + Error);
                m.Diagnostics.Add("layout=" + m.Layout + " junctions=" + Junctions(m) + " loops=" + (m.Links.Count - m.Places.Count + 1)
                    + " deadEnds=" + DeadEnds(m) + " ladders=" + m.Ladders.Count + " verticalSpan=" + (m.Places.Max(p => p.Anchor.y) - m.Places.Min(p => p.Anchor.y)));
                Plan = m; Error = null; yield return "地图计划校验完成"; yield break;
            }
            Error = "探索地图及有界保底失败：" + Error;
        }
        void Layout(NaturalMapPlan m)
        {
            var rng = Stream(m, "layout");
            var compositionRandom=Stream(m,"compositions");
            var compositions=settings.compositions;
            m.Layout = (ExplorationLayout)rng.Range(0, 2);
            int requested = rng.Range(settings.placeMin,settings.placeMax);
            if(m.Degraded){m.Layout=ExplorationLayout.DoubleLoop;requested=Mathf.Clamp(10,settings.placeMin,settings.placeMax);}
            if(requested==8)m.Layout=ExplorationLayout.DoubleLoop;
            bool mirror = !m.Degraded&&Stream(m, "direction").Chance(settings.startOnRightChance); m.StartsOnRight = mirror;
            int xStart = settings.edgeMargin + 18, xStep = (m.Width - 2 * settings.edgeMargin - 96) / 3;
            int top = Math.Min(m.Height - 36, settings.surfaceBaseY + 12), rowStep = Math.Min(24, (top - settings.undergroundMinY - 12) / 2);
            int[] cols = {0,1,2,3,0,1,2,3,1,2,0};
            int[] rows = {0,0,0,0,1,1,1,1,2,2,2};
            for (int i = 0; i < 10 + (requested >= 11 ? 1 : 0); i++)
            {
                int x = xStart + cols[i] * xStep + (rows[i] == 1 ? 52 : 0) + (m.Degraded?0:rng.Range(-3, 3));
                if (mirror) x = m.Width - 1 - x;
                int y = top - rows[i] * rowStep + (m.Degraded?0:rng.Range(-2, 2));
                int half = m.Degraded?8:rng.Range(7, 8), height = m.Degraded?12:rng.Range(8, 13);
                var space = stage == 2 ? MapSpaceKind.Underground : rows[i] == 0 ? MapSpaceKind.Outdoor : MapSpaceKind.Underground;
                if (stage == 3 && rows[i] < 2 && i != 0) space = MapSpaceKind.Building;
                if (i == 0) space = MapSpaceKind.Outdoor;
                var place = new ExplorationPlace { Id = (ushort)(i + 1), StableId = "place/" + i, Anchor = new Vector2Int(x, y), Space = space,
                    Bounds = new RectInt(x-half, y, half*2+1, height), Role = i == 0 ? ExplorationRole.Entry : ExplorationRole.Landmark,
                    Composition = compositions[i%compositions.Length].key };
                if(i>=compositions.Length)
                {
                    int roll=compositionRandom.Range(compositions.Sum(c=>c.weight));
                    foreach(var scene in compositions){roll-=scene.weight;if(roll<0){place.Composition=scene.key;break;}}
                }
                m.Places.Add(place);
            }
            int[,] pairs = m.Layout == ExplorationLayout.LayeredBranches
                ? new[,] {{0,4},{4,5},{5,1},{1,0},{5,6},{6,9},{9,8},{8,5},{1,2},{2,3},{6,7}}
                : new[,] {{0,1},{1,2},{2,3},{1,5},{5,6},{6,2},{5,4},{4,0},{6,9},{9,8},{3,7}};
            foreach (var pair in Enumerable.Range(0, pairs.GetLength(0)))
                m.Links.Add(new ExplorationLink { From = (ushort)(pairs[pair,0]+1), To = (ushort)(pairs[pair,1]+1) });
            if (m.Layout == ExplorationLayout.Convergence) m.Links.Add(new ExplorationLink { From=1,To=6,IsLoop=true });
            if (requested <= 9)
            {
                // 去掉一处末梢，保留其前一个地点作为可探索尽头。
                ushort removed = m.Layout == ExplorationLayout.LayeredBranches ? (ushort)4 : (ushort)8;
                m.Places.RemoveAll(p => p.Id == removed); m.Links.RemoveAll(l => l.From == removed || l.To == removed);
                if(requested==8){m.Places.RemoveAll(p=>p.Id==9);m.Links.RemoveAll(l=>l.From==9||l.To==9);}
            }
            else if (requested >= 11) m.Links.Add(new ExplorationLink {From=m.Layout == ExplorationLayout.Convergence?(ushort)6:(ushort)5,To=11});
            if(requested==12)
            {
                var a=Get(m,1);var b=Get(m,2);var anchor=new Vector2Int((a.Anchor.x+b.Anchor.x)/2,(a.Anchor.y+b.Anchor.y)/2);
                m.Places.Add(new ExplorationPlace{Id=12,StableId="place/11",Anchor=anchor,Bounds=new RectInt(anchor.x-8,anchor.y,17,10),
                    Space=b.Space,Role=ExplorationRole.Landmark,Composition=compositions[3].key});
                m.Links.RemoveAll(l=>(l.From==1&&l.To==2)||(l.From==2&&l.To==1));
                m.Links.Add(new ExplorationLink{From=1,To=12});m.Links.Add(new ExplorationLink{From=12,To=2});
            }
            var distances = new Dictionary<ushort,float> { [1] = 0 };
            var open = new List<ushort> {1};
            while (open.Count>0)
            {
                open.Sort((a,b)=>distances[a].CompareTo(distances[b])); ushort current=open[0];open.RemoveAt(0);
                foreach(var link in m.Links.Where(l=>l.From==current||l.To==current))
                {
                    ushort other=link.From==current?link.To:link.From;
                    float distance=distances[current]+Vector2.Distance(Get(m,current).Anchor,Get(m,other).Anchor);
                    if(distances.TryGetValue(other,out var old)&&old<=distance)continue;
                    distances[other]=distance;if(!open.Contains(other))open.Add(other);
                }
            }
            var goal=m.Places.Where(p=>p.Id!=1&&(stage!=2||p.Space==MapSpaceKind.Underground)&&(stage!=3||p.Space==MapSpaceKind.Building))
                .OrderByDescending(p=>distances[p.Id]).First();
            goal.Role=ExplorationRole.Objective;goal.CountsForCompletion=stage==3;
            m.EntryRegionId=1;m.ExitRegionId=goal.Id;m.EntryCell=m.Places[0].Anchor;m.GoalCell=goal.Anchor;
            m.ObjectiveRegionIds.Add(goal.Id);
            foreach(var place in m.Places)
            {
                int degree=m.Links.Count(l=>l.From==place.Id||l.To==place.Id);
                if(place.Role!=ExplorationRole.Entry&&place.Role!=ExplorationRole.Objective)
                    place.Role=degree==1?ExplorationRole.DeadEnd:degree>=3?ExplorationRole.Junction:ExplorationRole.Landmark;
                place.HasEncounter=place.Id!=1;
                m.Regions.Add(new EncounterRegion {Id=place.Id,StableId=place.StableId,Space=place.Space,
                    Bounds=new Rect(place.Bounds.x,place.Bounds.y,place.Bounds.width,place.Bounds.height),IsEntry=place.Id==1,Required=place.HasEncounter});
            }
        }
        static ExplorationPlace Get(NaturalMapPlan m,ushort id)=>m.Places.Find(p=>p.Id==id);
        bool RepairFrontier(NaturalMapPlan m)
        {
            var frontier=m.Links.FirstOrDefault(l=>m.Reachable[m.Index(Get(m,l.From).Anchor.x,Get(m,l.From).Anchor.y)]!=m.Reachable[m.Index(Get(m,l.To).Anchor.x,Get(m,l.To).Anchor.y)]);
            if(frontier==null)return false;
            var a=Get(m,frontier.From);var b=Get(m,frontier.To);
            m.Diagnostics.Add("修复连接 "+a.Id+"->"+b.Id);
            if(a.Anchor.y<b.Anchor.y){var swap=a;a=b;b=swap;}
            int direction=Math.Sign(b.Anchor.x-a.Anchor.x);
            int start=direction>0?a.Bounds.xMax-1:a.Bounds.xMin;
            int end=direction>0?b.Bounds.xMin:b.Bounds.xMax-1;
            int length=Math.Abs(end-start);
            if(length<Math.Abs(a.Anchor.y-b.Anchor.y)/2)return false;
            for(int n=0;n<=length;n++)
            {
                int x=start+direction*n,y=a.Anchor.y+(b.Anchor.y-a.Anchor.y)*n/Math.Max(1,length);
                bool descending=a.Anchor.y-b.Anchor.y>8;
                if(descending)y=n==0?a.Anchor.y:n==1?a.Anchor.y-1:n==2?a.Anchor.y-3:Math.Min(y,a.Anchor.y-5);
                int height=actor.Height+actor.MaxJumpHeight+3;
                if(descending&&n<3)height=a.Anchor.y-y+actor.Height+2;
                else
                {
                    foreach(var cell in m.StandingCells)
                        if(cell.x==x&&m.Reachable[m.Index(cell.x,cell.y)]&&cell.y-y>=actor.Height+2)
                            height=Math.Min(height,cell.y-y-1);
                }
                Carve(m,x,y,height,n<length/2?a.Id:b.Id,
                    a.Space==b.Space?a.Space:MapSpaceKind.Underground,true);
            }
            return true;
        }
        IEnumerable<int> Terrain(NaturalMapPlan m)
        {
            var rng=Stream(m,"terrain");int current=settings.surfaceBaseY+14;
            for(int x=0;x<m.Width;x++)
            {
                if(x%5==0)current=Mathf.Clamp(current+rng.Range(-2,2),settings.surfaceBaseY+5,Math.Min(m.Height-24,settings.surfaceBaseY+24));
                m.SurfaceY[x]=stage==2?Math.Min(m.Height-16,current+14):current;
                m.CaveFloor[x]=settings.undergroundMinY;
                for(int y=0;y<m.Height;y++)
                {int index=m.Index(x,y);m.Solid[index]=y<m.SurfaceY[x];m.SpaceCells[index]=(byte)(y<m.SurfaceY[x]-3?MapSpaceKind.Underground:MapSpaceKind.Outdoor);}
                if(x%settings.workColumnsPerFrame==0)yield return x;
            }
            // 地表以山脊隔断长直通路，真正的连接由地点图雕刻；不制造无提示的深坑。
            for(int n=0;n<3;n++)
            {
                int x=settings.edgeMargin+60+n*(m.Width-settings.edgeMargin*2-80)/3;
                for(int dx=-5;dx<=5;dx++)for(int y=m.SurfaceY[x+dx];y<m.SurfaceY[x+dx]+8-Math.Abs(dx);y++)
                    if(m.Inside(x+dx,y))m.Solid[m.Index(x+dx,y)]=true;
            }
            foreach(var place in m.Places)
            {
                int cx=place.Anchor.x, floor=place.Anchor.y;
                for(int x=place.Bounds.xMin;x<place.Bounds.xMax;x++)
                {
                    int extra=place.Space==MapSpaceKind.Outdoor?m.Height-floor-3:place.Bounds.height+(int)(Math.Sqrt(Math.Max(0,120-(x-cx)*(x-cx)))*.3f);
                    Carve(m,x,floor,extra,place.Id,place.Space,false);
                    if(place.Space==MapSpaceKind.Outdoor)m.SurfaceY[x]=floor;
                }
            }
        }
        static void Carve(NaturalMapPlan m,int x,int floor,int height,ushort region,MapSpaceKind space,bool protect)
        {
            if(x<2||x>=m.Width-2||floor<2||floor+height>=m.Height)return;
            m.Solid[m.Index(x,floor-1)]=true;
            for(int y=floor;y<Math.Min(m.Height-2,floor+height);y++)
            {int index=m.Index(x,y);m.Solid[index]=false;m.SpaceCells[index]=(byte)space;m.RegionCells[index]=region;if(protect&&y<floor+m.Actor.Height+1)m.Protected[index]=true;}
            m.SpaceCells[m.Index(x,floor-1)]=(byte)space;
        }
        void Connections(NaturalMapPlan m)
        {
            var rng=Stream(m,"connections");
            foreach(var link in m.Links)
            {
                var a=Get(m,link.From);var b=Get(m,link.To);link.Path.Add(a.Anchor);
                int direction=Math.Sign(b.Anchor.x-a.Anchor.x);
                int leave=direction>0?a.Bounds.xMax:a.Bounds.xMin-1;
                int enter=direction>0?b.Bounds.xMin-1:b.Bounds.xMax;
                link.Path.Add(new Vector2Int(leave,a.Anchor.y));
                link.Path.Add(new Vector2Int(enter,b.Anchor.y));
                link.Path.Add(b.Anchor);
                for(int i=1;i<link.Path.Count;i++)
                {
                    Vector2Int from=link.Path[i-1],to=link.Path[i];int length=Math.Abs(to.x-from.x),dir=Math.Sign(to.x-from.x);
                    for(int n=0;n<=length;n++)
                    {
                        int x=from.x+dir*n,y=from.y+(to.y-from.y)*n/Math.Max(1,length);
                        ushort region=n<length/2?a.Id:b.Id;MapSpaceKind space=a.Space==MapSpaceKind.Outdoor&&b.Space==MapSpaceKind.Outdoor?MapSpaceKind.Outdoor:MapSpaceKind.Underground;
                        int height=space==MapSpaceKind.Outdoor?m.Height-y-3:6+rng.Range(0,3);
                        Carve(m,x,y,height,region,space,true);
                        if(space==MapSpaceKind.Outdoor)m.SurfaceY[x]=Math.Min(m.SurfaceY[x],y);
                    }
                }
                m.Connections.Add(new RegionConnection {From=a.Id,To=b.Id,Position=(Vector2)(a.Anchor+b.Anchor)*.5f,IsLoop=link.IsLoop});
            }
            // 交叉口最后恢复地点的接地净空，连接施工不能覆盖出生点或内容锚点。
            foreach(var place in m.Places)
                for(int x=place.Bounds.xMin;x<place.Bounds.xMax;x++)
                    Carve(m,x,place.Anchor.y,place.Space==MapSpaceKind.Outdoor?m.Height-place.Anchor.y-3:place.Bounds.height,place.Id,place.Space,false);
            // 先恢复同层桥面，再从侧口下行，避免斜洞直接咬掉另一条路线的地板。
            var roads=new Dictionary<int,List<int>>();
            foreach(var link in m.Links)
            {
                var a=Get(m,link.From);var b=Get(m,link.To);
                if(Math.Abs(a.Anchor.y-b.Anchor.y)>8)continue;
                for(int i=1;i<link.Path.Count;i++)
                {
                    var from=link.Path[i-1];var to=link.Path[i];int length=Math.Abs(to.x-from.x),dir=Math.Sign(to.x-from.x);
                    for(int n=0;n<=length;n++)
                    {
                        int x=from.x+dir*n,y=from.y+(to.y-from.y)*n/Math.Max(1,length);
                        Carve(m,x,y,a.Space==MapSpaceKind.Outdoor&&b.Space==MapSpaceKind.Outdoor?m.Height-y-3:7,a.Id,a.Space,true);
                        if(!roads.TryGetValue(x,out var heights))roads[x]=heights=new List<int>();heights.Add(y);
                    }
                }
            }
            foreach(var link in m.Links)
            {
                var a=Get(m,link.From);var b=Get(m,link.To);if(Math.Abs(a.Anchor.y-b.Anchor.y)<=8)continue;
                var upper=a.Anchor.y>b.Anchor.y?a:b;var lower=upper==a?b:a;
                int dir=Math.Sign(lower.Anchor.x-upper.Anchor.x);
                int start=dir>0?upper.Bounds.xMax:upper.Bounds.xMin-1;
                int end=dir>0?lower.Bounds.xMin-1:lower.Bounds.xMax;
                int length=Math.Abs(end-start);
                for(int n=0;n<=length;n++)
                {
                    int x=start+n*dir,normal=upper.Anchor.y+(lower.Anchor.y-upper.Anchor.y)*n/Math.Max(1,length);
                    int y=n==0?upper.Anchor.y:n==1?upper.Anchor.y-1:n==2?upper.Anchor.y-3:Math.Min(normal,upper.Anchor.y-5);
                    int height=n<3?upper.Anchor.y-y+actor.Height+2:7;
                    if(n>=3&&roads.TryGetValue(x,out var over))foreach(int road in over)if(road-y>=actor.Height+2)height=Math.Min(height,road-y-1);
                    Carve(m,x,y,height,n<length/2?upper.Id:lower.Id,MapSpaceKind.Underground,true);
                }
            }
        }
        void Ladders(NaturalMapPlan m)
        {
            var rng=Stream(m,"ladders");int count=m.Degraded?settings.ladderMin:rng.Range(settings.ladderMin,settings.ladderMax);
            var eligible=m.Links.Where(l=>Math.Abs(Get(m,l.From).Anchor.x-Get(m,l.To).Anchor.x)<62&&Math.Abs(Get(m,l.From).Anchor.y-Get(m,l.To).Anchor.y)>12).ToList();
            for(int i=0;i<count&&eligible.Count>0;i++)
            {
                int index=rng.Range(eligible.Count);var link=eligible[index];eligible.RemoveAt(index);
                var a=Get(m,link.From);var b=Get(m,link.To);var lower=a.Anchor.y<b.Anchor.y?a:b;var upper=lower==a?b:a;
                int x=upper.Anchor.x+5;
                var ladder=new MapLadder {StableId="ladder/"+i,Bottom=new Vector2Int(x,lower.Anchor.y),Top=new Vector2Int(x-1,upper.Anchor.y),RegionId=upper.Id};
                for(int cx=x;cx<=x+1;cx++)for(int y=lower.Anchor.y;y<upper.Anchor.y+actor.Height+2;y++)
                {m.Solid[m.Index(cx,y)]=false;m.SpaceCells[m.Index(cx,y)]=(byte)MapSpaceKind.Underground;m.RegionCells[m.Index(cx,y)]=upper.Id;}
                // 上端为梯身旁的站立台，下端先连接原洞室地板。
                Carve(m,x-1,upper.Anchor.y,actor.Height+2,upper.Id,upper.Space,true);
                for(int cx=Math.Min(lower.Anchor.x,x);cx<=Math.Max(lower.Anchor.x,x+1);cx++)Carve(m,cx,lower.Anchor.y,actor.Height+2,lower.Id,lower.Space,true);
                link.UsesLadder=true;m.Ladders.Add(ladder);
            }
        }
        void CompositionGeometry(NaturalMapPlan m)
        {
            // 这些手工侧景布局只占据地点边缘，中心净空和连接口始终保留。
            foreach(var place in m.Places)
            {
                int style=Array.Find(settings.compositions,c=>c.key==place.Composition).style,left=place.Bounds.xMin+2,right=place.Bounds.xMax-3,floor=place.Anchor.y;
                if(place.Space==MapSpaceKind.Building)
                {
                    for(int x=left;x<=right;x++)
                    {int y=floor+place.Bounds.height-2;m.Solid[m.Index(x,y)]=true;m.SpaceCells[m.Index(x,y)]=(byte)MapSpaceKind.Building;}
                    // 门洞至少四格高；短柱和残墙提供轮廓，不封住进出路线。
                    for(int h=5;h<place.Bounds.height-1;h++)foreach(int x in new[]{left,right})m.Solid[m.Index(x,floor+h)]=true;
                }
                if(style==3&&place.Id!=1&&!m.Degraded)
                {
                    int x=left+1;
                    if(!m.Protected[m.Index(x,floor)]){m.Solid[m.Index(x,floor)]=true;m.SpaceCells[m.Index(x,floor)]=(byte)place.Space;}
                }
                // 每个地点保留独立语义，背景/装饰不通过碰撞体表达。
                for(int x=place.Bounds.xMin;x<place.Bounds.xMax;x++)
                    for(int y=floor;y<Math.Min(m.Height-2,floor+place.Bounds.height);y++)m.SpaceCells[m.Index(x,y)]=(byte)place.Space;
            }
        }
        static void PlaceActorAnchors(NaturalMapPlan m)
        {
            void Add(string id,MapPlacementKind kind,Vector2Int cell,ushort region)
            {m.Placements.Add(new MapPlacement {StableId=id,Kind=kind,RegionId=region,Footprint=new RectInt(cell.x,cell.y,1,2),Position=new Vector2(cell.x+.5f,cell.y+.05f),Facing=m.StartsOnRight?-1:1});}
            Add("player",MapPlacementKind.Player,m.EntryCell,1);
            int[] offsets={-6,-3,3};
            for(int i=0;i<3;i++)Add("ally/"+i,MapPlacementKind.Ally,m.EntryCell+Vector2Int.right*offsets[i],1);
            Add("exit",MapPlacementKind.Exit,m.GoalCell,m.ExitRegionId);
        }
        bool Populate(NaturalMapPlan m)
        {
            var context=new MapPlacementContext(m);var candidates=context.Candidates();
            bool Reserve(MapPlacementKind kind,string id,ushort region,Vector2Int near,bool avoidEntry=true)
            {
                var list=candidates.Where(c=>(region==0||m.RegionAt(c.x,c.y)==region)&&(!avoidEntry||Vector2.Distance(c,m.EntryCell)>settings.entrySafetyRadius+4))
                    .OrderBy(c=>Vector2.Distance(c,near)).ThenBy(c=>c.x).ThenBy(c=>c.y);
                foreach(var c in list)if(context.TryReserve(new MapPlacementRequest{StableId=id,Kind=kind,RegionId=region==0?m.RegionAt(c.x,c.y):region,Cell=c,AllowProtected=true},out _))return true;
                return false;
            }
            var enemyPlaces=m.Places.Where(p=>p.HasEncounter).OrderByDescending(p=>p.CountsForCompletion).ThenBy(p=>p.Id).ToArray();
            var enemyRng=Stream(m,"enemies");
            for(int n=0;n<settings.enemyBudget;n++)
            {
                var place=enemyPlaces[n%enemyPlaces.Length];
                var near=place.Anchor+Vector2Int.right*enemyRng.Range(-8,8);
                if(!Reserve(MapPlacementKind.Enemy,"enemy/"+n,place.Id,near))return false;
            }
            var contentRng=Stream(m,"content");
            foreach(var spec in new[]{Tuple.Create(MapPlacementKind.Npc,settings.npcCount),Tuple.Create(MapPlacementKind.Interactive,settings.interactiveCount),Tuple.Create(MapPlacementKind.Gatherable,settings.gatherableCount)})
                for(int n=0;n<spec.Item2;n++)
                {
                    var pool=m.Places.Where(p=>p.Id!=1).OrderByDescending(p=>p.Role==ExplorationRole.DeadEnd).ToArray();
                    var place=pool[(contentRng.Range(pool.Length)+n)%pool.Length];
                    if(!Reserve(spec.Item1,spec.Item1.ToString().ToLowerInvariant()+"/"+n,0,place.Anchor))return false;
                }
            var propRandom=Stream(m,"props");
            for(int n=0;n<settings.propCount;n++)
            {
                var pool=context.Candidates(0,solid:true);int offset=pool.Count==0?0:propRandom.Range(pool.Count);
                bool placed=false;
                for(int i=0;i<pool.Count;i++)
                {
                    var c=pool[(offset+i)%pool.Count];
                    if(context.TryReserve(new MapPlacementRequest{StableId="prop/"+n,Kind=MapPlacementKind.Prop,Cell=c,
                        RegionId=m.RegionAt(c.x,c.y),Width=1,Height=1,Solid=true},out _)){placed=true;break;}
                }
                if(!placed)m.Diagnostics.Add("实体杂物缺少非保护区候选，跳过 prop/"+n);
            }
            // 无碰撞装饰独立采样，绝不改变地形或此前的内容点位。
            var decor=Stream(m,"decorations");
            for(int n=0;n<settings.decorationCount;n++)
            {
                var c=candidates[decor.Range(candidates.Count)];
                if(m.Placements.Count>=settings.maxContentObjects)break;
                m.Placements.Add(new MapPlacement {StableId="decoration/"+n,Kind=MapPlacementKind.Decoration,RegionId=m.RegionAt(c.x,c.y),Position=new Vector2(c.x+.5f,c.y+.05f),Footprint=new RectInt(c.x,c.y,1,1)});
            }
            return true;
        }
        public static int Junctions(NaturalMapPlan m)=>m.Places.Count(p=>m.Links.Count(l=>l.From==p.Id||l.To==p.Id)>=3);
        public static int DeadEnds(NaturalMapPlan m)=>m.Places.Count(p=>m.Links.Count(l=>l.From==p.Id||l.To==p.Id)==1);
        public static List<string> Validate(NaturalMapPlan m)
        {
            var errors=new List<string>();
            if(m.Width!=m.Parameters.widthTiles||m.Height!=m.Parameters.heightTiles)errors.Add("地图尺寸不一致");
            if(m.Places.Count<8||m.Places.Count>12||Junctions(m)<3||Junctions(m)>5||DeadEnds(m)<2||DeadEnds(m)>4)errors.Add("地点或岔路/尽头数量不满足探索约束");
            int loops=m.Links.Count-m.Places.Count+1;if(loops<2||loops>3)errors.Add("回环数量不合法");
            if(m.Ladders.Count<m.Parameters.ladderMin||m.Ladders.Count>m.Parameters.ladderMax)errors.Add("梯子预算未满足");
            if(m.MainRouteLength<m.Parameters.mainRouteMinLength||m.MainRouteLength>m.Parameters.mainRouteMaxLength)errors.Add("目标实际路线长度超出配置范围");
            foreach(var p in m.Placements)
            {
                if(!m.Bounds.Contains(p.Footprint.min)||!m.Bounds.Contains(p.Footprint.max-Vector2Int.one))errors.Add("放置越界："+p.StableId);
                if(!p.Solid&&p.Kind!=MapPlacementKind.Decoration&&(!m.CanStand(p.Footprint.x,p.Footprint.y)||!m.Reachable[m.Index(p.Footprint.x,p.Footprint.y)]))errors.Add("放置缺少可达支撑："+p.StableId);
            }
            foreach(var ladder in m.Ladders)if(!m.CanStand(ladder.Bottom.x,ladder.Bottom.y)||!m.CanStand(ladder.Top.x,ladder.Top.y))errors.Add("梯子端点不可站立");
            foreach(var place in m.Places)if(place.MustBeReachable&&!m.Reachable[m.Index(place.Anchor.x,place.Anchor.y)])errors.Add("地点缺少可达路径："+place.StableId);
            return errors;
        }
    }
}
