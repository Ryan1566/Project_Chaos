using System;
using System.Collections.Generic;
using UnityEngine;

namespace RunMap
{
    /// <summary>One stage's explicit object ownership. Names and shared actor types never confer ownership.</summary>
    public sealed class RunObjectScope : IDisposable
    {
        private static long nextToken;
        private readonly HashSet<GameObject> members = new HashSet<GameObject>();
        private readonly Dictionary<string, GameObject> roots = new Dictionary<string, GameObject>();
        public long Token { get; }
        public GameObject Root { get; }
        public bool IsDisposed { get; private set; }

        public RunObjectScope()
        {
            Token = ++nextToken;
            Root = new GameObject("RunStageScope_" + Token);
            Register(Root);
        }

        public GameObject Register(GameObject actor)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(RunObjectScope));
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            for (Transform parent = actor.transform.parent; parent != null; parent = parent.parent)
            {
                RunObjectScopeMember ancestor = parent.GetComponent<RunObjectScopeMember>();
                if (ancestor != null && !ReferenceEquals(ancestor.Owner, this))
                    throw new InvalidOperationException("A run object cannot be registered beneath another stage's root");
            }
            RunObjectScopeMember mark = actor.GetComponent<RunObjectScopeMember>();
            if (mark == null) mark = actor.AddComponent<RunObjectScopeMember>();
            if (mark.Owner != null && !ReferenceEquals(mark.Owner, this))
                throw new InvalidOperationException("A run object cannot be transferred between stage scopes");
            if (mark.Retired) throw new InvalidOperationException("A retired run object cannot be registered again");
            mark.Owner = this;
            members.Add(actor);
            return actor;
        }

        public GameObject GetOrCreateRoot(string name)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(RunObjectScope));
            GameObject root;
            if (roots.TryGetValue(name, out root) && root != null && Owns(root)) return root;
            root = Register(new GameObject(name));
            root.transform.SetParent(Root.transform, false);
            roots[name] = root;
            return root;
        }

        public bool Owns(GameObject actor)
        {
            if (IsDisposed || actor == null) return false;
            for (Transform node = actor.transform; node != null; node = node.parent)
            {
                RunObjectScopeMember mark = node.GetComponent<RunObjectScopeMember>();
                if (mark != null) return !mark.Retired && ReferenceEquals(mark.Owner, this);
            }
            return false;
        }

        public void DestroyOwned(GameObject actor)
        {
            if (!Owns(actor)) return;
            // A detached registered actor remains owned; a foreign same-named object never does.
            RunObjectScopeMember mark = actor.GetComponent<RunObjectScopeMember>();
            if (mark == null)
            {
                mark = actor.AddComponent<RunObjectScopeMember>(); mark.Owner = this; members.Add(actor);
            }
            ProtectForeignScopes(actor);
            foreach (RunObjectScopeMember child in actor.GetComponentsInChildren<RunObjectScopeMember>(true))
                if (ReferenceEquals(child.Owner, this)) child.Retired = true;
            actor.SetActive(false);
            if (Application.isPlaying) UnityEngine.Object.Destroy(actor);
            else UnityEngine.Object.DestroyImmediate(actor);
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            IsDisposed = true;
            foreach (GameObject actor in members) if (actor != null) ProtectForeignScopes(actor);
            // Stop callbacks and physics before deferred destruction, including inactive descendants.
            if (Root != null) Root.SetActive(false);
            foreach (GameObject actor in members)
            {
                if (actor == null) continue;
                RunObjectScopeMember mark = actor.GetComponent<RunObjectScopeMember>();
                if (mark == null || mark.Retired || !ReferenceEquals(mark.Owner, this)) continue;
                mark.Retired = true;
                actor.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(actor);
                else UnityEngine.Object.DestroyImmediate(actor);
            }
            members.Clear(); roots.Clear();
        }

        private void ProtectForeignScopes(GameObject root)
        {
            foreach (RunObjectScopeMember mark in root.GetComponentsInChildren<RunObjectScopeMember>(true))
                if (mark.Owner != null && !ReferenceEquals(mark.Owner, this)) mark.transform.SetParent(null, true);
        }
    }
}
