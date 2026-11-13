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
        readonly Dictionary<int,Vector3> organicNormals = new Dictionary<int,Vector3>();
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
                    for(int j=0;j<n;j++) {int q=first+row*(n+1)+j;if(r==0)Tri(mat,center,q+1,q);else Tri(mat,center,q,q+1);}
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
        // An indexed isosurface joins the torso, shoulders, arms and legs into one
        // deforming skin. Shared grid edges reuse vertices, including tetrahedron edges.
        internal void OrganicSurface(Func<Vector3,float> field,Func<Vector3,BoneWeight> skin)
        {
            float step=detail>10?.032f:.052f;
            Vector3 min=new Vector3(-.56f,.07f,-.26f),max=new Vector3(.56f,1.77f,.29f);
            int nx=Mathf.CeilToInt((max.x-min.x)/step)+1,ny=Mathf.CeilToInt((max.y-min.y)/step)+1,nz=Mathf.CeilToInt((max.z-min.z)/step)+1;
            var positions=new Vector3[nx*ny*nz];var values=new float[positions.Length];
            for(int z=0;z<nz;z++)for(int y=0;y<ny;y++)for(int x=0;x<nx;x++)
            {int k=(z*ny+y)*nx+x;positions[k]=min+new Vector3(x,y,z)*step;values[k]=field(positions[k]);}
            int[,] tetra={{0,5,1,6},{0,1,2,6},{0,2,3,6},{0,3,7,6},{0,7,4,6},{0,4,5,6}};
            var edgeVertices=new Dictionary<ulong,int>();
            int Edge(int a,int b)
            {
                uint lo=(uint)Math.Min(a,b),hi=(uint)Math.Max(a,b);ulong key=((ulong)lo<<32)|hi;
                if(edgeVertices.TryGetValue(key,out int found))return found;
                float t=values[a]/(values[a]-values[b]);Vector3 p=Vector3.Lerp(positions[a],positions[b],t);
                int index=vertices.Count;Vertex(p,new Vector2(p.x+0.5f,p.y*.5f),skin(p));
                const float e=.001f;
                organicNormals[index]=new Vector3(field(p+Vector3.right*e)-field(p-Vector3.right*e),field(p+Vector3.up*e)-field(p-Vector3.up*e),field(p+Vector3.forward*e)-field(p-Vector3.forward*e)).normalized;
                edgeVertices.Add(key,index);return index;
            }
            void Face(int a,int b,int c,Vector3 outward)
            {
                Vector3 normal=Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]);
                if(normal.sqrMagnitude<1e-14f)return;
                if(Vector3.Dot(normal,outward)>0)Tri(0,a,b,c);else Tri(0,a,c,b);
            }
            var cube=new int[8];var inside=new int[4];var outside=new int[4];
            for(int z=0;z<nz-1;z++)for(int y=0;y<ny-1;y++)for(int x=0;x<nx-1;x++)
            {
                int k=(z*ny+y)*nx+x;
                cube[0]=k;cube[1]=k+1;cube[2]=k+1+nx;cube[3]=k+nx;
                cube[4]=k+nx*ny;cube[5]=cube[4]+1;cube[6]=cube[5]+nx;cube[7]=cube[4]+nx;
                for(int t=0;t<6;t++)
                {
                    int ni=0,no=0;Vector3 ci=Vector3.zero,co=Vector3.zero;
                    for(int j=0;j<4;j++){int v=cube[tetra[t,j]];if(values[v]<0){inside[ni++]=v;ci+=positions[v];}else{outside[no++]=v;co+=positions[v];}}
                    if(ni==0||ni==4)continue;Vector3 outward=co/no-ci/ni;
                    if(ni==1)Face(Edge(inside[0],outside[0]),Edge(inside[0],outside[1]),Edge(inside[0],outside[2]),outward);
                    else if(ni==3)Face(Edge(outside[0],inside[0]),Edge(outside[0],inside[1]),Edge(outside[0],inside[2]),outward);
                    else
                    {
                        int a=Edge(inside[0],outside[0]),b=Edge(inside[0],outside[1]),c=Edge(inside[1],outside[1]),d=Edge(inside[1],outside[0]);
                        Face(a,b,c,outward);Face(a,c,d,outward);
                    }
                }
            }
        }
        internal Mesh Finish(string name,Matrix4x4[] bindposes=null)
        {
            var mesh=new Mesh {name=name,indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.subMeshCount=6;
            for(int i=0;i<6;i++)mesh.SetTriangles(triangles[i],i);
            if(bindposes!=null) {mesh.boneWeights=weights.ToArray();mesh.bindposes=bindposes;}
            mesh.RecalculateNormals();
            // Loft and ellipsoid UV seams must shade as a continuous surface.
            var normals=mesh.normals;var groups=new Dictionary<Vector3Int,List<int>>();
            for(int i=0;i<vertices.Count;i++)
            {
                var p=vertices[i];var key=new Vector3Int(Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000),Mathf.RoundToInt(p.z*100000));
                if(!groups.TryGetValue(key,out var group)){group=new List<int>();groups.Add(key,group);}group.Add(i);
            }
            foreach(var group in groups.Values)if(group.Count>1)
            {
                Vector3 sum=Vector3.zero;foreach(int i in group)sum+=normals[i];
                if(sum.sqrMagnitude>.0001f){sum.Normalize();foreach(int i in group)normals[i]=sum;}
            }
            foreach(var pair in organicNormals)normals[pair.Key]=pair.Value;
            mesh.normals=normals;mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }
    }
}
