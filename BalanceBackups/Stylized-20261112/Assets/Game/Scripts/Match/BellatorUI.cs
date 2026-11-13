using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bellator.Match
{
    /// <summary>One style authority for all Bellator menus.</summary>
    public static class BellatorUI
    {
        public static readonly Color Gold=new Color(.85f,.67f,.35f), Parchment=new Color(.97f,.86f,.65f), Ink=new Color(.075f,.061f,.048f,.98f);
        public static GameObject Rect(Transform parent,string name,float x,float y,float r,float t)
        {
            var g=new GameObject(name,typeof(RectTransform)); g.transform.SetParent(parent,false);
            var rect=g.GetComponent<RectTransform>(); rect.anchorMin=new Vector2(x,y); rect.anchorMax=new Vector2(r,t); rect.offsetMin=rect.offsetMax=Vector2.zero; return g;
        }
        public static RectTransform Stage(Transform parent,string name)
        {
            var stage=Rect(parent,name,0,0,1,1).GetComponent<RectTransform>();
            var fit=stage.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio=16f/9; return stage;
        }
        public static bool Artwork(GameObject root,string key)
        {
            var sprite=Resources.Load<Sprite>("BellatorUI/Elements/"+(key=="CharacterSelection"?"SelectionArena":"Colosseum")); if(sprite==null) return false;
            var image=Rect(root.transform,"ColosseumBackground",0,0,1,1).AddComponent<Image>(); image.transform.SetAsFirstSibling(); image.sprite=sprite; image.raycastTarget=false; return true;
        }
        public static void Logo(Transform parent,float y=.72f,float top=.99f)
        {
            Rect(parent,"LogoSkyLight",.33f,y,.67f,top).AddComponent<BellatorSoftLight>();
            var image=Rect(parent,"OriginalBellatorLogo",.36f,y,.64f,top).AddComponent<Image>();
            image.sprite=Resources.Load<Sprite>("BellatorUI/Elements/OriginalLogo"); image.preserveAspect=true; image.raycastTarget=false;
        }
        public static BellatorFrame Frame(Transform parent,string name,float x,float y,float r,float t)
        {
            var f=Rect(parent,name,x,y,r,t).AddComponent<BellatorFrame>(); f.color=Ink; f.corner=30; f.border=4; f.raycastTarget=false;
            var shadow=f.gameObject.AddComponent<Shadow>(); shadow.effectColor=new Color(0,0,0,.7f); shadow.effectDistance=new Vector2(0,-5); return f;
        }
        public static TextMeshProUGUI Label(Transform parent,string name,string value,float x,float y,float r,float t,float size=28,bool serif=true)
        {
            var label=Rect(parent,name,x,y,r,t).AddComponent<TextMeshProUGUI>();
            if(serif) { var font=Resources.Load<TMP_FontAsset>("BellatorUI/Elements/Cinzel SDF"); if(font!=null) label.font=font; }
            label.text=value; label.fontSize=size; label.color=Parchment; label.alignment=TextAlignmentOptions.Center;
            label.raycastTarget=false; label.richText=false; label.enableAutoSizing=true; label.fontSizeMin=Mathf.Min(16,size); label.fontSizeMax=size; return label;
        }
        public static void Title(Transform parent,string title,float y=.665f,float top=.755f)
        { var f=Frame(parent,"TitleFrame",.285f,y,.715f,top); Label(f.transform,"Title",title,.025f,.08f,.975f,.92f,44); }
        public static void Rule(Transform parent,float y,float left=.08f,float right=.92f)
        {
            var line=Rect(parent,"GoldRule",left,y,right,y+.002f).AddComponent<Image>(); line.color=Gold; line.raycastTarget=false;
            Diamond(parent,"Diamond",.5f,y+.001f,11);
        }
        public static Image Diamond(Transform parent,string name,float x,float y,float size)
        {
            var g=Rect(parent,name,x,y,x,y); var rect=g.GetComponent<RectTransform>(); rect.sizeDelta=new Vector2(size,size); rect.localRotation=Quaternion.Euler(0,0,45);
            var mark=g.AddComponent<Image>(); mark.color=Gold; mark.raycastTarget=false; return mark;
        }
        public static Button Button(Transform parent,string name,string title,float x,float y,float r,float t,Action action,bool transparent=false)
        {
            var g=Rect(parent,name,x,y,r,t); DecorateButton(g,title); var button=g.GetComponent<Button>(); if(action!=null) button.onClick.AddListener(()=>action()); return button;
        }
        public static void DecorateButton(GameObject root,string title)
        {
            var frame=root.AddComponent<BellatorFrame>(); frame.color=BellatorButtonHover.Normal;
            var button=root.AddComponent<Button>(); button.targetGraphic=frame; button.transition=Selectable.Transition.None;
            var shadow=root.AddComponent<Shadow>(); shadow.effectColor=new Color(0,0,0,.65f); shadow.effectDistance=new Vector2(0,-4);
            Label(root.transform,"Label",title,.035f,.08f,.965f,.92f,34);
            var mark=Rect(root.transform,"KeyboardFocus",.14f,.055f,.86f,.075f).AddComponent<Image>(); mark.color=Gold; mark.raycastTarget=false; mark.gameObject.SetActive(false);
            root.AddComponent<BellatorButtonHover>();
        }
    }
}
