#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using Bellator.Character;
using Bellator.Classes;
using Bellator.Enemies;

namespace Bellator.Combat
{
    /// <summary>Only attached to the separate development scene. Never installed on the normal HUD.</summary>
    public sealed class BalanceTrainingLab : MonoBehaviour
    {
        public PlayerClassController player;
        public GameObject[] enemyPrefabs;
        public bool passiveTargets = true;
        readonly List<GameObject> targets = new List<GameObject>();
        readonly Dictionary<DamageReceiver,float> firstHit = new Dictionary<DamageReceiver,float>();
        readonly Dictionary<DamageReceiver,int> targetHits = new Dictionary<DamageReceiver,int>();
        readonly List<string> defeats = new List<string>();
        float started, stopped, caused, received, spent;
        int hits, abilities, initialDodges, initialAttacks;
        bool panel = true;
        public float DamageCaused => caused;
        public float DamageReceived => received;
        public float StaminaConsumed => spent;
        public int AbilityUses => abilities;
        public float Elapsed => (player.GetComponent<CharacterHealth>().IsAlive ? Time.time : stopped) - started;
        public int Dodges => player.GetComponent<ThirdPersonMotor>().DodgeSequence - initialDodges;
        public int Attacks => player.GetComponent<PlayerCombat>().AttackSequence - initialAttacks;
        public float EstimatedDps => caused / Mathf.Max(.001f,Elapsed);
        void Start()
        {
            DamageReceiver.Resolved += OnHit;
            player.GetComponent<CharacterStamina>().Consumed += OnSpent;
            player.GetComponent<PlayerAbility>().Activated += OnAbility;
            ResetMetrics(); SetPanel(true);
        }
        void OnDestroy()
        {
            DamageReceiver.Resolved -= OnHit;
            if(player == null) return;
            player.GetComponent<CharacterStamina>().Consumed -= OnSpent;
            player.GetComponent<PlayerAbility>().Activated -= OnAbility;
        }
        void OnSpent(float value) => spent += value;
        void OnAbility() => abilities++;
        void OnHit(DamageReceiver target, DamageInfo hit, DamageResult result)
        {
            if(target.transform == player.transform) { received += result.Damage; if(!target.Health.IsAlive) stopped=Time.time; }
            if(hit.Attacker != player.transform) return;
            caused += result.Damage; hits++;
            if(!firstHit.ContainsKey(target)) firstHit[target]=Time.time;
            targetHits[target]=targetHits.TryGetValue(target,out var count)?count+1:1;
            if(!target.Health.IsAlive) defeats.Add(target.name+": "+(Time.time-firstHit[target]).ToString("F2")+" s desde primeiro impacto | "+targetHits[target]+" impactos | "+Attacks+" ataques totais na tentativa");
        }
        public void ResetMetrics()
        {
            started=stopped=Time.time; caused=received=spent=0; hits=abilities=0;
            initialDodges=player.GetComponent<ThirdPersonMotor>().DodgeSequence;
            initialAttacks=player.GetComponent<PlayerCombat>().AttackSequence;
            firstHit.Clear(); targetHits.Clear(); defeats.Clear();
        }
        public void ResetTrial(GladiatorClassDefinition definition = null)
        {
            foreach(var target in targets) if(target != null) { target.SetActive(false); Destroy(target); }
            targets.Clear();
            player.Apply(definition != null ? definition : player.Current);
            ResetMetrics();
        }
        public EnemyBrain Spawn(int index)
        {
            var root=Instantiate(enemyPrefabs[index],player.transform.position+player.transform.forward*6,Quaternion.LookRotation(-player.transform.forward));
            targets.Add(root); var brain=root.GetComponent<EnemyBrain>(); brain.AutoThink=!passiveTargets;
            brain.SetTarget(player.GetComponent<DamageReceiver>());
            return brain;
        }
        public string Metrics() => "Classe: "+player.Current.displayName+" | Vida: "+player.GetComponent<CharacterHealth>().Current.ToString("F1")+" / "+player.GetComponent<CharacterHealth>().Maximum+
            "\nStamina: "+player.GetComponent<CharacterStamina>().Current.ToString("F1")+" / "+player.GetComponent<CharacterStamina>().Maximum+"\nTempo de sobrevivencia: "+Elapsed.ToString("F2")+
            " s\nDano causado/recebido: "+caused.ToString("F1")+" / "+received.ToString("F1")+
            "\nDPS observado (inclui espera): "+EstimatedDps.ToString("F2")+"\nAtaques / impactos: "+Attacks+" / "+hits+
            "\nEsquivas: "+Dodges+" | Stamina consumida: "+spent.ToString("F1")+" | Habilidades: "+abilities+"\n"+string.Join("\n",defeats);
        void Update() { if(Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame) SetPanel(!panel); }
        void SetPanel(bool visible)
        {
            panel=visible;
            player.GetComponent<ThirdPersonMotor>().AcceptInput=!visible;
            player.GetComponent<CombatInput>().enabled=!visible;
            player.GetComponent<ThirdPersonInput>().enabled=!visible;
            if(Camera.main != null) Camera.main.GetComponent<ThirdPersonCamera>().AcceptInput=!visible;
            Cursor.lockState=visible?CursorLockMode.None:CursorLockMode.Locked; Cursor.visible=visible;
        }
        void OnGUI()
        {
            if(!panel) { GUI.Label(new Rect(16,16,320,30),"Laboratorio: F8 para ferramentas"); return; }
            GUILayout.BeginArea(new Rect(12,12,430,650),GUI.skin.box);
            GUILayout.Label("BELLATOR — Laboratorio de balanceamento (F8)");
            foreach(var definition in player.Catalog.classes)
                if(GUILayout.Button(definition.displayName)) ResetTrial(definition);
            passiveTargets=GUILayout.Toggle(passiveTargets,"Alvos passivos (desmarque antes de criar para combater IA)");
            for(int i=0;i<enemyPrefabs.Length;i++) if(GUILayout.Button("Criar "+enemyPrefabs[i].name)) Spawn(i);
            if(GUILayout.Button("Restaurar classe e limpar alvos")) ResetTrial();
            if(GUILayout.Button("Zerar medicoes")) ResetMetrics();
            if(GUILayout.Button("Exportar medicoes")) { string path=Path.Combine(Application.persistentDataPath,"BalanceTrial-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss")+".txt"); File.WriteAllText(path,Metrics()); Debug.Log("Medicoes: "+path); }
            if(GUILayout.Button("Combater (F8 abre novamente)")) SetPanel(false);
            GUILayout.Label(Metrics()); GUILayout.EndArea();
        }
    }
}
#endif
