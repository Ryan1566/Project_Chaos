using UnityEngine;

namespace RunMap
{
    /// <summary>Runtime identity only; never inferred from a shared controller or an object's name.</summary>
    [DisallowMultipleComponent]
    public sealed class RunObjectScopeMember : MonoBehaviour
    {
        internal RunObjectScope Owner;
        internal bool Retired;
    }
}
