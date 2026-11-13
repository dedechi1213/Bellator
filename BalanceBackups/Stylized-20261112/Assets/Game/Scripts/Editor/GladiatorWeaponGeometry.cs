using UnityEngine;
using static Bellator.Editor.GladiatorMeshWorkshop;

namespace Bellator.Editor
{
    internal static class GladiatorWeaponGeometry
    {
        internal static void Build(GladiatorMeshWorkshop m,string name)
        {
            if(name=="ShortSword"||name=="HeavySword")Sword(m,name=="HeavySword");
            else if(name=="Shield")Shield(m);
            else if(name=="Bow")Bow(m);
            else Crossbow(m);
        }
        static void Sword(GladiatorMeshWorkshop m,bool heavy)
        {
            float length=heavy?1.3f:.85f,w=heavy?.085f:.058f;
            // Lenticular, tapered blade with a raised medial ridge and actual cutting edges.
            for(int side=0;side<2;side++)
            {
                float y=side==0?.024f:-.024f;
                for(int s=-1;s<=1;s+=2)
                    m.Panel(0,new[]{new Vector3(0,y,.24f),new Vector3(s*w,.002f,.31f),new Vector3(s*w*.88f,.002f,.23f+length*.74f),new Vector3(0,0,.23f+length),new Vector3(0,y,.23f+length*.70f)},.002f,0);
            }
            m.Ellipsoid(3,new Vector3(0,0,.19f),new Vector3(heavy?.18f:.13f,.045f,.065f),0,new Vector3(0,0,0),16);
            m.Tube(2,new[]{new Vector3(0,0,-.10f),new Vector3(0,0,.14f)},.044f,0,12);
            for(int i=0;i<9;i++)
            {
                float z=-.10f+i*.027f;var points=new Vector3[13];
                for(int j=0;j<13;j++){float a=j*Mathf.PI*2/12;points[j]=new Vector3(Mathf.Sin(a)*.046f,Mathf.Cos(a)*.046f,z+j*.0008f);}m.Tube(2,points,.004f,0,5);
            }
            m.Ellipsoid(3,new Vector3(0,0,-.135f),new Vector3(.064f,.052f,.045f),0,resolution:16);
            foreach(int s in new[]{-1,1})m.Ellipsoid(3,new Vector3(s*.09f,.034f,.18f),Vector3.one*.012f,0,resolution:8);
        }
        static void Shield(GladiatorMeshWorkshop m)
        {
            // Rounded oval wooden shield, domed surface, metal rim and radial metalwork.
            int n=48;var outline=new Vector3[n];
            for(int i=0;i<n;i++){float a=-i*2*Mathf.PI/n;outline[i]=new Vector3(Mathf.Sin(a)*.46f,Mathf.Cos(a)*.53f,0);}
            m.Panel(1,outline,.065f,0);
            var ring=new Vector3[n+1];for(int i=0;i<=n;i++){float a=i*2*Mathf.PI/n;ring[i]=new Vector3(Mathf.Sin(a)*.45f,Mathf.Cos(a)*.52f,.036f);}m.Tube(3,ring,.024f,0,8);
            m.Ellipsoid(3,new Vector3(0,0,.052f),new Vector3(.14f,.14f,.075f),0,resolution:24);
            for(int i=0;i<12;i++)
            {
                float a=i*2*Mathf.PI/12;var p=new Vector3(Mathf.Sin(a)*.408f,Mathf.Cos(a)*.475f,.05f);
                m.Ellipsoid(3,p,new Vector3(.011f,.011f,.008f),0,resolution:8);
                if(i%3==0)
                {
                    var side=new Vector3(Mathf.Cos(a)*.026f,-Mathf.Sin(a)*.026f,0);
                    var inner=new Vector3(Mathf.Sin(a)*.16f,Mathf.Cos(a)*.16f,.044f);
                    m.Panel(3,new[]{inner-side,p-side*.4f,p+side*.4f,inner+side},.007f,0);
                }
            }
            for(int i=-3;i<=3;i++)
            {
                float x=i*.105f,y=.46f*Mathf.Sqrt(1-x*x/(.46f*.46f));
                m.Tube(2,new[]{new Vector3(x,-y,.035f),new Vector3(x,y,.035f)},.002f,0,4);
            }
            m.Tube(2,new[]{new Vector3(0,-.15f,-.07f),new Vector3(0,.15f,-.07f)},.026f,0,10);
        }
        static void Bow(GladiatorMeshWorkshop m)
        {
            foreach(int s in new[]{-1,1})
            {
                var points=new Vector3[17];
                for(int i=0;i<17;i++){float t=(float)i/16;points[i]=new Vector3(0,s*(.10f+.62f*t),.13f*Mathf.Sin(t*Mathf.PI)-.15f*t);}
                m.Tube(1,points,.031f,0,10);
                m.Tube(5,new[]{points[16],new Vector3(0,0,-.18f)},.004f,0,5);
                for(int i=0;i<3;i++)m.Ellipsoid(3,new Vector3(0,s*(.18f+i*.13f),.045f),new Vector3(.035f,.018f,.034f),0,resolution:8);
            }
            m.Tube(2,new[]{new Vector3(0,-.12f,.005f),new Vector3(0,.12f,.005f)},.042f,0,12);
            for(int i=0;i<7;i++)m.Tube(5,new[]{new Vector3(-.036f,-.09f+i*.028f,.025f),new Vector3(.036f,-.08f+i*.028f,.025f)},.003f,0,4);
        }
        static void Crossbow(GladiatorMeshWorkshop m)
        {
            // Sculpted wooden stock with bevelled silhouette, steel rail, nut and trigger.
            int stock=m.vertices.Count;
            m.Loft(1,new[]{new Ring(-.27f,.054f,.069f),new Ring(-.21f,.068f,.075f),new Ring(-.07f,.061f,.060f),new Ring(.30f,.058f,.052f),new Ring(.60f,.050f,.043f)},Vector3.zero,p=>Weight(0),sides:8);
            m.TransformFrom(stock,Quaternion.Euler(90,0,0),Vector3.zero);
            m.Tube(1,new[]{new Vector3(0,-.26f,-.09f),new Vector3(0,-.015f,-.025f)},.048f,0,10);
            foreach(int s in new[]{-1,1})
            {
                var points=new Vector3[13];for(int i=0;i<13;i++){float t=(float)i/12;points[i]=new Vector3(s*t*.54f,0,.63f-.16f*t*t);}
                m.Tube(0,points,.027f,0,10);m.Tube(5,new[]{points[12],new Vector3(0,.033f,.02f)},.004f,0,5);
                m.Tube(0,new[]{new Vector3(s*.035f,.058f,-.10f),new Vector3(s*.035f,.058f,.63f)},.009f,0,6);
            }
            m.Ellipsoid(3,new Vector3(0,.055f,.017f),new Vector3(.055f,.028f,.036f),0,resolution:12);
            m.Tube(0,new[]{new Vector3(.045f,-.043f,.04f),new Vector3(.045f,-.13f,.04f),new Vector3(.045f,-.145f,-.035f),new Vector3(.045f,-.045f,-.08f)},.008f,0,6);
            m.Tube(3,new[]{new Vector3(0,-.005f,.70f),new Vector3(-.07f,-.005f,.76f),new Vector3(-.07f,-.005f,.87f),new Vector3(.07f,-.005f,.87f),new Vector3(.07f,-.005f,.76f),new Vector3(0,-.005f,.70f)},.012f,0,6);
            // Bolt in the rail is decorative; projectile spawning stays with the existing combat code.
            m.Tube(1,new[]{new Vector3(0,.071f,-.02f),new Vector3(0,.071f,.68f)},.005f,0,6);
            m.Panel(0,new[]{new Vector3(-.017f,.071f,.61f),new Vector3(.017f,.071f,.61f),new Vector3(0,.071f,.71f)},.006f,0);
        }
    }
}
