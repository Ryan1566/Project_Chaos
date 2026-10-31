using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 受控角色专用物理查询：只把朝上的接触视为可站立地面，并识别侧墙接触。
///
/// 空中贴墙时给角色自身挂一个运行时低摩擦材质，避免“水平持续推墙”产生的法向冲量
/// 被摩擦转换成对重力的支撑（表现为悬挂/攀墙）；落地后立即恢复原材质。
/// 材质只作用于角色自身，不修改共享地形材质；禁用、倒地、切图和销毁时都会恢复并释放。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerSurfacePhysics : MonoBehaviour
{
    /// <summary>与 RunMapPhysics 一致：法线 y 不足 0.5 的面不算可站立地面。</summary>
    public const float MinGroundNormalY = 0.5f;
    /// <summary>水平分量达到该值的接触按侧墙处理。</summary>
    public const float MinSideNormalX = 0.5f;

    private const float GroundProbeLift = 0.05f;
    private const float GroundProbeDepth = 0.12f;
    private const int ContactBufferSize = 16;

    private Rigidbody2D _body;
    private readonly List<Collider2D> _solidColliders = new List<Collider2D>();
    private readonly List<PhysicsMaterial2D> _originalColliderMaterials = new List<PhysicsMaterial2D>();
    private readonly ContactPoint2D[] _contacts = new ContactPoint2D[ContactBufferSize];
    private ContactFilter2D _contactFilter;
    private PhysicsMaterial2D _originalBodyMaterial;
    private PhysicsMaterial2D _slideMaterial;
    private bool _slideMaterialApplied;
    private bool _cached;

    public bool IsSlideMaterialActive { get { return _slideMaterialApplied; } }

    private void Awake()
    {
        EnsureCached();
    }

    private void OnEnable()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SetSlideMaterial(false);
    }

    private void OnDestroy()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        SetSlideMaterial(false);
    }

    private void OnSceneUnloaded(Scene scene)
    {
        SetSlideMaterial(false);
    }

    private void EnsureCached()
    {
        if (_cached && _body != null) return;
        _body = GetComponent<Rigidbody2D>();
        _contactFilter = new ContactFilter2D { useTriggers = false };
        _solidColliders.Clear();
        _originalColliderMaterials.Clear();
        _originalBodyMaterial = _body == null ? null : _body.sharedMaterial;
        if (_body == null) { _cached = true; return; }

        Collider2D[] found = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < found.Length; i++)
        {
            Collider2D collider = found[i];
            if (collider == null || collider.isTrigger || collider.attachedRigidbody != _body) continue;
            _solidColliders.Add(collider);
            _originalColliderMaterials.Add(collider.sharedMaterial);
        }
        _cached = true;
    }

    /// <summary>脚底盒形探测；只有法线朝上的真实碰撞面才算落地，侧墙不会误判为地面。</summary>
    public bool ProbeGround()
    {
        EnsureCached();
        for (int i = 0; i < _solidColliders.Count; i++)
            if (ProbeGround(_solidColliders[i], _body)) return true;
        return false;
    }

    public static bool ProbeGround(Collider2D collider, Rigidbody2D body)
    {
        if (collider == null || !collider.enabled || collider.isTrigger) return false;
        Bounds bounds = collider.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y + GroundProbeLift);
        Vector2 size = new Vector2(Mathf.Max(0.1f, bounds.size.x * 0.8f), 0.04f);
        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, size, 0f, Vector2.down, GroundProbeDepth);
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D hit = hits[i];
            if (hit.collider == null || hit.collider == collider || hit.collider.isTrigger) continue;
            if (body != null && hit.collider.attachedRigidbody == body) continue;
            if (body == null && hit.collider.transform.IsChildOf(collider.transform)) continue;
            if (!IsGroundNormal(hit.normal)) continue;
            return true;
        }
        return false;
    }

    /// <summary>可站立地面判定：朝上法线；竖直墙面与过陡斜面返回 false。</summary>
    public static bool IsGroundNormal(Vector2 normal)
    {
        return normal.y >= MinGroundNormalY;
    }

    /// <summary>是否正接触竖直/过陡的侧面。用于只在确实贴墙时切换低摩擦材质。</summary>
    public bool HasSideContact()
    {
        EnsureCached();
        if (_body == null || !_body.simulated) return false;
        int count = _body.GetContacts(_contactFilter, _contacts);
        for (int i = 0; i < count && i < _contacts.Length; i++)
            if (IsSideContactNormal(_contacts[i].normal)) return true;
        return false;
    }

    /// <summary>侧向接触判定（对法线方向不敏感）：水平分量大于竖直分量且达到墙面阈值。</summary>
    public static bool IsSideContactNormal(Vector2 normal)
    {
        float x = Mathf.Abs(normal.x);
        return x >= MinSideNormalX && x > Mathf.Abs(normal.y);
    }

    /// <summary>切换角色自身的低摩擦材质；关闭时恢复所有原材料并释放运行时材质。</summary>
    public void SetSlideMaterial(bool active)
    {
        if (!active)
        {
            RestoreOriginalMaterials();
            ReleaseSlideMaterial();
            return;
        }

        EnsureCached();
        if (_body == null || _solidColliders.Count == 0 || _slideMaterialApplied) return;

        if (_slideMaterial == null)
            _slideMaterial = new PhysicsMaterial2D("PlayerAirborneLowFriction")
            {
                friction = 0f,
                bounciness = 0f,
                hideFlags = HideFlags.HideAndDontSave
            };

        for (int i = 0; i < _solidColliders.Count; i++)
            if (_solidColliders[i] != null) _solidColliders[i].sharedMaterial = _slideMaterial;
        _body.sharedMaterial = _slideMaterial;
        _slideMaterialApplied = true;
    }

    private void RestoreOriginalMaterials()
    {
        if (!_slideMaterialApplied) return;
        for (int i = 0; i < _solidColliders.Count; i++)
            if (_solidColliders[i] != null) _solidColliders[i].sharedMaterial = _originalColliderMaterials[i];
        if (_body != null) _body.sharedMaterial = _originalBodyMaterial;
        _slideMaterialApplied = false;
    }

    private void ReleaseSlideMaterial()
    {
        if (_slideMaterial == null) return;
        if (Application.isPlaying) Destroy(_slideMaterial);
        else DestroyImmediate(_slideMaterial);
        _slideMaterial = null;
    }
}
