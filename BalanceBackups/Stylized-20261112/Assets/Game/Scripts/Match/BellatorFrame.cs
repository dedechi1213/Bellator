using UnityEngine;
using UnityEngine.UI;

namespace Bellator.Match
{
    /// <summary>Resolution independent, clipped gold frame shared by panels and controls.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BellatorFrame : MaskableGraphic
    {
        public float corner=14, border=3;
        static Texture2D grain;
        public override Texture mainTexture
        {
            get
            {
                if(grain!=null) return grain;
                grain=new Texture2D(128,128,TextureFormat.RGB24,false){name="BellatorSubtleGrain",hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Bilinear};
                var pixels=new Color[128*128]; uint seed=271828;
                for(int y=0;y<128;y++) for(int x=0;x<128;x++) { seed=1664525*seed+1013904223; float noise=.76f+(seed%1000)/4200f+.04f*Mathf.Sin(x*.15f+y*.08f); pixels[y*128+x]=new Color(noise,noise,noise); }
                grain.SetPixels(pixels); grain.Apply(false,true); return grain;
            }
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r=GetPixelAdjustedRect();
            Polygon(vh,r,corner,new Color(.34f,.18f,.055f),new Color(.93f,.68f,.29f));
            Polygon(vh,Inset(r,1.2f),corner-1,new Color(.94f,.67f,.29f),new Color(1,.89f,.61f));
            Polygon(vh,Inset(r,border),corner-border,color*new Color(.65f,.65f,.65f,1),color*new Color(1.5f,1.5f,1.5f,1));
            Polygon(vh,Inset(r,border+2),corner-border-2,color,color*new Color(1.18f,1.18f,1.18f,1));
        }
        static Rect Inset(Rect r,float n) => new Rect(r.x+n,r.y+n,Mathf.Max(0,r.width-2*n),Mathf.Max(0,r.height-2*n));
        static void Polygon(VertexHelper vh,Rect r,float cut,Color bottom,Color top)
        {
            float c=Mathf.Clamp(cut,0,Mathf.Min(r.width,r.height)*.3f); int start=vh.currentVertCount;
            Vector2[] p={new Vector2(r.xMin+c,r.yMin),new Vector2(r.xMax-c,r.yMin),new Vector2(r.xMax,r.yMin+c),new Vector2(r.xMax,r.yMax-c),new Vector2(r.xMax-c,r.yMax),new Vector2(r.xMin+c,r.yMax),new Vector2(r.xMin,r.yMax-c),new Vector2(r.xMin,r.yMin+c)};
            vh.AddVert(r.center,Color.Lerp(bottom,top,.5f),r.center/128f);
            foreach(var v in p) vh.AddVert(v,Color.Lerp(bottom,top,Mathf.InverseLerp(r.yMin,r.yMax,v.y)),v/128f);
            for(int i=0;i<8;i++) vh.AddTriangle(start,start+1+i,start+1+(i+1)%8);
        }
    }
}
