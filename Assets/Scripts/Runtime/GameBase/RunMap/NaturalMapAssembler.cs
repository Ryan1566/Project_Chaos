using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RunMap
{
    /// <summary>Only this layer creates Unity objects. The plan has already passed geometric validation.</summary>
    public sealed class NaturalMapAssembler
    {
        public GameObject Root { get; private set; }
        public string Error { get; private set; }
        private readonly NaturalMapPlan plan;
        public NaturalMapAssembler(NaturalMapPlan source) { plan = source; }

        public IEnumerable<string> Steps(Transform parent)
        {
            Root = new GameObject("NaturalRunMap_v2", typeof(Grid));
            Root.SetActive(false); Root.transform.SetParent(parent, false);
            var groundGo = new GameObject("Tilemap_Ground", typeof(Tilemap), typeof(TilemapRenderer));
            groundGo.transform.SetParent(Root.transform, false);
            var ground = groundGo.GetComponent<Tilemap>();
            var backdropGo = new GameObject("Tilemap_UndergroundBackdrop", typeof(Tilemap), typeof(TilemapRenderer));
            backdropGo.transform.SetParent(Root.transform, false);
            var backdrop = backdropGo.GetComponent<Tilemap>();
            backdropGo.GetComponent<TilemapRenderer>().sortingOrder = -10;
            backdrop.color = new Color(.30f, .35f, .43f, 1f);
            var fills = new TileBase[3]; var tops = new TileBase[3];
            TileBase backgroundTile = null;
            string[] themes = { "Surface", "Underground", "Building" };
            for (int i = 0; i < themes.Length; i++)
            {
                var source = Resources.Load<GameObject>("Prefabs/RunMap/Kenney/" + themes[i] + "/Chunk_" + themes[i] + "_Combat");
                var palette = source == null ? null : source.GetComponent<RunMapChunkTiles>();
                if (palette == null || palette.fill == null)
                { Error = "缺少地形瓦片主题：" + themes[i]; Dispose(); yield break; }
                fills[i] = palette.fill; tops[i] = palette.top != null ? palette.top : palette.fill;
                if (i == 1)
                {
                    var decoration = source.transform.Find("Tilemap_Deco");
                    if (decoration != null) backgroundTile = decoration.GetComponent<Tilemap>().GetTile(new Vector3Int(5, 5, 0));
                }
            }
            if (backgroundTile == null) backgroundTile = fills[1];
            groundGo.GetComponent<TilemapRenderer>().sortingOrder = 0;
            yield return "创建地形瓦片";
            var clock = System.Diagnostics.Stopwatch.StartNew();
            for (int x = 0; x < plan.Width; x++)
            {
                var cells = new List<Vector3Int>(); var tiles = new List<TileBase>();
                var backgroundCells = new List<Vector3Int>();
                for (int y = 0; y < plan.Height; y++)
                {
                    if (!plan.Solid[plan.Index(x, y)])
                    {
                        if (y < plan.SurfaceY[x] - 3) backgroundCells.Add(new Vector3Int(x, y, 0));
                        continue;
                    }
                    int space = (int)plan.SpaceAt(x, y);
                    cells.Add(new Vector3Int(x, y, 0)); tiles.Add(!plan.IsSolid(x, y + 1) ? tops[space] : fills[space]);
                }
                ground.SetTiles(cells.ToArray(), tiles.ToArray());
                var backgrounds = new TileBase[backgroundCells.Count];
                for (int i = 0; i < backgrounds.Length; i++) backgrounds[i] = backgroundTile;
                backdrop.SetTiles(backgroundCells.ToArray(), backgrounds);
                foreach (var cell in cells) ground.SetColliderType(cell, Tile.ColliderType.Grid);
                if ((x + 1) % plan.Parameters.workColumnsPerFrame == 0 || clock.Elapsed.TotalMilliseconds >= plan.Parameters.frameBudgetMs)
                { yield return null; clock.Restart(); }
            }
            yield return "落地手工结构";
            foreach (var structure in plan.Structures)
            {
                var prefab = Resources.Load<GameObject>(structure.Template.ResourcePath);
                if (prefab == null) { Error = "手工结构资源失效"; Dispose(); yield break; }
                var instance = UnityEngine.Object.Instantiate(prefab, Root.transform);
                instance.name = structure.StableId.Replace('/', '_');
                instance.transform.localPosition = new Vector3(structure.MirrorX ? structure.Bounds.xMax : structure.Bounds.x, structure.Bounds.y);
                instance.transform.localScale = new Vector3(structure.MirrorX ? -1 : 1, 1, 1);
                foreach (var collider in instance.GetComponentsInChildren<Collider2D>(true))
                    if (collider.name == "CameraBounds") collider.enabled = false;
                // Geometry/collision is the stamped plan. Authored tile renderers remain as the visual structure.
                foreach (var tilemap in instance.GetComponentsInChildren<Tilemap>(true))
                {
                    if (tilemap.name != "Tilemap_Ground" && tilemap.name != "Tilemap_Deco") continue;
                    var localCollider = tilemap.GetComponent<TilemapCollider2D>();
                    if (localCollider != null) localCollider.enabled = false;
                    if (tilemap.name == "Tilemap_Ground")
                        for (int x = 0; x < structure.Bounds.width; x++)
                            for (int y = 0; y < structure.Bounds.height; y++)
                            {
                                var cell = new Vector3Int(x, y, 0);
                                int wx = structure.MirrorX ? structure.Bounds.xMax - 1 - x : structure.Bounds.x + x;
                                if (!plan.IsSolid(wx, structure.Bounds.y + y)) tilemap.SetTile(cell, null);
                            }
                    for (int x = structure.Template.ShaftColumn - 1; x <= structure.Template.ShaftColumn + 1; x++)
                    {
                        if (tilemap.name != "Tilemap_Ground") continue;
                        for (int y = 0; y < structure.Template.EntranceFloor; y++) tilemap.SetTile(new Vector3Int(x, y, 0), fills[2]);
                        for (int y = structure.Bounds.height - 2; y < structure.Bounds.height; y++) tilemap.SetTile(new Vector3Int(x, y, 0), fills[2]);
                    }
                }
                yield return null;
            }
            yield return "建立地形碰撞";
            var body = groundGo.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Static;
            var tileCollider = groundGo.AddComponent<TilemapCollider2D>(); tileCollider.usedByComposite = true;
            var composite = groundGo.AddComponent<CompositeCollider2D>(); composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            tileCollider.ProcessTilemapChanges(); composite.GenerateGeometry();
            yield return null;
            yield return "创建内容占位物";
            foreach (var p in plan.Placements)
            {
                if (p.Kind == MapPlacementKind.Player || p.Kind == MapPlacementKind.Ally || p.Kind == MapPlacementKind.Enemy) continue;
                CreatePlaceholder(p);
            }
            Root.SetActive(true);
            Physics2D.SyncTransforms();
            if (!VerifyPhysics(groundGo)) { Error = "场景碰撞与计划中的必需支撑面不一致"; Dispose(); yield break; }
            yield return "场景地图校验完成";
        }

        private bool VerifyPhysics(GameObject ground)
        {
            // Pending containers are inactive during loading: use the component's local geometry there.
            // An active-root verification runs again when the pending map moves into WorldScene.
            if (!Root.activeInHierarchy) return true;
            var collider = ground.GetComponent<CompositeCollider2D>();
            foreach (var p in plan.Placements)
            {
                if (p.Solid || p.Kind == MapPlacementKind.Decoration) continue;
                Vector2 support = p.Position + Vector2.down * .10f;
                if (!collider.OverlapPoint(support)) return false;
            }
            return true;
        }

        public static bool VerifyActive(NaturalMapPlan plan, GameObject root)
        {
            if (plan == null || root == null || !root.activeInHierarchy) return false;
            var ground = root.transform.Find("Tilemap_Ground");
            var collider = ground == null ? null : ground.GetComponent<CompositeCollider2D>();
            if (collider == null || collider.pathCount == 0) return false;
            foreach (var p in plan.Placements)
            {
                if (p.Solid || p.Kind == MapPlacementKind.Decoration) continue;
                if (!collider.OverlapPoint(p.Position + Vector2.down * .10f)) return false;
            }
            return true;
        }

        private void CreatePlaceholder(MapPlacement p)
        {
            var go = new GameObject(p.StableId.Replace('/', '_'), typeof(SpriteRenderer)); go.transform.SetParent(Root.transform, false);
            go.transform.localPosition = p.Position + Vector2.up * (p.Kind == MapPlacementKind.Npc ? .8f : .45f);
            var sr = go.GetComponent<SpriteRenderer>(); sr.sprite = MapPopulationPlaceholder.WhiteSprite;
            sr.sortingOrder = p.Kind == MapPlacementKind.Decoration ? -1 : 5;
            sr.color = p.Kind == MapPlacementKind.Npc ? new Color(.2f, .75f, 1f) : p.Kind == MapPlacementKind.Interactive ? new Color(1f, .8f, .15f)
                : p.Kind == MapPlacementKind.Prop ? new Color(.55f, .35f, .15f) : p.Kind == MapPlacementKind.Exit ? new Color(.25f, 1f, .4f, .6f) : new Color(.25f, .6f, .25f, .7f);
            go.transform.localScale = p.Kind == MapPlacementKind.Npc ? new Vector3(.6f, 1.6f, 1) : new Vector3(.8f, .8f, 1);
            if (p.Kind == MapPlacementKind.Npc || p.Kind == MapPlacementKind.Interactive || p.Kind == MapPlacementKind.Exit)
            {
                var trigger = go.AddComponent<BoxCollider2D>(); trigger.isTrigger = true;
                var marker = go.AddComponent<MapPopulationPlaceholder>(); marker.StableId = p.StableId; marker.Kind = p.Kind;
            }
            // Solid prop collision is already baked into the plan and the common ground collider.
        }

        public void Dispose()
        {
            if (Root == null) return;
            Root.SetActive(false);
            if (Application.isPlaying) UnityEngine.Object.Destroy(Root); else UnityEngine.Object.DestroyImmediate(Root);
            Root = null;
        }
    }
}
