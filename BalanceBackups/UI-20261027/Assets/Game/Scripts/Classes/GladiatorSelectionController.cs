using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace Bellator.Classes
{
    public sealed class GladiatorSelectionController : MonoBehaviour
    {
        [SerializeField] private GladiatorClassCatalog catalog;
        [SerializeField] private Button[] classButtons;
        [SerializeField] private Outline[] outlines;
        [SerializeField] private Text[] titles,details;
        [SerializeField] private GameObject[] halos;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Text status;
        private int selected;
        private GladiatorClassDefinition confirmed;
        private bool loading;
        public int SelectedIndex => selected;
        public GladiatorClassDefinition Confirmed => confirmed;
        public GladiatorClassCatalog Catalog => catalog;
        public void Configure(GladiatorClassCatalog value,Button[] buttons,Outline[] borders,Text[] names,Text[] descriptions,GameObject[] marks,Button confirm,Text label)
        { catalog=value; classButtons=buttons; outlines=borders; titles=names; details=descriptions; halos=marks; confirmButton=confirm; status=label; }
        private void Start()
        {
            Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
            for(int i=0;i<classButtons.Length;i++) { int index=i; classButtons[i].onClick.AddListener(()=>Select(index)); }
            confirmButton.onClick.AddListener(Confirm);
            selected=catalog.defaultClass;
            if(ClassSelectionState.Selected!=null)
                for(int i=0;i<catalog.classes.Length;i++) if(catalog.classes[i]==ClassSelectionState.Selected) selected=i;
            RefreshPresentation();
        }
        private void Update()
        {
            var keyboard=Keyboard.current; if(keyboard==null || loading) return;
            if(keyboard.digit1Key.wasPressedThisFrame) Select(0);
            if(keyboard.digit2Key.wasPressedThisFrame) Select(1);
            if(keyboard.digit3Key.wasPressedThisFrame) Select(2);
            if(keyboard.digit4Key.wasPressedThisFrame) Select(3);
        }
        public void Select(int index)
        {
            if(loading || index<0 || index>=catalog.classes.Length) return;
            selected=index; confirmed=null; RefreshPresentation();
        }
        public void Confirm()
        {
            if(loading) return;
            confirmed=catalog.classes[selected]; ClassSelectionState.Confirm(confirmed);
            loading=true; RefreshPresentation();
            SceneManager.LoadSceneAsync(Bellator.Match.GameInterface.MainScene,LoadSceneMode.Single);
        }
        public void RefreshPresentation()
        {
            for(int i=0;i<catalog.classes.Length;i++)
            {
                var c=catalog.classes[i]; titles[i].text=c.displayName.ToUpperInvariant();
                details[i].text=c.description+"\n\nVIDA "+c.health+"     RESISTÊNCIA "+c.stamina+
                    "\nVelocidade "+c.walkSpeed.ToString("0.0")+"  |  Defesa "+Mathf.RoundToInt(c.defense*100)+"%"+
                    "\nDano corpo a corpo "+c.meleeDamage.ToString("0.00")+"x  |  distância "+c.rangedDamage.ToString("0.00")+"x"+
                    "\nArmas: "+string.Join(" + ",System.Array.ConvertAll(c.equipment,w=>w.displayName))+(c.shield!=null?" + escudo":"")+
                    "\n\n"+c.ability.displayName+"\n"+c.ability.description+
                    "\nRecarga: "+c.ability.cooldown+" s  |  Custo: "+c.ability.staminaCost;
                outlines[i].enabled=i==selected; halos[i].SetActive(i==selected);
            }
            confirmButton.interactable=!loading && confirmed!=catalog.classes[selected];
            status.text=loading?"Seleção salva. Voltando ao menu...":"Selecionado: "+catalog.classes[selected].displayName+" — confirme para voltar ao menu";
        }
    }
}


