using UnityEngine;
using UnityEngine.InputSystem;
using Bellator.Character;

namespace Bellator.Combat
{
    [RequireComponent(typeof(ThirdPersonInput))]
    public sealed class CombatInput : MonoBehaviour
    {
        [SerializeField] private string heavyAttackBinding="<Keyboard>/e";
        private ThirdPersonInput movementInput;
        private InputActionMap actions;
        private InputAction lightAttack,heavy,guard,reload,reset,front,back;
        private InputAction ability,selection;
        public bool AbilityPressed => HasControl && ability.WasPressedThisFrame();
        public bool SelectionPressed => selection.WasPressedThisFrame();
        private int recapturedFrame=-1;
        public bool HasControl => movementInput!=null && movementInput.HasControl && Keyboard.current?.escapeKey.isPressed!=true;
        public bool LightPressed => HasControl && recapturedFrame!=Time.frameCount && lightAttack.WasPressedThisFrame();
        public bool HeavyPressed => HasControl && heavy.WasPressedThisFrame();
        public bool GuardHeld => HasControl && guard.IsPressed();
        public bool ReloadPressed => HasControl && reload.WasPressedThisFrame();
        public bool ResetPressed => HasControl && reset.WasPressedThisFrame();
        public bool FrontPressed => HasControl && front.WasPressedThisFrame();
        public bool BackPressed => HasControl && back.WasPressedThisFrame();
        public string HeavyDisplay => heavy?.GetBindingDisplayString() ?? "E";
        private void Awake()
        {
            movementInput=GetComponent<ThirdPersonInput>(); actions=new InputActionMap("BellatorCombat");
            lightAttack=actions.AddAction("Light",InputActionType.Button,"<Mouse>/leftButton");
            heavy=actions.AddAction("Heavy",InputActionType.Button,heavyAttackBinding);
            ability=actions.AddAction("Ability",InputActionType.Button,"<Keyboard>/q");
            selection=actions.AddAction("Selection",InputActionType.Button,"<Keyboard>/f1");
            guard=actions.AddAction("Guard",InputActionType.Button,"<Mouse>/rightButton");
            reload=actions.AddAction("Reload",InputActionType.Button,"<Keyboard>/r");
            reset=actions.AddAction("ResetTraining",InputActionType.Button,"<Keyboard>/t");
            front=actions.AddAction("TestFrontHit",InputActionType.Button,"<Keyboard>/h");
            back=actions.AddAction("TestRearHit",InputActionType.Button,"<Keyboard>/j");
            lightAttack.performed+=_=> { if(!movementInput.HasControl) recapturedFrame=Time.frameCount; };
        }
        public void RebindHeavy(string path)
        {
            if(string.IsNullOrWhiteSpace(path)) return;
            heavyAttackBinding=path;
            if(heavy!=null) heavy.ApplyBindingOverride(path);
        }
        private void OnEnable() => actions.Enable();
        private void OnDisable() => actions.Disable();
        private void OnDestroy() => actions.Dispose();
    }
}



