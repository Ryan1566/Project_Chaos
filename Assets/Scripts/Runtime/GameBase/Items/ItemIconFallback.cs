using UnityEngine;

namespace Chaos.Gameplay.Items
{
    /// <summary>Small cached procedural icons until authored sprites are assigned. No shared Resources assets are destroyed.</summary>
    public static class ItemIconFallback
    {
        static readonly Sprite[] Icons = new Sprite[6];
        public static Sprite Resolve(ItemParentCategory category)
        {
            int index = Mathf.Clamp((int)category, 1, 5);
            if (Icons[index] != null) return Icons[index];
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { name = "ItemPlaceholder" + category, hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color[32 * 32];
            Color c = index == 1 ? new Color(1, .78f, .25f) : index == 2 ? new Color(.95f, .3f, .3f) : index == 3 ? new Color(.3f, .7f, 1) : Color.gray;
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                bool inside = index == 2 ? (x >= 13 && x <= 18 && y >= 5 && y <= 26) || (y >= 13 && y <= 18 && x >= 5 && x <= 26)
                    : index == 1 ? Mathf.Abs(x - 16) + Mathf.Abs(y - 16) <= 12 : x >= 7 && x <= 24 && y >= 6 && y <= 25;
                pixels[y * 32 + x] = inside ? c : Color.clear;
            }
            tex.SetPixels(pixels); tex.Apply(false, true);
            return Icons[index] = Sprite.Create(tex, new Rect(0, 0, 32, 32), Vector2.one * .5f, 32);
        }
    }
}
