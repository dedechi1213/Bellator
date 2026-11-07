using UnityEngine;
using UnityEngine.AI;

namespace Bellator.Enemies
{
    [RequireComponent(typeof(NavMeshAgent),typeof(CharacterController))]
    public sealed class EnemyNavigation : MonoBehaviour
    {
        private NavMeshAgent agent;
        private CharacterController body;
        private Vector3 goal,previous;
        private float vertical,repath,stuck;
        private bool moving;
        private Vector3 impulse;
        private float impulseRemaining;
        public bool BeingPushed => impulseRemaining>0;
        public void AddImpulse(Vector3 speed,float duration) { if(!Ready) return; Stop(); impulse=speed; impulseRemaining=duration; }
        public float Speed { get; private set; }
        public bool Ready => agent!=null && agent.enabled && agent.isOnNavMesh;
        public bool Moving => moving;
        private void Awake()
        {
            agent=GetComponent<NavMeshAgent>(); body=GetComponent<CharacterController>();
            agent.updatePosition=false; agent.updateRotation=false; previous=transform.position;
        }
        private void Start() { if(!Ready) Warp(transform.position,transform.rotation); }
        public bool Warp(Vector3 position,Quaternion rotation)
        {
            if(agent==null) agent=GetComponent<NavMeshAgent>();
            if(body==null) body=GetComponent<CharacterController>();
            if(!NavMesh.SamplePosition(position,out var sample,2,NavMesh.AllAreas)) return false;
            body.enabled=false; transform.SetPositionAndRotation(sample.position+Vector3.up*.025f,rotation);
            agent.enabled=true; agent.updatePosition=false; agent.updateRotation=false;
            bool success=agent.Warp(transform.position); body.enabled=true;
            impulseRemaining=0; impulse=Vector3.zero;
            previous=transform.position; vertical=0; stuck=0; Stop(); return success;
        }
        public void GoTo(Vector3 destination)
        {
            if(!Ready || BeingPushed) return;
            if(!NavMesh.SamplePosition(destination,out var hit,2,NavMesh.AllAreas)) { Stop(); return; }
            if((goal-hit.position).sqrMagnitude>.12f || !moving || repath<=0)
            {
                goal=hit.position; agent.isStopped=false;
                if(agent.SetDestination(goal)) { moving=true; repath=.25f; }
            }
        }
        public void Stop()
        {
            moving=false; Speed=0;
            if(Ready) { agent.isStopped=true; if(agent.hasPath) agent.ResetPath(); agent.velocity=Vector3.zero; }
        }
        public void Die()
        { Stop(); impulseRemaining=0; if(agent!=null) agent.enabled=false; if(body!=null) body.enabled=false; Speed=0; }
        public void Face(Vector3 position,float degreesPerSecond=360)
        {
            Vector3 delta=position-transform.position; delta.y=0;
            if(delta.sqrMagnitude>.001f) transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(delta),degreesPerSecond*Time.deltaTime);
        }
        private void Update()
        {
            if(!Ready || !body.enabled) return;
            float dt=Mathf.Min(Time.deltaTime,.05f); repath-=dt;
            Vector3 velocity=moving?Vector3.ProjectOnPlane(agent.desiredVelocity,Vector3.up):Vector3.zero;
            if(BeingPushed) { velocity=impulse; impulseRemaining=Mathf.Max(0,impulseRemaining-dt); }
            if(body.isGrounded && vertical<0) vertical=-2;
            vertical=Mathf.Max(-35,vertical-25*dt);
            Vector3 before=transform.position;
            body.Move((velocity+Vector3.up*vertical)*dt);
            agent.nextPosition=transform.position;
            Vector3 actual=transform.position-before; actual.y=0;
            Speed=dt>0?actual.magnitude/dt:0;
            if(moving && velocity.sqrMagnitude>.2f && (transform.position-previous).sqrMagnitude<.00002f) stuck+=dt; else stuck=Mathf.Max(0,stuck-dt*2);
            if(moving && stuck>1.2f)
            {
                // Short connected side-step breaks a local crowd deadlock; the brain then repaths to its target.
                Vector3 side=Vector3.Cross(Vector3.up,(goal-transform.position).normalized)*((GetInstanceID()&1)==0?1:-1);
                if(NavMesh.SamplePosition(transform.position+side*1.5f,out var sample,1,NavMesh.AllAreas))
                    agent.SetDestination(sample.position);
                repath=.8f; stuck=0;
            }
            previous=transform.position;
            if(moving && velocity.sqrMagnitude>.04f) Face(transform.position+velocity);
        }
        private void OnDisable() { if(agent!=null && agent.enabled && agent.isOnNavMesh) Stop(); }
    }
}


