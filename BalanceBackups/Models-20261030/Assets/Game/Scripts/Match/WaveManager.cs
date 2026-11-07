using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Bellator.Arena;
using Bellator.Enemies;
using Bellator.Combat;
using Bellator.Character;

namespace Bellator.Match
{
    public enum WaveState { Idle, Preparing, Spawning, Fighting, Interval, Complete, Stopped }
    /// <summary>Owns only this run's enemies. Signals progression without knowing GameManager or UI.</summary>
    public sealed class WaveManager : MonoBehaviour
    {
        [SerializeField] private WaveSchedule schedule;
        [SerializeField] private ArenaSpawnPoint[] spawnPoints;
        [SerializeField] private DamageReceiver player;
        [SerializeField] private Transform actorsRoot;
        [SerializeField] private DifficultyScaler infiniteDifficulty=new DifficultyScaler();
        readonly List<EnemyBrain> actors=new List<EnemyBrain>();
        readonly Queue<EnemyBrain> pending=new Queue<EnemyBrain>();
        sealed class TrackedEnemy
        {
            public CharacterHealth health;
            public Action<float,float> callback;
            public bool defeated;
        }
        readonly Dictionary<EnemyBrain,TrackedEnemy> tracked=new Dictionary<EnemyBrain,TrackedEnemy>();
        readonly Dictionary<Bellator.Enemies.EnemyRole,EnemyBrain> archetypes=new Dictionary<Bellator.Enemies.EnemyRole,EnemyBrain>();
        int index,entrance;
        float timer;
        public event Action<int> WaveStarted,WaveCleared;
        public event Action AllWavesCleared;
        public event Action<EnemyRole,int> EnemyDefeated;
        public WaveState State { get; private set; }=WaveState.Idle;
        public WaveSchedule Schedule => schedule;
        public DifficultyScaler InfiniteDifficulty => infiniteDifficulty;
        public bool IsInfinite { get; private set; }
        public int TotalInWave { get; private set; }
        public int DefeatedInWave { get; private set; }
        public IReadOnlyList<EnemyBrain> Actors => actors;
        public int WaveNumber => State==WaveState.Idle?0:index+1;
        public int WaveCount => schedule!=null && schedule.waves!=null?schedule.waves.Length:0;
        public int PendingCount => pending.Count;
        public int AliveCount { get { int n=0; foreach(var actor in actors) if(actor!=null && actor.GetComponent<CharacterHealth>().IsAlive) n++; return n; } }
        public float Countdown => Mathf.Max(0,timer);
        public bool SpawnBlocked { get; private set; }
        public bool ManualSimulation { get; set; }
        public void Configure(WaveSchedule value,ArenaSpawnPoint[] entries,DamageReceiver opponent,Transform root)
        { schedule=value; spawnPoints=entries; player=opponent; actorsRoot=root; }
        public void Begin(GameMode mode=GameMode.Story)
        {
            Stop(true); index=entrance=0; SpawnBlocked=false;
            IsInfinite=mode==GameMode.Infinite; TotalInWave=DefeatedInWave=0;
            if(infiniteDifficulty==null) infiniteDifficulty=new DifficultyScaler();
            if(IsInfinite)
            {
                archetypes.Clear();
                if(schedule!=null && schedule.waves!=null) foreach(var wave in schedule.waves)
                    if(wave!=null && wave.groups!=null) foreach(var group in wave.groups)
                        if(group!=null && group.prefab!=null && group.prefab.Definition!=null) archetypes[group.prefab.Definition.role]=group.prefab;
                foreach(EnemyRole role in Enum.GetValues(typeof(EnemyRole))) if(!archetypes.ContainsKey(role))
                { Debug.LogError("Bellator: missing infinite archetype: "+role,this); State=WaveState.Stopped; return; }
                timer=Mathf.Max(0,infiniteDifficulty.initialCountdown); State=WaveState.Preparing; return;
            }
            if(WaveCount==0) { State=WaveState.Complete; AllWavesCleared?.Invoke(); return; }
            timer=Mathf.Max(0,schedule.initialCountdown); State=WaveState.Preparing;
        }
        private void Update() { if(!ManualSimulation) Tick(Time.deltaTime); }
        public void Tick(float deltaTime)
        {
            if(deltaTime<=0 || player==null || !player.Health.IsAlive || State==WaveState.Idle || State==WaveState.Complete || State==WaveState.Stopped) return;
            if(IsInfinite) PruneDefeated();
            if(State==WaveState.Stopped) return;
            if(State==WaveState.Preparing || State==WaveState.Interval)
            {
                timer=Mathf.Max(0,timer-deltaTime);
                if(timer>0) return;
                BeginWave();
            }
            if(State==WaveState.Spawning)
            {
                timer=Mathf.Max(0,timer-deltaTime);
                if(timer<=0 && pending.Count>0 && (!IsInfinite || AliveCount<Mathf.Clamp(infiniteDifficulty.maximumActiveEnemies,1,15)))
                {
                    var prefab=pending.Peek();
                    if(TryFindSpawn(prefab,out var position,out var rotation))
                    {
                        var enemy=Instantiate(prefab,position,rotation,actorsRoot); enemy.name="Wave"+WaveNumber+"_"+prefab.name;
                        if(IsInfinite) infiniteDifficulty.Apply(enemy,WaveNumber);
                        enemy.SetTarget(player); enemy.PursueAssignedTarget=true; actors.Add(enemy); pending.Dequeue();
                        Track(enemy);
                        SpawnBlocked=false; entrance=(entrance+1)%spawnPoints.Length; timer=Mathf.Max(.05f,IsInfinite?infiniteDifficulty.spawnInterval:schedule.spawnInterval);
                    }
                    else { SpawnBlocked=true; timer=Mathf.Max(.05f,schedule.blockedSpawnRetry); }
                }
                if(pending.Count==0) { State=WaveState.Fighting; SpawnBlocked=false; }
            }
            if(State==WaveState.Fighting && AliveCount==0) CompleteWave();
        }
        private void BeginWave()
        {
            CleanupActors(); pending.Clear();
            DefeatedInWave=0;
            if(IsInfinite)
            {
                TotalInWave=infiniteDifficulty.EnemyCount(WaveNumber);
                for(int i=0;i<TotalInWave;i++) pending.Enqueue(archetypes[infiniteDifficulty.RoleFor(WaveNumber,i,TotalInWave)]);
                timer=0; State=WaveState.Spawning; WaveStarted?.Invoke(WaveNumber); return;
            }
            var wave=schedule.waves[index];
            if(wave.groups!=null) foreach(var group in wave.groups)
            {
                if(group==null || group.count<=0) continue;
                if(group.prefab==null) { Debug.LogError("Bellator: wave contains an unassigned enemy prefab.",this); State=WaveState.Stopped; return; }
                for(int i=0;i<group.count;i++) pending.Enqueue(group.prefab);
            }
            TotalInWave=pending.Count; timer=0; State=WaveState.Spawning; WaveStarted?.Invoke(WaveNumber);
        }
        private void CompleteWave()
        {
            int completed=WaveNumber;
            if(IsInfinite)
            {
                index++; State=WaveState.Interval; timer=Mathf.Max(0,infiniteDifficulty.betweenWaves); WaveCleared?.Invoke(completed);
            }
            else if(index+1>=WaveCount)
            {
                State=WaveState.Complete; WaveCleared?.Invoke(completed); AllWavesCleared?.Invoke();
            }
            else
            {
                index++; State=WaveState.Interval; timer=Mathf.Max(0,schedule.betweenWaves); WaveCleared?.Invoke(completed);
            }
        }
        public bool TryFindSpawn(EnemyBrain prefab,out Vector3 position,out Quaternion rotation)
        {
            position=default; rotation=Quaternion.identity;
            if(prefab==null || spawnPoints==null || spawnPoints.Length==0 || player==null) return false;
            var body=prefab.GetComponent<CharacterController>(); var agent=prefab.GetComponent<NavMeshAgent>();
            float radius=body.radius+.08f,height=Mathf.Max(body.height,radius*2+.05f);
            var filter=new NavMeshQueryFilter { agentTypeID=agent.agentTypeID,areaMask=NavMesh.AllAreas };
            if(!NavMesh.SamplePosition(player.transform.position,out var playerPoint,2,filter)) return false;
            var path=new NavMeshPath();
            for(int point=0;point<spawnPoints.Length;point++)
            {
                var entry=spawnPoints[(entrance+point)%spawnPoints.Length]; if(entry==null) continue;
                for(int offset=0;offset<9;offset++)
                {
                    float side=offset==0?0:((offset%2==0?-1:1)*((offset+1)/2)*.65f);
                    Vector3 candidate=entry.transform.position+entry.transform.right*side+entry.transform.forward*(offset>4?1.1f:0);
                    if(!NavMesh.SamplePosition(candidate,out var sample,.55f,filter)) continue;
                    Vector3 pos=sample.position+Vector3.up*.04f;
                    if(Vector3.ProjectOnPlane(pos-player.transform.position,Vector3.up).sqrMagnitude<schedule.minimumPlayerDistance*schedule.minimumPlayerDistance) continue;
                    if(Physics.CheckCapsule(pos+Vector3.up*(radius+.08f),pos+Vector3.up*(height-radius),radius,~0,QueryTriggerInteraction.Ignore)) continue;
                    if(!NavMesh.CalculatePath(sample.position,playerPoint.position,filter,path) || path.status!=NavMeshPathStatus.PathComplete) continue;
                    Vector3 forward=Vector3.ProjectOnPlane(player.transform.position-pos,Vector3.up);
                    position=pos; rotation=forward.sqrMagnitude>.001f?Quaternion.LookRotation(forward):entry.transform.rotation; return true;
                }
            }
            return false;
        }
        public void Stop(bool cleanup=false)
        {
            pending.Clear(); timer=0; SpawnBlocked=false; State=WaveState.Stopped;
            foreach(var entry in tracked.Values) if(entry.health!=null) entry.health.Changed-=entry.callback;
            tracked.Clear(); if(cleanup) CleanupActors();
        }
        private void Track(EnemyBrain enemy)
        {
            var item=new TrackedEnemy { health=enemy.GetComponent<CharacterHealth>() };
            var role=enemy.Definition.role; int wave=WaveNumber;
            item.callback=(current,maximum)=>
            {
                if(current>0 || item.defeated || State==WaveState.Stopped) return;
                item.defeated=true; item.health.Changed-=item.callback; DefeatedInWave++;
                EnemyDefeated?.Invoke(role,wave);
            };
            tracked.Add(enemy,item); item.health.Changed+=item.callback;
        }
        private void PruneDefeated()
        {
            for(int i=actors.Count-1;i>=0;i--)
            {
                var actor=actors[i];
                if(actor==null || !actor.gameObject.activeSelf)
                {
                    // External removal is not a kill. End spawning rather than award unearned points or silently skip an enemy.
                    Debug.LogError("Bellator: an active wave enemy was removed without defeat.",this); Stop(true); return;
                }
                if(!tracked.TryGetValue(actor,out var item) || !item.defeated) continue;
                item.health.Changed-=item.callback; tracked.Remove(actor); actors.RemoveAt(i);
                actor.gameObject.SetActive(false); Destroy(actor.gameObject);
            }
        }
        private void CleanupActors()
        {
            foreach(var entry in tracked.Values) if(entry.health!=null) entry.health.Changed-=entry.callback;
            tracked.Clear();
            foreach(var actor in actors) if(actor!=null) { actor.gameObject.SetActive(false); Destroy(actor.gameObject); } actors.Clear();
        }
        private void OnDisable() => Stop(true);
    }
}
