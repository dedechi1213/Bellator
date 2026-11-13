using UnityEngine;
using UnityEngine.UI;

namespace Bellator.Match
{
    /// <summary>Small native silhouettes; no replacement character artwork.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BellatorMenuIcon : MaskableGraphic
    {
        public int kind;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            switch(kind)
            {
                case 0: Poly(vh,new Vector2(.22f,.08f),new Vector2(.84f,.5f),new Vector2(.22f,.92f)); break;
                case 1:
                    Disk(vh,.49f,.53f,.30f); Box(vh,.20f,.20f,.72f,.46f); Box(vh,.62f,.13f,.74f,.56f);
                    for(int i=0;i<9;i++) { float a=.25f+i*.23f; var p=new Vector2(.5f+Mathf.Cos(a)*.39f,.50f+Mathf.Sin(a)*.39f); Disk(vh,p.x,p.y,.065f); }
                    break;
                case 2:
                    Poly(vh,new Vector2(.22f,.86f),new Vector2(.78f,.86f),new Vector2(.66f,.43f),new Vector2(.50f,.33f),new Vector2(.34f,.43f));
                    Box(vh,.45f,.16f,.55f,.42f); Box(vh,.30f,.08f,.70f,.18f);
                    Ring(vh,.23f,.65f,.17f,.115f); Ring(vh,.77f,.65f,.17f,.115f); break;
                case 3:
                    for(int i=0;i<8;i++) { float a=i*Mathf.PI/4; Disk(vh,.5f+Mathf.Cos(a)*.33f,.5f+Mathf.Sin(a)*.33f,.10f); }
                    Ring(vh,.5f,.5f,.36f,.18f); break;
                case 4:
                    Poly(vh,new Vector2(.12f,.20f),new Vector2(.45f,.12f),new Vector2(.45f,.79f),new Vector2(.12f,.88f));
                    Poly(vh,new Vector2(.55f,.12f),new Vector2(.88f,.20f),new Vector2(.88f,.88f),new Vector2(.55f,.79f));
                    Box(vh,.02f,.10f,.08f,.79f); Box(vh,.92f,.10f,.98f,.79f); break;
                default:
                    Box(vh,.10f,.12f,.18f,.88f); Box(vh,.10f,.80f,.53f,.88f); Box(vh,.10f,.12f,.53f,.20f); Box(vh,.38f,.44f,.77f,.56f);
                    Poly(vh,new Vector2(.65f,.25f),new Vector2(.95f,.5f),new Vector2(.65f,.75f)); break;
            }
        }
        void Box(VertexHelper vh,float x,float y,float r,float t) => Poly(vh,new Vector2(x,y),new Vector2(r,y),new Vector2(r,t),new Vector2(x,t));
        void Poly(VertexHelper vh,params Vector2[] points)
        {
            int first=vh.currentVertCount; var r=GetPixelAdjustedRect();
            foreach(var p in points) vh.AddVert(new Vector2(r.xMin+p.x*r.width,r.yMin+p.y*r.height),color,Vector2.zero);
            for(int i=1;i<points.Length-1;i++) vh.AddTriangle(first,first+i,first+i+1);
        }
        void Disk(VertexHelper vh,float x,float y,float radius)
        { var p=new Vector2[32]; for(int i=0;i<32;i++) { float a=i*Mathf.PI/16; p[i]=new Vector2(x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius); } Poly(vh,p); }
        void Ring(VertexHelper vh,float x,float y,float outer,float inner)
        {
            for(int i=0;i<32;i++)
            { float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16; Poly(vh,new Vector2(x+Mathf.Cos(a)*outer,y+Mathf.Sin(a)*outer),new Vector2(x+Mathf.Cos(b)*outer,y+Mathf.Sin(b)*outer),new Vector2(x+Mathf.Cos(b)*inner,y+Mathf.Sin(b)*inner),new Vector2(x+Mathf.Cos(a)*inner,y+Mathf.Sin(a)*inner)); }
        }
    }
}
