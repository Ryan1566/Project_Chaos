using Chaos.Gameplay.Items;
using Spine.Unity;
using UnityEngine;

/// <summary>Configured Spine overlay when a clip exists; a raised item icon provides explicit prototype feedback otherwise.</summary>
public sealed class ItemUseAnimation : MonoBehaviour
{
    float _remaining, _duration;
    GameObject _feedback;
    SkeletonAnimation _skeleton;
    Spine.TrackEntry _entry;
    public void Play(ItemBase item)
    {
        Cleanup();
        _duration = Mathf.Clamp(item.Profile.animationSeconds, .1f, 5); _remaining = _duration;
        _skeleton = GetComponentInChildren<SkeletonAnimation>();
        string clip = item.Profile.animationName;
        if (_skeleton != null && !string.IsNullOrEmpty(clip) && _skeleton.Skeleton.Data.FindAnimation(clip) != null)
        { _entry = _skeleton.AnimationState.SetAnimation(1, clip, false); _entry.MixDuration = .1f; }
        _feedback = new GameObject("ItemUseFeedback"); _feedback.transform.SetParent(transform, false);
        var renderer = _feedback.AddComponent<SpriteRenderer>(); renderer.sprite = item.Icon ?? ItemIconFallback.Resolve(item.ParentCategory);
        renderer.sortingOrder = 200; _feedback.transform.localScale = Vector3.one * .55f;
        InputManager.Instance.SetGameplayInputBlocked(this, true);
    }
    void Update()
    {
        if (_remaining <= 0) return;
        _remaining -= Time.deltaTime;
        if (_feedback != null) _feedback.transform.localPosition = new Vector3(.35f, 1.5f + Mathf.Sin((1 - _remaining / _duration) * Mathf.PI) * .35f, 0);
        if (_remaining <= 0) Cleanup();
    }
    void OnDisable() { Cleanup(); }
    void Cleanup()
    {
        _remaining = 0;
        if (_feedback != null) Destroy(_feedback);
        if (_entry != null && _skeleton != null && _skeleton.AnimationState.GetCurrent(1) == _entry) _skeleton.AnimationState.SetEmptyAnimation(1, .1f);
        _entry = null; InputManager.Instance.SetGameplayInputBlocked(this, false);
    }
}
