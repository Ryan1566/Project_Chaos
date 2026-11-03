using Chaos.MVP.Model.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>战斗相机拥有的飘字池，使用屏幕坐标避免镜头缩放和夜色改变文字。</summary>
public sealed class DamageFloaters : MonoBehaviour
{
    public const int Capacity = 48;
    private const float Lifetime = .8f;
    private static readonly Color EnemyColor = new Color(1f, .82f, .40f);
    private static readonly Color TeamColor = new Color(1f, .60f, .58f);

    private sealed class Entry
    {
        public TextMeshProUGUI Label;
        public Vector3 Origin;
        public Vector2 Offset;
        public Color Tint;
        public float Age;
        public int TargetId;
        public ulong Order;
        public bool Active;
    }

    private static DamageFloaters current;
    private readonly Entry[] entries = new Entry[Capacity];
    private Camera outputCamera;
    private RectTransform overlay;
    private ulong sequence;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntime() { current = null; }

    public static void Show(Transform target, int actual, bool team)
    {
        if (target == null || actual <= 0 || !RunEncounter.IsSceneReady) return;
        var camera = Camera.main;
        if (camera == null) return;
        if (current == null || current.outputCamera != camera)
        {
            current = camera.GetComponent<DamageFloaters>();
            if (current == null) current = camera.gameObject.AddComponent<DamageFloaters>();
        }
        current.Spawn(target, actual, team);
    }

    public static void Clear()
    {
        if (current == null) return;
        foreach (var entry in current.entries)
            if (entry != null) Hide(entry);
    }

    private void Awake() => outputCamera = GetComponent<Camera>();

    private void EnsureOverlay()
    {
        if (overlay != null) return;
        // 相机可跨场景保留，而场景中的覆盖层会卸载；下一次受击时重新建池。
        System.Array.Clear(entries, 0, entries.Length);
        var root = new GameObject("DamageFloaterCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GameplayCanvas));
        overlay = (RectTransform)root.transform;
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 79;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
    }

    private void Spawn(Transform target, int actual, bool team)
    {
        EnsureOverlay();
        int targetId = target.GetInstanceID(), lane = 0;
        Entry free = null, oldest = null;
        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            if (entry == null)
            {
                if (free == null) free = entries[i] = CreateEntry();
                continue;
            }
            if (!entry.Active) { if (free == null) free = entry; continue; }
            if (entry.TargetId == targetId) lane++;
            if (oldest == null || entry.Order < oldest.Order) oldest = entry;
        }
        var next = free ?? oldest;
        next.Active = true;
        next.Age = 0;
        next.Order = ++sequence;
        next.TargetId = targetId;
        next.Origin = CombatQuery.Center(target) + Vector2.up * .55f;
        next.Offset = new Vector2(lane == 0 ? 0 : (lane % 2 == 0 ? -1 : 1) * (10 + lane * 4), lane * 8);
        next.Tint = team ? TeamColor : EnemyColor;
        next.Label.text = actual.ToString();
        next.Label.gameObject.SetActive(true);
        Render(next);
    }

    private Entry CreateEntry()
    {
        var child = new GameObject("DamageNumber", typeof(RectTransform), typeof(TextMeshProUGUI));
        child.transform.SetParent(overlay, false);
        var label = child.GetComponent<TextMeshProUGUI>();
        label.rectTransform.sizeDelta = new Vector2(96, 36);
        label.font = RunTeamHud.ResolveFont();
        label.fontSize = 27;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return new Entry { Label = label };
    }

    private void LateUpdate()
    {
        if (overlay == null) return;
        float elapsed = RunControlModel.IsWorldPaused ? 0 : Time.deltaTime;
        foreach (var entry in entries)
        {
            if (entry == null || !entry.Active) continue;
            entry.Age += elapsed;
            if (entry.Age >= Lifetime) Hide(entry);
            else Render(entry);
        }
    }

    private void Render(Entry entry)
    {
        Vector3 screen = outputCamera.WorldToScreenPoint(entry.Origin);
        if (screen.z <= 0) { entry.Label.enabled = false; return; }
        entry.Label.enabled = true;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay, screen, null, out Vector2 point);
        float progress = entry.Age / Lifetime;
        entry.Label.rectTransform.anchoredPosition = point + entry.Offset + Vector2.up * Mathf.Lerp(0, 55, progress);
        var tint = entry.Tint;
        tint.a = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.25f, 1, progress));
        entry.Label.color = tint;
    }

    private static void Hide(Entry entry)
    {
        entry.Active = false;
        if (entry.Label != null) entry.Label.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
        if (overlay != null) Destroy(overlay.gameObject);
    }
}
