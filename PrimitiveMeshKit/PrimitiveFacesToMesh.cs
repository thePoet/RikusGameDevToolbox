using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RikusGameDevToolbox.Discrete3d;

namespace RikusGameDevToolbox.PrimitiveMeshKit
{
    public class PrimitiveFacesToMesh
    {
        static int numMaterials = 4;
        int largestIdx = 0;
        SortedDictionary<int, FaceWrapper> faces;

        struct FaceWrapper
        {
            // public PrimitiveFace face;
            public int faceId;
            public int material;
            public Vec position;
        }

        public PrimitiveFacesToMesh()
        {
            faces = new SortedDictionary<int, FaceWrapper>();
        }

        public void Clear()
        {
            faces.Clear();
        }
        public int AddFace(int faceId, Vec position, int material)
        {
            FaceWrapper fw = new FaceWrapper();
            fw.faceId = faceId;
            fw.position = position;
            fw.material = material;
            largestIdx++;
            faces.Add(largestIdx, fw);
            return largestIdx;
        }

        public void RemoveFace(int idx)
        {
            faces.Remove(idx);
        }

        public void SaveToMesh(Mesh mesh)
        {
            SaveToMesh(mesh, Vector3.zero);
        }
        public void SaveToMesh(Mesh mesh, Vector3 offset)
        {

            int numVertices = 0;
            int[] numTriangles = new int[numMaterials];
            for (int m = 0; m < numMaterials; m++)
            {
                numTriangles[m] = 0;
            }

            foreach (KeyValuePair<int, FaceWrapper> kvp in faces)
            {
                PrimitiveGeometry.Face face = PrimitiveGeometry.GetFaceById(kvp.Value.faceId);


                if (face.numVertices == 4)
                {
                    numVertices += 4;
                    numTriangles[kvp.Value.material] += 2;
                }
                else
                {
                    numVertices += 3;
                    numTriangles[kvp.Value.material]++;
                }
            }

            int[][] triangles = new int[numMaterials][];

            for (int m = 0; m < numMaterials; m++)
            {
                triangles[m] = new int[numTriangles[m] * 3];
            }
            //int[] triangles = new int[numTriangles*3];

            Vector3[] vertices = new Vector3[numVertices];
            Vector3[] normals = new Vector3[numVertices];
            Vector2[] uv = new Vector2[numVertices];

            int v = 0;
            int[] t = new int[numMaterials];

            for (int m = 0; m < numMaterials; m++) // voiko poistaa?
            {
                t[m] = 0;
            }

            foreach (KeyValuePair<int, FaceWrapper> kvp in faces)
            {
                PrimitiveGeometry.Face face = PrimitiveGeometry.GetFaceById(kvp.Value.faceId);

                int material = kvp.Value.material;

                for (int i = 0; i < face.numVertices; i++)
                {
                    vertices[v + i] = face.vertexPositions[i] + kvp.Value.position.ToVector3() + offset;
                    normals[v + i] = face.normal;
                    uv[v + i] = Uv(i, material);
                }

                triangles[material][t[material]] = v;
                triangles[material][t[material] + 1] = v + 1;
                triangles[material][t[material] + 2] = v + 2;

                if (face.vertexIndices.Length == 4)
                {
                    triangles[material][t[material] + 3] = v;
                    triangles[material][t[material] + 4] = v + 2;
                    triangles[material][t[material] + 5] = v + 3;
                    v++;
                    t[material] = t[material] + 3;
                }

                v = v + 3;
                t[material] = t[material] + 3;

            }

            mesh.Clear();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Needed for large meshes.
            mesh.subMeshCount = numMaterials;
            mesh.vertices = vertices;
            mesh.normals = normals;
            //        mesh.triangles = triangles;

            for (int m = 0; m < numMaterials; m++)
            {
                mesh.SetTriangles(triangles[m], m);
            }

            mesh.uv = uv;

            //     mesh.Optimize();
            //       mesh.RecalculateNormals();
        }

        Vector2 Uv(int n, int material)
        {
            Vector2 uvOffset = Vector2.zero;

            if (material == 1)
                uvOffset = new Vector2(0.5f, 0f);
            if (material == 2)
                uvOffset = new Vector2(0.5f, 0.5f);
            if (material == 3)
                uvOffset = new Vector2(0f, 0.5f);

            if (n == 0)
                return new Vector2(0.0f, 0.0f) + uvOffset;
            if (n == 1)
                return new Vector2(0.5f, 0.0f) + uvOffset;
            if (n == 2)
                return new Vector2(0.5f, 0.5f) + uvOffset;
            if (n == 3)
                return new Vector2(0.0f, 0.5f) + uvOffset;

            return Vector2.zero;
        }


    }
}
