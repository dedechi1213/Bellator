using System;
using UnityEngine;
using static Bellator.Editor.GladiatorMeshWorkshop;

namespace Bellator.Editor
{
    internal static class GladiatorOrganicBody
    {
        static float Join(float a,float b,float k)
        {float h=Mathf.Clamp01(.5f+.5f*(b-a)/k);return Mathf.Lerp(b,a,h)-k*h*(1-h);}
        static float Section(Vector3 p,Ring[] rings,float offsetX=0)
        {
            float y=Mathf.Clamp(p.y,rings[0].y,rings[rings.Length-1].y);int i=0;
            while(i<rings.Length-2&&y>rings[i+1].y)i++;
            var a=rings[i];var b=rings[i+1];float t=Mathf.InverseLerp(a.y,b.y,y);
            float rx=Mathf.Lerp(a.x,b.x,t),rz=Mathf.Lerp(a.z,b.z,t),cz=Mathf.Lerp(a.cz,b.cz,t);
            float x=(p.x-offsetX)/rx,z=(p.z-cz)/rz;
            float radial=(Mathf.Sqrt(x*x+z*z)-1)*Mathf.Min(rx,rz);
            return Mathf.Max(radial,Mathf.Max(rings[0].y-p.y,p.y-rings[rings.Length-1].y));
        }
        internal static void Create(GladiatorMeshWorkshop m,int profile,int[] b)
        {
            bool lean=profile==0,heavy=profile==1||profile==5,hunter=profile==3||profile==6;
            float mass=lean?.93f:heavy?1.12f:hunter?.98f:1.04f;
            var torso=new[]{new Ring(.84f,.17f*mass,.108f),new Ring(.96f,.235f*mass,.148f),new Ring(1.10f,.215f*mass,.14f),new Ring(1.23f,.258f*mass,.16f),new Ring(1.36f,.32f*mass,.184f),new Ring(1.46f,.335f*mass,.18f),new Ring(1.53f,.285f*mass,.13f),new Ring(1.60f,.12f,.09f)};
            var neck=new[]{new Ring(1.53f,.11f,.085f),new Ring(1.62f,.085f,.08f),new Ring(1.77f,.09f,.085f)};
            var arm=new[]{new Ring(.80f,.043f,.04f),new Ring(.88f,.062f,.06f),new Ring(.98f,.080f*mass,.083f),new Ring(1.08f,.091f*mass,.094f,.008f),new Ring(1.20f,.073f,.078f),new Ring(1.28f,.096f*mass,.102f),new Ring(1.40f,.112f*mass,.116f),new Ring(1.50f,.116f*mass,.11f),new Ring(1.57f,.065f,.06f),new Ring(1.59f,.015f,.015f)};
            var leg=new[]{new Ring(.10f,.066f,.07f),new Ring(.20f,.070f,.073f),new Ring(.32f,.105f*mass,.105f,-.015f),new Ring(.43f,.092f,.09f),new Ring(.50f,.084f,.09f,.013f),new Ring(.60f,.115f*mass,.12f,.014f),new Ring(.76f,.14f*mass,.15f),new Ring(.91f,.135f*mass,.135f),new Ring(1.00f,.09f,.09f)};
            float Clearance(float y)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.27f,1.08f,y))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.68f,.76f,y));
            float Field(Vector3 p)
            {
                // Muscle planes are part of the continuous surface, not floating spheres.
                Vector3 q=p;
                if(p.z>0&&p.y>1.08f&&p.y<1.54f)
                {
                    float pec=Mathf.Exp(-Mathf.Pow((p.y-1.415f)/.073f,2))*Mathf.Sin(Mathf.Clamp01(Mathf.Abs(p.x)/(.33f*mass))*Mathf.PI);
                    q.z-=pec*(lean?.012f:.033f);
                }
                float d=Join(Section(q,torso),Section(p,neck),.045f);
                d=Join(d,Section(p,leg,p.x<0?-.17f:.17f),.035f);
                // Preserve a sampleable air gap even at LOD1. The original rest
                // pose puts wrists close to the thighs; their skins must not weld.
                float clear=Clearance(p.y);
                d=Mathf.Max(d,Mathf.Abs(p.x)-Mathf.Lerp(.7f,.28f,clear));
                float armField=Mathf.Max(Section(p,arm,p.x<0?-.38f:.38f),Mathf.Lerp(0,.35f,clear)-Mathf.Abs(p.x));
                // Only shoulders merge into the torso. A broad smooth union near
                // the hips would accidentally weld a resting wrist to a thigh.
                float shoulderBlend=Mathf.Lerp(.003f,.055f,Mathf.InverseLerp(1.28f,1.50f,p.y));
                d=Join(d,armField,shoulderBlend);
                return d;
            }
            BoneWeight Skin(Vector3 p)
            {
                int side=p.x<0?0:1;
                float clear=Clearance(p.y);
                float armDistance=Mathf.Max(Section(p,arm,side==0?-.38f:.38f),Mathf.Lerp(0,.35f,clear)-Mathf.Abs(p.x));
                float coreDistance=Mathf.Max(Mathf.Min(Section(p,torso),Section(p,leg,side==0?-.17f:.17f)),Mathf.Abs(p.x)-Mathf.Lerp(.7f,.28f,clear));
                if(Mathf.Abs(p.x)>.275f&&p.y>.79f&&armDistance<coreDistance+.003f)
                {
                    int upper=b[3+side*2],lower=b[4+side*2];
                    if(p.y>1.28f)return Blend(b[1],upper,Mathf.SmoothStep(0,1,Mathf.Clamp01(.5f+.5f*(coreDistance-armDistance)/.055f)));
                    return Blend(upper,lower,Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.32f,1.15f,p.y)));
                }
                if(p.y>1.55f)return Blend(b[1],b[2],Mathf.InverseLerp(1.60f,1.73f,p.y));
                if(p.y<.96f)
                {
                    int thigh=b[7+side*3],shin=b[8+side*3],foot=b[9+side*3];
                    if(p.y>.81f)return Blend(b[0],thigh,Mathf.InverseLerp(.96f,.81f,p.y));
                    if(p.y>.42f)return Blend(thigh,shin,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.62f,.42f,p.y)));
                    return Blend(shin,foot,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.21f,.095f,p.y)));
                }
                return Blend(b[0],b[1],Mathf.InverseLerp(1.02f,1.20f,p.y));
            }
            m.OrganicSurface(Field,Skin);
        }
    }
}
