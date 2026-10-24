using UnityEngine;
using System.Collections.Generic;

namespace Chaos.MVP.Model.Gameplay
{
    /// <summary>Gameplay input/pause adapter. Views do not search combat actors or change their behaviour.</summary>
    public static class RunControlModel
    {
        private struct BodySnapshot { public bool Simulated; public Vector2 Velocity; public float Angular; }
        private static readonly Dictionary<Rigidbody2D, BodySnapshot> Bodies = new Dictionary<Rigidbody2D, BodySnapshot>();
        private static readonly Dictionary<Behaviour, bool> Behaviours = new Dictionary<Behaviour, bool>();
        private static bool worldPaused;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { Bodies.Clear(); Behaviours.Clear(); worldPaused = false; }
        public static void SetPlayerInput(bool enabled)
        {
            bool allow = enabled && !RunEncounter.IsSpectator;
            foreach (var actor in Object.FindObjectsOfType<PlayerLocomotion>()) actor.enabled = allow;
            foreach (var actor in Object.FindObjectsOfType<PlayerAttack>()) actor.enabled = allow;
        }
        public static void PauseWorld(bool paused)
        {
            if (paused == worldPaused) return;
            worldPaused = paused;
            if (paused)
            {
                // Capture velocities before disabling AI cancels its current movement command.
                foreach (var body in Object.FindObjectsOfType<Rigidbody2D>())
                {
                    if (body.bodyType == RigidbodyType2D.Static) continue;
                    Bodies[body] = new BodySnapshot { Simulated = body.simulated, Velocity = body.velocity, Angular = body.angularVelocity };
                    body.simulated = false;
                }
                foreach (var behaviour in Object.FindObjectsOfType<Behaviour>())
                {
                    if (!(behaviour is PlayerLocomotion) && !(behaviour is PlayerAttack)
                        && !(behaviour is AllyController) && !(behaviour is EnemyPlaceholder)) continue;
                    Behaviours[behaviour] = behaviour.enabled; behaviour.enabled = false;
                }
                return;
            }
            foreach (var pair in Bodies)
            {
                if (pair.Key == null) continue;
                pair.Key.simulated = pair.Value.Simulated;
                pair.Key.velocity = pair.Value.Velocity; pair.Key.angularVelocity = pair.Value.Angular;
            }
            foreach (var pair in Behaviours)
            {
                if (pair.Key == null) continue;
                bool enable = pair.Value;
                if (pair.Key is AllyController ally && ally.IsDowned) enable = false;
                if ((pair.Key is PlayerLocomotion || pair.Key is PlayerAttack) && RunEncounter.IsSpectator) enable = false;
                if (pair.Key is EnemyPlaceholder enemy && enemy.IsDead) enable = false;
                pair.Key.enabled = enable;
            }
            Bodies.Clear(); Behaviours.Clear();
        }
    }
}
