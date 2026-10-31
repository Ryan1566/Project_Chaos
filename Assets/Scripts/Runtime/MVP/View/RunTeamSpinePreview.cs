using System;
using Spine;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One cached Spine SkeletonGraphic for the passive Team view.
/// The graphic is created lazily, reused for every Render, and only re-created when the
/// requested asset or material changes. Skin changes are applied in place on the cached
/// skeleton; the idle track keeps playing (and its time) across equivalent Shows.
/// The shared material is never destroyed or instanced.
/// The viewport clips via a RectMask2D plus geometry-based fitting so the skeleton is
/// centred and scaled inside the viewport instead of overflowing it.
/// </summary>
public sealed class RunTeamSpinePreview : MonoBehaviour
{
    const string IdleAnimationName = "Idle_Normal";
    const float FillRatio = 0.96f;
    const float PortraitHorizontalFill = 0.55f; // crop horizontally for a head shot
    const float MinViewportSize = 1f;

    [SerializeField] string _graphicName = "SpineGraphic";

    RectTransform _viewport;
    RectMask2D _mask;
    SkeletonGraphic _graphic;
    SkeletonDataAsset _asset;
    string _skin;
    Material _material;
    bool _portrait;
    bool _hasRequest;
    bool _configured;
    bool _fitted;
    float[] _boundsBuffer;
    int _syncedRenderers = -1;

    /// <summary>Viewport hosting the preview; available after Awake.</summary>
    public RectTransform Viewport
    {
        get { return _viewport; }
    }

    /// <summary>True when a live skeleton graphic is currently displayed.</summary>
    public bool IsShowing
    {
        get { return _hasRequest && _configured && _graphic != null && _graphic.gameObject.activeSelf; }
    }

    void Awake()
    {
        EnsureViewport();
        SetGraphicVisible(false);
    }

    void OnEnable()
    {
        // Restore only what was last requested; a hidden preview stays hidden.
        if (_hasRequest) Show(_asset, _skin, _material, _portrait);
    }

    void OnDisable()
    {
        // Release the graphic's visibility while the panel is hidden; the graphic is reused.
        SetGraphicVisible(false);
    }

