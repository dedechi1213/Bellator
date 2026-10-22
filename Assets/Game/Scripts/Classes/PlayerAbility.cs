using System.Collections.Generic;
using UnityEngine;
using Bellator.Character;
using Bellator.Combat;
using Bellator.Enemies;

namespace Bellator.Classes
{
    [DefaultExecutionOrder(-20)]
    public sealed class PlayerAbility : MonoBehaviour
    {
        private AbilityDefinition definition;
        private ActorAttributes attributes;
        private ThirdPersonMotor motor;
        private PlayerCombat combat;
        private CharacterHealth health;
        private CharacterStamina stamina;
        private WeaponEquipment equipment;
        private ShieldGuard guard;
        private CombatInput input;
        private float cooldown,remaining,castElapsed,flashRemaining;
        private bool casting,emitted;
        private GameObject aura;
        private Material auraMaterial;
        private Transform body,leftArm,rightArm;
        public bool ManualSimulation { get; set; }
        public bool SimulationSuspended { get; set; }
        public AbilityDefinition Definition => definition;
        public float CooldownRemaining => cooldown;
        public float DurationRemaining => remaining;
        public bool IsCasting => casting;
        public bool IsActive => casting || remaining>0;
        public float CastProgress => casting?Mathf.Clamp01(castElapsed/(definition.windup+definition.recovery)):0;
        public string Status { get; private set; }="";
        public int AreaHits { get; private set; }
        private void Awake()
        {
            attributes=GetComponent<ActorAttributes>(); motor=GetComponent<ThirdPersonMotor>(); combat=GetComponent<PlayerCombat>();
            health=GetComponent<CharacterHealth>(); stamina=GetComponent<CharacterStamina>(); equipment=GetComponent<WeaponEquipment>(); guard=GetComponent<ShieldGuard>(); input=GetComponent<CombatInput>();
        }
        private void OnEnable() { TrainingArenaControls.TrainingReset+=ResetAbility; health.Changed+=HealthChanged; }
        private void OnDisable() { TrainingArenaControls.TrainingReset-=ResetAbility; if(health!=null) health.Changed-=HealthChanged; ClearEffects(); }
        private void OnDestroy() { if(auraMaterial!=null) Destroy(auraMaterial); }
        private void HealthChanged(float current,float maximum) { if(current<=0) { ClearEffects(); Status=""; } }
        public void Configure(AbilityDefinition value) { ResetAbility(); definition=value; }
        public void BindPose(Transform torso,Transform left,Transform right) { body=torso; leftArm=left; rightArm=right; }
        private void Update()
        {
            if(ManualSimulation || SimulationSuspended) return;
            if(input.AbilityPressed) TryActivate();
            TickAbility(Time.deltaTime);
        }
        public bool TryActivate()
        {
            if(SimulationSuspended || definition==null || !health.IsAlive || cooldown>0 || IsActive || combat.IsBusy || motor.IsDodging || guard.IsBlocking || guard.IsBroken)
            { Status=cooldown>0?"Habilidade recarregando":"Habilidade indisponível agora"; return false; }
            if(definition.kind==AbilityKind.PreciseShot && (equipment.Current==null || equipment.Current.type!=EquipmentType.Crossbow || !combat.CrossbowLoaded))
            { Status="Equipe a besta carregada"; return false; }
            if(!stamina.TryConsume(definition.staminaCost)) { Status="Sem resistência para habilidade"; return false; }
            cooldown=definition.cooldown; flashRemaining=.45f; Status=definition.displayName; emitted=false;
            if(definition.kind==AbilityKind.DesperateSteps)
            {
                remaining=definition.duration; motor.SpeedBuff=definition.speedMultiplier; motor.DodgeCostMultiplier=definition.dodgeCostMultiplier;
                motor.DodgeDistanceMultiplier=definition.dodgeDistanceMultiplier; motor.DodgeCooldownMultiplier=definition.dodgeCooldownMultiplier;
            }
            else if(definition.kind==AbilityKind.CombatStance)
            { remaining=definition.duration; attributes.AbilityDamage=definition.damageMultiplier; attributes.AbilityDefense=definition.defenseBonus; attributes.BlockCostMultiplier=definition.blockCostMultiplier; }
            else
            {
                if(definition.kind==AbilityKind.PreciseShot && Camera.main!=null)
                {
                    Vector3 forward=Vector3.ProjectOnPlane(Camera.main.transform.forward,Vector3.up);
                    if(forward.sqrMagnitude>.001f) transform.rotation=Quaternion.LookRotation(forward);
                }
                casting=true; castElapsed=0; combat.ExternalActionBusy=true; guard.Request(false);
            }
            EnsureAura(); auraMaterial.SetColor("_BaseColor",definition.feedbackColor); aura.SetActive(true);
            return true;
        }
        public void TickAbility(float deltaTime)
        {
            if(SimulationSuspended) return;
            float dt=Mathf.Max(0,deltaTime); cooldown=Mathf.Max(0,cooldown-dt); flashRemaining=Mathf.Max(0,flashRemaining-dt);
            if(!health.IsAlive) { ClearEffects(); return; }
            if(remaining>0)
            {
                remaining=Mathf.Max(0,remaining-dt);
                if(remaining<=0) ClearEffects();
            }
            if(casting)
            {
                castElapsed+=dt;
                if(!emitted && castElapsed>=definition.windup)
                {
                    emitted=true;
                    if(definition.kind==AbilityKind.DevastatingBlow) AreaStrike();
                    else combat.TryFireSpecialShot(definition.shotMultiplier);
                }
                if(castElapsed>=definition.windup+definition.recovery)
                { casting=false; combat.ExternalActionBusy=false; combat.TryReload(); }
            }
            if(aura!=null)
            {
                aura.SetActive(IsActive || flashRemaining>0);
                aura.transform.localRotation=Quaternion.Euler(0,Time.time*100,0);
            }
        }
        private void AreaStrike()
        {
            var struck=new HashSet<DamageReceiver>();
            Vector3 origin=transform.position+Vector3.up;
            foreach(var collider in Physics.OverlapSphere(origin,definition.areaRadius,~0,QueryTriggerInteraction.Ignore))
            {
                if(CombatProjectile.Own(collider,transform)) continue;
                var victim=collider.GetComponentInParent<DamageReceiver>();
                if(victim==null || !victim.Health.IsAlive || !CombatFaction.CanDamage(transform,victim) || !struck.Add(victim)) continue;
                Vector3 point=collider.ClosestPoint(origin);
                if(CombatProjectile.Trace(origin,point-origin,.025f,transform,out var hit) && hit.collider.GetComponentInParent<DamageReceiver>()!=victim) continue;
                if(victim.Receive(new DamageInfo(definition.areaDamage*attributes.DamageMultiplier(false),point,transform.position,transform)))
                {
                    AreaHits++;
                    var navigation=victim.GetComponent<EnemyNavigation>();
                    if(navigation!=null && victim.Health.IsAlive)
                    {
                        Vector3 away=Vector3.ProjectOnPlane(victim.transform.position-transform.position,Vector3.up);
                        navigation.AddImpulse((away.sqrMagnitude>.001f?away.normalized:transform.forward)*definition.pushSpeed,definition.pushDuration);
                    }
                }
            }
        }
        private void EnsureAura()
        {
            if(aura!=null) return;
            aura=new GameObject("AbilityFeedback_Primitives"); aura.transform.SetParent(transform,false);
            auraMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            for(int i=0;i<10;i++)
            {
                var orb=GameObject.CreatePrimitive(PrimitiveType.Sphere); orb.name="Pulse_"+i;
                orb.transform.SetParent(aura.transform,false); float angle=i*Mathf.PI*2/10;
                orb.transform.localPosition=new Vector3(Mathf.Sin(angle)*.85f,.2f,Mathf.Cos(angle)*.85f);
                orb.transform.localScale=Vector3.one*.1f; Destroy(orb.GetComponent<Collider>());
                var renderer=orb.GetComponent<Renderer>(); renderer.sharedMaterial=auraMaterial; renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
        public void ApplyPose()
        {
            if(!casting || body==null) return;
            if(definition.kind==AbilityKind.DevastatingBlow)
            {
                float t=Mathf.Clamp01(castElapsed/definition.windup);
                rightArm.localRotation=Quaternion.Euler(Mathf.Lerp(-160,-55,t),30,0);
                body.localRotation*=Quaternion.Euler(castElapsed<definition.windup?-10:20,0,0);
            }
        }
        public void ResetAbility() { cooldown=0; ClearEffects(); Status=""; }
        private void ClearEffects()
        {
            remaining=0; casting=false; emitted=false; flashRemaining=0;
            if(attributes!=null) attributes.ClearTemporary();
            if(motor!=null) { motor.SpeedBuff=motor.DodgeCostMultiplier=motor.DodgeDistanceMultiplier=motor.DodgeCooldownMultiplier=1; }
            if(combat!=null) combat.ExternalActionBusy=false;
            if(aura!=null) aura.SetActive(false);
        }
    }
}



