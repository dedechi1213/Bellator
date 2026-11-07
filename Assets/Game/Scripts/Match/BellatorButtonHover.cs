using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bellator.Match
{
    // Keyboard focus stays gold. Red requires an actual pointer hover.
    [RequireComponent(typeof(Button))]
    public sealed class BellatorButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        public static readonly Color Normal=new Color(.075f,.061f,.048f), Hover=new Color(.36f,.038f,.025f), Pressed=new Color(.22f,.018f,.012f);
        Button button; BellatorFrame frame; bool over,down,focused,lastInteractable;
        void Awake() { button=GetComponent<Button>(); frame=GetComponent<BellatorFrame>(); }
        void OnEnable() { over=down=focused=false; Apply(); }
        void OnDisable() { over=down=focused=false; if(frame!=null) frame.color=Normal; }
        void Update() { if(lastInteractable!=button.IsInteractable()) Apply(); }
        public void OnPointerEnter(PointerEventData e) { over=true; Apply(); }
        public void OnPointerExit(PointerEventData e) { over=down=false; Apply(); }
        public void OnPointerDown(PointerEventData e) { if(e.button==PointerEventData.InputButton.Left) { down=true; Apply(); } }
        public void OnPointerUp(PointerEventData e) { down=false; Apply(); }
        public void OnSelect(BaseEventData e) { focused=true; Apply(); }
        public void OnDeselect(BaseEventData e) { focused=false; Apply(); }
        void Apply()
        {
            if(button==null) button=GetComponent<Button>(); if(frame==null) frame=GetComponent<BellatorFrame>();
            lastInteractable=button.IsInteractable(); frame.color=!lastInteractable?new Color(.10f,.10f,.10f):over?(down?Pressed:Hover):Normal;
            var mark=transform.Find("KeyboardFocus"); if(mark!=null) mark.gameObject.SetActive(focused&&lastInteractable);
            var caption=GetComponentInChildren<TMPro.TextMeshProUGUI>(); if(caption!=null) caption.color=lastInteractable?BellatorUI.Parchment:new Color(.48f,.46f,.40f);
        }
    }
}
