using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Bellator.Character;
using Bellator.Combat;

namespace Bellator.Enemies
{
    public enum EnemyState { Idle, Chase, Attack, Recover, Retreat, Defend, Stunned, Dead }
    [RequireComponent(typeof(EnemyNavigation),typeof(DamageReceiver),typeof(WeaponEquipment))]
    public sealed class EnemyBrain : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition definition;
        [SerializeField] private DamageReceiver target;
        private CharacterHealth health;
        private CharacterStamina stamina;
        private DamageReceiver receiver;
        private ShieldGuard guard;
        private WeaponEquipment equipment;
        private EnemyNavigation navigation;
        private AttackConfiguration attack;
        private readonly HashSet<DamageReceiver> struck=new HashSet<DamageReceiver>();
        private Vector3 lockedForward,home;
        private Quaternion homeRotation;
        private float timer,elapsed,windup,active,seenAgo=10,decisionDelay,defenseDelay,retreatRepath;
        private bool emitted;
        private ActorPosture posture;
        private float reactionImmunity;
        private float attackRate=1,minimumPause;
        public void ConfigureAggression(float rate,float pause)
        { attackRate=Mathf.Clamp(rate,1,1.8f); minimumPause=Mathf.Max(.15f,pause); }
        private Vector3 flankGoal;
        private float flankRemaining;
        public bool AutoThink { get; set; }=true;
        public bool PursueAssignedTarget { get; set; }
        public EnemyState State { get; private set; }=EnemyState.Idle;
        public EnemyDefinition Definition => definition;
        public DamageReceiver Target => target;
        public float AttackProgress => State==EnemyState.Attack?Mathf.Clamp01(elapsed/(windup+active)):0;
        public int AttacksStarted { get; private set; }
        public int ProjectilesFired { get; private set; }
        public int SuccessfulHits { get; private set; }
        public void Configure(EnemyDefinition value,DamageReceiver opponent=null) { definition=value; target=opponent; }
        public void SetTarget(DamageReceiver value) { target=value; seenAgo=10; }
        private void Awake()
        {
            health=GetComponent<CharacterHealth>(); stamina=GetComponent<CharacterStamina>(); receiver=GetComponent<DamageReceiver>();
            guard=GetComponent<ShieldGuard>(); equipment=GetComponent<WeaponEquipment>(); navigation=GetComponent<EnemyNavigation>();
            health.ConfigureMaximum(definition.maximumHealth);
            home=transform.position; homeRotation=transform.rotation;
        }
        private void OnEnable()
        {
            if(receiver==null) return;
            receiver.Impacted+=OnImpact; health.Changed+=OnHealthChanged;
        }
        private void Start()
        {
            posture = GetComponent<ActorPosture>();
            if (posture != null) posture.Configure(definition.posture);
            if(target==null)
                foreach(var faction in FindObjectsByType<CombatFaction>(FindObjectsSortMode.None))
                    if(faction.Team==FactionTeam.Player) { target=faction.GetComponent<DamageReceiver>(); break; }
            navigation.Warp(transform.position,transform.rotation);
        }
        private void Update() { if(AutoThink && Time.deltaTime>0) Tick(Time.deltaTime); }
        public void Tick(float deltaTime)
        {
            if(!health.IsAlive) { Die(); return; }
            float dt=Mathf.Clamp(deltaTime,0,.1f);
            reactionImmunity = Mathf.Max(0, reactionImmunity - dt);
            if (posture != null && posture.IsStaggered && State != EnemyState.Stunned) EnterStun(posture.StaggerRemaining);
            if(navigation.BeingPushed) { EnemyAttackCoordinator.Release(this); State=EnemyState.Stunned; timer=Mathf.Max(timer,.1f); navigation.Stop(); guard.Request(false); return; }
            defenseDelay-=dt; decisionDelay-=dt;
            if(State==EnemyState.Dead) return;
            if(State==EnemyState.Stunned || State==EnemyState.Recover || State==EnemyState.Defend)
            {
                navigation.Stop(); timer-=dt;
                if(State==EnemyState.Defend)
                {
                    if(target!=null) navigation.Face(target.transform.position);
                    if(guard.IsBroken) { EnterStun(.9f); return; }
                }
                if(timer<=0 && !(posture != null && posture.IsStaggered)) { EnemyAttackCoordinator.Release(this); guard.Request(false); State=EnemyState.Chase; }
                return;
            }
            if(target==null || !target.Health.IsAlive)
            { State=EnemyState.Idle; navigation.Stop(); guard.Request(false); return; }
            float distance=Vector3.Distance(transform.position,target.transform.position);
            bool visible=distance<=definition.detectionRange && HasLineOfSight();
            seenAgo=visible?0:seenAgo+dt;
            if(State==EnemyState.Attack) { AdvanceAttack(dt); return; }
            if(seenAgo>4 && !PursueAssignedTarget)
            { State=EnemyState.Idle; navigation.Stop(); guard.Request(false); return; }
            if(!navigation.Ready) { State=EnemyState.Idle; return; }
            bool ranged=definition.role==EnemyRole.Archer;
            if(ranged && distance<definition.retreatRange)
            { Retreat(dt); return; }
            if(ranged && !visible && distance<=definition.preferredRange+4)
            { RepositionForSight(dt); return; }
            if(ranged && distance<=definition.preferredRange+2 && visible)
            {
                navigation.Stop(); navigation.Face(target.transform.position);
                if(FacingTarget() && distance>definition.retreatRange) TryBeginAttack();
                else State=EnemyState.Chase;
                return;
            }
            if(!ranged && distance<=definition.engageDistance && visible)
            {
                navigation.Stop(); navigation.Face(target.transform.position);
                if(defenseDelay<=0 && equipment.HasShield)
                {
                    defenseDelay=2.2f;
                    var opponent=target.GetComponent<PlayerCombat>();
                    float chance=opponent!=null && opponent.IsAttacking?Mathf.Min(1,definition.defenseChance+.25f):definition.defenseChance;
                    if(Random.value<chance) { BeginDefense(.65f); return; }
                }
                if(FacingTarget()) TryBeginAttack();
                return;
            }
            State=EnemyState.Chase; guard.Request(false);
            Vector3 away=transform.position-target.transform.position; away.y=0;
            if(away.sqrMagnitude<.01f) away=transform.forward;
            float stop=ranged?definition.preferredRange:definition.engageDistance*.82f;
            Vector3 goal=target.transform.position+away.normalized*stop;
            navigation.GoTo(goal);
        }
        public bool HasLineOfSight()
        {
            if(target==null) return false;
            Vector3 from=transform.position+Vector3.up*(definition.height*.8f),to=target.transform.position+Vector3.up*1.55f;
            if(CombatProjectile.Trace(from,to-from,.04f,transform,out var hit))
                return hit.collider.GetComponentInParent<DamageReceiver>()==target;
            return true;
        }
        private void RepositionForSight(float dt)
        {
            State=EnemyState.Chase; guard.Request(false); flankRemaining-=dt;
            if(flankRemaining>0 && navigation.Moving) { navigation.GoTo(flankGoal); return; }
            flankRemaining=.8f;
            Vector3 away=transform.position-target.transform.position; away.y=0; if(away.sqrMagnitude<.01f) away=transform.forward;
            var path=new NavMeshPath(); float best=float.PositiveInfinity; bool found=false;
            foreach(float angle in new[]{45f,-45f,90f,-90f,135f,-135f,180f})
            {
                Vector3 candidate=target.transform.position+Quaternion.Euler(0,angle,0)*away.normalized*definition.preferredRange;
                if(!NavMesh.SamplePosition(candidate,out var sample,1.5f,NavMesh.AllAreas)) continue;
                Vector3 origin=sample.position+Vector3.up*(definition.height*.8f),delta=target.transform.position+Vector3.up*1.55f-origin;
                if(CombatProjectile.Trace(origin,delta,.04f,transform,out var hit) && hit.collider.GetComponentInParent<DamageReceiver>()!=target) continue;
                if(!NavMesh.CalculatePath(transform.position,sample.position,NavMesh.AllAreas,path) || path.status!=NavMeshPathStatus.PathComplete) continue;
                float length=0; for(int i=1;i<path.corners.Length;i++) length+=Vector3.Distance(path.corners[i-1],path.corners[i]);
                if(length<best) { found=true; best=length; flankGoal=sample.position; }
            }
            if(found) navigation.GoTo(flankGoal); else navigation.Stop();
        }        private bool FacingTarget()
        {
            Vector3 d=target.transform.position-transform.position; d.y=0;
            return d.sqrMagnitude<.001f || Vector3.Dot(transform.forward,d.normalized)>.94f;
        }
        public bool TryBeginAttack()
        {
            if(!health.IsAlive || target==null || !target.Health.IsAlive || !navigation.Ready || equipment.Current==null || decisionDelay > 0 || State==EnemyState.Dead || State==EnemyState.Attack || State==EnemyState.Recover || State==EnemyState.Stunned || guard.IsBroken || (posture != null && posture.IsStaggered)) return false;
            float distance=Vector3.Distance(transform.position,target.transform.position);
            if(!HasLineOfSight() || !FacingTarget() || distance>(definition.role==EnemyRole.Archer?definition.preferredRange+2:definition.engageDistance+.15f)) return false;
            var weapon=equipment.Current;
            if (!weapon.IsRanged && !EnemyAttackCoordinator.TryAcquire(this, target, definition.maximumConcurrentMeleeAttackers, definition.attackStartSpacing))
            { decisionDelay = .12f; return false; }
            if(weapon.lightCombo==null || weapon.lightCombo.Length==0 || !stamina.TryConsume(weapon.staminaCost))
            { Recover(1.3f); return false; }
            guard.Request(false); navigation.Stop(); State=EnemyState.Attack;
            attack=weapon.lightCombo[0]; elapsed=0; emitted=false; struck.Clear(); lockedForward=transform.forward;
            windup=(attack.windup+attack.execution)/(weapon.attackSpeed*attackRate); active=attack.active/(weapon.attackSpeed*attackRate);
            if(attackRate>1) { windup=Mathf.Max(.16f,windup); active=Mathf.Max(.10f,active); }
            AttacksStarted++;
            return true;
        }
        private void AdvanceAttack(float dt)
        {
            navigation.Stop(); guard.Request(false);
            float previous=elapsed; elapsed+=dt;
            if(elapsed>=windup && previous<=windup+active && health.IsAlive)
            {
                if(equipment.Current.IsRanged)
                {
                    if(!emitted)
                    {
                        emitted=true;
                        if(target!=null && target.Health.IsAlive && HasLineOfSight())
                        {
                            Vector3 muzzle=equipment.Muzzle!=null?equipment.Muzzle.position:transform.position+Vector3.up*1.4f+lockedForward*.7f;
                            var projectile=CombatQueries.Fire(transform,equipment.Current,muzzle,target.transform.position+Vector3.up*1.15f);
                            if(projectile!=null) ProjectilesFired++;
                        }
                    }
                }
                else SuccessfulHits+=CombatQueries.Melee(transform,lockedForward,equipment.Current,attack,struck);
            }
            if(elapsed>=windup+active) Recover(Mathf.Max(minimumPause,(attack.recovery/equipment.Current.attackSpeed+equipment.Current.recoveryTime+definition.attackPause)/attackRate));
        }
        public bool BeginDefense(float duration)
        {
            if(!health.IsAlive || !equipment.HasShield || guard.IsBroken || State==EnemyState.Attack || State==EnemyState.Stunned || State==EnemyState.Dead) return false;
            State=EnemyState.Defend; timer=duration; navigation.Stop(); guard.Request(true); return true;
        }
        private void Recover(float duration) { State=EnemyState.Recover; timer=duration; navigation.Stop(); guard.Request(false); }
        private void Retreat(float dt)
        {
            State=EnemyState.Retreat; guard.Request(false); retreatRepath-=dt;
            if(retreatRepath>0 && navigation.Moving) return;
            retreatRepath=.45f;
            Vector3 away=transform.position-target.transform.position; away.y=0; if(away.sqrMagnitude<.01f) away=transform.forward;
            // Search several retreat directions, retaining only complete paths that increase separation.
            var path=new NavMeshPath(); float best=-1; Vector3 destination=transform.position;
            foreach(float angle in new[]{0f,45f,-45f,90f,-90f,135f,-135f})
            {
                Vector3 candidate=transform.position+Quaternion.Euler(0,angle,0)*away.normalized*4;
                if(!NavMesh.SamplePosition(candidate,out var hit,1.8f,NavMesh.AllAreas)) continue;
                float separation=Vector3.Distance(hit.position,target.transform.position);
                if(separation<=Vector3.Distance(transform.position,target.transform.position)+.4f || !NavMesh.CalculatePath(transform.position,hit.position,NavMesh.AllAreas,path) || path.status!=NavMeshPathStatus.PathComplete) continue;
                if(separation>best) { best=separation; destination=hit.position; }
            }
            if(best>=0) navigation.GoTo(destination);
            else { navigation.Stop(); navigation.Face(target.transform.position); if(FacingTarget()) TryBeginAttack(); }
        }
        private void OnImpact(DamageResult result)
        {
            if(State==EnemyState.Dead) return;
            if(result.GuardBroken) EnterStun(.9f);
            else if(result.Staggered) EnterStun(posture != null ? posture.StaggerRemaining : .75f);
            else if(!result.Blocked && result.Damage>0 && reactionImmunity <= 0 && State != EnemyState.Stunned)
            { reactionImmunity = definition.hitReactionImmunity; EnterStun(definition.stunSeconds); }
        }
        private void EnterStun(float seconds)
        { if(!health.IsAlive) { Die(); return; } EnemyAttackCoordinator.Release(this); State=EnemyState.Stunned; timer=seconds; struck.Clear(); navigation.Stop(); guard.Request(false); }
        private void OnHealthChanged(float current,float maximum)
        {
            if(current<=0) Die();
            else if(State==EnemyState.Dead) ResetActor();
        }
        private void Die()
        {
            if(State==EnemyState.Dead) return;
            EnemyAttackCoordinator.Release(this);
            State=EnemyState.Dead; guard.Request(false); struck.Clear(); timer=elapsed=0; navigation.Die();
            foreach(var projectile in FindObjectsByType<CombatProjectile>(FindObjectsSortMode.None)) projectile.CancelForOwner(transform);
        }
        public void SuspendCombat()
        {
            EnemyAttackCoordinator.Release(this);
            AutoThink=false; if(State!=EnemyState.Dead) State=EnemyState.Idle;
            timer=elapsed=0; struck.Clear(); guard.Request(false); navigation.Stop();
        }
        public void ResetActor()
        {
            EnemyAttackCoordinator.Release(this); posture?.ResetPosture(); reactionImmunity = 0;
            guard.ResetGuard(); stamina.Restore(); State=EnemyState.Idle; timer=elapsed=decisionDelay=0; seenAgo=10; retreatRepath=0; defenseDelay=0; flankRemaining=0;
            navigation.Warp(home,homeRotation);
        }
        private void OnDisable()
        {
            EnemyAttackCoordinator.Release(this);
            if(receiver!=null) receiver.Impacted-=OnImpact;
            if(health!=null) health.Changed-=OnHealthChanged;
            if(guard!=null) guard.Request(false);
            if(navigation!=null) navigation.Stop();
        }
    }
}






