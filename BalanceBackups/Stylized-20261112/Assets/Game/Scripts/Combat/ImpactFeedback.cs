using UnityEngine;
using Bellator.Character;

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
        private void OnEnable() { receiver = GetComponent<DamageReceiver>(); receiver.Impacted += Impact; }
        private void Impact(DamageResult result)
        {
            var orbit = Camera.main != null ? Camera.main.GetComponent<ThirdPersonCamera>() : null;
            if (orbit != null && (orbit.IsFollowing(transform) || orbit.IsFollowing(receiver.LastAttacker)) && (result.Damage >= 24 || result.GuardBroken || result.Staggered))
                orbit.Impact(result.GuardBroken || result.Staggered ? 1 : .45f);
        }
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
                Color tint=receiver.LastImpact.Blocked ? Color.cyan : (receiver.LastImpact.GuardBroken || receiver.LastImpact.Staggered ? Color.yellow:new Color(1,.08f,.04f));
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
        private void OnGUI()
        {
            if (Time.timeScale <= 0 || receiver == null || Camera.main == null || Time.time - receiver.LastImpactTime > .12f) return;
            Vector3 p = Camera.main.WorldToScreenPoint(receiver.LastImpactPoint);
            if (p.z <= 0) return;
            Color old = GUI.color; GUI.color = receiver.LastImpact.Blocked ? Color.cyan : new Color(1, .85f, .6f);
            GUI.DrawTexture(new Rect(p.x-7,Screen.height-p.y-1,14,2),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(p.x-1,Screen.height-p.y-7,2,14),Texture2D.whiteTexture); GUI.color = old;
        }
        private void OnDisable() { if (receiver != null) receiver.Impacted -= Impact; Restore(); }
    }
}



