using UnityEngine;
using static Bellator.Editor.GladiatorMeshWorkshop;

namespace Bellator.Editor
{
    internal static class GladiatorStylizedArmor
    {
        internal static void Build(GladiatorMeshWorkshop m,int profile,int[] b,float mass)
        {
            int torso=b[1],head=b[2];bool warrior=profile==2,heavy=profile==1||profile==5;
            if(warrior)
            {
                // Sculpted leather muscle cuirass, with a fitted back and gold borders.
                Ring[] rings={new Ring(1.035f,.241f,.166f),new Ring(1.12f,.238f,.176f),new Ring(1.22f,.28f,.193f),new Ring(1.33f,.326f,.207f),new Ring(1.42f,.35f,.211f),new Ring(1.50f,.317f,.162f),new Ring(1.55f,.249f,.125f)};
                m.Loft(2,rings,Vector3.zero,p=>Weight(torso),(p,a)=>
                {
                    float front=Mathf.Pow(Mathf.Max(0,Mathf.Cos(a)),3);
                    float pec=Mathf.Exp(-Mathf.Pow((p.y-1.415f)/.062f,2))*Mathf.Sin(Mathf.Clamp01(Mathf.Abs(p.x)/.34f)*Mathf.PI);
                    p.z+=front*(pec*.035f-.013f*Mathf.Exp(-Mathf.Pow(p.x/.022f,2)));return p;
                },sides:m.detail>10?40:20);
                foreach(int s in new[]{-1,1})
                {
                    m.Tube(4,new[]{new Vector3(s*.012f,1.358f,.215f),new Vector3(s*.075f,1.342f,.222f),new Vector3(s*.16f,1.352f,.212f),new Vector3(s*.24f,1.39f,.185f)},.0065f,torso,6);
                    for(int j=0;j<3;j++)
                    {
                        float y=1.16f+j*.061f;
                        m.Ellipsoid(2,new Vector3(s*.056f,y,.184f+j*.009f),new Vector3(.064f,.036f,.026f),torso,resolution:m.detail);
                    }
                    m.Tube(3,new[]{new Vector3(s*.23f,1.04f,.071f),new Vector3(s*.13f,1.018f,.151f),new Vector3(0,.994f,.181f)},.014f,torso,8);
                    m.Strap(3,new Vector3(s*.205f,1.52f,.136f),new Vector3(s*.28f,1.43f,.178f),.025f,torso);
                }
                m.Tube(4,new[]{new Vector3(0,1.11f,.189f),new Vector3(0,1.23f,.219f),new Vector3(0,1.34f,.225f)},.0045f,torso,5);
                Cape(m,b,.87f,1.57f);
                Crest(m,head);
                // Angular cheek plates and narrow nose guard echo the reference helmet.
                foreach(int s in new[]{-1,1})
                {
                    m.Panel(3,new[]{new Vector3(s*.034f,1.863f,.178f),new Vector3(s*.12f,1.887f,.135f),new Vector3(s*.153f,1.943f,.095f),new Vector3(s*.158f,1.79f,.102f),new Vector3(s*.07f,1.72f,.148f)},.019f,head);
                    m.Panel(3,new[]{new Vector3(s*.018f,1.943f,.175f),new Vector3(s*.13f,1.932f,.133f),new Vector3(s*.14f,1.955f,.15f),new Vector3(s*.021f,1.982f,.175f)},.014f,head);
                    m.Tube(3,new[]{new Vector3(s*.16f,1.96f,.11f),new Vector3(s*.105f,2.04f,.149f),new Vector3(0,2.082f,.159f)},.016f,head,8);
                }
                m.Panel(3,new[]{new Vector3(-.014f,1.87f,.19f),new Vector3(.014f,1.87f,.19f),new Vector3(.019f,1.98f,.178f),new Vector3(-.019f,1.98f,.178f)},.018f,head);
            }
            if(heavy)
            {
                // A dark under-cuirass joins the previously disconnected metal bands.
                m.Loft(2,new[]{new Ring(1.06f,.27f*mass,.184f),new Ring(1.20f,.28f*mass,.198f),new Ring(1.37f,.335f*mass,.211f),new Ring(1.49f,.33f*mass,.185f),new Ring(1.54f,.26f*mass,.14f)},Vector3.zero,p=>Weight(torso));
                for(int i=0;i<5;i++)
                {
                    float y=1.09f+i*.082f;
                    foreach(int s in new[]{-1,1})m.Panel(3,new[]{new Vector3(s*.012f,y,.212f),new Vector3(s*.24f,y+.012f,.176f),new Vector3(s*.26f,y+.07f,.168f),new Vector3(s*.012f,y+.064f,.222f)},.018f,torso);
                }
                if(profile==1)Cape(m,b,1.02f,1.57f);
            }
            if(profile==3||profile==6)
            {
                // A continuous fitted vest underneath the existing bags and quiver.
                m.Loft(2,new[]{new Ring(1.06f,.236f,.17f),new Ring(1.23f,.27f,.183f),new Ring(1.41f,.32f,.20f),new Ring(1.50f,.285f,.145f)},Vector3.zero,p=>Weight(torso));
                foreach(int s in new[]{-1,1})m.Strap(3,new Vector3(s*.19f,1.45f,.162f),new Vector3(s*.21f,1.32f,.185f),.013f,torso);
            }
        }
        static void Crest(GladiatorMeshWorkshop m,int head)
        {
            // A solid swept crest with broad silhouette, rather than disconnected rods.
            int sides=m.detail>10?22:12;
            var outline=new Vector3[(sides+1)*2];
            for(int j=0;j<=sides;j++)
            {
                float a=Mathf.Lerp(-.45f,2.00f,j/(float)sides);
                outline[j]=new Vector3(0,2.06f+Mathf.Cos(a)*.29f,Mathf.Sin(a)*.24f-.05f);
                outline[outline.Length-1-j]=new Vector3(0,2.06f+Mathf.Cos(a)*.15f,Mathf.Sin(a)*.16f-.05f);
            }
            // Panel extrusion operates along local Z; rotate a profile from XY into YZ.
            for(int j=0;j<outline.Length;j++){var p=outline[j];outline[j]=new Vector3(p.z,p.y,0);}
            int first=m.vertices.Count;m.Panel(1,outline,.065f,head);
            m.TransformFrom(first,Quaternion.Euler(0,90,0),Vector3.zero);
            for(int j=0;j<9;j++)
            {
                float a=Mathf.Lerp(-.40f,1.94f,j/8f);
                m.Tube(1,new[]{new Vector3(.035f,2.06f+Mathf.Cos(a)*.17f,-Mathf.Sin(a)*.17f+.05f),new Vector3(.035f,2.06f+Mathf.Cos(a)*.28f,-Mathf.Sin(a)*.235f+.05f)},.005f,head,4);
            }
        }
        static void Cape(GladiatorMeshWorkshop m,int[] b,float width,float top)
        {
            // Broad, doubled cloth panels with volume and skinned lower corners.
            int rows=m.detail>10?12:6,columns=m.detail>10?18:10;
            for(int row=0;row<rows;row++)for(int col=0;col<columns;col++)
            {
                Vector3 Point(int r,int c)
                {
                    float t=r/(float)rows,u=c/(float)columns*2-1;
                    float w=Mathf.Lerp(.28f,width*.5f,t);
                    return new Vector3(u*w,top-t*(top-.47f)+.055f*t*t*Mathf.Cos(u*6),-.145f-.15f*t-.04f*Mathf.Sin(u*5+t)*Mathf.Sin(t*Mathf.PI));
                }
                m.Panel(1,new[]{Point(row,col),Point(row+1,col),Point(row+1,col+1),Point(row,col+1)},.012f,b[1]);
            }
            // Scarf and visible bronze brooch rest above the cuirass collar.
            m.Loft(1,new[]{new Ring(1.49f,.23f,.158f),new Ring(1.54f,.27f,.142f),new Ring(1.60f,.12f,.105f)},Vector3.zero,p=>Weight(b[1]),sides:m.detail);
            m.Ellipsoid(3,new Vector3(-.18f,1.55f,.142f),new Vector3(.038f,.04f,.014f),b[1],resolution:16);
        }
    }
}
