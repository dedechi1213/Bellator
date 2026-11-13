using System.Linq;
using Bellator.Character;
using Bellator.Classes;
using Bellator.Combat;
using Unity.Netcode;
using UnityEngine;

namespace Bellator.Multiplayer
{
    [DefaultExecutionOrder(20)]
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkGladiator : NetworkBehaviour
    {
        public NetworkVariable<int> ClassIndex = new NetworkVariable<int>();
        public int InitialClass { get; set; }
        ThirdPersonMotor motor;
        ThirdPersonInput movement;
        CombatInput buttons;
        PlayerCombat combat;
        PlayerAbility ability;
        PlayerLockOn target;
        CharacterHealth health;
        CharacterStamina stamina;
        PlayerAnimationBridge bridge;
        ShieldGuard guard;
        Camera aim;
        GameObject viewCamera, aura;
        Material remoteAuraMaterial;
        sealed class MenuCameraState { public Camera camera; public bool enabled; public string tag; public AudioListener listener; public bool listenerEnabled; }
        readonly System.Collections.Generic.List<MenuCameraState> menuCameras = new System.Collections.Generic.List<MenuCameraState>();
        OnlineInput outgoing, pending, consumed;
        float lastInputTime, sendTime, snapshotTime;
        uint tick, acceptedTick, highestSequence;
        OnlineSnapshot previous, latest;
        float snapshotReceived;
        int impactSequence, lastImpact;
        DamageResult impact;
        Vector3 hitPoint;
        Quaternion visualBaseRotation;
        readonly System.Collections.Generic.List<GameObject> bolts = new System.Collections.Generic.List<GameObject>();
        public string CurrentVisualState => IsServer ? bridge?.VisualState : latest?.animation;
        public float DisplayStamina { get; private set; }
        public float DisplayCooldown { get; private set; }
        public bool IsActive => OnlineGame.Instance != null && OnlineGame.Instance.Room.phase == OnlinePhase.Fighting;
        public override void OnNetworkSpawn()
        {
            if(IsServer) ClassIndex.Value=InitialClass;
            motor = GetComponent<ThirdPersonMotor>(); movement = GetComponent<ThirdPersonInput>();
            buttons = GetComponent<CombatInput>(); combat = GetComponent<PlayerCombat>(); ability = GetComponent<PlayerAbility>();
            target = GetComponent<PlayerLockOn>(); health = GetComponent<CharacterHealth>(); stamina = GetComponent<CharacterStamina>();
            bridge = GetComponent<PlayerAnimationBridge>(); guard = GetComponent<ShieldGuard>();
            var classes = GetComponent<PlayerClassController>(); classes.enabled = false;
            classes.Apply(OnlineGame.Instance.Configuration.catalog.classes[ClassIndex.Value]); classes.ConfigureCombatContext(true);
            GetComponent<CombatFaction>().Configure(FactionTeam.Gladiators); GetComponent<CombatFaction>().AllowFriendlyFire = true;
            combat.enabled = ability.enabled = IsServer;
            motor.AcceptInput = false; combat.ManualSimulation = true; ability.ManualSimulation = true;
            movement.enabled = buttons.enabled = IsOwner; target.enabled = IsOwner;
            health.RemoteReplica = !IsServer;
            visualBaseRotation=bridge.Animator!=null?bridge.Animator.transform.localRotation:Quaternion.identity;
            // Client replicas never execute combat, stamina, ability or collision simulation.
            if (!IsServer)
            {
                combat.enabled = false; ability.enabled = false; stamina.enabled = false; bridge.enabled = false;
                GetComponent<PlayerActionStateMachine>().enabled = false;
                GetComponent<CombatPoseAnimator>().enabled = false;
                GetComponent<AbilityPoseOverlay>().enabled = false;
            }
            var aimRoot = new GameObject("HostValidatedAim"); aimRoot.transform.SetParent(transform, false);
            aim = aimRoot.AddComponent<Camera>(); aim.enabled = false; aim.cullingMask = 0; aim.tag = "Untagged";
            motor.Configure(aim.transform); combat.Configure(aim); ability.OnlineAim = aim.transform;
            if (IsServer) GetComponent<DamageReceiver>().Impacted += Impacted;
            if (IsOwner)
            {
                foreach(var source in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                {
                    if(source.gameObject.scene.name!="MainMenu") continue;
                    var listener=source.GetComponent<AudioListener>();
                    menuCameras.Add(new MenuCameraState { camera=source, enabled=source.enabled, tag=source.tag, listener=listener, listenerEnabled=listener!=null && listener.enabled });
                    source.enabled=false; source.tag="Untagged"; if(listener!=null) listener.enabled=false;
                }
                viewCamera = Instantiate(OnlineGame.Instance.Configuration.cameraPrefab);
                viewCamera.name = "OnlineLocalCamera";
                var camera = viewCamera.GetComponent<Camera>(); camera.depth = 10;
                var orbit = viewCamera.GetComponent<ThirdPersonCamera>(); orbit.Configure(transform, movement); orbit.SetTargetHeight(classes.Current.colliderHeight * .74f);
                target.Configure(camera);
            }
            lastInputTime = Time.unscaledTime;
        }
        void Impacted(DamageResult value) { impactSequence++; impact = value; hitPoint = GetComponent<DamageReceiver>().LastImpactPoint; }
        void Update()
        {
            if (!IsSpawned || movement == null) return;
            if (IsOwner)
            {
                outgoing.axis = movement.Movement; outgoing.sprint = movement.Sprint; outgoing.guard = buttons.GuardHeld;
                outgoing.locked = target.IsLocked;
                if (viewCamera != null)
                {
                    outgoing.yaw = viewCamera.transform.eulerAngles.y;
                    outgoing.pitch = Mathf.DeltaAngle(0, viewCamera.transform.eulerAngles.x);
                }
                if (buttons.LightPressed) outgoing.light++;
                if (buttons.HeavyPressed) outgoing.heavy++;
                if (buttons.AbilityPressed) outgoing.ability++;
                if (buttons.ReloadPressed) outgoing.reload++;
                if (movement.DodgePressed) outgoing.dodge++;
                if (movement.QuickstepPressed) outgoing.step++;
                if (Time.unscaledTime >= sendTime)
                {
                    sendTime = Time.unscaledTime + 1f / 30;
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (OnlineSmokeProbe.Running) outgoing=OnlineSmokeProbe.Drive(this,outgoing);
#endif
                    outgoing.sequence++;
                    if (IsServer) Accept(outgoing); else InputRpc(outgoing.sequence, outgoing.axis, outgoing.yaw, outgoing.pitch, outgoing.sprint, outgoing.guard, outgoing.locked,
                        outgoing.light, outgoing.heavy, outgoing.dodge, outgoing.step, outgoing.ability, outgoing.reload);
                }
            }
            if (!IsServer) RenderReplica();
        }
        [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
        void InputRpc(uint sequence, Vector2 axis, float yaw, float pitch, bool sprint, bool blocking, bool locked, uint light, uint heavy, uint dodge, uint step, uint special, uint reload, RpcParams rpc = default)
        {
            if (rpc.Receive.SenderClientId != OwnerClientId) return;
            Accept(new OnlineInput { sequence=sequence, axis=axis, yaw=yaw, pitch=pitch, sprint=sprint, guard=blocking, locked=locked,
                light=light, heavy=heavy, dodge=dodge, step=step, ability=special, reload=reload });
        }
        void Accept(OnlineInput input)
        {
            if (!input.IsValid || input.sequence <= highestSequence) return;
            // Commands carry no time step, position, damage, health or chosen victim.
            highestSequence = input.sequence; pending = input; lastInputTime = Time.unscaledTime;
        }
        static bool Press(uint requested, uint processed) => requested != processed;
        void FixedUpdate()
        {
            if (!IsSpawned || !IsServer || motor == null) return;
            bool active = IsActive;
            combat.ControlsSuspended = !active; ability.SimulationSuspended = !active;
            OnlineInput frame = pending;
            if (!active || Time.unscaledTime - lastInputTime > .25f) { frame.axis = Vector2.zero; frame.sprint = frame.guard = false; frame.light=consumed.light; frame.heavy=consumed.heavy; frame.dodge=consumed.dodge; frame.step=consumed.step; frame.ability=consumed.ability; frame.reload=consumed.reload; }
            aim.transform.position = transform.position + Vector3.up * 1.4f;
            aim.transform.rotation = Quaternion.Euler(frame.pitch, frame.yaw, 0);
            if (frame.locked && active && !IsOwner)
            {
                var opponent = FindObjectsByType<NetworkGladiator>(FindObjectsSortMode.None).FirstOrDefault(a => a != this && a.health != null && a.health.IsAlive);
                var receiver = opponent != null ? opponent.GetComponent<DamageReceiver>() : null;
                target.SetOnlineTarget(receiver != null && Vector3.Distance(transform.position, opponent.transform.position) <= 26 && target.HasSight(receiver) ? receiver : null);
            }
            else if(!IsOwner || !active) target.Clear();
            guard.Request(active && frame.guard);
            if (active)
            {
                if (Press(frame.ability, consumed.ability)) ability.TryActivate();
                if (Press(frame.light, consumed.light)) combat.TryAttack(false);
                if (Press(frame.heavy, consumed.heavy)) combat.TryAttack(true);
                if (Press(frame.reload, consumed.reload)) combat.TryReload();
                ability.TickAbility(Time.fixedDeltaTime); combat.TickCombat(Time.fixedDeltaTime);
                motor.ProcessMovement(frame.axis, frame.sprint, Press(frame.dodge, consumed.dodge), Time.fixedDeltaTime, Press(frame.step, consumed.step));
            }
            else motor.CancelDodge();
            // Discard presses while frozen instead of buffering an attack into the countdown.
            consumed = pending;
            DisplayStamina = stamina.Current; DisplayCooldown = ability.CooldownRemaining;
            if (Time.unscaledTime >= snapshotTime)
            {
                snapshotTime = Time.unscaledTime + 1f / 20;
                SendSnapshot();
            }
        }
        void SendSnapshot()
        {
            var a = bridge.Animator;
            var local = transform.InverseTransformDirection(motor.ActualPlanarVelocity);
            var snapshot = new OnlineSnapshot {
                round = OnlineGame.Instance.Room.round, tick = ++tick, position = transform.position, rotation = transform.rotation,
                health=health.Current, stamina=stamina.Current, cooldown=ability.CooldownRemaining,
                speed = a != null ? a.GetFloat("Speed") : 0, x=local.x / Mathf.Max(.1f,motor.WalkSpeed), y=local.z / Mathf.Max(.1f,motor.WalkSpeed),
                animation=bridge.VisualState, progress=a != null ? a.GetCurrentAnimatorStateInfo(0).normalizedTime : 0,
                running=motor.IsRunning, locked=target.IsLocked, blocking=guard.IsBlocking, aura=ability.IsActive,
                impact=impactSequence, damage=impact.Damage, blocked=impact.Blocked, broken=impact.GuardBroken, staggered=impact.Staggered,
                hitPoint=hitPoint, dodgeDirection=motor.DodgeDirection,
                projectiles=FindObjectsByType<CombatProjectile>(FindObjectsSortMode.None).Where(p=>p.Owner==transform && !p.HasHit).Take(8).Select(p=>p.transform.position).ToArray()
            };
            // Bounded binary snapshots stay below the MTU. Repeated state tolerates loss; stale ticks are rejected.
            SnapshotRpc(snapshot);
        }
        [Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)]
        void SnapshotRpc(OnlineSnapshot data, RpcParams rpc = default)
        {
            if(rpc.Receive.SenderClientId != Unity.Netcode.NetworkManager.ServerClientId) return;
            if (data == null || data.round != OnlineGame.Instance.Room.round || data.tick <= acceptedTick) return;
            acceptedTick = data.tick;
            previous = latest ?? data; latest = data; snapshotReceived = Time.unscaledTime;
            health.ApplyNetworkSnapshot(data.health); DisplayStamina=data.stamina; DisplayCooldown=data.cooldown;
            if (data.impact != lastImpact)
            {
                lastImpact=data.impact;
                GetComponent<DamageReceiver>().ApplyNetworkImpact(new DamageResult(data.damage,data.blocked,data.broken,data.staggered),data.hitPoint);
            }
        }
        void RenderReplica()
        {
            if (latest == null) return;
            float blend = Mathf.Clamp01((Time.unscaledTime - snapshotReceived) / .05f);
            Vector3 point = Vector3.Lerp(previous.position,latest.position,blend);
            Quaternion facing = Quaternion.Slerp(previous.rotation,latest.rotation,blend);
            var controller = GetComponent<CharacterController>();
            controller.enabled=false; transform.SetPositionAndRotation(point,facing); controller.enabled=true;
            var animator = bridge.Animator;
            if (animator != null && !string.IsNullOrEmpty(latest.animation))
            {
                animator.SetFloat("Speed",latest.speed); animator.SetFloat("MoveX",latest.x); animator.SetFloat("MoveY",latest.y);
                animator.SetFloat("LocomotionRate",1); animator.SetBool("IsLockedOn",latest.locked); animator.SetBool("IsRunning",latest.running); animator.SetBool("IsBlocking",latest.blocking);
                bool looping=latest.animation.Contains("Locomotion") || latest.animation.Contains("Running");
                float progress=latest.progress;
                if (looping) progress += Mathf.Min(.1f,Time.unscaledTime-snapshotReceived);
                else progress=Mathf.Clamp(progress,0,.99999f);
                animator.speed=0; animator.Play(latest.animation,0,progress); animator.Update(0);
                if (latest.animation == "DodgeRoll")
                {
                    var d=transform.InverseTransformDirection(latest.dodgeDirection);
                    animator.transform.localRotation=visualBaseRotation*Quaternion.Euler(0,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,0);
                }
                else animator.transform.localRotation=visualBaseRotation;
            }
            ShowAura(latest.aura);
            for (int i=0;i<Mathf.Max(latest.projectiles.Length,bolts.Count);i++)
            {
                if(i>=bolts.Count)
                {
                                        var prefab=GetComponent<WeaponEquipment>().Current?.projectilePrefab;
                    var bolt=prefab!=null?Instantiate(prefab.gameObject):GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    var simulation=bolt.GetComponent<CombatProjectile>(); if(simulation!=null) simulation.enabled=false;
                    foreach(var collider in bolt.GetComponentsInChildren<Collider>()) collider.enabled=false;
                    if(prefab==null) bolt.transform.localScale=new Vector3(.04f,.24f,.04f);
                    bolt.transform.SetPositionAndRotation(latest.projectiles[i],transform.rotation); bolts.Add(bolt);
                }
                bolts[i].SetActive(i<latest.projectiles.Length);
                                if(i<latest.projectiles.Length)
                {
                    var delta=latest.projectiles[i]-bolts[i].transform.position;
                    if(delta.sqrMagnitude>.00001f) bolts[i].transform.rotation=Quaternion.LookRotation(delta);
                    bolts[i].transform.position=latest.projectiles[i];
                }
            }
        }
        void ShowAura(bool active)
        {
            if (active && aura == null)
            {
                aura=new GameObject("RemoteAbilityFeedback"); aura.transform.SetParent(transform,false);
                remoteAuraMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                remoteAuraMaterial.SetColor("_BaseColor",ability.Definition.feedbackColor);
                for(int i=0;i<10;i++)
                {
                    var orb=GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(orb.GetComponent<Collider>()); orb.transform.SetParent(aura.transform,false);
                    orb.transform.localPosition=new Vector3(Mathf.Sin(i*Mathf.PI/5)*.85f,.2f,Mathf.Cos(i*Mathf.PI/5)*.85f); orb.transform.localScale=Vector3.one*.1f;
                    orb.GetComponent<Renderer>().sharedMaterial=remoteAuraMaterial;
                }
            }
            if(aura!=null) { aura.SetActive(active); aura.transform.localRotation=Quaternion.Euler(0,Time.time*100,0); }
        }
        public override void OnNetworkDespawn()
        {
            if (IsServer) GetComponent<DamageReceiver>().Impacted -= Impacted;
            if (viewCamera != null) { viewCamera.SetActive(false); Destroy(viewCamera); }
            foreach(var saved in menuCameras)
            {
                if(saved.camera!=null) { saved.camera.enabled=saved.enabled; saved.camera.tag=saved.tag; }
                if(saved.listener!=null) saved.listener.enabled=saved.listenerEnabled;
            }
            menuCameras.Clear();
            if(remoteAuraMaterial!=null) Destroy(remoteAuraMaterial);
            foreach(var bolt in bolts) if(bolt != null) Destroy(bolt);
            if(IsOwner) { Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
        }
    }
}




