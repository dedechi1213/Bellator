using System.Collections;
using Bellator.Character;
using Bellator.Classes;
using Bellator.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Bellator.Match
{
    /// <summary>Story-only escape. Moves the existing actor with its CharacterController and animation bridge.</summary>
    [DefaultExecutionOrder(60)]
    public sealed class EscapeVictorySequence : MonoBehaviour
    {
        [SerializeField] Transform gateFrame;
        [SerializeField] float exteriorStart=29,stopDepth=39,walkingSpeed=2.25f;
        [SerializeField] AudioSource arenaAmbience,outdoorAmbience;
        [SerializeField] Light corridorLight,exteriorLight;
        GameManager game;
        ThirdPersonMotor motor;
        PlayerCombat combat;
        ThirdPersonCamera orbit;
        Transform actor,movementReference;
        Camera view;
        Canvas fadeCanvas;
        Image fade;
        Vector3 startPosition;
        Quaternion startRotation;
        bool used;
        Coroutine running;
        float previousSpeed;
        bool previousSprint,previousDodge,previousRotation;
        public bool IsPlaying { get; private set; }
        public bool Completed { get; private set; }
        public string Shot { get; private set; }="";
        public float DistanceWalked { get; private set; }
        public float FadeAmount => fade!=null?fade.color.a:0;
        public Transform GateFrame => gateFrame;
        public void Configure(Transform frame,AudioSource crowd,AudioSource outside,Light insideLight,Light outsideLight)
        { gateFrame=frame; arenaAmbience=crowd; outdoorAmbience=outside; corridorLight=insideLight; exteriorLight=outsideLight; }
        void Start()
        {
            game=GetComponent<GameManager>();
            if(game!=null && !game.IsInfinite && arenaAmbience!=null) { arenaAmbience.volume=.075f; arenaAmbience.Play(); }
        }
        public bool TryBegin(GameManager match,Transform player)
        {
            if(used || IsPlaying || Completed || gateFrame==null || match==null || match.IsInfinite || match.Mode!=GameMode.Story ||
               match.State!=MatchState.Escape || !match.Gate.IsOpen || match.Waves.State!=WaveState.Complete ||
               match.Waves.WaveCount!=5 || match.Waves.WaveNumber!=5 || match.Waves.AliveCount!=0 || match.Waves.PendingCount!=0 ||
               player!=match.Player.transform || !player.GetComponent<CharacterHealth>().IsAlive ||
               player.GetComponent<Bellator.Multiplayer.NetworkGladiator>()!=null ||
               Bellator.Multiplayer.OnlineGame.Instance!=null && Bellator.Multiplayer.OnlineGame.Instance.Connected) return false;
            view=Camera.main; if(view==null) return false;
            game=match; actor=player; motor=actor.GetComponent<ThirdPersonMotor>(); combat=actor.GetComponent<PlayerCombat>(); orbit=view.GetComponent<ThirdPersonCamera>();
            startPosition=view.transform.position; startRotation=view.transform.rotation;
            if(!game.PrepareEscapeCinematic()) return false;
            used=IsPlaying=true; DistanceWalked=0;
            movementReference=new GameObject("EscapeMovementReference").transform; movementReference.SetParent(transform,false); movementReference.rotation=gateFrame.rotation;
            previousSprint=motor.SprintAllowed; previousDodge=motor.DodgeAllowed; previousRotation=motor.RotationAllowed;
            motor.Configure(movementReference); motor.AcceptInput=false; motor.SprintAllowed=motor.DodgeAllowed=false; motor.RotationAllowed=true;
            previousSpeed=motor.MovementSpeedMultiplier; motor.MovementSpeedMultiplier=Mathf.Clamp(walkingSpeed/motor.WalkSpeed,.2f,1);
            if(orbit!=null) { orbit.AcceptInput=false; orbit.enabled=false; }
            BuildFade();
            if(outdoorAmbience!=null) { outdoorAmbience.volume=0; outdoorAmbience.Play(); }
            running=StartCoroutine(Play()); return true;
        }
        void BuildFade()
        {
            fadeCanvas=new GameObject("EscapeFadeCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler)).GetComponent<Canvas>();
            fadeCanvas.transform.SetParent(transform,false); fadeCanvas.renderMode=RenderMode.ScreenSpaceOverlay; fadeCanvas.sortingOrder=29;
            var image=new GameObject("FadeToFreedom",typeof(RectTransform),typeof(Image)); image.transform.SetParent(fadeCanvas.transform,false);
            var rect=image.GetComponent<RectTransform>(); rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
            fade=image.GetComponent<Image>(); fade.color=Color.clear; fade.raycastTarget=false;
        }
        IEnumerator Play()
        {
            yield return null; // Leave the movement callback before taking cinematic control.
            float elapsed=0,blocked=0;
            Vector3 previous=actor.position;
            while(gateFrame.InverseTransformPoint(actor.position).z<stopDepth-.12f)
            {
                float dt=Time.deltaTime; elapsed+=dt;
                float localZ=gateFrame.InverseTransformPoint(actor.position).z;
                Vector3 target=gateFrame.TransformPoint(new Vector3(0,0,stopDepth));
                Vector3 local=movementReference.InverseTransformDirection(Vector3.ProjectOnPlane(target-actor.position,Vector3.up));
                Vector2 axis=Vector2.ClampMagnitude(new Vector2(local.x,local.z),1);
                // Brake into the stop instead of snapping the actor into a pose.
                if(localZ>stopDepth-.8f) axis*=Mathf.Clamp01((stopDepth-localZ)/.8f);
                Walk(axis,dt);
                float moved=Vector3.Distance(previous,actor.position); DistanceWalked+=moved;
                blocked=moved<.0002f?blocked+dt:0; previous=actor.position;
                if(blocked>3 || elapsed>50) { Abort("O caminho de saída está bloqueado. A sequência foi interrompida com segurança."); yield break; }
                float progress=Mathf.InverseLerp(4,exteriorStart,localZ);
                if(arenaAmbience!=null) arenaAmbience.volume=Mathf.Lerp(.075f,0,progress);
                if(outdoorAmbience!=null) outdoorAmbience.volume=Mathf.Lerp(0,.28f,progress);
                if(corridorLight!=null) corridorLight.intensity=Mathf.Lerp(.6f,1.25f,progress);
                if(exteriorLight!=null) exteriorLight.intensity=Mathf.Lerp(1,2.1f,progress);
                Vector3 focus=actor.position+Vector3.up*actor.GetComponent<CharacterController>().height*.65f;
                Vector3 desired; Quaternion rotation;
                if(elapsed<3)
                {
                    Shot="Entrando no corredor";
                    desired=actor.position+gateFrame.TransformDirection(new Vector3(-1.9f,2.5f,4));
                    rotation=Quaternion.LookRotation(focus-desired);
                    float blend=Mathf.SmoothStep(0,1,elapsed/3);
                    desired=Vector3.Lerp(startPosition,desired,blend); rotation=Quaternion.Slerp(startRotation,rotation,blend);
                }
                else if(localZ<exteriorStart)
                {
                    Shot="Caminhando para a liberdade";
                                        float around=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-3)/2));
                    float angle=Mathf.Lerp(-25,-184,around)*Mathf.Deg2Rad;
                    float radius=Mathf.Lerp(4.43f,5.2f,around);
                    desired=actor.position+gateFrame.TransformDirection(new Vector3(Mathf.Sin(angle)*radius,2.65f,Mathf.Cos(angle)*radius));
                    rotation=Quaternion.LookRotation(focus+gateFrame.forward*1.8f-desired);
                }
                else
                {
                    Shot="Saindo do Coliseu";
                    float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(exteriorStart,stopDepth,localZ));
                    Vector3 behind=actor.position+gateFrame.TransformDirection(new Vector3(.35f,2.65f,-5.2f));
                    desired=Vector3.Lerp(behind,gateFrame.TransformPoint(new Vector3(10,6.2f,48)),blend);
                    rotation=Quaternion.LookRotation(focus-desired);
                }
                SetCamera(desired,rotation,focus,dt,elapsed<3);
                yield return null;
            }
            Shot="Observando o exterior";
            for(float t=0;t<3;t+=Time.deltaTime)
            {
                Walk(Vector2.zero,Time.deltaTime);
                actor.rotation=Quaternion.RotateTowards(actor.rotation,gateFrame.rotation*Quaternion.Euler(0,-25,0),12*Time.deltaTime);
                Vector3 desired=gateFrame.TransformPoint(Vector3.Lerp(new Vector3(10,6.2f,48),new Vector3(12,7.4f,55),Mathf.SmoothStep(0,1,t/3)));
                Vector3 focus=actor.position+Vector3.up*1.5f;
                SetCamera(desired,Quaternion.LookRotation(focus-desired),focus,Time.deltaTime,false);
                yield return null;
            }
            Shot="A liberdade foi conquistada";
            for(float t=0;t<1.6f;t+=Time.deltaTime) { fade.color=new Color(0,0,0,Mathf.SmoothStep(0,1,t/1.6f)); yield return null; }
            fade.color=Color.black;
            IsPlaying=false; Completed=true; running=null;
            game.CompleteEscapeCinematic();
        }
        void Walk(Vector2 axis,float dt)
        {
            bool suspended=combat.ControlsSuspended;
            try { combat.ControlsSuspended=false; motor.ProcessMovement(axis,false,false,dt); }
            finally { combat.ControlsSuspended=suspended; }
        }
        void SetCamera(Vector3 desired,Quaternion rotation,Vector3 focus,float dt,bool entering)
        {
            Vector3 delta=desired-focus; float distance=delta.magnitude;
            foreach(var hit in Physics.SphereCastAll(focus,.18f,delta.normalized,distance,~0,QueryTriggerInteraction.Ignore))
            {
                if(hit.collider.transform.IsChildOf(actor) || hit.collider.transform.IsChildOf(view.transform)) continue;
                distance=Mathf.Min(distance,Mathf.Max(.55f,hit.distance-.20f));
            }
            desired=focus+delta.normalized*distance;
            float blend=entering?1:1-Mathf.Exp(-3.5f*dt);
            view.transform.position=Vector3.Lerp(view.transform.position,desired,blend);
            view.transform.rotation=Quaternion.Slerp(view.transform.rotation,rotation,blend);
        }
        void Abort(string reason)
        {
            Debug.LogError(reason,this); IsPlaying=false; used=false; running=null;
            if(fadeCanvas!=null) Destroy(fadeCanvas.gameObject);
            Restore(); if(game!=null) game.AbortEscapeCinematic();
        }
        void Restore()
        {
            if(motor!=null) { motor.Configure(view!=null?view.transform:null); motor.MovementSpeedMultiplier=previousSpeed; motor.SprintAllowed=previousSprint; motor.DodgeAllowed=previousDodge; motor.RotationAllowed=previousRotation; }
            if(orbit!=null) { orbit.enabled=true; orbit.AcceptInput=true; }
            if(movementReference!=null) Destroy(movementReference.gameObject);
        }
        void OnDisable()
        {
            if(running!=null) StopCoroutine(running);
            if(IsPlaying) { IsPlaying=false; if(game!=null && game.isActiveAndEnabled && !game.Loading) { Restore(); game.AbortEscapeCinematic(); } }
            if(arenaAmbience!=null) arenaAmbience.Stop(); if(outdoorAmbience!=null) outdoorAmbience.Stop();
        }
    }
}
