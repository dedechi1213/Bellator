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
        public void Configure(GladiatorClassCatalog value,Transform oldVisual,WeaponDefinition[] oldWeapons,WeaponDefinition oldShield)
        { catalog=value; legacyVisual=oldVisual; legacyEquipment=oldWeapons; legacyShield=oldShield; }
        private void Awake()
        { motor=GetComponent<ThirdPersonMotor>(); combat=GetComponent<PlayerCombat>(); equipment=GetComponent<WeaponEquipment>(); attributes=GetComponent<ActorAttributes>(); ability=GetComponent<PlayerAbility>(); }
        private void Start() { if(enabled) Apply(ClassSelectionState.Selected!=null?ClassSelectionState.Selected:catalog.Default); }
        private void Update()
        { if(GetComponent<CombatInput>().SelectionPressed) ReturnToSelection(); }
        public void ReturnToSelection()
        {
            Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
            SceneManager.LoadSceneAsync(SelectionScene,LoadSceneMode.Single);
        }
        public void Apply(GladiatorClassDefinition definition)
        {
            if(definition==null || definition.visualPrefab==null || definition.equipment==null || definition.equipment.Length==0) return;
            ability.ResetAbility(); combat.ResetTrainingState();
            if(currentVisual!=null) { currentVisual.SetActive(false); Destroy(currentVisual); }
            legacyVisual.gameObject.SetActive(false);
            currentVisual=Instantiate(definition.visualPrefab,transform,false); currentVisual.name="ClassVisual_"+definition.id;
            Current=definition; attributes.Configure(definition);
            GetComponent<CharacterHealth>().ConfigureMaximum(definition.health);
            GetComponent<CharacterStamina>().ConfigureMaximum(definition.stamina);
            motor.ConfigureSpeeds(definition.walkSpeed,definition.runSpeed);
            var controller=GetComponent<CharacterController>();
            controller.height=definition.colliderHeight; controller.radius=definition.colliderRadius; controller.center=Vector3.up*(definition.colliderHeight*.5f);
            BindVisual(currentVisual.transform,definition.equipment,definition.shield);
            ability.Configure(definition.ability);
            if(Camera.main!=null) Camera.main.GetComponent<ThirdPersonCamera>()?.SetTargetHeight(definition.colliderHeight*.74f);
            GetComponent<ImpactFeedback>().RefreshVisuals();
        }
        private void BindVisual(Transform visual,WeaponDefinition[] weapons,WeaponDefinition shield,bool fixedEquipment=true)
        {
            var body=visual.Find("BodyPivot"); var left=body.Find("LeftShoulder"); var right=body.Find("RightShoulder");
            var animator=visual.GetComponent<PrimitiveHumanoidAnimator>();
            if(animator==null) animator=visual.gameObject.AddComponent<PrimitiveHumanoidAnimator>();
            animator.Configure(motor,body,left,right,body.Find("LeftHip"),body.Find("RightHip"));
            GetComponent<CombatPoseAnimator>().Configure(body,left,right);
            if(fixedEquipment) equipment.ConfigureFixed(right.Find("RightHandSocket"),left.Find("LeftHandSocket"),weapons[0],shield);
            else { equipment.Configure(right.Find("RightHandSocket"),left.Find("LeftHandSocket"),weapons,shield); equipment.Equip(0); }
            ability.BindPose(body,left,right);
        }
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



