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
        Transform pendingPreview;
        Mesh boundsMesh;
        const float PreviewAspect=1f;

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
            previewTexture=new RenderTexture(1024,1024,24,RenderTextureFormat.ARGB32){name="BellatorLiveGladiator"}; previewTexture.Create();
            previewCamera.targetTexture=previewTexture; previewCamera.rect=new Rect(0,0,1,1);
            previewCamera.clearFlags=CameraClearFlags.SolidColor; previewCamera.backgroundColor=Color.clear;
            var viewport=BellatorUI.Rect(artworkRoot.transform,"PreviewViewport",.36f,.195f,.64f,.605f);
            var previewImage=BellatorUI.Rect(viewport.transform,"Live3DPreview",0,0,1,1).AddComponent<RawImage>();
            previewImage.texture=previewTexture; previewImage.raycastTarget=false;
            var fit=previewImage.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio=PreviewAspect;
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
            selectionHint.text=loading?"Salvando seleção...":"Confirme para salvar a escolha e voltar ao menu.";
            Transform active=null;
            if(previews!=null) foreach(Transform child in previews) { bool show=child.name=="Preview_"+c.id; child.gameObject.SetActive(show); if(show) active=child; }
            pendingPreview=active;
            if(active!=null)
            {
                active.GetComponent<ClassPreviewPose>()?.SetSelectionPose();
                // Show and measure one detail level, rather than combining mutually exclusive LODs.
                foreach(var group in active.GetComponentsInChildren<LODGroup>())
                {
                    group.ForceLOD(0);
                    var levels=group.GetLODs();
                    var visible=new System.Collections.Generic.HashSet<Renderer>();
                    if(levels.Length>0) foreach(var renderer in levels[0].renderers) if(renderer!=null) visible.Add(renderer);
                    foreach(var level in levels) foreach(var renderer in level.renderers)
                        if(renderer!=null) renderer.enabled=visible.Contains(renderer);
                }
            }
        }
        // Frame the posed geometry before rendering, rather than animation culling envelopes.
        void LateUpdate()
        {
            if(pendingPreview==null || previewCamera==null) return;
            var active=pendingPreview; pendingPreview=null;
            if(!TryGetPreviewBounds(active,out var bounds)) return;
            previewCamera.orthographic=true; previewCamera.aspect=PreviewAspect;
            previewCamera.orthographicSize=Mathf.Max(.01f,Mathf.Max(bounds.extents.y,bounds.extents.x/PreviewAspect)*1.12f);
            float distance=Mathf.Max(12f,bounds.extents.z+2f);
            previewCamera.transform.SetPositionAndRotation(bounds.center+Vector3.back*distance,Quaternion.identity);
            previewCamera.nearClipPlane=.01f; previewCamera.farClipPlane=distance+bounds.extents.z+2f;
        }
        bool TryGetPreviewBounds(Transform root,out Bounds bounds)
        {
            bounds=default; bool found=false;
            foreach(var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if(!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                Mesh mesh=null;
                var geometryToWorld=renderer.localToWorldMatrix;
                Vector3[] posedVertices=null;
                if(renderer is SkinnedMeshRenderer skin && skin.sharedMesh!=null)
                {
                    mesh=skin.sharedMesh;
                    if(mesh.isReadable && mesh.blendShapeCount==0)
                    {
                        // Evaluate bone matrices in world space so inherited nonuniform scale
                        // is applied exactly once, regardless of BakeMesh scale conventions.
                        posedVertices=mesh.vertices; var bind=mesh.bindposes; var bones=skin.bones;
                        var matrices=new Matrix4x4[bind.Length];
                        for(int i=0;i<matrices.Length;i++) matrices[i]=bones[i].localToWorldMatrix*bind[i];
                        using(var counts=mesh.GetBonesPerVertex())
                        using(var weights=mesh.GetAllBoneWeights())
                        {
                            int offset=0;
                            for(int i=0;i<posedVertices.Length;i++)
                            {
                                var source=posedVertices[i]; var point=Vector3.zero;
                                for(int j=0;j<counts[i];j++)
                                {
                                    var weight=weights[offset++];
                                    point+=matrices[weight.boneIndex].MultiplyPoint3x4(source)*weight.weight;
                                }
                                posedVertices[i]=point;
                            }
                        }
                        geometryToWorld=Matrix4x4.identity;
                    }
                    else
                    {
                        // Unity can bake non-readable imported meshes and active blend shapes.
                        if(boundsMesh==null) boundsMesh=new Mesh {name="SelectionBoundsScratch"};
                        skin.BakeMesh(boundsMesh); mesh=boundsMesh;
                    }
                }
                else if(renderer is MeshRenderer) mesh=renderer.GetComponent<MeshFilter>()?.sharedMesh;
                // Do not include particles or trails in the character framing.
                if(mesh==null || mesh.vertexCount==0) continue;
                if(mesh.isReadable)
                {
                    foreach(var vertex in posedVertices??mesh.vertices)
                    {
                        var point=geometryToWorld.MultiplyPoint3x4(vertex);
                        if(!found) { bounds=new Bounds(point,Vector3.zero); found=true; }
                        else bounds.Encapsulate(point);
                    }
                }
                else
                {
                    // Imported static meshes expose geometry bounds even when CPU data is disabled.
                    var local=mesh.bounds;
                    for(int corner=0;corner<8;corner++)
                    {
                        var point=renderer.transform.TransformPoint(local.center+Vector3.Scale(local.extents,
                            new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1)));
                        if(!found) { bounds=new Bounds(point,Vector3.zero); found=true; }
                        else bounds.Encapsulate(point);
                    }
                }
            }
            return found;
        }
        public void Back()
        {
            if(loading) return; loading=true; SelectionNavigation.Back(); SceneManager.LoadSceneAsync(GameInterface.MainScene,LoadSceneMode.Single);
        }
        void OnDestroy()
        {
            if(boundsMesh!=null) Destroy(boundsMesh);
            if(previewCamera!=null) previewCamera.targetTexture=null;
            if(previewTexture!=null) { previewTexture.Release(); Destroy(previewTexture); }
        }
    }
}
