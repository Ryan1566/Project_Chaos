using UnityEngine;

namespace Chaos.MVP.Model.Gameplay
{
    /// <summary>Gameplay input/pause adapter. Views do not search combat actors or change their behaviour.</summary>
    public static class RunControlModel
    {
        public static void SetPlayerInput(bool enabled)
        {
            bool allow = enabled && !RunEncounter.IsSpectator;
            foreach (var actor in Object.FindObjectsOfType<PlayerLocomotion>()) actor.enabled = allow;
            foreach (var actor in Object.FindObjectsOfType<PlayerAttack>()) actor.enabled = allow;
        }
        public static void PauseWorld(bool paused)
        {
            SetPlayerInput(!paused);
            foreach (var actor in Object.FindObjectsOfType<AllyController>()) actor.enabled = !paused;
            foreach (var actor in Object.FindObjectsOfType<EnemyPlaceholder>()) actor.enabled = !paused;
        }
    }
}
