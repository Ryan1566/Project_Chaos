using System.Collections.Generic;
using Chaos.Gameplay.Interaction;
using UnityEngine;

namespace RunMap
{
    /// <summary>每处地点一个批量布景网格；表现不写碰撞、路线或内容占用。</summary>
    public sealed class ExplorationMapPresentation : MonoBehaviour
    {
        static Material material;
        readonly List<Mesh> meshes=new List<Mesh>();
        public static void Build(NaturalMapPlan plan,Transform parent)
        {
            if(plan.Version<3)return;
            var owner=parent.gameObject.AddComponent<ExplorationMapPresentation>();
            if(material==null)material=new Material(Shader.Find("Sprites/Default")){name="ExplorationFlatArt",mainTexture=Texture2D.whiteTexture,hideFlags=HideFlags.DontSave};
            foreach(var place in plan.Places)
            {
                var art=new Batch();
                var random=MapRandom.Stream(plan.RunSeed,plan.SceneIndex,"site-art/"+place.StableId);
                float left=place.Bounds.xMin+1,right=place.Bounds.xMax-1,y=place.Anchor.y;
                var scene=System.Array.Find(plan.Parameters.compositions,c=>c.key==place.Composition);
                int style=scene.style;
                var timber=new Color(.34f,.22f,.12f,.9f);
                if(plan.SceneIndex==1)
                {
                    if(style==0)
                    {
                        for(int i=0;i<4;i++){float x=left+i*1.6f;art.Triangle(new Vector2(x,y),new Vector2(x+2.5f,y+random.Range(2,5)),new Vector2(x+4,y),new Color(.24f,.29f,.25f));}
                    }
                    else if(style==1)
                    {
                        foreach(float x in new[]{left+1,right-2})
                        {float h=random.Range(5,8);art.Quad(x-.2f,y,.4f,h,timber);art.Triangle(new Vector2(x-2.5f,y+h-2),new Vector2(x,y+h+2),new Vector2(x+2.5f,y+h-2),new Color(.16f,.32f,.23f,.85f));}
                    }
                    else if(style==2)
                    {
                        for(int i=0;i<7;i++)art.Quad(left+i*2,y+.08f,1.2f,.25f,new Color(.39f,.32f,.22f,.75f));
                        art.Quad(right-2,y,.22f,3.3f,timber);art.Quad(right-2.8f,y+2,.9f,.2f,timber);
                    }
                    else
                    {
                        art.Quad(left,y,.35f,4,timber);art.Quad(left+5,y,.35f,4,timber);
                        art.Triangle(new Vector2(left-1,y+4),new Vector2(left+2.5f,y+5.5f),new Vector2(left+6,y+4),new Color(.33f,.25f,.18f));
                        art.Quad(left,y+3.7f,5.5f,.3f,timber);
                    }
                }
                else if(plan.SceneIndex==2)
                {
                    var stone=new Color(.19f,.22f,.25f,.9f);
                    if(style==0)
                        for(int i=0;i<5;i++)art.Triangle(new Vector2(left+i*3,y+place.Bounds.height-.2f),new Vector2(left+i*3+1,y+place.Bounds.height-random.Range(2,4)),new Vector2(left+i*3+2,y+place.Bounds.height-.2f),stone);
                    else if(style==1)
                        foreach(float x in new[]{left+1,right-2}){art.Quad(x,y,.8f,place.Bounds.height-1,stone);art.Quad(x-.6f,y+place.Bounds.height-2,2,.5f,stone);}
                    else if(style==2)
                    {
                        foreach(float x in new[]{left+1,right-1})art.Quad(x,y,.35f,place.Bounds.height-1,timber);
                        art.Quad(left,y+place.Bounds.height-1,right-left+1,.45f,timber);
                        art.Quad(left+2,y+.3f,2.7f,.8f,new Color(.24f,.25f,.27f));
                        art.Quad(left+2.3f,y,.4f,.4f,Color.black);art.Quad(left+4,y,.4f,.4f,Color.black);
                    }
                    else
                        for(int i=0;i<6;i++){float x=left+i*.9f;art.Triangle(new Vector2(x,y),new Vector2(x+.5f,y+random.Range(1,3)),new Vector2(x+1.5f,y),stone);}
                }
                else
                {
                    var wall=new Color(.32f,.29f,.25f,.9f);
                    if(style==0)
                    {
                        art.Quad(left,y,.3f,6,timber);art.Triangle(new Vector2(left+.3f,y+6),new Vector2(left+2.5f,y+5.4f),new Vector2(left+.3f,y+4.8f),new Color(.48f,.25f,.16f));
                        art.Quad(right-2,y,1.5f,1.2f,wall);
                    }
                    else if(style==1)
                        foreach(float x in new[]{left,right-2}){art.Quad(x,y,1.3f,3.5f,wall);art.Quad(x,y+3.5f,.6f,1.2f,wall);}
                    else if(style==2)
                        for(int i=0;i<3;i++)for(int j=0;j<2-(i%2);j++){float x=left+i*1.5f;art.Quad(x,y+j*1.2f,1.2f,1.1f,timber);art.Quad(x+.15f,y+j*1.2f+.1f,.1f,.9f,new Color(.55f,.4f,.22f));}
                    else
                    {
                        art.Quad(left,y,.4f,4,timber);art.Quad(left+3.5f,y,.4f,4,timber);art.Quad(left,y+4,4,.4f,timber);
                        for(int i=0;i<4;i++)art.Quad(left+4+i*.7f,y,.6f,(4-i)*.35f,wall);
                    }
                }
                if(place.Role==ExplorationRole.Junction)
                {float x=right-1;art.Quad(x,y,.16f,2.3f,timber);art.Quad(x-.6f,y+1.8f,1.5f,.25f,timber);}
                if(place.CountsForCompletion)
                {float x=place.Anchor.x+3;art.Quad(x,y,.2f,6,timber);art.Triangle(new Vector2(x+.2f,y+6),new Vector2(x+3,y+5),new Vector2(x+.2f,y+4),new Color(.85f,.37f,.12f));}
                owner.Emit(art,parent,"Scenery_"+place.StableId.Replace('/','_'),-2);
            }
            foreach(var ladder in plan.Ladders)
            {
                var go=new GameObject(ladder.StableId.Replace('/','_'));go.transform.SetParent(parent,false);
                go.transform.localPosition=new Vector3(ladder.Bottom.x+.5f,(ladder.Bottom.y+ladder.Top.y)*.5f);
                var trigger=go.AddComponent<BoxCollider2D>();trigger.isTrigger=true;trigger.size=new Vector2(1.6f,ladder.Top.y-ladder.Bottom.y+2);
                var interaction=go.AddComponent<RunLadderInteractable>();interaction.ConfigureLadder(ladder,plan);
                var marker=go.AddComponent<RunMapMarkerPortrait>();marker.Kind=RunMapMarkerKind.Ladder;marker.RememberWhenHidden=true;
                var art=new Batch();float x=ladder.Bottom.x+.5f,bottom=ladder.Bottom.y,top=ladder.Top.y+.2f;
                var color=new Color(.68f,.47f,.24f);
                art.Quad(x-.36f,bottom,.12f,top-bottom,color);art.Quad(x+.24f,bottom,.12f,top-bottom,color);
                for(float y=bottom+.3f;y<top;y+=.65f)art.Quad(x-.3f,y,.6f,.09f,color);
                owner.Emit(art,parent,"Visual_"+ladder.StableId.Replace('/','_'),3);
            }
            if(plan.SceneIndex==3)
            {
                var go=new GameObject("CoreObjective");go.transform.SetParent(parent,false);
                go.transform.localPosition=new Vector3(plan.GoalCell.x+.5f,plan.GoalCell.y+.5f);
                go.AddComponent<RunObjectiveMarker>().Configure("core-objective");
                var marker=go.AddComponent<RunMapMarkerPortrait>();marker.Kind=RunMapMarkerKind.Objective;marker.RememberWhenHidden=true;
            }
        }
        void Emit(Batch art,Transform parent,string name,int order)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);
            var mesh=new Mesh{name=name};mesh.SetVertices(art.Vertices);mesh.SetColors(art.Colors);mesh.SetTriangles(art.Triangles,0);mesh.SetUVs(0,art.Uvs);mesh.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=order;meshes.Add(mesh);
        }
        void OnDestroy(){foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);}
        sealed class Batch
        {
            public readonly List<Vector3> Vertices=new List<Vector3>();public readonly List<Color> Colors=new List<Color>();
            public readonly List<Vector2> Uvs=new List<Vector2>();public readonly List<int> Triangles=new List<int>();
            public void Triangle(Vector2 a,Vector2 b,Vector2 c,Color color)
            {int first=Vertices.Count;foreach(var p in new[]{a,b,c}){Vertices.Add(p);Colors.Add(color);Uvs.Add(Vector2.zero);}Triangles.Add(first);Triangles.Add(first+1);Triangles.Add(first+2);}
            public void Quad(float x,float y,float width,float height,Color color)
            {Triangle(new Vector2(x,y),new Vector2(x,y+height),new Vector2(x+width,y+height),color);Triangle(new Vector2(x,y),new Vector2(x+width,y+height),new Vector2(x+width,y),color);}
        }
    }
    /// <summary>核心区只提供探索标记，通关仍由遭遇服务登记的敌人决定。</summary>
    public sealed class RunObjectiveMarker : RunInteractable
    {
        public override string DisplayName=>"核心目标区";
        public override bool Completed=>RunEncounter.RunCleared;
        public override bool CanInteract(GameObject actor,out string reason){reason="清理本区指定敌人";return false;}
        protected override bool Perform(GameObject actor,out string result){result="";return false;}
    }
}
