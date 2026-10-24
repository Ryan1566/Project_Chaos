using UnityEngine;

/// <summary>Small world-space windup/strike cue, independent of character animation assets.</summary>
public sealed class RunCombatFeedback : MonoBehaviour
{
    private LineRenderer _line;
    private Material _material;
    private float _until;

    public static RunCombatFeedback For(Component actor)
    {
        RunCombatFeedback feedback = actor.GetComponent<RunCombatFeedback>();
        return feedback != null ? feedback : actor.gameObject.AddComponent<RunCombatFeedback>();
    }

    public void Windup(Vector2 from, Vector2 to)
    {
        Draw(from, to, new Color(1f, 0.65f, 0.1f, 0.85f), 0.08f);
    }

    public void Strike(Vector2 from, Vector2 to)
    {
        Draw(from, to, new Color(1f, 0.25f, 0.2f, 1f), 0.12f);
    }

    private void Draw(Vector2 from, Vector2 to, Color color, float duration)
    {
        if (_line == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) return;
            GameObject cue = new GameObject("CombatCue");
            cue.transform.SetParent(transform, false);
            _line = cue.AddComponent<LineRenderer>();
            _material = new Material(shader);
            _line.sharedMaterial = _material;
            _line.useWorldSpace = true;
            _line.positionCount = 2;
            _line.startWidth = _line.endWidth = 0.065f;
            _line.sortingOrder = 100;
        }
        _line.startColor = _line.endColor = color;
        _line.SetPosition(0, new Vector3(from.x, from.y, -0.1f));
        _line.SetPosition(1, new Vector3(to.x, to.y, -0.1f));
        _line.enabled = true;
        _until = Time.time + duration;
    }

    public void Clear()
    {
        _until = 0f;
        if (_line != null) _line.enabled = false;
    }

    private void Update() { if (_until > 0f && Time.time >= _until) Clear(); }
    private void OnDisable() { Clear(); }
    private void OnDestroy() { if (_material != null) Destroy(_material); }
}
