using System;
using Bellator.Match;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bellator.Classes
{
    public sealed partial class GladiatorSelectionController
    {
        TextMeshProUGUI selectedName, description, equipmentLabel, abilityLabel, selectionHint;
        readonly TextMeshProUGUI[] statValues = new TextMeshProUGUI[4];
        readonly Image[] statBars = new Image[4];
        Button[] artworkButtons;
        Button continueButton;
        Transform previews;
        Camera previewCamera;
        RenderTexture previewTexture;
        GameObject artworkRoot;
        Sprite selectedCard, normalCard;

        void BuildArtworkSelection()
        {
            var selectionCanvas = confirmButton.GetComponentInParent<Canvas>();
            // Keep serialized controls available to older tools, but render only the new presentation.
            foreach (Transform child in selectionCanvas.transform) child.gameObject.SetActive(false);
            var scaler = selectionCanvas.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
            artworkRoot = BellatorUI.Rect(selectionCanvas.transform,"BellatorCharacterSelection",0,0,1,1);
            BellatorUI.Artwork(artworkRoot,"CharacterSelection");
            var artwork=Resources.Load<Sprite>("BellatorUI/CharacterSelection");
            selectedCard=Sprite.Create(artwork.texture,new Rect(62,artwork.texture.height-374,407,111),new Vector2(.5f,.5f));
            normalCard=Sprite.Create(artwork.texture,new Rect(62,artwork.texture.height-495,407,111),new Vector2(.5f,.5f));
            artworkButtons = new Button[catalog.classes.Length];
            float[] bottoms = {.605f,.477f,.349f,.220f};
            for (int i=0; i<artworkButtons.Length; i++)
            {
                int index=i; float bottom = bottoms[i];
                artworkButtons[i] = BellatorUI.Button(artworkRoot.transform,"CharacterButton_"+i,"",.038f,bottom,.279f,bottom+.115f,()=>Select(index),true);
                BellatorUI.Label(artworkButtons[i].transform,"ClassName",catalog.classes[i].displayName,.03f,.12f,.97f,.88f,34);
                var border=artworkButtons[i].gameObject.AddComponent<Outline>(); border.effectColor=BellatorUI.Gold; border.effectDistance=new Vector2(2,-2);
            }
            var nameMask=BellatorUI.Rect(artworkRoot.transform,"NameMask",.741f,.742f,.963f,.791f).AddComponent<Image>();
            nameMask.color=new Color(.065f,.06f,.05f);
            selectedName=BellatorUI.Label(nameMask.transform,"SelectedName","",0,0,1,1,36);
            description=BellatorUI.Label(artworkRoot.transform,"Description","",.746f,.608f,.956f,.713f,26);
            float[] statY={.567f,.532f,.497f,.461f};
            for(int i=0;i<4;i++)
            {
                var track=BellatorUI.Rect(artworkRoot.transform,"StatTrack_"+i,.827f,statY[i],.954f,statY[i]+.018f).AddComponent<Image>();
                track.color=new Color(.11f,.105f,.095f);
                statBars[i]=BellatorUI.Rect(track.transform,"Fill",0,0,1,1).AddComponent<Image>();
                statBars[i].color=new Color(.58f,.08f,.055f);
                statValues[i]=BellatorUI.Label(track.transform,"Value","",0,-.22f,1,1.22f,19);
            }
            equipmentLabel=BellatorUI.Label(artworkRoot.transform,"Equipment","",.794f,.333f,.955f,.387f,24);
            abilityLabel=BellatorUI.Label(artworkRoot.transform,"Ability","",.794f,.194f,.955f,.255f,23);
            // Additional ability details sit in the otherwise empty icon and description areas.
            BellatorUI.Button(artworkRoot.transform,"AbilityDetailsButton","",.744f,.188f,.958f,.265f,ShowAbilityDetails,true);
            continueButton=BellatorUI.Button(artworkRoot.transform,"ContinueButton","",.726f,.053f,.975f,.143f,Confirm,true);
            BellatorUI.Button(artworkRoot.transform,"SelectionBackButton","",.026f,.879f,.192f,.938f,Back,true);
            var hintPanel=BellatorUI.Rect(artworkRoot.transform,"SelectionHintPanel",.31f,.035f,.715f,.139f).AddComponent<Image>();
            hintPanel.color=new Color(.13f,.105f,.085f,.9f); hintPanel.raycastTarget=false;
            selectionHint=BellatorUI.Label(hintPanel.transform,"SelectionHint","",.025f,.06f,.975f,.94f,23);
            previews=GameObject.Find("ClassPreviews")?.transform;
            previewCamera=Camera.main;
            previewTexture=new RenderTexture(768,1024,24,RenderTextureFormat.ARGB32) { name="BellatorLiveGladiator" };
            previewTexture.Create();
            previewCamera.targetTexture=previewTexture; previewCamera.rect=new Rect(0,0,1,1);
            previewCamera.clearFlags=CameraClearFlags.SolidColor; previewCamera.backgroundColor=Color.clear;
            var previewImage=BellatorUI.Rect(artworkRoot.transform,"Live3DPreview",.30f,.16f,.72f,.77f).AddComponent<RawImage>();
            previewImage.texture=previewTexture; previewImage.raycastTarget=false;
            previewImage.gameObject.AddComponent<AspectRatioFitter>().aspectMode=AspectRatioFitter.AspectMode.FitInParent;
            previewImage.GetComponent<AspectRatioFitter>().aspectRatio=768f/1024;
            // Reparent into a bounded viewport: AspectRatioFitter should fit the central space only.
            var viewport=BellatorUI.Rect(artworkRoot.transform,"PreviewViewport",.30f,.16f,.72f,.77f);
            previewImage.transform.SetParent(viewport.transform,false);
            if(EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(artworkButtons[selected].gameObject);
        }
        void RefreshArtworkSelection()
        {
            if(artworkRoot==null) return;
            var c=catalog.classes[selected]; selectedName.text=c.displayName; description.text=c.description;
            equipmentLabel.text=string.Join(" + ",Array.ConvertAll(c.equipment,w=>w.displayName))+(c.shield!=null?" + escudo":"");
            abilityLabel.text=c.ability!=null?c.ability.displayName:"—";
            float[] values={c.health,Mathf.Max(c.meleeDamage,c.rangedDamage),c.walkSpeed,c.defense};
            for(int i=0;i<4;i++)
            {
                float max=0;
                foreach(var entry in catalog.classes) max=Mathf.Max(max,i==0?entry.health:i==1?Mathf.Max(entry.meleeDamage,entry.rangedDamage):i==2?entry.walkSpeed:entry.defense);
                statBars[i].rectTransform.anchorMax=new Vector2(max>0?values[i]/max:0,1);
                statValues[i].text=i==0?values[i].ToString("0"):i==1?values[i].ToString("0.00")+"×":i==2?values[i].ToString("0.0")+" m/s":(values[i]*100).ToString("0")+"%";
            }
            for(int i=0;i<artworkButtons.Length;i++)
            {
                artworkButtons[i].GetComponent<Outline>().enabled=i==selected;
                var card=artworkButtons[i].GetComponent<Image>(); card.color=Color.white; card.sprite=i==selected?selectedCard:normalCard;
                artworkButtons[i].interactable=!loading;
            }
            continueButton.interactable=!loading;
            selectionHint.text=loading?"Carregando...":SelectionNavigation.StartAfterConfirmation?"Continuar inicia a partida.\nTrocar de gladiador apenas atualiza a prévia.":"Continuar salva a escolha e volta ao menu.";
            Transform active=null;
            if(previews!=null) foreach(Transform child in previews)
            {
                bool show=child.name=="Preview_"+c.id;
                child.gameObject.SetActive(show); if(show) active=child;
            }
            if(active==null) return;
            var renderers=active.GetComponentsInChildren<Renderer>();
            if(renderers.Length==0) return;
            var bounds=renderers[0].bounds; foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            previewCamera.orthographic=true;
            previewCamera.orthographicSize=Mathf.Max(bounds.extents.y*1.16f,bounds.extents.x/(768f/1024)*1.16f);
            previewCamera.transform.position=bounds.center+new Vector3(0,.15f,-12);
            previewCamera.transform.LookAt(bounds.center);
        }
        void ShowAbilityDetails()
        {
            var ability=catalog.classes[selected].ability;
            if(ability!=null) selectionHint.text=ability.description+"\nRecarga: "+ability.cooldown+" s • Custo: "+ability.staminaCost;
        }
        public void Back()
        {
            if(loading) return;
            loading=true; SelectionNavigation.Back();
            SceneManager.LoadSceneAsync(GameInterface.MainScene,LoadSceneMode.Single);
        }
        void OnDestroy()
        {
            if(previewCamera!=null) previewCamera.targetTexture=null;
            if(previewTexture!=null) { previewTexture.Release(); Destroy(previewTexture); }
            if(selectedCard!=null) Destroy(selectedCard);
            if(normalCard!=null) Destroy(normalCard);
        }
    }
}
