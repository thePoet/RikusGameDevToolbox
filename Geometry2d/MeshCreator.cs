using System;
using System.Linq;
using UnityEngine;

namespace RikusGameDevToolbox.Geometry2d
{
    public static class MeshCreator
    {
        /// <summary>
        /// Creates a mesh from a polygon. It can have holes in it. 
        /// </summary>
        public static Mesh FromPolygon(Polygon polygon)
        {
            var tempGameObject = new GameObject();
   
            var collider = tempGameObject.AddComponent<PolygonCollider2D>();
            collider.pathCount = polygon.PathsD.Count;
            for (int i = 0; i < polygon.PathsD.Count; i++)
            {
                collider.SetPath(i, polygon.PathsD[i].Select(p => new Vector2((float)p.x, (float)p.y)).ToArray());
            }
            var mesh = collider.CreateMesh(false, false);

            UnityEngine.Object.Destroy(tempGameObject);
            return mesh;
        }
        
        public static Mesh FromPolygonWithUV(Polygon polygon, Func<Vector2, Vector2> positionToUv)
        {
            Mesh mesh = FromPolygon(polygon);
            Vector2[] uvs = new Vector2[mesh.vertices.Length];
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                uvs[i] = positionToUv(mesh.vertices[i]);
            }
            mesh.uv = uvs;
            return mesh;
        }
        
        public static Mesh FromPolygonWith2UVs(Polygon polygon, Func<Vector2, Vector2> positionToUv, Func<Vector2, Vector2> positionToUv2)
        {
            Mesh mesh = FromPolygon(polygon);
            
            Vector2[] uvs = new Vector2[mesh.vertices.Length];
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                uvs[i] = positionToUv(mesh.vertices[i]);
            }
            mesh.uv = uvs;
            
            Vector2[] uvs2 = new Vector2[mesh.vertices.Length];
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                uvs2[i] = positionToUv2(mesh.vertices[i]);
            }
            mesh.uv2 = uvs2;
            return mesh;
        }

    }
}