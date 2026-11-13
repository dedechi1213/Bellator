using UnityEngine;
using UnityEngine.SceneManagement;
using Bellator.Character;
using Bellator.Combat;

namespace Bellator.Classes
{
    [DefaultExecutionOrder(5)]
    public sealed class PlayerClassController : MonoBehaviour
    {
        public const string SelectionScene="Assets/Game/Scenes/GladiatorSelection.unity";
        public const string ArenaScene="Assets/Game/Scenes/RomanArena.unity";
        [SerializeField] private GladiatorClassCatalog catalog;
        [SerializeField] private Transform legacyVisual;
        [SerializeField] private WeaponDefinition[] legacyEquipment;
        [SerializeField] private WeaponDefinition legacyShield;
        private GameObject currentVisual;
        private ThirdPersonMotor motor;
        private PlayerCombat combat;
        private WeaponEquipment equipment;
        private ActorAttributes attributes;
        private PlayerAbility ability;
        public GladiatorClassDefinition Current { get; private set; }
        public GladiatorClassCatalog Catalog => catalog;
        public CombatRuleConfiguration ContextRules => Current == null ? null : (usePvpRecovery ? Current.pvp : Current.pve);
        private bool usePvpRecovery;
        /// <summary>Reserved for a future match context. No current game mode calls this with true.</summary>
        public void ConfigureCombatContext(bool pvp)
        {
            usePvpRecovery=pvp;
            if(Current != null) GetComponent<CharacterStamina>().ConfigureRecovery(Current.staminaRegeneration * ContextRules.staminaRecovery,Current.staminaRegenerationDelay,Current.blockingRegeneration);
        }
        public void Configure(GladiatorClassCatalog value,Transform oldVisual,WeaponDefinition[] oldWeapons,WeaponDefinition oldShield)
        { catalog=value; legacyVisual=oldVisual; legacyEquipment=oldWeapons; legacyShield=oldShield; }
        private void Awake()
        { motor=GetComponent<ThirdPersonMotor>(); combat=GetComponent<PlayerCombat>(); equipment=GetComponent<WeaponEquipment>(); attributes=GetComponent<ActorAttributes>(); ability=GetComponent<PlayerAbility>(); }
        private void Start() { if(enabled) Apply(ClassSelectionState.Selected!=null?ClassSelectionState.Selected:catalog.Default); }
        private void Update()
        { if(GetComponent<CombatInput>().SelectionPressed) ReturnToSelection(); }
        public void ReturnToSelection()
        {
            GetComponent<ThirdPersonInput>().enabled = false; GetComponent<CombatInput>().enabled = false;
            GetComponent<PlayerLockOn>()?.Clear();
            Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
            SceneManager.LoadSceneAsync(SelectionScene,LoadSceneMode.Single);
        }
        public void Apply(GladiatorClassDefinition definition)
        {
            if(definition==null || definition.visualPrefab==null || definition.equipment==null || definition.equipment.Length==0) return;
            ability.ResetAbility(); combat.ResetTrainingState();
            if(currentVisual!=null) { currentVisual.SetActive(false); Destroy(currentVisual); }
            legacyVisual.gameObject.SetActive(false);
            currentVisual=Instantiate(definition.animatedVisualPrefab != null ? definition.animatedVisualPrefab : definition.visualPrefab,transform,false); currentVisual.name="ClassVisual_"+definition.id;
            Current=definition; attributes.Configure(definition);
            GetComponent<CharacterHealth>().ConfigureMaximum(definition.health);
            GetComponent<CharacterStamina>().ConfigureMaximum(definition.stamina);
            ConfigureCombatContext(false);
            motor.ConfigureSpeeds(definition.walkSpeed,definition.runSpeed);
            motor.ConfigureMovement(definition.acceleration, definition.deceleration, definition.turnSmoothTime, definition.sprintDrain);
            motor.ConfigureDodge(definition.dodge);
            motor.ConfigureQuickstep(definition.hasQuickstep ? definition.quickstep : null);
            GetComponent<ActorPosture>()?.Configure(definition.posture);
            GetComponent<PlayerActionStateMachine>()?.ResetState();
            GetComponent<PlayerLockOn>()?.Clear();
            var controller=GetComponent<CharacterController>();
            controller.height=definition.colliderHeight; controller.radius=definition.colliderRadius; controller.center=Vector3.up*(definition.colliderHeight*.5f);
            BindVisual(currentVisual.transform,definition.equipment,definition.shield);
            ability.Configure(definition.ability);
            if(Camera.main!=null) Camera.main.GetComponent<ThirdPersonCamera>()?.SetTargetHeight(definition.colliderHeight*.74f);
            GetComponent<ImpactFeedback>().RefreshVisuals();
        }
        private void BindVisual(Transform visual,WeaponDefinition[] weapons,WeaponDefinition shield,bool fixedEquipment=true)
        {
            var skeleton=FindSocket(visual,"BodyPivot");
            var body=skeleton != null && skeleton.Find("TorsoPivot") != null ? skeleton.Find("TorsoPivot") : skeleton;
            var left=body != null ? body.Find("LeftShoulder") : null; var right=body != null ? body.Find("RightShoulder") : null;
            var humanoid = visual.GetComponentInChildren<Animator>();
            if (humanoid == null && body != null && left != null && right != null)
            {
                var animator=visual.GetComponent<PrimitiveHumanoidAnimator>();
                if(animator==null) animator=visual.gameObject.AddComponent<PrimitiveHumanoidAnimator>();
                animator.Configure(motor,body,left,right,skeleton.Find("LeftHip"),skeleton.Find("RightHip"));
            }
            GetComponent<CombatPoseAnimator>().Configure(body,left,right);
            Transform rightSocket = FindSocket(visual, "RightHandSocket"), leftSocket = FindSocket(visual, "LeftHandSocket");
            if (humanoid != null && humanoid.isHuman)
            {
                if (rightSocket == null) rightSocket = humanoid.GetBoneTransform(HumanBodyBones.RightHand);
                if (leftSocket == null) leftSocket = humanoid.GetBoneTransform(HumanBodyBones.LeftHand);
            }
            if (rightSocket == null) rightSocket = visual;
            if (leftSocket == null) leftSocket = visual;
            if(fixedEquipment) equipment.ConfigureFixed(rightSocket,leftSocket,weapons[0],shield);
            else { equipment.Configure(rightSocket,leftSocket,weapons,shield); equipment.Equip(0); }
            ability.BindPose(body,left,right);
            var bridge = GetComponent<PlayerAnimationBridge>();
            if (bridge == null) bridge = gameObject.AddComponent<PlayerAnimationBridge>();
            bridge.Bind(humanoid);
        }
        private static Transform FindSocket(Transform root, string name)
        { foreach (var child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child; return null; }
        /// <summary>Used only by prior-stage regression suites to retain their original baseline.</summary>
        public void RestoreLegacyForValidation()
        {
            ability.ResetAbility(); ability.enabled=false; combat.ResetTrainingState();
            if(currentVisual!=null) { currentVisual.SetActive(false); Destroy(currentVisual); currentVisual=null; }
            Current=null; legacyVisual.gameObject.SetActive(true); attributes.Legacy();
            GetComponent<CharacterHealth>().ConfigureMaximum(100); GetComponent<CharacterStamina>().ConfigureMaximum(100);
            motor.ConfigureSpeeds(3.5f,6.2f);
            var controller=GetComponent<CharacterController>(); controller.height=2.1f; controller.radius=.34f; controller.center=Vector3.up*1.05f;
            BindVisual(legacyVisual,legacyEquipment,legacyShield,false);
            Camera.main?.GetComponent<ThirdPersonCamera>()?.SetTargetHeight(1.55f);
            GetComponent<ImpactFeedback>().RefreshVisuals(); enabled=false;
        }
    }
}



