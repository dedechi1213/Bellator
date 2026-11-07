using System;
using UnityEngine;
using static Bellator.Editor.GladiatorMeshWorkshop;

namespace Bellator.Editor
{
    internal static class GladiatorArtGeometry
    {
        // Material slots: skin, dyed linen, hide, worked metal, hair/charcoal, bone/linen.
        internal static void Character(GladiatorMeshWorkshop m,int profile,int[] b)
        {
            bool lean=profile==0,heavy=profile==1||profile==5,hunter=profile==3||profile==6;
            int torso=b[1],head=b[2];float mass=lean?.86f:heavy?1.10f:hunter?.94f:1;
            Func<Vector3,BoneWeight> core=p=>Blend(b[0],torso,Mathf.InverseLerp(1.02f,1.20f,p.y));
            m.Loft(0,new[]{new Ring(.86f,.16f*mass,.105f),new Ring(.94f,.235f*mass,.14f),new Ring(1.04f,.23f*mass,.145f),new Ring(1.13f,.218f*mass,.135f),new Ring(1.25f,.255f*mass,.16f),new Ring(1.39f,.31f*mass,.175f),new Ring(1.49f,.32f*mass,.153f),new Ring(1.56f,.26f*mass,.125f),new Ring(1.61f,.12f,.09f)},Vector3.zero,core,(p,a)=>
            {
                float front=Mathf.Pow(Mathf.Max(0,Mathf.Cos(a)),5);
                // Pectoral separation, sternum and abdominal planes are sculpted into the surface.
                float pec=Mathf.Exp(-Mathf.Pow((p.y-1.405f)/.082f,2))*Mathf.Sin(Mathf.Clamp01(Mathf.Abs(p.x)/(.31f*mass))*Mathf.PI);
                float abs=Mathf.Sin((p.y-1.10f)*57)*.003f*Mathf.Exp(-Mathf.Pow(p.x/.11f,4));
                p.z+=front*(pec*(lean?.012f:.033f)+abs-.008f*Mathf.Exp(-Mathf.Pow(p.x/.025f,2)));
                if(lean)p.z+=front*.008f*Mathf.Sin(p.y*88)*Mathf.Clamp01((Mathf.Abs(p.x)-.1f)*7);return p;
            });
            m.Loft(0,new[]{new Ring(1.53f,.115f,.086f),new Ring(1.60f,.092f,.079f),new Ring(1.69f,.083f,.082f),new Ring(1.75f,.11f,.087f)},Vector3.zero,p=>Blend(torso,head,Mathf.InverseLerp(1.60f,1.73f,p.y)));
            // Trapezius and clavicles meet the neck and shoulders without spherical joints.
            foreach(int s in new[]{-1,1})
            {
                m.Ellipsoid(0,new Vector3(s*.225f,1.54f,-.015f),new Vector3(.17f,.066f,.105f),torso,new Vector3(0,0,s*-17));
                if(!heavy)m.Tube(0,new[]{new Vector3(s*.07f,1.55f,.079f),new Vector3(s*.17f,1.555f,.117f),new Vector3(s*.28f,1.52f,.109f)},.012f,torso);
            }
            Face(m,profile,head);
            for(int side=0;side<2;side++)
            {
                int s=side==0?-1:1,upper=b[3+side*2],lower=b[4+side*2],thigh=b[7+side*3],shin=b[8+side*3],foot=b[9+side*3];
                float ax=s*.38f,legx=s*.17f;
                Func<Vector3,BoneWeight> arm=p=>p.y>1.44f
                    ?Blend(torso,upper,Mathf.Lerp(1,Mathf.InverseLerp(.275f,.405f,Mathf.Abs(p.x)),Mathf.InverseLerp(1.44f,1.57f,p.y)))
                    :Blend(upper,lower,Mathf.InverseLerp(1.285f,1.18f,p.y));
                m.Loft(0,new[]{new Ring(.875f,.057f,.062f),new Ring(.94f,.065f,.065f),new Ring(1.035f,.080f*mass,.084f*mass,.014f),new Ring(1.15f,.075f*mass,.078f),new Ring(1.23f,.066f,.069f),new Ring(1.30f,.089f*mass,.098f*mass),new Ring(1.40f,.105f*mass,.106f*mass),new Ring(1.49f,.115f*mass,.108f),new Ring(1.56f,.09f,.08f),new Ring(1.58f,.025f,.03f)},new Vector3(ax,0,0),arm,(p,a)=>{p.z+=Mathf.Max(0,Mathf.Cos(a))*.012f*Mathf.Exp(-Mathf.Pow((p.y-1.36f)/.075f,2));return p;});
                // Sculpted palm, separate curled fingers and opposing thumb retain the original grip point.
                m.Ellipsoid(0,new Vector3(ax,.87f,.004f),new Vector3(.067f,.087f,.046f),lower);
                for(int f=0;f<4;f++)
                {
                    float x=ax+(f-1.5f)*.027f,y=.82f+Mathf.Abs(f-1.5f)*.008f;
                    m.Tube(0,new[]{new Vector3(x,.86f,.027f),new Vector3(x,y,.043f),new Vector3(x,y-.019f,.005f),new Vector3(x,y+.002f,-.023f)},.0145f,lower,m.detail>10?8:5);
                    if(m.detail>10)m.Ellipsoid(0,new Vector3(x,.836f,.043f),new Vector3(.015f,.02f,.009f),lower,resolution:8);
                }
                m.Tube(0,new[]{new Vector3(ax-s*.044f,.91f,.008f),new Vector3(ax-s*.083f,.87f,.039f),new Vector3(ax-s*.063f,.835f,.052f)},.021f,lower,8);
                Func<Vector3,BoneWeight> leg=p=>p.y>.50f?Blend(thigh,shin,Mathf.InverseLerp(.59f,.45f,p.y)):Blend(shin,foot,Mathf.InverseLerp(.20f,.10f,p.y));
                m.Loft(0,new[]{new Ring(.10f,.068f,.071f),new Ring(.19f,.070f,.073f),new Ring(.30f,.092f*mass,.102f*mass,-.017f),new Ring(.40f,.095f*mass,.09f),new Ring(.49f,.078f,.083f,.015f),new Ring(.55f,.092f*mass,.09f,.022f),new Ring(.69f,.123f*mass,.132f*mass),new Ring(.82f,.135f*mass,.145f*mass),new Ring(.94f,.132f*mass,.127f),new Ring(.99f,.085f,.08f)},new Vector3(legx,0,0),leg);
                m.Ellipsoid(0,new Vector3(legx,.122f,.078f),new Vector3(.096f,.072f,.18f),foot);
                m.Ellipsoid(2,new Vector3(legx,.065f,.09f),new Vector3(.105f,.025f,.19f),foot);
                for(int f=0;f<5;f++)m.Ellipsoid(0,new Vector3(legx+(f-2)*.034f,.111f,.231f-Mathf.Abs(f-1)*.012f),new Vector3(.020f,.027f,.047f),foot,resolution:8);
                // Caligae: crossing vamp straps, ankle ties and stitched sole.
                for(int k=0;k<3;k++)m.Strap(2,new Vector3(legx-.09f,.14f,.03f+k*.064f),new Vector3(legx+.09f,.14f,.06f+k*.060f),.023f,foot);
                m.Loft(2,new[]{new Ring(.14f,.075f,.078f),new Ring(.174f,.075f,.079f)},new Vector3(legx,0,0),p=>Weight(shin));
                if(!lean && (side==0||heavy))
                {
                    // Shaped greave, open at the back with raised central ridge.
                    m.Loft(3,new[]{new Ring(.18f,.078f,.083f),new Ring(.26f,.091f,.107f),new Ring(.40f,.100f,.100f),new Ring(.47f,.09f,.101f),new Ring(.51f,.062f,.106f)},new Vector3(legx,0,0),p=>Weight(shin),sides:m.detail,start:-1.65f,arc:3.30f,cap:false);
                    m.Tube(3,new[]{new Vector3(legx,.20f,.088f),new Vector3(legx,.37f,.112f),new Vector3(legx,.49f,.112f)},.009f,shin);
                    for(int k=0;k<2;k++)m.Loft(2,new[]{new Ring(.23f+k*.18f,.098f,.108f),new Ring(.253f+k*.18f,.098f,.108f)},new Vector3(legx,0,0),p=>Weight(shin));
                }
                if(lean || hunter || side==1)
                {
                    int count=lean?4:3;
                    for(int k=0;k<count;k++)m.Loft(lean?5:2,new[]{new Ring(.955f+k*.039f,.075f+k*.002f,.080f),new Ring(.98f+k*.039f,.078f+k*.002f,.081f)},new Vector3(ax,0,0),p=>Weight(lower));
                }
                if(heavy || (profile==2 && side==1) || (profile==4 && side==0))
                {
                    m.Ellipsoid(3,new Vector3(ax,1.51f,-.012f),new Vector3(.143f,.115f,.137f),upper);
                    int count=heavy?7:4;
                    for(int k=0;k<count;k++)
                    {
                        float y=1.42f-k*.058f;int joint=y<1.22f?lower:upper;
                        m.Loft(3,new[]{new Ring(y-.044f,.102f,.104f),new Ring(y,.111f,.116f)},new Vector3(ax,0,0),p=>Weight(joint),sides:m.detail,start:-2.15f,arc:4.3f,cap:false);
                        if(m.detail>10)foreach(int r in new[]{-1,1})m.Ellipsoid(3,new Vector3(ax+r*.077f,y-.017f,.084f),Vector3.one*.009f,joint,resolution:6);
                    }
                }
            }
            Outfit(m,profile,b,mass);
        }
        static void Face(GladiatorMeshWorkshop m,int profile,int head)
        {
            bool gaunt=profile==0,heavy=profile==1||profile==5;float width=heavy?1.08f:gaunt?.92f:1;
            m.Loft(0,new[]{new Ring(1.69f,.063f,.065f,.027f),new Ring(1.73f,.115f*width,.098f,.018f),new Ring(1.79f,.139f*width,.125f,.002f),new Ring(1.85f,.149f*width,.142f),new Ring(1.91f,.155f*width,.145f),new Ring(1.98f,.157f*width,.146f,-.005f),new Ring(2.045f,.139f*width,.129f,-.012f),new Ring(2.09f,.09f*width,.087f,-.017f),new Ring(2.112f,.009f,.01f,-.016f)},Vector3.zero,p=>Weight(head),(p,a)=>
            {
                float front=Mathf.Max(0,Mathf.Cos(a)),x=Mathf.Abs(p.x);
                float cheek=Mathf.Exp(-Mathf.Pow((x-.105f)/.033f,2)-Mathf.Pow((p.y-1.845f)/.046f,2));
                float socket=Mathf.Exp(-Mathf.Pow((x-.071f)/.033f,2)-Mathf.Pow((p.y-1.914f)/.022f,2));
                p.z+=front*(cheek*(gaunt?.002f:.018f)-socket*.017f);
                p.z+=front*.013f*Mathf.Exp(-Mathf.Pow((p.y-1.77f)/.024f,2));return p;
            },sides:m.detail>10?32:16);
            foreach(int s in new[]{-1,1})
            {
                m.Ellipsoid(0,new Vector3(s*.158f*width,1.862f,-.002f),new Vector3(.035f,.062f,.033f),head,resolution:12);
                m.Ellipsoid(2,new Vector3(s*.178f*width,1.865f,.021f),new Vector3(.010f,.034f,.012f),head,resolution:8);
                m.Ellipsoid(5,new Vector3(s*.068f,1.913f,.134f),new Vector3(.033f,.012f,.010f),head,resolution:12);
                m.Ellipsoid(4,new Vector3(s*.065f,1.912f,.144f),new Vector3(.011f,.011f,.004f),head,resolution:10);
                m.Tube(0,new[]{new Vector3(s*.033f,1.913f,.143f),new Vector3(s*.065f,1.928f,.145f),new Vector3(s*.097f,1.916f,.130f)},.008f,head);
                m.Tube(4,new[]{new Vector3(s*.030f,1.944f,.145f),new Vector3(s*.070f,1.949f,.148f),new Vector3(s*.109f,1.936f,.129f)},gaunt?.008f:.012f,head);
            }
            // Nose bridge, alae and tip; the face no longer uses a cube for its nose.
            m.Loft(0,new[]{new Ring(1.837f,.023f,.018f,.164f),new Ring(1.853f,.029f,.027f,.171f),new Ring(1.874f,.020f,.030f,.167f),new Ring(1.926f,.014f,.017f,.146f),new Ring(1.949f,.014f,.009f,.137f)},Vector3.zero,p=>Weight(head),sides:12);
            foreach(int s in new[]{-1,1})m.Ellipsoid(4,new Vector3(s*.018f,1.844f,.184f),new Vector3(.009f,.004f,.005f),head,resolution:8);
            m.Tube(2,new[]{new Vector3(-.046f,1.795f,.138f),new Vector3(-.018f,1.8f,.155f),new Vector3(0,1.796f,.16f),new Vector3(.019f,1.8f,.155f),new Vector3(.046f,1.795f,.138f)},.0045f,head);
            m.Tube(0,new[]{new Vector3(-.037f,1.787f,.143f),new Vector3(0,1.784f,.158f),new Vector3(.037f,1.787f,.143f)},.007f,head);
            if(profile==0 || profile==2 || profile==5)
                m.Tube(2,new[]{new Vector3(.103f,1.942f,.136f),new Vector3(.098f,1.906f,.145f),new Vector3(.114f,1.875f,.125f)},.0035f,head);
            if(profile!=1 && profile!=4)
            {
                // Irregular hairline, fitted to the skull instead of a separate round primitive.
                m.Loft(4,new[]{new Ring(1.983f,.158f*width,.151f,-.008f),new Ring(2.042f,.144f*width,.135f,-.01f),new Ring(2.092f,.095f,.096f,-.014f),new Ring(2.128f,.008f,.009f,-.011f)},Vector3.zero,p=>Weight(head),(p,a)=>{p.y+=.009f*Mathf.Sin(a*9+profile);if(Mathf.Cos(a)<0)p.y-=.038f;return p;});
                if(m.detail>10 && profile!=0)for(int i=0;i<13;i++)
                {
                    float a=i*2*Mathf.PI/13;
                    m.Tube(4,new[]{new Vector3(Mathf.Sin(a)*.151f*width,1.991f,Mathf.Cos(a)*.145f-.012f),new Vector3(Mathf.Sin(a)*.126f,2.055f,Mathf.Cos(a)*.114f-.012f),new Vector3(Mathf.Sin(a)*.036f,2.107f,Mathf.Cos(a)*.03f)},.005f,head);
                }
                if(profile==3 || profile==6)
                {
                    m.Ellipsoid(4,new Vector3(0,1.982f,-.154f),new Vector3(.064f,.059f,.064f),head,resolution:12);
                    m.Tube(2,new[]{new Vector3(-.035f,1.95f,-.19f),new Vector3(.035f,1.95f,-.19f)},.009f,head);
                }
            }
            if(profile==1 || profile==2 || profile==5 || profile==6)
            {
                m.Loft(4,new[]{new Ring(1.704f,.055f,.074f,.038f),new Ring(1.727f,.11f*width,.109f,.018f),new Ring(1.773f,.137f*width,.13f,.006f),new Ring(1.803f,.139f*width,.132f)},Vector3.zero,p=>Weight(head),sides:m.detail,start:-1.5f,arc:3f,cap:false);
                if(m.detail>10)for(int i=-4;i<=4;i++)m.Tube(2,new[]{new Vector3(i*.022f,1.775f,.132f-Mathf.Abs(i)*.007f),new Vector3(i*.016f,1.728f,.119f-Mathf.Abs(i)*.003f)},.0025f,head,4);
            }
            if(profile==1 || profile==4 || profile==5) Helmet(m,profile,head,width);
        }
        static void Helmet(GladiatorMeshWorkshop m,int profile,int head,float width)
        {
            m.Loft(3,new[]{new Ring(1.968f,.181f*width,.170f,-.01f),new Ring(2.025f,.166f*width,.156f,-.012f),new Ring(2.09f,.133f*width,.124f,-.014f),new Ring(2.132f,.068f,.069f),new Ring(2.146f,.008f,.01f)},Vector3.zero,p=>Weight(head));
            m.Loft(3,new[]{new Ring(1.958f,.182f*width,.172f,-.01f),new Ring(1.984f,.189f*width,.18f,-.01f)},Vector3.zero,p=>Weight(head));
            foreach(int s in new[]{-1,1})
            {
                m.Panel(3,new[]{new Vector3(s*.105f,1.79f,.11f),new Vector3(s*.161f,1.8f,.066f),new Vector3(s*.183f,1.965f,.072f),new Vector3(s*.125f,1.951f,.146f)},.015f,head);
                for(int j=0;j<3;j++)m.Ellipsoid(4,new Vector3(s*(.135f+j*.007f),1.844f+j*.027f,.113f-j*.002f),new Vector3(.008f,.004f,.004f),head,resolution:6);
                m.Ellipsoid(3,new Vector3(s*.18f,1.953f,.082f),Vector3.one*.017f,head,resolution:8);
            }
            if(profile==1)
            {
                // Murmillo-inspired longitudinal crest, not a generic fantasy horned helmet.
                m.Panel(3,new[]{new Vector3(-.024f,2.10f,-.14f),new Vector3(.024f,2.10f,-.14f),new Vector3(.024f,2.265f,.07f),new Vector3(-.024f,2.265f,.07f)},.055f,head);
                for(int i=0;i<10;i++)
                {
                    float z=-.15f+i*.034f;
                    m.Tube(1,new[]{new Vector3(0,2.13f,z),new Vector3(0,2.26f+Mathf.Sin(i*Mathf.PI/9)*.055f,z+.014f)},.024f,head,6);
                }
            }
        }
        static void Outfit(GladiatorMeshWorkshop m,int profile,int[] b,float mass)
        {
            bool lean=profile==0,heavy=profile==1||profile==5,hunter=profile==3||profile==6;int torso=b[1];
            // Linen skirt is a fluted loft with a torn/scalloped hem, not a cylinder primitive.
            m.Loft(1,new[]{new Ring(.73f,.295f*mass,.197f),new Ring(.84f,.277f*mass,.177f),new Ring(.94f,.247f*mass,.16f),new Ring(1.032f,.239f*mass,.156f)},Vector3.zero,p=>Weight(b[0]),(p,a)=>{float fold=1+.043f*Mathf.Sin(a*14);p.x*=fold;p.z*=fold;if(p.y<.74f)p.y+=.020f*Mathf.Sin(a*(lean?7:12)+profile);return p;},sides:m.detail>10?40:20,cap:false);
            m.Loft(2,new[]{new Ring(.976f,.25f*mass,.170f),new Ring(1.05f,.247f*mass,.168f)},Vector3.zero,p=>Weight(b[0]));
            m.Panel(3,new[]{new Vector3(-.046f,1.001f,.18f),new Vector3(.046f,1.001f,.18f),new Vector3(.046f,1.046f,.18f),new Vector3(-.046f,1.046f,.18f)},.014f,b[0]);
            if(!lean)
            {
                for(int i=0;i<12;i++)
                {
                    float a=i*2*Mathf.PI/12,x=Mathf.Sin(a)*.267f*mass,z=Mathf.Cos(a)*.183f;
                    var rot=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);var center=new Vector3(x,.96f,z);
                    var strip=new[]{new Vector3(-.035f,-.23f,.025f),new Vector3(.035f,-.23f,.025f),new Vector3(.041f,0,0),new Vector3(-.041f,0,0)};
                    for(int j=0;j<strip.Length;j++)strip[j]=center+rot*strip[j];m.Panel(2,strip,.012f,b[0]);
                    if(m.detail>10)m.Ellipsoid(3,center+rot*new Vector3(0,-.19f,.038f),Vector3.one*.01f,b[0],resolution:6);
                }
            }
            if(heavy)
            {
                for(int i=0;i<5;i++)
                {
                    float y=1.09f+i*.078f,w=(i<2?.25f:.30f)*mass;
                    m.Loft(3,new[]{new Ring(y,w,.168f),new Ring(y+.065f,w+.012f,.183f)},Vector3.zero,p=>Weight(torso),sides:m.detail,start:-2.45f,arc:4.90f,cap:false);
                    foreach(int s in new[]{-1,1})m.Ellipsoid(3,new Vector3(s*w*.70f,y+.032f,.144f),new Vector3(.014f,.014f,.008f),torso,resolution:8);
                }
                foreach(int s in new[]{-1,1})m.Strap(2,new Vector3(s*.15f,1.12f,.182f),new Vector3(s*.18f,1.55f,.142f),.035f,torso);
            }
            else if(hunter)
            {
                // Two shaped leather vest fronts leave a V opening over the sternum.
                foreach(int s in new[]{-1,1})
                {
                    m.Panel(2,new[]{new Vector3(s*.04f,1.08f,.145f),new Vector3(s*.23f,1.08f,.111f),new Vector3(s*.30f,1.45f,.118f),new Vector3(s*.20f,1.55f,.11f),new Vector3(s*.075f,1.32f,.173f)},.022f,torso);
                    m.Strap(5,new Vector3(s*.042f,1.09f,.163f),new Vector3(s*.076f,1.32f,.187f),.009f,torso);
                }
                ChestStrap(m,new Vector3(-.20f,1.08f,0),new Vector3(.22f,1.55f,0),.056f,torso,mass);
                // Rear leather quiver and actual visible bolts/arrows.
                m.Loft(2,new[]{new Ring(1.13f,.077f,.075f),new Ring(1.20f,.088f,.080f),new Ring(1.63f,.098f,.085f)},new Vector3(.16f,0,-.235f),p=>Weight(torso));
                m.Loft(3,new[]{new Ring(1.60f,.104f,.09f),new Ring(1.635f,.104f,.09f)},new Vector3(.16f,0,-.235f),p=>Weight(torso));
                for(int i=0;i<5;i++)
                {
                    float x=.11f+(i%3)*.045f,z=-.22f-(i/3)*.047f,top=1.82f+(i%2)*.04f;
                    m.Tube(2,new[]{new Vector3(x,1.36f,z),new Vector3(x,top,z)},.006f,torso,5);
                    m.Panel(5,new[]{new Vector3(x-.022f,top-.11f,z),new Vector3(x,top-.14f,z),new Vector3(x+.022f,top-.11f,z),new Vector3(x,top,z)},.004f,torso);
                }
                for(int i=0;i<2;i++)
                {
                    var p=new Vector3(-.24f+i*.40f,.99f,.09f);
                    m.Ellipsoid(2,p,new Vector3(.078f,.103f,.062f),b[0]);
                    m.Panel(2,new[]{p+new Vector3(-.062f,-.015f,.045f),p+new Vector3(0,-.040f,.069f),p+new Vector3(.062f,-.015f,.045f),p+new Vector3(.062f,.060f,.045f),p+new Vector3(-.062f,.060f,.045f)},.01f,b[0]);
                    m.Ellipsoid(3,p+new Vector3(0,-.015f,.075f),Vector3.one*.013f,b[0],resolution:8);
                }
                // Short green shoulder mantle, fitted over the back, clear of the shooting hands.
                m.Loft(1,new[]{new Ring(1.21f,.24f,.19f,-.014f),new Ring(1.44f,.32f,.19f),new Ring(1.56f,.27f,.132f),new Ring(1.61f,.11f,.09f)},Vector3.zero,p=>Weight(torso),(p,a)=>{p.z-=.012f*Mathf.Sin(a*11);return p;},sides:m.detail,start:1.45f,arc:3.4f,cap:false);
            }
            else
            {
                ChestStrap(m,new Vector3(-.22f*mass,1.08f,0),new Vector3(.23f,1.54f,0),lean?.043f:.067f,torso,mass);
                ChestStrap(m,new Vector3(-.22f*mass,1.08f,0),new Vector3(.23f,1.54f,0),lean?.043f:.067f,torso,mass,true);
                if(!lean)
                {
                    m.Loft(2,new[]{new Ring(1.26f,.275f,.171f),new Ring(1.32f,.29f,.18f)},Vector3.zero,p=>Weight(torso));
                    m.Ellipsoid(3,new Vector3(.10f,1.40f,.18f),new Vector3(.04f,.046f,.012f),torso,resolution:12);
                }
                else
                {
                    m.Panel(5,new[]{new Vector3(-.11f,.78f,.188f),new Vector3(-.025f,.77f,.207f),new Vector3(-.01f,.90f,.177f),new Vector3(-.10f,.91f,.173f)},.004f,b[0]);
                    for(int i=0;i<4;i++)m.Tube(2,new[]{new Vector3(-.114f,.80f+i*.024f,.194f),new Vector3(-.091f,.81f+i*.024f,.199f)},.0025f,b[0],4);
                }
            }
        }
        static void ChestStrap(GladiatorMeshWorkshop m,Vector3 from,Vector3 to,float width,int bone,float mass,bool back=false)
        {
            Vector3 previous=Vector3.zero;
            for(int i=0;i<=12;i++)
            {
                var p=Vector3.Lerp(from,to,i/12f);float t=Mathf.InverseLerp(1.08f,1.40f,p.y);
                float rx=Mathf.Lerp(.23f,.32f,t)*mass,rz=Mathf.Lerp(.145f,.18f,t);
                p.z=(rz*Mathf.Sqrt(Mathf.Max(.08f,1-p.x*p.x/(rx*rx)))+.032f)*(back?-1:1);
                if(i>0)m.Strap(2,previous,p,width,bone);previous=p;
            }
        }
    }
}
