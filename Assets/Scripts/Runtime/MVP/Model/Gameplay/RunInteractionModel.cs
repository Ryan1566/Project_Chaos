using Chaos.Gameplay.Interaction;
using RunMap;
using UnityEngine;

namespace Chaos.MVP.Model.Gameplay
{
    public sealed class RunInteractionState
    {
        public bool Visible, Available;
        public string Text, TargetId;
    }
    public sealed class RunInteractionModel
    {
        private readonly GameObject actor;
        private readonly RunInteractionService service;
        private RunInteractable target;
        private string feedback;
        private float feedbackUntil;
        public RunInteractionModel(GameObject actor)
        { this.actor = actor; service = new RunInteractionService(RunBehaviorSettings.Load("Ally").interactionRange); }
        public RunInteractionState Refresh()
        {
            target = service.Select(actor);
            if (!RunInteractionService.CanControl(actor)) return new RunInteractionState();
            if (target == null) return new RunInteractionState { Visible = Time.unscaledTime < feedbackUntil, Text = feedback };
            string reason; bool available = target.CanInteract(actor, out reason);
            var device = (InputDeviceType)SettingsManager.Instance.Applied.inputDevice;
            var kind = device == InputDeviceType.Keyboard ? InputManager.BindingKind.Keyboard : InputManager.BindingKind.Gamepad;
            string binding = InputManager.Instance.GetBindingDisplay(InputManager.ActionInteract, kind, device);
            return new RunInteractionState { Visible = true, Available = available, TargetId = target.StableId,
                Text = available ? RunInteractionText.Get("Prompt", binding, target.DisplayName + " · " + RunInteractionText.Get("Inspect"))
                    : target.DisplayName + " · " + reason };
        }
        public bool Interact()
        {
            bool success = service.TryExecute(actor, target, out feedback);
            feedbackUntil = Time.unscaledTime + 1.5f; return success;
        }
    }
}
