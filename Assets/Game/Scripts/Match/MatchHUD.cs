using UnityEngine;
using UnityEngine.UI;

namespace Bellator.Match
{
    /// <summary>Presentation and button bindings only; no spawning or victory rules.</summary>
    public sealed class MatchHUD : MonoBehaviour
    {
        [SerializeField] private GameManager game;
        [SerializeField] private Text waveLabel,message,countdown,remaining,panelTitle,panelDescription;
        [SerializeField] private GameObject modal;
        [SerializeField] private Button resume,restart,menu;
        public void Configure(GameManager manager,Text wave,Text banner,Text time,Text alive,GameObject panel,Text title,Text description,Button continueButton,Button retryButton,Button menuButton)
        { game=manager; waveLabel=wave; message=banner; countdown=time; remaining=alive; modal=panel; panelTitle=title; panelDescription=description; resume=continueButton; restart=retryButton; menu=menuButton; }
        private void Start()
        { resume.onClick.AddListener(game.Resume); restart.onClick.AddListener(()=>game.Restart()); menu.onClick.AddListener(()=>game.ReturnToMenu()); }
        private void Update()
        {
            var waves=game.Waves; var state=game.State;
            waveLabel.text=state==MatchState.Preparing?"SOBREVIVÊNCIA NA ARENA":("ONDA "+waves.WaveNumber+" / "+waves.WaveCount+(waves.WaveNumber==waves.WaveCount?" · ONDA FINAL":""));
            remaining.text=state==MatchState.Fighting?"Inimigos vivos: "+waves.AliveCount+"   A chegar: "+waves.PendingCount:"Esc / P: pausa   F1: seleção";
            countdown.text="";
            switch(state)
            {
                case MatchState.Preparing: message.text="Prepare-se!"; countdown.text="Primeira onda em "+Mathf.CeilToInt(waves.Countdown)+" s"; break;
                case MatchState.Fighting: message.text=waves.SpawnBlocked?"Entrada bloqueada. Aguardando espaço...":(waves.WaveNumber==waves.WaveCount?"Onda final — lute por sua liberdade!":"Onda "+waves.WaveNumber+" — sobreviva!"); break;
                case MatchState.Intermission: message.text="Onda concluída!"; countdown.text="Próxima onda em "+Mathf.CeilToInt(waves.Countdown)+" s"; break;
                case MatchState.Escape: message.text="Você conquistou sua liberdade!"; countdown.text=game.Gate.IsOpen?"Atravesse o portão principal iluminado para sair.":"O portão principal está abrindo..."; remaining.text="Saída a "+Mathf.CeilToInt(Vector3.Distance(game.Player.transform.position,game.Gate.transform.position))+" m"; break;
            }
            bool show=state==MatchState.Paused || game.IsTerminal;
            waveLabel.transform.parent.gameObject.SetActive(!show);
            modal.SetActive(show); resume.gameObject.SetActive(state==MatchState.Paused);
            resume.interactable=restart.interactable=menu.interactable=!game.Loading;
            if(show)
            {
                panelTitle.text=state==MatchState.Paused?"PAUSA":state==MatchState.Defeat?"GAME OVER":"VITÓRIA";
                panelDescription.text=game.Loading?"Carregando...":state==MatchState.Paused?"A arena aguarda. Retome a luta quando estiver pronto.":state==MatchState.Defeat?"Sua luta terminou. Tente novamente ou escolha outro gladiador.":"Você atravessou o portão e conquistou sua liberdade.";
                restart.GetComponentInChildren<Text>().text="REINICIAR PARTIDA"; menu.GetComponentInChildren<Text>().text="VOLTAR À SELEÇÃO";
            }
        }
    }
}

