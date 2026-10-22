using UnityEngine;

namespace Bellator.Combat
{
    [RequireComponent(typeof(DamageReceiver))]
    public sealed class ImpactFeedback : MonoBehaviour
    {
        [SerializeField] private Transform dummyReactionPivot;
        private Renderer[] renderers;
        private MaterialPropertyBlock[] original;
        private Color[] colors;
        private MaterialPropertyBlock flash;
        private DamageReceiver receiver;
        private Quaternion rest;
        private bool changed;
        public void Configure(Transform dummyVisual) => dummyReactionPivot=dummyVisual;
        private void Awake() => RefreshVisuals();
        public void RefreshVisuals()
        {
            Restore();
            flash=new MaterialPropertyBlock();
            receiver=GetComponent<DamageReceiver>(); renderers=GetComponentsInChildren<Renderer>();
            original=new MaterialPropertyBlock[renderers.Length]; colors=new Color[renderers.Length];
            for(int i=0;i<renderers.Length;i++) { original[i]=new MaterialPropertyBlock(); renderers[i].GetPropertyBlock(original[i]); colors[i]=renderers[i].sharedMaterial.GetColor("_BaseColor"); }
            if(dummyReactionPivot!=null) rest=dummyReactionPivot.localRotation;
        }
        private void Update()
        {
            float age=Time.time-receiver.LastImpactTime;
            if(age<.22f)
            {
                Color tint=receiver.LastImpact.Blocked ? Color.cyan : (receiver.LastImpact.GuardBroken?Color.yellow:new Color(1,.08f,.04f));
                for(int i=0;i<renderers.Length;i++) if(renderers[i]!=null)
                { renderers[i].GetPropertyBlock(flash); flash.SetColor("_BaseColor",Color.Lerp(tint,colors[i],age/.22f)); renderers[i].SetPropertyBlock(flash); }
                changed=true;
            }
            else if(changed) Restore();
            if(dummyReactionPivot!=null)
            {
                Quaternion goal=rest*Quaternion.Euler(receiver.Health.IsAlive?(age<.25f?-14*(1-age/.25f):0):65,0,0);
                dummyReactionPivot.localRotation=Quaternion.Slerp(dummyReactionPivot.localRotation,goal,1-Mathf.Exp(-18*Time.deltaTime));
            }
        }
        private void Restore()
        { if(renderers!=null) for(int i=0;i<renderers.Length;i++) if(renderers[i]!=null) renderers[i].SetPropertyBlock(original[i]); changed=false; }
        private void OnDisable() => Restore();
    }
}