    void OnDestroy()
    {
        // Only this preview's own child graphic is destroyed; the shared material is untouched.
        var host = _graphic != null ? _graphic.gameObject : null;
        _graphic = null;
        _configured = false;
        _hasRequest = false;
        if (host == null) return;

        host.SetActive(false);
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(host);
            return;
        }
#endif
        Destroy(host);
    }

    void LateUpdate()
    {
        // Retry while zero-sized viewports could not be fitted (TryRender re-shows the
        // graphic once the fit succeeds).
        if (!_hasRequest || _graphic == null || !gameObject.activeInHierarchy) return;
        if (!_fitted) TryRender();
        if (_fitted && _syncedRenderers != _graphic.canvasRenderers.Count) SyncRendererBounds();
    }

    /// <summary>
    /// Shows the requested character. A missing asset or material hides the preview so the
    /// owning view can fall back to readable initials. Repeated identical calls keep the
    /// cached graphic, its animation track and its playback time untouched.
    /// </summary>
    public void Show(SkeletonDataAsset asset, string skin, Material material, bool portrait)
    {
        EnsureViewport();

        if (asset == null || material == null)
        {
            Hide();
            return;
        }

        _hasRequest = true;
        bool sameConfig = _graphic != null
                          && _asset == asset
                          && _skin == skin
                          && _material == material
                          && _portrait == portrait;

        if (sameConfig)
        {
            _configured = _graphic.Skeleton != null;
            if (!_fitted || !_graphic.gameObject.activeSelf) TryRender();
            return;
        }

        bool reuseGraphic = _graphic != null && _asset == asset && _material == material;

        _asset = asset;
        _skin = skin;
        _material = material;
        _portrait = portrait;
        _fitted = false;

        if (reuseGraphic)
        {
            // Same asset + material: keep the skeleton and its playing idle track, only swap
            // the skin in place and refit for the new display mode.
            ApplySkin(_graphic.Skeleton);
            _configured = true;
            ApplyVisibility();
            return;
        }

        Configure();
    }

    /// <summary>Hides the preview without discarding the cached graphic.</summary>
    public void Hide()
    {
        _hasRequest = false;
        _configured = false;
        _fitted = false;
        SetGraphicVisible(false);
    }

    void Configure()
    {
        if (_asset == null || _material == null || _viewport == null)
        {
            Hide();
            return;
        }

        if (_graphic == null || _graphic.SkeletonDataAsset != _asset || _graphic.material != _material)
        {
            // Material is fixed at creation time; a different material needs a new graphic.
            DestroyGraphic();
            _graphic = CreateGraphic();
            if (_graphic == null)
            {
                Hide();
                return;
            }
        }

        _graphic.unscaledTime = true;
        _graphic.Initialize(false);
        _graphic.allowMultipleCanvasRenderers = true;

        var data = _asset.GetSkeletonData(false);
        if (data == null || data.Animations == null || data.Animations.Count == 0)
        {
            Hide();
            return;
        }

        var skeleton = _graphic.Skeleton;
        var animationState = _graphic.AnimationState;
        if (skeleton == null || animationState == null)
        {
            Hide();
            return;
        }

        ApplySkin(skeleton);

        string animation = ResolveAnimation(data);
        if (string.IsNullOrEmpty(animation))
        {
            Hide();
            return;
        }

        skeleton.SetToSetupPose();
        animationState.ClearTracks();
        animationState.SetAnimation(0, animation, true);
        animationState.Apply(skeleton);
        skeleton.UpdateWorldTransform();

        _configured = true;
        ApplyVisibility();
    }

    void ApplySkin(Skeleton skeleton)
    {
        if (skeleton == null) return;

        var data = skeleton.Data;
        Skin target = null;
        if (!string.IsNullOrEmpty(_skin) && data != null) target = data.FindSkin(_skin);
        if (target == null && data != null) target = data.DefaultSkin;

        skeleton.SetSkin(target);
        skeleton.SetSlotsToSetupPose();
    }

    SkeletonGraphic CreateGraphic()
    {
        // Material is fixed at creation time; the shared material is only referenced.
        var created = SkeletonGraphic.NewSkeletonGraphicGameObject(_asset, _viewport, _material);
        if (created == null) return null;

        created.name = string.IsNullOrEmpty(_graphicName) ? "SpineGraphic" : _graphicName;
        created.raycastTarget = false;
        created.allowMultipleCanvasRenderers = true;
        created.maskable = true;

        var rt = created.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;

        return created;
    }

    void DestroyGraphic()
    {
        var host = _graphic != null ? _graphic.gameObject : null;
        _graphic = null;
        if (host == null) return;

        host.SetActive(false);
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(host);
            return;
        }
