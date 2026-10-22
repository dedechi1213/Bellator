using UnityEngine;
using UnityEngine.InputSystem;

namespace Bellator.Character
{
    /// <summary>Own action map; preserves the project's existing InputSystem_Actions asset.</summary>
    public sealed class ThirdPersonInput : MonoBehaviour
    {
        private InputActionMap actions;
        private InputAction move, sprint, dodge, look, zoom, release, capture;
        private bool focused = true;
        public bool HasControl => focused && Cursor.lockState == CursorLockMode.Locked;
        public Vector2 Movement => HasControl ? move.ReadValue<Vector2>() : Vector2.zero;
        public Vector2 Look => HasControl ? look.ReadValue<Vector2>() : Vector2.zero;
        public float Zoom => HasControl ? zoom.ReadValue<Vector2>().y : 0;
        public bool Sprint => HasControl && sprint.IsPressed();
        public bool DodgePressed => HasControl && dodge.WasPressedThisFrame();
        private void Awake()
        {
            actions = new InputActionMap("BellatorThirdPerson");
            move = actions.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector").With("Up","<Keyboard>/w").With("Down","<Keyboard>/s").With("Left","<Keyboard>/a").With("Right","<Keyboard>/d");
            sprint = actions.AddAction("Sprint", InputActionType.Button);
            sprint.AddBinding("<Keyboard>/leftShift"); sprint.AddBinding("<Keyboard>/rightShift");
            dodge = actions.AddAction("Dodge", InputActionType.Button,"<Keyboard>/space");
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
        private void LockCursor() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        private void UnlockCursor() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
    }
}

