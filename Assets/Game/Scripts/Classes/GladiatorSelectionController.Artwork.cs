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
        TextMeshProUGUI selectedName,selectionHint;
        Button[] artworkButtons;
        Button continueButton;
        Transform previews;
        Camera previewCamera;
        RenderTexture previewTexture;
        GameObject artworkRoot;

        void BuildArtworkSelection()
        {
            var selectionCanvas=confirmButton.GetComponentInParent<Canvas>();
            foreach(Transform child in selectionCanvas.transform) child.gameObject.SetActive(false);
            var scaler=selectionCanvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f;
            artworkRoot=BellatorUI.Stage(selectionCanvas.transform,"BellatorCharacterSelection").gameObject;
            BellatorUI.Artwork(artworkRoot,"CharacterSelection"); BellatorUI.Logo(artworkRoot.transform,.76f,.995f);
            BellatorUI.Title(artworkRoot.transform,"SELEÇÃO DE PERSONAGENS",.657f,.745f);
            artworkButtons=new Button[catalog.classes.Length];
            for(int i=0;i<artworkButtons.Length;i++)
            {
                int index=i; float bottom=.568f-i*.116f;
                artworkButtons[i]=BellatorUI.Button(artworkRoot.transform,"CharacterButton_"+i,catalog.classes[i].displayName,.03f,bottom,.255f,bottom+.095f,()=>Select(index));
                BellatorUI.Diamond(artworkButtons[i].transform,"SelectedMarker",.055f,.5f,11).gameObject.SetActive(false);
            }
            selectedName=BellatorUI.Label(artworkRoot.transform,"SelectedName","",.34f,.605f,.66f,.652f,30);
            var nameShadow=selectedName.gameObject.AddComponent<Shadow>(); nameShadow.effectDistance=new Vector2(1,-2); nameShadow.effectColor=new Color(0,0,0,.8f);
            continueButton=BellatorUI.Button(artworkRoot.transform,"ContinueButton","CONFIRMAR",.775f,.05f,.977f,.137f,Confirm);
            BellatorUI.Button(artworkRoot.transform,"SelectionBackButton","VOLTAR",.023f,.05f,.21f,.137f,Back);
            BellatorUI.Button(artworkRoot.transform,"PreviousCharacterButton","‹",.345f,.405f,.375f,.46f,()=>Select((selected+catalog.classes.Length-1)%catalog.classes.Length));
            BellatorUI.Button(artworkRoot.transform,"NextCharacterButton","›",.685f,.405f,.715f,.46f,()=>Select((selected+1)%catalog.classes.Length));
            selectionHint=BellatorUI.Label(artworkRoot.transform,"SelectionHint","",.29f,.025f,.74f,.105f,22,false);
            previews=GameObject.Find("ClassPreviews")?.transform; previewCamera=Camera.main;
            if(previewCamera==null || previews==null) { selectionHint.text="Prévia 3D indisponível."; return; }
            previewTexture=new RenderTexture(768,1024,24,RenderTextureFormat.ARGB32){name="BellatorLiveGladiator"}; previewTexture.Create();
            previewCamera.targetTexture=previewTexture; previewCamera.rect=new Rect(0,0,1,1);
            previewCamera.clearFlags=CameraClearFlags.SolidColor; previewCamera.backgroundColor=Color.clear;
            var viewport=BellatorUI.Rect(artworkRoot.transform,"PreviewViewport",.39f,.195f,.67f,.605f);
            var previewImage=BellatorUI.Rect(viewport.transform,"Live3DPreview",0,0,1,1).AddComponent<RawImage>();
            previewImage.texture=previewTexture; previewImage.raycastTarget=false;
            var fit=previewImage.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio=768f/1024;
            if(EventSystem.current!=null) EventSystem.current.SetSelectedGameObject(artworkButtons[selected].gameObject);
        }
        void RefreshArtworkSelection()
        {
            if(artworkRoot==null) return;
            var c=catalog.classes[selected]; selectedName.text=c.displayName;
            for(int i=0;i<artworkButtons.Length;i++)
            {
                artworkButtons[i].transform.Find("SelectedMarker").gameObject.SetActive(i==selected); artworkButtons[i].interactable=!loading;
            }
            continueButton.interactable=!loading;
            selectionHint.text=loading?"Carregando...":SelectionNavigation.StartAfterConfirmation?"Confirme para entrar na arena.":"Confirme para salvar a escolha e voltar ao menu.";
            Transform active=null;
            if(previews!=null) foreach(Transform child in previews) { bool show=child.name=="Preview_"+c.id; child.gameObject.SetActive(show); if(show) active=child; }
            if(active==null || previewCamera==null) return;
            var renderers=active.GetComponentsInChildren<Renderer>(); if(renderers.Length==0) return;
            var bounds=renderers[0].bounds; foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            previewCamera.orthographic=true; previewCamera.orthographicSize=Mathf.Max(bounds.extents.y*1.12f,bounds.extents.x/(768f/1024)*1.12f);
            previewCamera.transform.position=bounds.center+new Vector3(0,.15f,-12); previewCamera.transform.LookAt(bounds.center);
        }
        public void Back()
        {
            if(loading) return; loading=true; SelectionNavigation.Back(); SceneManager.LoadSceneAsync(GameInterface.MainScene,LoadSceneMode.Single);
        }
        void OnDestroy()
        {
            if(previewCamera!=null) previewCamera.targetTexture=null;
            if(previewTexture!=null) { previewTexture.Release(); Destroy(previewTexture); }
        }
    }
}
