using UnityEngine;
using UnityEngine.InputSystem;

namespace Bellator.Character
{
    /// <summary>Own action map; preserves the project's existing InputSystem_Actions asset.</summary>
    public sealed class ThirdPersonInput : MonoBehaviour
    {
        private InputActionMap actions;
        private InputAction move, sprint, dodge, quickstep, look, zoom, release, capture;
        [SerializeField] private string quickstepBinding = "<Keyboard>/leftAlt";
        [SerializeField] private string nextTargetBinding = "<Keyboard>/c", previousTargetBinding = "<Keyboard>/z";
        private InputAction lockTarget, nextTarget, previousTarget;
        private bool focused = true;
        private bool captured;
        // Input ownership is explicit. Cursor locking is a platform presentation side effect.
        public bool HasControl => focused && captured;
        public Vector2 Movement => HasControl ? move.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => HasControl ? look.ReadValue<Vector2>() : Vector2.zero;
        public float Zoom => HasControl ? zoom.ReadValue<Vector2>().y : 0;
        public bool Sprint => HasControl && sprint.IsPressed();
        public bool DodgePressed => HasControl && dodge.WasPressedThisFrame();
        public bool QuickstepPressed => HasControl && quickstep.WasPressedThisFrame();
        public string QuickstepKey => quickstep?.GetBindingDisplayString() ?? "Alt";
        public void RebindQuickstep(string binding)
        {
            if (string.IsNullOrWhiteSpace(binding)) return;
            quickstepBinding = binding;
            quickstep?.ApplyBindingOverride(binding);
        }
        public bool LockPressed => HasControl && lockTarget.WasPressedThisFrame();
        public bool NextTargetPressed => HasControl && nextTarget.WasPressedThisFrame();
        public bool PreviousTargetPressed => HasControl && previousTarget.WasPressedThisFrame();
        public void RebindTargetSwitch(string next, string previous)
        {
            if (!string.IsNullOrWhiteSpace(next)) { nextTargetBinding = next; nextTarget.ApplyBindingOverride(next); }
            if (!string.IsNullOrWhiteSpace(previous)) { previousTargetBinding = previous; previousTarget.ApplyBindingOverride(previous); }
        }
        private void Awake()
        {
            actions = new InputActionMap("BellatorThirdPerson");
            move = actions.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
            sprint = actions.AddAction("Sprint", InputActionType.Button);
            sprint.AddBinding("<Keyboard>/leftShift"); sprint.AddBinding("<Keyboard>/rightShift");
            dodge = actions.AddAction("Dodge", InputActionType.Button,"<Keyboard>/space");
            quickstep = actions.AddAction("Quickstep", InputActionType.Button,quickstepBinding);
            lockTarget = actions.AddAction("LockOn", InputActionType.Button,"<Keyboard>/tab");
            nextTarget = actions.AddAction("NextTarget", InputActionType.Button,nextTargetBinding);
            previousTarget = actions.AddAction("PreviousTarget", InputActionType.Button,previousTargetBinding);
            look = actions.AddAction("Look", InputActionType.Value,"<Mouse>/delta");
            zoom = actions.AddAction("Zoom", InputActionType.Value,"<Mouse>/scroll");
            release = actions.AddAction("ReleaseCursor", InputActionType.Button,"<Keyboard>/escape");
            capture = actions.AddAction("CaptureCursor", InputActionType.Button,"<Mouse>/leftButton");
        }
        private void OnEnable() { actions.Enable(); LockCursor(); }
        private void Update()
        {
            if (release.WasPressedThisFrame()) UnlockCursor();
            else if (focused && capture.WasPressedThisFrame()) LockCursor();
        }
        private void OnApplicationFocus(bool value) { focused = value; if (!value) UnlockCursor(); }
        private void OnDisable() { actions.Disable(); UnlockCursor(); }
        private void OnDestroy() => actions.Dispose();
        private void LockCursor() { captured = true; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        private void UnlockCursor() { captured = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}

