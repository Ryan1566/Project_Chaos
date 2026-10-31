using UnityEngine;

/// <summary>Village lifecycle adapter; Cinemachine owns every output movement.</summary>
public sealed class VillageCamera : MonoBehaviour
{
    GameplayCameraRig _rig;
    Transform _target;
    public void Initialize(Transform target, VillageSceneSettings config)
    {
        _rig = GameplayCameraRig.For(GetComponent<Camera>()); _target = target;
        _rig.BindVillage(target, config.widthTiles, config.groundY + 4.5f);
    }
    void OnDisable() { if (_rig != null) _rig.Release(_target); _target = null; }
}
