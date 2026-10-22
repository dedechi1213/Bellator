using UnityEngine;
using Bellator.Character;

namespace Bellator.Combat
{
    public sealed class CombatHUD : MonoBehaviour
    {
        [SerializeField] private PlayerCombat player;
        [SerializeField] private bool survivalMode;
        private float nextTargetsRefresh;
        public bool SurvivalMode { get => survivalMode; set => survivalMode=value; }
        public bool NativePresentation { get; set; }
        private WeaponEquipment equipment;
        private CharacterHealth health;
        private CharacterStamina stamina;
        private CombatInput input;
        private ShieldGuard guard;
        private GUIStyle text,title;
        private Bellator.Classes.PlayerClassController selectedClass; private Bellator.Classes.PlayerAbility ability;
        private DamageReceiver[] targets;
        public void Configure(PlayerCombat value) => player=value;
        private void Start()
        { equipment=player.GetComponent<WeaponEquipment>(); health=player.GetComponent<CharacterHealth>(); stamina=player.GetComponent<CharacterStamina>(); input=player.GetComponent<CombatInput>(); guard=player.GetComponent<ShieldGuard>(); targets=FindObjectsByType<DamageReceiver>(FindObjectsSortMode.None); selectedClass=player.GetComponent<Bellator.Classes.PlayerClassController>(); ability=player.GetComponent<Bellator.Classes.PlayerAbility>(); }
        private void Update()
        {
            if(Time.unscaledTime<nextTargetsRefresh) return;
            nextTargetsRefresh=Time.unscaledTime+.4f; targets=FindObjectsByType<DamageReceiver>(FindObjectsSortMode.None);
        }
        private void OnGUI()
        {
            if(NativePresentation) return;
            if(equipment==null || equipment.Current==null) return;
            if(text==null) { text=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true}; text.normal.textColor=Color.white; title=new GUIStyle(text){fontSize=18,fontStyle=FontStyle.Bold}; }
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);
            if(scale<=0) return;
            Matrix4x4 old=GUI.matrix; GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            float w=Screen.width/scale,h=Screen.height/scale;
            GUI.Box(new Rect(18,18,385,157),GUIContent.none);
            GUI.Label(new Rect(32,28,360,28),(selectedClass!=null && selectedClass.Current!=null?selectedClass.Current.displayName+" · ":"")+equipment.Current.displayName,title);
            Bar(new Rect(32,65,270,14),health.Current/health.Maximum,new Color(.75f,.14f,.1f));
            GUI.Label(new Rect(310,60,90,26),"Vida "+Mathf.CeilToInt(health.Current),text);
            Bar(new Rect(32,92,270,14),stamina.Current/stamina.Maximum,new Color(.25f,.7f,.32f));
            GUI.Label(new Rect(310,87,90,26),"Fôlego "+Mathf.CeilToInt(stamina.Current),text);
            string state=guard.IsBroken?"GUARDA QUEBRADA":(guard.IsBlocking?"Defendendo":player.Status);
            if(equipment.Current.type==EquipmentType.Crossbow) state=player.IsReloading?"Recarga "+Mathf.RoundToInt(player.ReloadProgress*100)+"%":(player.CrossbowLoaded?"Besta carregada":state);
            GUI.Label(new Rect(32,122,360,40),state,text);
            GUI.Box(new Rect(18,h-128,850,109),GUIContent.none);
            GUI.Label(new Rect(32,h-120,825,100),ControlHints(equipment,input.HeavyDisplay)+(survivalMode?"":"\nT: restaurar treino   F1: seleção"),text);
            if(equipment.Current.IsRanged)
            {
                Color oldColor=GUI.color; GUI.color=Color.white;
                GUI.DrawTexture(new Rect(w/2-10,h/2-1,7,2),Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(w/2+3,h/2-1,7,2),Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(w/2-1,h/2-10,2,7),Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(w/2-1,h/2+3,2,7),Texture2D.whiteTexture); GUI.color=oldColor;
            }
            if(Camera.main!=null && targets!=null)
                foreach(var target in targets)
                {
                    if(target==null || !target.gameObject.activeInHierarchy || !target.Health.IsAlive || target.transform==player.transform) continue;
                    var body=target.GetComponent<CharacterController>();
                    Vector3 p=Camera.main.WorldToScreenPoint(target.transform.position+Vector3.up*(body!=null?body.height+.35f:2.45f));
                    if(p.z<=0 || p.z>35) continue;
                    float x=p.x/scale,y=(Screen.height-p.y)/scale;
                    Bar(new Rect(x-35,y,70,6),target.Health.Current/target.Health.Maximum,new Color(.8f,.24f,.1f));
                    GUI.Label(new Rect(x-35,y-24,85,25),Mathf.CeilToInt(target.Health.Current)+" / "+target.Health.Maximum,text);
                }            GUI.matrix=old;
        }
        public static string ControlHints(WeaponEquipment equipment,string heavyKey)
        {
            var weapon=equipment.Current;
            return "WASD mover  •  Shift correr  •  Espaço esquivar\nMouse esquerdo "+(weapon!=null && weapon.IsRanged?"disparar":"atacar  •  "+heavyKey+" ataque pesado")+
                (equipment.HasShield?"  •  Mouse direito defender":"")+(weapon!=null && weapon.type==EquipmentType.Crossbow?"  •  R recarregar":"")+"  •  Q habilidade  •  Esc pausa";
        }
        private static void Bar(Rect r,float fraction,Color color)
        {
            Color old=GUI.color; GUI.color=new Color(.09f,.09f,.09f,.9f); GUI.DrawTexture(r,Texture2D.whiteTexture);
            GUI.color=color; GUI.DrawTexture(new Rect(r.x,r.y,r.width*Mathf.Clamp01(fraction),r.height),Texture2D.whiteTexture); GUI.color=old;
        }
    }
}






