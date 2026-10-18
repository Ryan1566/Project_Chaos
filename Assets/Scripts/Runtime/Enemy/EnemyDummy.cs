using ChaosDebug;
using UnityEngine;

/// <summary>
/// 训练靶（W3.0 spike）：实现 <see cref="IDamageable"/>，被打闪白、HP 归零销毁。
/// 只为验证"能打到东西"，不含 AI/寻路（W3 后续）。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class EnemyDummy : MonoBehaviour, IDamageable
{
    public int maxHp = 50;
    public int Hp { get; private set; }

    private SpriteRenderer _renderer;
    private Color _baseColor;
    private float _flashUntil;
    private bool _dead;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer != null) _baseColor = _renderer.color;
        Hp = maxHp;
    }

    public void TakeDamage(int amount)
    {
        if (_dead) return;
        if (amount <= 0) return;

        Hp -= amount;
        _flashUntil = Time.time + 0.08f;
        if (_renderer != null) _renderer.color = Color.white;

        ChaosLog.Info(LogChannel.UI, "[训练靶] 受到 " + amount + " 伤害，剩余 HP " + Mathf.Max(0, Hp) + "/" + maxHp);

        if (Hp <= 0)
        {
            _dead = true;
            ChaosLog.Success(LogChannel.UI, "[训练靶] HP 归零，销毁训练靶 「" + name + "」");
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (_renderer != null && _flashUntil > 0f && Time.time >= _flashUntil)
        {
            _flashUntil = 0f;
            _renderer.color = _baseColor;
        }
    }
}
