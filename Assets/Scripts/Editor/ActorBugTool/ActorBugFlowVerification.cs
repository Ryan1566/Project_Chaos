#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using RunMap;
using Spine.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>Owned test storage, real prefabs, fixed-step tile-wall physics and the formal three-map lifecycle.</summary>
public static class ActorBugFlowVerification
{
    public static string Status { get; private set; } = "idle";
    static readonly StringBuilder Report = new StringBuilder();
    static Keyboard keys;
    static string storage, overrides;
    static BasePanel Top => UIManager.Instance.panelStack.Count == 0 ? null : UIManager.Instance.panelStack.Peek();
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string label)
    { if (!value) throw new Exception(label); Report.AppendLine("PASS " + label); Flush(); }
    static void Flush()
    { Directory.CreateDirectory(Application.dataPath + "/QA/ActorMapBugs"); File.WriteAllText(Application.dataPath + "/QA/ActorMapBugs/flow.txt", Report.ToString()); }
    static IEnumerator Press(Key key)
    { InputSystem.QueueStateEvent(keys, new KeyboardState(key)); yield return new WaitForSecondsRealtime(.2f); InputSystem.QueueStateEvent(keys, new KeyboardState()); yield return new WaitForSecondsRealtime(.2f); }
    static void Click(string name)
    { var b = Top.GetComponentsInChildren<Button>(false).Single(x => x.name == name && x.interactable); ExecuteEvents.Execute(b.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler); }
    public static void Start()
    {
        if (!Application.isPlaying || Status == "running") throw new Exception("requires idle Play");
        Status = "running"; Report.Clear();
        storage = Path.Combine(Path.GetTempPath(), "ChaosVillageIsolated-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(storage);
        SaveSlotService.UseEditorTestDirectory(storage);
        overrides = InputManager.Instance.SaveOverridesJson(); InputManager.Instance.LoadOverridesJson("");
        keys = InputSystem.AddDevice<Keyboard>("ChaosActorBugKeyboard");
        UnityEngine.Object.FindObjectOfType<MonoController>().StartCoroutine(Guard(Run()));
    }
    static IEnumerator Guard(IEnumerator source)
    {
        var stack = new Stack<IEnumerator>(); stack.Push(source);
        while (stack.Count > 0)
        {
            object value = null; bool more;
            try { more = stack.Peek().MoveNext(); if (more) value = stack.Peek().Current; }
            catch (Exception e) { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); Status = "failed"; Report.AppendLine("FAIL " + e); Cleanup(); Flush(); yield break; }
            if (!more) { stack.Pop(); continue; }
            if (value is IEnumerator nested) { stack.Push(nested); continue; }
            yield return value;
        }
        Status = "passed"; Cleanup(); Report.AppendLine("DONE isolated storage=" + storage); Flush();
    }
    static void Cleanup()
    { InputManager.Instance.LoadOverridesJson(overrides); if (keys != null && keys.added) InputSystem.RemoveDevice(keys); }
    static IEnumerator Run()
    {
        GameSession.Instance.BeginSandbox(new SavePayload { resMoney = 1000, resGoods = 1000, resMaterial = 1000, resBlueprint = 1000 });
        for (int i = 0; i < 4; i++) RetainerService.Instance.RecruitRandom();
        var payload = JsonUtility.FromJson<SavePayload>(JsonUtility.ToJson(GameSession.Instance.LoadedPayload));
        GameSession.Instance.Clear(); Check(SaveSlotService.Instance.Save(3, payload), "isolated save created");
        GameSession.Instance.Begin(3, payload, "MainScene"); VillageSceneFlow.EnterFromSave();
        float deadline = Time.realtimeSinceStartup + 40;
        while (!VillageSceneBootstrap.IsReady && Time.realtimeSinceStartup < deadline) yield return null;
        Check(VillageSceneBootstrap.IsReady, "formal village ready");
        yield return WallPhysics(false);
        yield return Press(Key.M); Check(Top is WorldMapPanel, "M opens world map"); Click("EnterRunBtn"); yield return new WaitForSecondsRealtime(.3f);
        string reason; foreach (var r in RetainerService.Instance.GetAll()) DispatchService.Instance.Toggle(r.id, out reason);
        Click("Btn_Depart"); yield return RunMapFlowVerification.ConfirmBattlePreparation(); deadline = Time.realtimeSinceStartup + 40;
        while ((!RunEncounter.IsSceneReady || RunSceneFlow.IsTransitioning) && Time.realtimeSinceStartup < deadline) yield return null;
        Check(RunEncounter.IsSceneReady && AllyController.All.Count == 3, "formal map1 ready with three dispatched allies");
        var enemies = new List<EnemyPlaceholder>(); RunEncounter.CopyMapEnemies(enemies);
        foreach (var e in enemies) if (e != null) e.enabled = false;
        yield return DamagePresentation();
        yield return WallPhysics(true);
        var player = PlayerSpawner.CurrentPlayer; var move = player.GetComponent<PlayerLocomotion>();
        var followers = AllyController.All.ToArray(); var before = followers.Select(a => (Vector2)a.transform.position).ToArray();
        move.InjectInput(NaturalMapRuntime.Current.StartsOnRight ? -1 : 1, false, 1.3f); yield return new WaitForSeconds(1.4f);
        Check(followers.All(a => a.GetComponent<RunMapNavigator>() != null && !a.IsDowned), "three real allies retain live navigator and health");
        Check(followers.Where((a,i) => Vector2.Distance(a.transform.position,before[i]) > .1f).Any(), "dispatched AI follows moving controlled character");
        yield return Press(Key.I); Check(Top is RunMenuPanel && Time.timeScale == 0, "inventory pause");
        Check(followers.All(a => !a.GetComponent<PlayerSurfacePhysics>().IsSlideMaterialActive), "pause restores every dispatched ally material");
        yield return Press(Key.Escape);
        var oldMap = RunMapBootstrap.CurrentMapRoot;
        for (int stage = 1; stage <= 3; stage++)
        {
            Check(PhysicsAligned(), "active map" + stage + " physics matches solid cells");
            RunEncounter.CopyMapEnemies(enemies); foreach (var e in enemies) if (e != null && !e.IsDead) e.TakeDamage(99999);
            RunEncounter.RefreshNow(); deadline = Time.realtimeSinceStartup + 40;
            if (stage < 3)
            {
                while ((RunEncounter.CurrentSceneIndex != stage + 1 || !RunEncounter.IsSceneReady || RunSceneFlow.IsTransitioning) && Time.realtimeSinceStartup < deadline) yield return null;
                Check(RunEncounter.IsSceneReady && RunEncounter.CurrentSceneIndex == stage + 1, "next map ready " + (stage+1));
                Check(AllyController.All.Count == 3 && AllyController.All.All(a => a.GetComponent<PlayerSurfacePhysics>() != null), "next map creates three physics-capable allies");
                RunEncounter.CopyMapEnemies(enemies); foreach (var e in enemies) if (e != null) e.enabled = false;
            }
            else { while (!(Top is RunSettlementPanel) && Time.realtimeSinceStartup < deadline) yield return null; Check(Top is RunSettlementPanel, "three-map settlement"); }
        }
        yield return Press(Key.Escape); deadline = Time.realtimeSinceStartup + 40;
        while ((!VillageSceneBootstrap.IsReady || !VillageSceneFlow.IsVillageScene) && Time.realtimeSinceStartup < deadline) yield return null;
        Check(VillageSceneBootstrap.IsReady && VillageSceneFlow.IsVillageScene, "settlement returns physical village");
        yield return null; Check(oldMap == null && AllyController.All.Count == 0, "old map and dispatched allies cleaned up");
        Check(SaveSlotService.SavesDirectory == storage, "save isolation preserved across entire flow");
    }
    static bool PhysicsAligned()
    {
        var plan = NaturalMapRuntime.Current; var root = RunMapBootstrap.CurrentMapRoot;
        var hits = new Collider2D[32]; var filter = new ContactFilter2D { useTriggers = false };
        for (int y=0;y<plan.Height;y++) for(int x=0;x<plan.Width;x++)
        {
            Vector2 p = new Vector2(x+.517f,y+.523f); bool actual=false;
            int count=Physics2D.OverlapPoint(p,filter,hits); if(count==hits.Length)return false;
            for(int i=0;i<count;i++) if(hits[i].transform.IsChildOf(root.transform)){actual=true;break;}
            if(actual!=plan.IsSolid(x,y))return false;
        }
        return NaturalMapAssembler.VerifyActive(plan,root);
    }
    static IEnumerator DamagePresentation()
    {
        var go=PlayerSpawner.CurrentPlayer; var hp=go.GetComponent<PlayerHealth>(); var move=go.GetComponent<PlayerLocomotion>();
        var body=go.GetComponent<Rigidbody2D>(); var skeleton=go.GetComponentInChildren<SkeletonAnimation>();
        Check(skeleton != null && skeleton.Skeleton != null, "actual Spine character available");
        bool oldSim=body.simulated; body.simulated=false; float invuln=hp.invulnSeconds; hp.invulnSeconds=.1f;
        move.InjectInput(0,false,10); var spineScale=skeleton.transform.localScale; float sx=skeleton.Skeleton.ScaleX, sy=skeleton.Skeleton.ScaleY;
        try
        {
            foreach(int facing in new[]{1,-1})
            {
                move.SetFacing(facing); var scale=go.transform.localScale;
                Check(hp.ApplyContactDamage(1), "damage accepted facing " + facing);
                for(int i=0;i<12;i++)
                {
                    yield return new WaitForSecondsRealtime(.025f);
                    Check(go.transform.localScale==scale && skeleton.transform.localScale==spineScale && skeleton.Skeleton.ScaleX==sx && skeleton.Skeleton.ScaleY==sy, "hit frame scale/facing stable " + facing+"/"+i);
                    if(i==2){move.SetFacing(-facing);scale=go.transform.localScale;}
                }
            }
            hp.enabled=false; yield return null; hp.enabled=true;
            var stable=go.transform.localScale;
            Check(go.transform.localScale==stable && skeleton.transform.localScale==spineScale, "health disable/re-enable preserves transforms");
            var current=hp.Hp; hp.SetRunHealthBonus(10); Check(hp.Hp==current && hp.RestoreHealth(1)==1, "growth does not heal and normal healing still works");
            hp.ForceDown(); yield return null; Check(go.transform.localScale==stable && skeleton.transform.localScale==spineScale, "forced down preserves facing and scale");
            hp.RefillFullHp(); yield return null; Check(hp.Hp==hp.maxHp && go.transform.localScale==stable, "camp revival preserves facing and scale");
            hp.SetRunHealthBonus(0);
        }
        finally { body.simulated=oldSim; hp.invulnSeconds=invuln; }
        hp.invulnSeconds=100000; hp.ApplyContactDamage(1);
    }
    static IEnumerator WallPhysics(bool natural)
    {
        var root=new GameObject("ActorBugTemporaryWall"); root.transform.position=new Vector3(10000,10000);
        var material=new PhysicsMaterial2D("ActorBugOwnedFriction") {friction=.8f};
        var tile=ScriptableObject.CreateInstance<Tile>(); tile.colliderType=Tile.ColliderType.Grid;
        var allyPrefab=Resources.Load<GameObject>("Player/Ally");
        try
        {
            var grid=root.AddComponent<Grid>(); var terrain=new GameObject("TileWall",typeof(Tilemap),typeof(TilemapRenderer)); terrain.transform.SetParent(root.transform,false);
            var map=terrain.GetComponent<Tilemap>();
            for(int x=-20;x<=20;x++)map.SetTile(new Vector3Int(x,-1,0),tile);
            for(int y=0;y<30;y++)map.SetTile(new Vector3Int(1,y,0),tile);
            var tc=terrain.AddComponent<TilemapCollider2D>();tc.sharedMaterial=material;tc.ProcessTilemapChanges();
            root.SetActive(false);
            var actor=UnityEngine.Object.Instantiate(allyPrefab,root.transform.position+new Vector3(.5f,12),Quaternion.identity,root.transform);
            var ai=actor.GetComponent<AllyController>(); var body=actor.GetComponent<Rigidbody2D>(); var col=actor.GetComponent<Collider2D>();
            col.sharedMaterial=material; root.SetActive(true);
            var surface=actor.GetComponent<PlayerSurfacePhysics>(); var cachedMaterial=col.sharedMaterial;
            var push=actor.AddComponent<ActorBugWallPush>();push.speed=5;
            yield return null; Physics2D.SyncTransforms();
            Check((actor.GetComponent<RunMapNavigator>()!=null)==natural, "fixture uses expected " + (natural?"navigator":"legacy") + " ally setup");
            ai.enabled=false;
            for(int i=0;i<20;i++)yield return new WaitForFixedUpdate();
            Check(!surface.IsSlideMaterialActive && Mathf.Abs(body.velocity.y)<.2f, "control without slide reproduces high-friction wall hang " + natural);
            ai.enabled=true;
            float start=body.position.y;bool sliding=false;
            for(int i=0;i<35;i++){yield return new WaitForFixedUpdate(); sliding|=surface.IsSlideMaterialActive;}
            Check(sliding && body.position.y < start-2 && body.velocity.y < -4, "real ally falls while pushed into Tilemap wall " + natural);
            Check(!surface.ProbeGround() && !(bool)typeof(AllyController).GetMethod("IsGrounded",Private).Invoke(ai,null), "tile side is not ally ground " + natural);
            int jumps=ai.JumpCount;typeof(AllyController).GetMethod("TryJumpIfBlocked",Private).Invoke(ai,new object[]{1});
            Check(ai.JumpCount==jumps && body.velocity.y<0, "airborne wall cannot trigger legacy jump " + natural);
            push.speed=0; body.position=(Vector2)root.transform.position+new Vector2(-5,.03f);body.velocity=Vector2.zero;Physics2D.SyncTransforms();
            for(int i=0;i<10;i++)yield return new WaitForFixedUpdate();
            Check(surface.ProbeGround() && !surface.IsSlideMaterialActive && col.sharedMaterial==cachedMaterial, "flat landing restores original material " + natural);
            var other=UnityEngine.Object.Instantiate(allyPrefab,root.transform.position+new Vector3(-10,7),Quaternion.identity,root.transform);
            var otherBody=other.GetComponent<Rigidbody2D>(); otherBody.bodyType=RigidbodyType2D.Kinematic;
            Physics2D.SyncTransforms(); var otherShape=other.GetComponent<Collider2D>();
            float feetOffset=body.position.y-col.bounds.min.y;
            body.position=new Vector2(otherShape.bounds.center.x,otherShape.bounds.max.y+feetOffset+.02f); Physics2D.SyncTransforms();
            Check(surface.ProbeGround() && !(bool)typeof(AllyController).GetMethod("IsGrounded",Private).Invoke(ai,null), "other characters remain excluded from ally terrain grounding " + natural);
            otherBody.bodyType=RigidbodyType2D.Dynamic;
            body.position=(Vector2)root.transform.position+new Vector2(-5,.03f);body.velocity=Vector2.zero; Physics2D.SyncTransforms();
            var otherSurface=other.GetComponent<PlayerSurfacePhysics>(); surface.SetSlideMaterial(true);otherSurface.SetSlideMaterial(true);
            Check(col.sharedMaterial!=other.GetComponent<Collider2D>().sharedMaterial && col.sharedMaterial.friction==0 && material.friction==.8f, "each ally owns separate low-friction material " + natural);
            ai.enabled=false;Check(!surface.IsSlideMaterialActive && AllyController.All.Contains(ai), "disable restores material and retains All membership " + natural);
            ai.enabled=true;surface.SetSlideMaterial(true);ai.ForceDown();Check(!surface.IsSlideMaterialActive && !ai.enabled && AllyController.All.Contains(ai), "down restores material and preserves roster " + natural);
            var slope=new GameObject("Slope");slope.transform.SetParent(root.transform,false);slope.transform.localPosition=new Vector3(-15,0);slope.transform.rotation=Quaternion.Euler(0,0,30);
            var sc=slope.AddComponent<BoxCollider2D>();sc.size=new Vector2(8,.4f);
            var ob=other.GetComponent<Rigidbody2D>();ob.position=(Vector2)root.transform.position+new Vector2(-15,.4f);ob.velocity=Vector2.zero;other.GetComponent<AllyController>().enabled=false;Physics2D.SyncTransforms();
            for(int i=0;i<12;i++)yield return new WaitForFixedUpdate();
            Check(otherSurface.ProbeGround(), "walkable slope remains ground " + natural);
            otherSurface.SetSlideMaterial(true);var owned=other.GetComponent<Collider2D>().sharedMaterial;UnityEngine.Object.Destroy(other);yield return null;yield return null;
            Check(owned==null && material.friction==.8f, "destroy releases owned material without editing terrain " + natural);
            Check(Mathf.Abs(body.gravityScale-3)<.001f && Mathf.Abs(ai.moveSpeed-5)<.001f && Mathf.Abs(ai.jumpForce-11)<.001f, "ally physics values unchanged " + natural);
        }
        finally {UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(material);UnityEngine.Object.Destroy(tile);}
        yield return null;
    }
}

/// <summary>Test-only commanded horizontal speed, applied after normal AI fixed updates and before physics.</summary>
[DefaultExecutionOrder(10000)]
public sealed class ActorBugWallPush : MonoBehaviour
{
    public float speed;
    void FixedUpdate(){var b=GetComponent<Rigidbody2D>();b.velocity=new Vector2(speed,b.velocity.y);}
}
#endif
