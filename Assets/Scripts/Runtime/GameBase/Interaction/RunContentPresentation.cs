using Spine.Unity;
using UnityEngine;
using RunMap;

namespace Chaos.Gameplay.Interaction
{
    /// <summary>Map-owned presentation only; no actor AI, body, item quantity or reward logic.</summary>
    public sealed class RunContentPresentation : MonoBehaviour
    {
        RunContentInteractable target;
        RunContentVisualSettings visuals;
        RunContentSession session;
        SpriteRenderer icon;
        bool openedOnce;
        static Sprite chest, openChest, herb, rock, stump;

        public void Initialize(RunContentInteractable content)
        {
            target = content; visuals = RunContentVisualSettings.Load();
            var marker = gameObject.AddComponent<RunMapMarkerPortrait>();
            marker.Kind = content.Definition.kind == RunContentKind.Npc ? RunMapMarkerKind.Npc : RunMapMarkerKind.Interactive;
            marker.RememberWhenHidden = content.Definition.kind != RunContentKind.Npc;
            if (content.Definition.kind == RunContentKind.Npc)
            {
                BuildSpine(); marker.Portrait = visuals.npcPortrait;
            }
            else
            {
                var visual = new GameObject("ContentSprite", typeof(SpriteRenderer)); visual.transform.SetParent(transform, false);
                visual.transform.localPosition = new Vector3(0, -.05f);
                icon = visual.GetComponent<SpriteRenderer>(); icon.sortingOrder = 5;
                Refresh(); marker.Portrait = icon.sprite;
            }
        }

        void BuildSpine()
        {
            var go = new GameObject("Spine", typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(transform, false);
            var animation = go.AddComponent<SkeletonAnimation>();
            animation.skeletonDataAsset = visuals.npcSkeleton; animation.initialSkinName = visuals.npcSkin;
            animation.Initialize(true);
            var idle = CharacterSpineClips.Find(animation.Skeleton?.Data, visuals.npcIdle);
            if (idle == null)
                throw new System.InvalidOperationException("NPC Spine缺少配置的待机动画：" + visuals.npcIdle);
            animation.AnimationState.SetAnimation(0, idle, true); animation.Update(0);
            float[] vertices = null;
            animation.Skeleton.GetBounds(out float x, out float y, out float width, out float height, ref vertices);
            if (height <= 0) throw new System.InvalidOperationException("NPC Spine无有效待机边界");
            float scale = visuals.npcHeight / height;
            go.transform.localScale = Vector3.one * scale;
            go.transform.localPosition = new Vector3(-(x + width * .5f) * scale, -.8f - y * scale);
            go.GetComponent<MeshRenderer>().sortingOrder = 5;
        }

        public void OnInteractionOpened(RunContentSession opened)
        {
            if (opened.Target != target) return;
            openedOnce = true;
            if (session != null) session.Changed -= Refresh;
            session = opened; session.Changed += Refresh; Refresh();
        }
        void Refresh()
        {
            if (icon == null || target == null) return;
            bool used = target.Completed;
            bool isChest = target.Definition.kind == RunContentKind.Chest;
            icon.sprite = isChest ? (openedOnce || used ? visuals.chestOpened : visuals.chestClosed)
                : used ? visuals.depleted : target.Definition.id == "ore" ? visuals.ore : visuals.herbs;
            if (icon.sprite == null) icon.sprite = Fallback(isChest, isChest ? openedOnce || used : used, target.Definition.id == "ore");
            icon.color = used ? new Color(.62f, .66f, .70f) : Color.white;
        }
        void OnDestroy()
        {
            if (session != null) session.Changed -= Refresh;
        }

        // Small readable silhouettes while art is pending. Authored sprites above take precedence.
        static Sprite Fallback(bool isChest, bool used, bool isOre)
        {
            if (isChest) return used ? openChest ?? (openChest = Draw(1)) : chest ?? (chest = Draw(0));
            return used ? stump ?? (stump = Draw(4)) : isOre ? rock ?? (rock = Draw(3)) : herb ?? (herb = Draw(2));
        }
        static Sprite Draw(int kind)
        {
            const int size = 24;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { name = "RunContentSilhouette" + kind, filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                Color c = Color.clear;
                if (kind < 2)
                {
                    if (x >= 3 && x <= 20 && y >= 3 && y <= 12) c = new Color(.55f, .30f, .12f);
                    if (x >= 3 && x <= 20 && y >= (kind == 0 ? 13 : 18) && y <= (kind == 0 ? 18 : 21)) c = new Color(.85f, .58f, .20f);
                    if ((x == 5 || x == 18) && y >= 3 && y <= 18) c = new Color(.95f, .77f, .30f);
                    if (x >= 10 && x <= 13 && y >= 10 && y <= 14) c = new Color(1, .9f, .3f);
                }
                else if (kind == 2)
                {
                    if (x >= 11 && x <= 12 && y >= 2 && y < 20) c = new Color(.22f, .53f, .17f);
                    if (y >= 7 && y <= 16 && (Mathf.Abs(x - (18 - y / 2)) + Mathf.Abs(y - 11) < 6
                        || Mathf.Abs(x - (10 + y / 2)) + Mathf.Abs(y - 14) < 6)) c = new Color(.35f, .85f, .26f);
                }
                else if (kind == 3)
                {
                    if (y >= 3 && y <= 18 && x >= 3 + y / 3 && x <= 22 - y / 4) c = x < 13 ? new Color(.48f, .58f, .70f) : new Color(.72f, .80f, .88f);
                }
                else if (y >= 2 && y <= 5 && x >= 6 && x <= 18) c = new Color(.45f, .36f, .25f);
                pixels[y * size + x] = c;
            }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
            sprite.hideFlags = HideFlags.HideAndDontSave; return sprite;
        }
    }
}
