using UnityEngine;
using UnityEngine.UI;

namespace Bellator.Match
{
    // A separate sky glow keeps the original black wordmark readable without recoloring it.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BellatorSoftLight : MaskableGraphic
    {
        protected override void Awake() { base.Awake(); raycastTarget=false; }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r=GetPixelAdjustedRect(); var center=new Vector2(r.center.x,r.yMin+r.height*.27f);
            vh.AddVert(center,new Color(1,.87f,.65f,.72f),Vector2.zero);
            for(int ring=1;ring<=5;ring++) for(int i=0;i<48;i++)
            {
                float a=i*Mathf.PI*2/48, radius=ring/5f;
                var p=center+new Vector2(Mathf.Cos(a)*r.width*.5f*radius,Mathf.Sin(a)*r.height*.32f*radius);
                vh.AddVert(p,new Color(1,.87f,.65f,.72f*Mathf.Pow(1-radius,1.1f)),Vector2.zero);
            }
            for(int i=0;i<48;i++) vh.AddTriangle(0,1+i,1+(i+1)%48);
            for(int ring=1;ring<5;ring++) for(int i=0;i<48;i++)
            { int a=1+(ring-1)*48+i,b=1+(ring-1)*48+(i+1)%48,c=1+ring*48+i,d=1+ring*48+(i+1)%48; vh.AddTriangle(a,c,b); vh.AddTriangle(b,c,d); }
        }
    }
}