#endif
        Destroy(host);
    }

    static string ResolveAnimation(SkeletonData data)
    {
        if (data.FindAnimation(IdleAnimationName) != null) return IdleAnimationName;

        var animations = data.Animations;
        for (int i = 0; i < animations.Count; i++)
        {
            var a = animations.Items[i];
            if (a == null || string.IsNullOrEmpty(a.Name)) continue;
            if (a.Name.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0) return a.Name;
        }

        var first = animations.Items[0];
        return first != null ? first.Name : null;
    }

    void EnsureViewport()
    {
        if (_viewport != null) return;

        _viewport = GetComponent<RectTransform>();
        if (_viewport == null) _viewport = gameObject.AddComponent<RectTransform>();

        var image = GetComponent<Image>();
        if (image == null)
        {
            image = gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.004f);
        }
        // Decorative background; never swallows clicks.
        image.raycastTarget = false;

        EnsureMask();
    }

    void EnsureMask()
    {
        if (_mask == null) _mask = GetComponent<RectMask2D>();
        if (_mask == null) _mask = gameObject.AddComponent<RectMask2D>();
        _mask.enabled = true;
    }

    void ApplyVisibility()
    {
        if (_graphic == null || !_configured)
        {
            SetGraphicVisible(false);
            return;
        }

        // TryRender manages visibility (hidden while the layout is zero-sized, retried later).
        if (!TryRender()) SetGraphicVisible(false);
    }

    void SetGraphicVisible(bool visible)
    {
        if (_graphic != null && _graphic.gameObject.activeSelf != visible)
            _graphic.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Fits skeleton bounds into the viewport by scaling and centring the graphic transform.
    /// Returns false while geometry or the viewport is unusable.
    /// </summary>
    bool TryRender()
    {
        if (_graphic == null || _viewport == null) return false;

        var skeleton = _graphic.Skeleton;
        if (skeleton == null) return false;

        bool wasActive = _graphic.gameObject.activeSelf;
        Vector2 size = _viewport.rect.size;
        if (size.x <= MinViewportSize || size.y <= MinViewportSize)
        {
            if (!wasActive) SetGraphicVisible(false);
            return false;
        }
        if (float.IsNaN(size.x) || float.IsNaN(size.y))
        {
            if (!wasActive) SetGraphicVisible(false);
            return false;
        }

        if (_boundsBuffer == null || _boundsBuffer.Length < 8) _boundsBuffer = new float[64];
        float x, y, w, h;
        skeleton.GetBounds(out x, out y, out w, out h, ref _boundsBuffer);
        bool usable = IsUsable(w) && IsUsable(h);
        float centerX, centerY, scale;

        // SkeletonGraphic renders in skeleton units around its rect pivot.
        var rt = _graphic.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.pivot = new Vector2(.5f, .5f);
        rt.sizeDelta = Vector2.zero;

        if (usable)
        {
            if (_portrait)
            {
                scale = Mathf.Max(size.x / (w * PortraitHorizontalFill), size.y / (h * 0.5f));
                centerX = x + w * 0.5f;
                centerY = y + h * 0.78f; // head area of the full-body pose
            }
            else
            {
                scale = Mathf.Min(size.x / w, size.y / h) * FillRatio;
                centerX = x + w * 0.5f;
                centerY = y + h * 0.5f;
            }

            usable = IsUsable(scale);
        }
        else
        {
            // Nothing can be measured yet: keep it centred and untransformed so the fallback
            // stays visible and LateUpdate can retry instead of leaving a hidden black box.
            scale = 1f;
            centerX = 0f;
            centerY = 0f;
        }

        // SkeletonGraphic multiplies mesh vertices by the canvas reference PPU.
        // Match both transform and culling bounds to those UI-space vertices.
        float ppu = _graphic.canvas != null ? Mathf.Max(1f, _graphic.canvas.referencePixelsPerUnit) : 100f;
        rt.localScale = new Vector3(scale / ppu, scale / ppu, 1f);
        if (usable)
        {
            rt.sizeDelta = new Vector2(w * ppu, h * ppu);
            rt.pivot = new Vector2(-x / w, -y / h);
        }
        rt.anchoredPosition = new Vector2(-centerX * scale, -centerY * scale);
        if (!wasActive) SetGraphicVisible(true);

        _graphic.SetVerticesDirty();
        _syncedRenderers = -1;
        _fitted = true;
        return true;
    }

    void SyncRendererBounds()
    {
        var source = _graphic.rectTransform;
        foreach (var renderer in _graphic.canvasRenderers)
        {
            if (renderer == null) continue;
            var rect = renderer.transform as RectTransform;
            if (rect == null) continue;
            // The child's origin must stay at the skeleton origin. RectMask2D uses
            // RectTransform bounds rather than mesh bounds for its culling decision.
            rect.anchorMin = rect.anchorMax = source.pivot;
            rect.pivot = source.pivot;
            rect.sizeDelta = source.sizeDelta;
            rect.anchoredPosition = Vector2.zero;
        }
        _syncedRenderers = _graphic.canvasRenderers.Count;
    }

    static bool IsUsable(float value)
    {
        return value > 0.0001f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
