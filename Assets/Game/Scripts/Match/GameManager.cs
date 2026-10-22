using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Bellator.Arena;
using Bellator.Character;
using Bellator.Classes;
using Bellator.Combat;

namespace Bellator.Match
{
    public enum MatchState { None, Preparing, Fighting, Intermission, Escape, Paused, Victory, Defeat }
    [DefaultExecutionOrder(50)]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private PlayerClassController player;
        [SerializeField] private WaveManager waves;
        [SerializeField] private MainGateController gate;
        [SerializeField] private ArenaExitTrigger exit;
        [SerializeField] private Transform playerSpawn;
        [SerializeField] private GameObject exitBeacon;
        [SerializeField] private CombatHUD combatHUD;
        CharacterHealth health;
        ThirdPersonInput movementInput;
        ThirdPersonMotor motor;
        ThirdPersonCamera orbit;
        CombatInput combatInput;
        PlayerCombat combat;
        PlayerAbility ability;
        MatchState beforePause;
        float savedTimeScale=1;
        bool started,loading;
        ScoreManager score;
        public GameMode Mode { get; private set; }
        public bool IsInfinite => Mode==GameMode.Infinite;
        public MatchStatistics Statistics => score?.Current.Copy();
        public MatchStatistics FinalStatistics { get; private set; }
        public LocalRankingManager Ranking { get; private set; }
        public bool ResultSaved { get; private set; }
        public bool NewRecord { get; private set; }
        public bool CanRecordResult => IsInfinite && score!=null && score.CanRecord;
        public string RankingError => Ranking?.LastError;
        public event Action StateChanged;
        public MatchState State { get; private set; }=MatchState.None;
        public PlayerClassController Player => player;
        public WaveManager Waves => waves;
        public MainGateController Gate => gate;
        public ArenaExitTrigger Exit => exit;
        public bool IsTerminal => State==MatchState.Victory || State==MatchState.Defeat;
        public bool Loading => loading;
        public void Configure(PlayerClassController actor,WaveManager manager,MainGateController mainGate,ArenaExitTrigger crossing,Transform spawn,GameObject beacon,CombatHUD hud)
        { player=actor; waves=manager; gate=mainGate; exit=crossing; playerSpawn=spawn; exitBeacon=beacon; combatHUD=hud; }
        private void Awake()
        {
            Time.timeScale=1;
            Mode=GameModeManager.Selected;
            if(IsInfinite) Ranking=new LocalRankingManager();
            health=player.GetComponent<CharacterHealth>(); motor=player.GetComponent<ThirdPersonMotor>(); movementInput=player.GetComponent<ThirdPersonInput>();
            combat=player.GetComponent<PlayerCombat>(); combatInput=player.GetComponent<CombatInput>(); ability=player.GetComponent<PlayerAbility>();
            orbit=Camera.main!=null?Camera.main.GetComponent<ThirdPersonCamera>():null;
            if(exitBeacon!=null) exitBeacon.SetActive(false);
        }
        private void OnEnable()
        { health.Changed+=HealthChanged; waves.WaveStarted+=WaveStarted; waves.WaveCleared+=WaveCleared; waves.EnemyDefeated+=EnemyDefeated; waves.AllWavesCleared+=UnlockExit; exit.Crossed+=CrossedExit; }
        private void Start()
        {
            started=true; loading=false; gate.Close();
            var chosen=ClassSelectionState.Selected!=null?ClassSelectionState.Selected:player.Catalog.Default;
            score=new ScoreManager(Mode,chosen.id.ToString(),chosen.displayName);
            motor.Teleport(playerSpawn.position,playerSpawn.rotation);
            SetControls(true); ChangeState(MatchState.Preparing); waves.Begin(Mode);
        }
        private void Update()
        {
            if(loading || IsTerminal || !started) return;
            if(State!=MatchState.Paused) score.Tick(Time.deltaTime);
            var keyboard=Keyboard.current;
            var ui=FindAnyObjectByType<GameInterface>();
            if(ui!=null && ui.SettingsVisible) return;
            if(keyboard!=null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame)) TogglePause();
        }
        private void ChangeState(MatchState value) { State=value; StateChanged?.Invoke(); }
        private void EnemyDefeated(Bellator.Enemies.EnemyRole role,int wave) { if(!IsTerminal && !loading) score?.Defeated(role,wave); }
        private void WaveStarted(int number) { if(!IsTerminal && !loading) { score?.BeginWave(number); ChangeState(MatchState.Fighting); } }
        private void WaveCleared(int number)
        { if(IsTerminal || loading) return; score?.CompleteWave(number); if(waves.State!=WaveState.Complete) ChangeState(MatchState.Intermission); }
        private void UnlockExit()
        {
            if(IsInfinite || IsTerminal || loading || !health.IsAlive) return;
            ChangeState(MatchState.Escape); gate.Open(); if(exitBeacon!=null) exitBeacon.SetActive(true);
        }
        public void TogglePause()
        {
            if(!started || IsTerminal || loading) return;
            if(State==MatchState.Paused) { Resume(); return; }
            beforePause=State; savedTimeScale=Time.timeScale; Time.timeScale=0; SetControls(false); ChangeState(MatchState.Paused);
        }
        public void Resume()
        {
            if(State!=MatchState.Paused || loading) return;
            Time.timeScale=savedTimeScale>0?savedTimeScale:1; SetControls(true); ChangeState(beforePause);
        }
        private void SetControls(bool active)
        {
            combat.ControlsSuspended=!active; ability.SimulationSuspended=!active;
            motor.AcceptInput=active; movementInput.enabled=active; combatInput.enabled=active;
            if(orbit!=null) orbit.AcceptInput=active;
            if(combatHUD!=null) combatHUD.enabled=active;
            Cursor.lockState=active?CursorLockMode.Locked:CursorLockMode.None; Cursor.visible=!active;
        }
        private void HealthChanged(float current,float maximum)
        { if(started && current<=0 && !IsTerminal && !loading) Finish(MatchState.Defeat); }
        private void CrossedExit(Transform actor)
        {
            if(IsInfinite || !enabled || State!=MatchState.Escape || !health.IsAlive || !gate.IsOpen || !exit.IsBeyondThreshold(actor)) return;
            Finish(MatchState.Victory);
        }
        private void Finish(MatchState result)
        {
            if(IsInfinite && result==MatchState.Victory) return;
            FinalStatistics=score.Finish();
            NewRecord=CanRecordResult && FinalStatistics.score>Ranking.BestForProfile(FinalStatistics.profileId);
            Time.timeScale=1; waves.Stop();
            combat.ResetTrainingState(); ability.ResetAbility();
            foreach(var enemy in waves.Actors) if(enemy!=null) enemy.SuspendCombat();
            foreach(var projectile in FindObjectsByType<CombatProjectile>(FindObjectsSortMode.None)) { projectile.gameObject.SetActive(false); Destroy(projectile.gameObject); }
            SetControls(false); ChangeState(result);
            if(CanRecordResult && result==MatchState.Defeat) SaveInfiniteResult();
        }
        public AsyncOperation Restart()
        {
            if(loading) return null;
            if(player.Current!=null) ClassSelectionState.Confirm(player.Current);
            GameModeManager.Select(Mode);
            return Load(PlayerClassController.ArenaScene);
        }
        public AsyncOperation ReturnToMenu() => loading?null:Load(GameInterface.MainScene);
        public bool SaveInfiniteResult(string ignoredName=null)
        {
            if(ResultSaved) return true;
            if(!CanRecordResult || State!=MatchState.Defeat || FinalStatistics==null) return false;
            ResultSaved=score.SaveResult(Ranking); return ResultSaved;
        }
        private AsyncOperation Load(string scene)
        {
            loading=true; Time.timeScale=1; waves.Stop(); SetControls(false); StateChanged?.Invoke();
            return SceneManager.LoadSceneAsync(scene,LoadSceneMode.Single);
        }
        private void OnDisable()
        {
            if(health!=null) health.Changed-=HealthChanged;
            if(waves!=null) { waves.WaveStarted-=WaveStarted; waves.WaveCleared-=WaveCleared; waves.EnemyDefeated-=EnemyDefeated; waves.AllWavesCleared-=UnlockExit; waves.Stop(true); }
            if(exit!=null) exit.Crossed-=CrossedExit;
            Time.timeScale=1;
        }
    }
}
