using RunMap;
using UnityEngine;

public static partial class RunEncounter
{
    private static RunObjectScope _objectScope;
    private static GameObject _enemyRoot, _dropRoot;

    internal static RunObjectScope EnsureObjectScope()
    {
        if (_objectScope == null || _objectScope.IsDisposed) _objectScope = new RunObjectScope();
        return _objectScope;
    }

    public static bool OwnsRuntimeObject(GameObject actor)
    {
        return _objectScope != null && _objectScope.Owns(actor);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetObjectOwnership()
    {
        // Subsystem reset must not act on a previous play session's scene or camera.
        _objectScope = null;
        _enemyRoot = _dropRoot = null;
        _ticker = null;
    }
}
