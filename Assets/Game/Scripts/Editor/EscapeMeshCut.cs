using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bellator.Editor
{
    // Subtracts a box from the actual triangles; does not hide a wall or just remove its collider.
    public static class EscapeMeshCut
    {
        struct V
        {
            public Vector3 p,n,cut; public Vector2 uv;
            public static V Lerp(V a,V b,float t) => new V { p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,cut=Vector3.Lerp(a.cut,b.cut,t),uv=Vector2.Lerp(a.uv,b.uv,t) };
        }
        public static Mesh Subtract(Mesh source,Transform renderer,Transform gate,Bounds volume)
        {
            var sourceP=source.vertices; var sourceN=source.normals; var sourceUV=source.uv;
            var converted=new V[sourceP.Length]; var matrix=gate.worldToLocalMatrix*renderer.localToWorldMatrix;
            for(int i=0;i<converted.Length;i++) converted[i]=new V { p=sourceP[i],cut=matrix.MultiplyPoint3x4(sourceP[i]),n=sourceN.Length==sourceP.Length?sourceN[i]:Vector3.up,uv=sourceUV.Length==sourceP.Length?sourceUV[i]:Vector2.zero };
            Vector3[] normals={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
            float[] offsets={-volume.min.x,volume.max.x,-volume.min.y,volume.max.y,-volume.min.z,volume.max.z};
            var vertices=new List<Vector3>(); var vertexNormals=new List<Vector3>(); var uvs=new List<Vector2>();
            var submeshes=new List<int[]>();
            for(int sub=0;sub<source.subMeshCount;sub++)
            {
                var indices=new List<int>(); var triangles=source.GetTriangles(sub);
                for(int i=0;i<triangles.Length;i+=3)
                {
                    var poly=new List<V> { converted[triangles[i]],converted[triangles[i+1]],converted[triangles[i+2]] };
                    var triBounds=new Bounds(poly[0].cut,Vector3.zero); triBounds.Encapsulate(poly[1].cut); triBounds.Encapsulate(poly[2].cut);
                    if(!triBounds.Intersects(volume)) { Emit(poly,vertices,vertexNormals,uvs,indices); continue; }
                    for(int plane=0;plane<6 && poly.Count>=3;plane++)
                    {
                        Emit(Clip(poly,-normals[plane],-offsets[plane]),vertices,vertexNormals,uvs,indices);
                        poly=Clip(poly,normals[plane],offsets[plane]);
                    }
                }
                submeshes.Add(indices.ToArray());
            }
            var mesh=new Mesh { name=source.name+"_PhysicalEscapeCut",indexFormat=vertices.Count>65535?IndexFormat.UInt32:IndexFormat.UInt16 };
            mesh.SetVertices(vertices); mesh.SetNormals(vertexNormals); mesh.SetUVs(0,uvs); mesh.subMeshCount=submeshes.Count;
            for(int i=0;i<submeshes.Count;i++) mesh.SetTriangles(submeshes[i],i);
            mesh.RecalculateBounds(); return mesh;
        }
        static List<V> Clip(List<V> input,Vector3 normal,float offset)
        {
            var output=new List<V>(); if(input.Count==0) return output;
            V previous=input[input.Count-1]; float previousD=Vector3.Dot(normal,previous.cut)+offset;
            foreach(var current in input)
            {
                float d=Vector3.Dot(normal,current.cut)+offset;
                bool inside=d>=0,previousInside=previousD>=0;
                if(inside!=previousInside) output.Add(V.Lerp(previous,current,previousD/(previousD-d)));
                if(inside) output.Add(current);
                previous=current; previousD=d;
            }
            return output;
        }
        static void Emit(List<V> polygon,List<Vector3> p,List<Vector3> n,List<Vector2> uv,List<int> triangles)
        {
            if(polygon.Count<3) return;
            for(int i=1;i<polygon.Count-1;i++)
            {
                if(Vector3.Cross(polygon[i].p-polygon[0].p,polygon[i+1].p-polygon[0].p).sqrMagnitude<.00000001f) continue;
                foreach(var v in new[]{polygon[0],polygon[i],polygon[i+1]}) { triangles.Add(p.Count); p.Add(v.p); n.Add(v.n); uv.Add(v.uv); }
            }
        }
    }
}
