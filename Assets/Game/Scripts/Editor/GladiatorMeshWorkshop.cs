using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bellator.Editor
{
    // Offline mesh authoring. No procedural geometry is generated during gameplay.
    internal sealed class GladiatorMeshWorkshop
    {
        internal readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<BoneWeight> weights = new List<BoneWeight>();
        readonly List<int>[] triangles = new List<int>[6];
        internal readonly int detail;
        internal GladiatorMeshWorkshop(int lod) { detail = lod == 0 ? 20 : 10; for(int i=0;i<6;i++) triangles[i]=new List<int>(); }
        internal static BoneWeight Weight(int bone) => new BoneWeight { boneIndex0=bone, weight0=1 };
        internal static BoneWeight Blend(int a,int b,float t) => new BoneWeight { boneIndex0=a,boneIndex1=b,weight0=1-Mathf.Clamp01(t),weight1=Mathf.Clamp01(t) };
        void Vertex(Vector3 p,Vector2 tex,BoneWeight w) { vertices.Add(p);uv.Add(tex);weights.Add(w); }
        void Tri(int m,int a,int b,int c) { triangles[m].Add(a);triangles[m].Add(b);triangles[m].Add(c); }
        internal struct Ring
        {
            internal float y,x,z,cz;
            internal Ring(float y,float x,float z,float cz=0) { this.y=y;this.x=x;this.z=z;this.cz=cz; }
        }
        // Anatomical cross sections with optional sculpt displacement and smooth bone blending.
        internal void Loft(int mat,Ring[] rings,Vector3 offset,Func<Vector3,BoneWeight> skin,Func<Vector3,float,Vector3> sculpt=null,int sides=0,float start=0,float arc=6.2831853f,bool cap=true)
        {
            int n=sides>0?sides:detail, first=vertices.Count;
            for(int r=0;r<rings.Length;r++) for(int j=0;j<=n;j++)
            {
                float a=start+arc*j/n; var ring=rings[r];
                var p=new Vector3(Mathf.Sin(a)*ring.x,ring.y,Mathf.Cos(a)*ring.z+ring.cz)+offset;
                if(sculpt!=null) p=sculpt(p,a);
                Vertex(p,new Vector2((float)j/n*2,ring.y*2),skin(p));
                if(r>0 && j>0) {int q=first+r*(n+1)+j;Tri(mat,q,q-1,q-n-2);Tri(mat,q,q-n-2,q-n-1);}
            }
            if(cap && arc>6.28f)
            {
                for(int r=0;r<2;r++)
                {
                    int row=r==0?0:rings.Length-1;var ring=rings[row];int center=vertices.Count;
                    var p=offset+new Vector3(0,ring.y,ring.cz);Vertex(p,new Vector2(.5f,.5f),skin(p));
                    for(int j=0;j<n;j++) {int q=first+row*(n+1)+j;if(r==0)Tri(mat,center,q,q+1);else Tri(mat,center,q+1,q);}
                }
            }
        }
        internal void Ellipsoid(int mat,Vector3 p,Vector3 radius,int bone,Vector3 euler=default,int resolution=0)
        {
            int n=resolution>0?resolution:detail, rows=Mathf.Max(6,n/2),first=vertices.Count;var rotation=Quaternion.Euler(euler);
            for(int r=0;r<=rows;r++) for(int j=0;j<=n;j++)
            {
                float v=(float)r/rows,a=2*Mathf.PI*j/n,t=Mathf.PI*v;
                var q=new Vector3(Mathf.Sin(t)*Mathf.Sin(a)*radius.x,Mathf.Cos(t)*radius.y,Mathf.Sin(t)*Mathf.Cos(a)*radius.z);
                Vertex(p+rotation*q,new Vector2((float)j/n,v),Weight(bone));
                if(r>0 && j>0) {int k=first+r*(n+1)+j;Tri(mat,k,k-n-2,k-1);Tri(mat,k,k-n-1,k-n-2);}
            }
        }
        internal void Tube(int mat,Vector3[] path,float radius,int bone,int sides=6)
        {
            int first=vertices.Count;
            for(int i=0;i<path.Length;i++)
            {
                var dir=(path[Math.Min(i+1,path.Length-1)]-path[Math.Max(i-1,0)]).normalized;
                var x=Vector3.Cross(dir,Mathf.Abs(dir.y)>.9f?Vector3.forward:Vector3.up).normalized;var z=Vector3.Cross(x,dir);
                for(int j=0;j<=sides;j++)
                {
                    float a=2*Mathf.PI*j/sides;Vertex(path[i]+(x*Mathf.Sin(a)+z*Mathf.Cos(a))*radius,new Vector2((float)j/sides,i*.3f),Weight(bone));
                    if(i>0 && j>0) {int q=first+i*(sides+1)+j;Tri(mat,q,q-1,q-sides-2);Tri(mat,q,q-sides-2,q-sides-1);}
                }
            }
        }
        // A solid curved panel, with a visible edge rather than a floating flat polygon.
        internal void Panel(int mat,Vector3[] outline,float thickness,int bone)
        {
            int first=vertices.Count;Vector3 center=Vector3.zero;foreach(var p in outline)center+=p;center/=outline.Length;
            for(int face=0;face<2;face++)
            {
                Vector3 dz=Vector3.forward*(face==0?thickness*.5f:-thickness*.5f);
                Vertex(center+dz,new Vector2(.5f,.5f),Weight(bone));
                foreach(var p in outline)Vertex(p+dz,new Vector2(p.x*3,p.y*3),Weight(bone));
                for(int i=0;i<outline.Length;i++)
                {int a=first+face*(outline.Length+1),b=a+1+i,c=a+1+(i+1)%outline.Length;if(face==0)Tri(mat,a,b,c);else Tri(mat,a,c,b);}
            }
            for(int i=0;i<outline.Length;i++)
            {int a=first+1+i,b=first+1+(i+1)%outline.Length,c=b+outline.Length+1,d=a+outline.Length+1;Tri(mat,a,c,b);Tri(mat,a,d,c);}
        }
        internal void Strap(int mat,Vector3 from,Vector3 to,float width,int bone)
        {
            Vector3 side=Vector3.Cross(to-from,Vector3.forward).normalized*width*.5f;
            Panel(mat,new[]{from-side,to-side,to+side,from+side},.012f,bone);
        }
        internal void TransformFrom(int first,Quaternion rotation,Vector3 offset)
        {for(int i=first;i<vertices.Count;i++)vertices[i]=rotation*vertices[i]+offset;}
        internal Mesh Finish(string name,Matrix4x4[] bindposes=null)
        {
            var mesh=new Mesh {name=name,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.subMeshCount=6;
            for(int i=0;i<6;i++)mesh.SetTriangles(triangles[i],i);
            if(bindposes!=null) {mesh.boneWeights=weights.ToArray();mesh.bindposes=bindposes;}
            mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }
    }
}
