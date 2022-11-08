using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using RikusGameDevToolbox.Discrete3d;
using static RikusGameDevToolbox.GeneralUse.Generic;

namespace RikusGameDevToolbox.PrimitiveMeshKit
{
    // Geometry and related functions for the four primitive shapes (cube, prism, tetrahedron and notched cube).
    public class PrimitiveGeometry
    {
        const int numFaces = 58 + 24; // Primitives are build from combinations of 58 different faces.
                                      // In addition we need 24 faces that are needed for shapes of 
                                      // combined primitives.

        public class Face
        {
            public int id;
            public int numVertices;
            public int[] vertexIndices;
            public Vector3[] vertexPositions;
            public Vector3 normal;
            public Direction normalDirection;
            public Vec facingPosition;
        }


        static readonly Vector3[] vertexPositions = {
                                                        new Vector3(-0.5f, -0.5f, -0.5f),
                                                        new Vector3(-0.5f,  0.5f, -0.5f),
                                                        new Vector3( 0.5f,  0.5f, -0.5f),
                                                        new Vector3( 0.5f, -0.5f, -0.5f),
                                                        new Vector3(-0.5f, -0.5f,  0.5f),
                                                        new Vector3( 0.5f, -0.5f,  0.5f),
                                                        new Vector3( 0.5f,  0.5f,  0.5f),
                                                        new Vector3(-0.5f,  0.5f,  0.5f),

                                                        // These six vertices are needed for shapes
                                                        // formed by combining the primitives.
                                                        new Vector3(-0.5f,  0.0f,  0.0f),
                                                        new Vector3( 0.5f,  0.0f,  0.0f),
                                                        new Vector3( 0.0f, -0.5f,  0.0f),
                                                        new Vector3( 0.0f,  0.5f,  0.0f),
                                                        new Vector3( 0.0f,  0.0f, -0.5f),
                                                        new Vector3( 0.0f,  0.0f,  0.5f),


                                                };



        static Face[][][] facesForShapes;
        static Face[] faces;
        static bool[][][] verticesPresent;
        static int[][][] faceOverlapLookup;


        // The constructor pre-calculates geometry data for all shapes in all orientations
        static PrimitiveGeometry()
        {
            faces = new Face[numFaces];

            int numShapes = NumEnumerators<Primitive.Shape>();
            int numOrientations = GridOrientation.all.Length;

            facesForShapes = new Face[numShapes][][];
            for (int s = 0; s < numShapes; s++)
            {
                facesForShapes[s] = new Face[numOrientations][];

                for (int o = 0; o < numOrientations; o++)
                {
                    int numFaces = GetNumFaces((Primitive.Shape)s);

                    facesForShapes[s][o] = new Face[numFaces];
                }
            }


            for (int s = 0; s < numShapes; s++)
            {
                Primitive.Shape shape = (Primitive.Shape)s;

                for (int o = 0; o < GridOrientation.all.Length; o++)
                {
                    int numFaces = GetNumFaces((Primitive.Shape)s);
                    GridOrientation orientation = GridOrientation.all[o];

                    for (int f = 0; f < numFaces; f++)
                    {
                        int[] neutralVertices = VertexIndicesNeutralOrientation(shape)[f];
                        int[] rotatedVertices = RotateVertices(neutralVertices, orientation);

                        Face face = GetFaceWithVertices(rotatedVertices);

                        facesForShapes[s][o][f] = face;

                    }
                }
            }




            GetFaceWithVertices(new int[] { 8, 1, 0 });
            GetFaceWithVertices(new int[] { 8, 7, 1 });
            GetFaceWithVertices(new int[] { 8, 4, 7 });
            GetFaceWithVertices(new int[] { 8, 0, 4 });

            GetFaceWithVertices(new int[] { 9, 3, 2 });
            GetFaceWithVertices(new int[] { 9, 5, 3 });
            GetFaceWithVertices(new int[] { 9, 6, 5 });
            GetFaceWithVertices(new int[] { 9, 2, 6 });

            GetFaceWithVertices(new int[] { 10, 0, 3 });
            GetFaceWithVertices(new int[] { 10, 3, 5 });
            GetFaceWithVertices(new int[] { 10, 5, 4 });
            GetFaceWithVertices(new int[] { 10, 4, 0 });

            GetFaceWithVertices(new int[] { 11, 2, 1 });
            GetFaceWithVertices(new int[] { 11, 6, 2 });
            GetFaceWithVertices(new int[] { 11, 7, 6 });
            GetFaceWithVertices(new int[] { 11, 1, 7 });

            GetFaceWithVertices(new int[] { 12, 0, 1 });
            GetFaceWithVertices(new int[] { 12, 1, 2 });
            GetFaceWithVertices(new int[] { 12, 2, 3 });
            GetFaceWithVertices(new int[] { 12, 3, 0 });

            GetFaceWithVertices(new int[] { 13, 4, 5 });
            GetFaceWithVertices(new int[] { 13, 5, 6 });
            GetFaceWithVertices(new int[] { 13, 6, 7 });
            GetFaceWithVertices(new int[] { 13, 7, 4 });

            FillPresentVertices();

            faceOverlapLookup = CreateFaceOverlapLookupTable();
        }




        public static int GetNumFaces(Primitive.Shape shape)
        {
            if (shape == Primitive.Shape.CUBE)
                return 6;
            else if (shape == Primitive.Shape.PRISM)
                return 5;
            else if (shape == Primitive.Shape.TETRAHEDRON)
                return 4;
            else
                return 7; // NOTCHED_CUBE
        }

        public static Face GetFaceById(int faceId)
        {
            return faces[faceId];
        }

        public static Face GetFace(int n, Primitive.Shape shape, GridOrientation orientation)
        {
            return facesForShapes[(int)shape][orientation.id][n];
        }


        /// <summary>
        /// Checks if given face is overlapped by given shape. Returns faceId for part of the face that is not
        /// covered by the shape. Return -1 if the whole face is covered.
        /// </summary>
        public static int FaceNotOverlappedByShape(int faceId, Primitive.Shape shape, GridOrientation shapeOrientation)
        {
            return faceOverlapLookup[faceId][(int)shape][shapeOrientation.id];
        }


        // Given two faces, this function returns a faceId representing the part
        // of the first face that is not covered by the other. Returns -1 if the
        // whole face is covered
        static int DistinctPartOfFace(int faceId, int otherFaceId)
        {
            if (IsTinyFace(otherFaceId)) // Should not be a, since tiny triangles are only used in amalgam shapes
            {
                throw new System.Exception("Tiny triangle as otherFace.");
            }

            if (!OppositeNormals(faceId, otherFaceId))
            {
                return faceId;
            }

            // This may be redundant
            if (IsOnTileBoundary(faceId) != IsOnTileBoundary(otherFaceId))
            {
                return faceId;
            }

            if (IsOnTileBoundary(faceId))
            {
                // Cube face overlaps everything.
                if (IsCubeFace(otherFaceId))
                {
                    return -1;
                }

                // Cube vs regular triangles
                if (IsCubeFace(faceId))
                {
                    List<int> resultVertices = new List<int>();
                    foreach (int v in faces[faceId].vertexIndices)
                    {
                        if (v != RightAngleIndex(otherFaceId))
                        {
                            resultVertices.Add(v);
                        }
                    }

                    return GetFaceWithVertices(resultVertices.ToArray()).id;
                }

                // Tiny triangle vs regular triangle
                if (IsTinyFace(faceId))
                {
                    if (NumSameVertices(faceId, otherFaceId) == 2)
                    {
                        return -1;
                    }
                    else
                    {
                        return faceId;
                    }
                }

                // Regular triangle vs regular triangle
                int right = RightAngleIndex(faceId);
                int rightOther = RightAngleIndex(otherFaceId);

                if (right == rightOther)
                {
                    return -1;
                }

                if (!HasVertex(faceId, rightOther))
                {
                    return faceId;
                }

                int[] vertices = new int[3] { right, rightOther, CenterVertexIdx(faceId) };
                return GetFaceWithVertices(vertices).id;


            }
            else // not on tile boundary
            {
                // Inside faces are only blocked by exactly similar but opposite
                // facing faces due to shapes of the primitives.
                if (HasSameVertices(faceId, otherFaceId))
                {
                    return -1;
                }

                return faceId;
            }


            // ---------- Inside functions ----------

            // Is the face on the outer eddge of the tile?
            bool IsOnTileBoundary(int faceId)
            {
                return faces[faceId].facingPosition == Vec.zero;
            }

            bool OppositeNormals(int faceId1, int faceId2)
            {
                return faces[faceId1].normalDirection == faces[faceId2].normalDirection.Opposite();
            }

            bool HasVertex(int faceId, int vertexId)
            {
                foreach (int v in faces[faceId].vertexIndices)
                {
                    if (v == vertexId)
                    {
                        return true;
                    }
                }
                return false;
            }

            bool HasSameVertices(int faceId1, int faceId2)
            {
                bool sameLength = faces[faceId1].numVertices == faces[faceId2].numVertices;
                return sameLength && NumSameVertices(faceId1, faceId2) == faces[faceId1].numVertices;
            }

            int NumSameVertices(int faceId1, int faceId2)
            {
                int num = 0;
                foreach (int i in faces[faceId1].vertexIndices)
                {
                    foreach (int j in faces[faceId2].vertexIndices)
                    {
                        if (i == j)
                        {
                            num++;
                        }
                    }
                }
                return num;
            }

            bool IsCubeFace(int faceId)
            {
                return faces[faceId].numVertices == 4;
            }

            bool IsTinyFace(int faceId)
            {
                return faceId > 57;
            }

            // Returns the vertexIndex of the right angle
            int RightAngleIndex(int faceId)
            {
                if (IsCubeFace(faceId) || IsTinyFace(faceId) || !IsOnTileBoundary(faceId))
                {
                    throw new Exception("Invalid shape of face for RightAngleIndex.");
                }

                Vector3 a = faces[faceId].vertexPositions[0];
                Vector3 b = faces[faceId].vertexPositions[1];
                Vector3 c = faces[faceId].vertexPositions[2];

                // find the hypotenuse
                if ((a - b).magnitude > 1.1f)
                    return 2;
                if ((a - c).magnitude > 1.1f)
                    return 1;
                if ((b - c).magnitude > 1.1f)
                    return 0;

                throw new Exception("Could not find the right angle.");

            }

            // Return the index of the vertex on a center of the tile face that the  given face is on;
            // Only works for right triangles on cube sides)
            int CenterVertexIdx(int faceId)
            {
                Vector3 coords = faces[faceId].normal * 0.5f;
                for (int v = 58; v < 82; v++)
                {
                    if ((coords - vertexPositions[v]).magnitude < 0.01f)
                    {
                        return v;
                    }
                }
                throw new Exception("Could not find center vertex.");
            }

            // --------------------------------------
        }


        //-----

        static Face GetFaceWithVertices(int[] vertexIndices)
        {
            int[] arrangedVertices = ArrangeVertexIndices(vertexIndices);

            for (int i = 0; i < faces.Length; i++)
            {
                if (faces[i] == null)
                {
                    faces[i] = CreateFace(arrangedVertices);
                    faces[i].id = i;
                    return faces[i];
                }
                if (IsSameFace(arrangedVertices, faces[i].vertexIndices))
                {
                    return faces[i];
                }
            }

            return null;
        }

        static Face CreateFace(int[] vertexIndices)
        {
            Face face = new Face();

            face.vertexIndices = vertexIndices;
            face.numVertices = face.vertexIndices.Length;
            face.vertexPositions = new Vector3[face.numVertices];
            for (int v = 0; v < face.numVertices; v++)
            {
                int vertexIdx = face.vertexIndices[v];
                face.vertexPositions[v] = vertexPositions[vertexIdx];
            }
            try
            {
                face.normal = FaceNormal(face.vertexIndices);
            }
            catch
            {
                Debug.Log("VIRHE " + vertexIndices);
            }
            face.normalDirection = Direction.FromVector3(face.normal);

            if (face.normalDirection.IsCardinalOrVerticalDirection())
            {
                // If face is facing cardinal direction, it's on the edge of the cube facing next position on that direction
                face.facingPosition = face.normalDirection.asVec;
            }
            else
            {
                // If face is not on the cube's edge, it's facing the same position it's in.
                face.facingPosition = Vec.zero;
            }

            return face;

        }



        // Tells whether the two set of indices represent the same face i.e. they
        // have the same values in the same order (clockwise / anti-clockwise)
        static bool IsSameFace(int[] vertexIndicesA, int[] vertexIndicesB)
        {
            if (vertexIndicesA.Length != vertexIndicesB.Length)
                return false;

            int[] aArranged = ArrangeVertexIndices(vertexIndicesA);
            int[] bArranged = ArrangeVertexIndices(vertexIndicesB);

            for (int i = 0; i < aArranged.Length; i++)
            {
                if (aArranged[i] != bArranged[i])
                {
                    return false;
                }
            }
            return true;
        }

        // This helper function rotates values in array so that the smallest will be first.
        // E.g.  4 3 2 1 5  ->    1 5 4 3 2
        static int[] ArrangeVertexIndices(int[] indices)
        {
            // Find the array position for smallest index
            int iSmallest = 0;
            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] < indices[iSmallest])
                {
                    iSmallest = i;
                }
            }

            int[] result = new int[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                if (iSmallest + i < indices.Length)
                {
                    result[i] = indices[iSmallest + i];
                }
                else
                {
                    result[i] = indices[iSmallest + i - indices.Length];
                }
            }

            return result;
        }


        static int[] RotateVertices(int[] vertices, GridOrientation orientation)
        {
            int[] result = new int[vertices.Length];
            Quaternion quaternion = orientation.asQuaternion;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 rotatedPosition = quaternion * vertexPositions[vertices[i]];
                result[i] = IndexForVertexPosition(rotatedPosition);
            }

            return result;


            int IndexForVertexPosition(Vector3 position)
            {
                for (int idx = 0; idx < 8; idx++)
                {
                    if (SameCoordinates(position, vertexPositions[idx]))
                    {
                        return idx;
                    }
                }
                Debug.LogError("Could not find index for vertex coordinates!");
                return 0;
            }

            bool SameCoordinates(Vector3 a, Vector3 b)
            {
                return Mathf.RoundToInt(a.x + 0.5f) == Mathf.RoundToInt(b.x + 0.5f) &&
                       Mathf.RoundToInt(a.y + 0.5f) == Mathf.RoundToInt(b.y + 0.5f) &&
                       Mathf.RoundToInt(a.z + 0.5f) == Mathf.RoundToInt(b.z + 0.5f);
            }
        }

        // Calculate face's normal based on it's vertex indices.
        static Vector3 FaceNormal(int[] vertices)
        {
            Vector3 vec1 = vertexPositions[vertices[1]] - vertexPositions[vertices[0]];
            Vector3 vec2 = vertexPositions[vertices[2]] - vertexPositions[vertices[1]];
            Vector3 normal = Vector3.Cross(vec1, vec2).normalized;
            return normal;
        }

        static void FillPresentVertices()
        {
            verticesPresent = new bool[4][][];
            for (int s = 0; s < 4; s++)
            {
                verticesPresent[s] = new bool[24][];
                for (int o = 0; o < GridOrientation.all.Length; o++)
                {
                    verticesPresent[s][o] = new bool[8];

                    foreach (Face face in facesForShapes[s][o])
                    {
                        foreach (int vertexIdx in face.vertexIndices)
                        {
                            verticesPresent[s][o][vertexIdx] = true;
                        }
                    }
                }
            }
        }
         
        
        static bool[] VerticesSeenFromAdjacentPosition(bool[] verticesHere, Direction adjacentToHere)
        {
            bool[] vertices = new bool[8];

            if (adjacentToHere == Direction.forward)
            {
                vertices[7] = verticesHere[1];
                vertices[6] = verticesHere[2];
                vertices[5] = verticesHere[3];
                vertices[4] = verticesHere[0];
            }
            else if (adjacentToHere == Direction.back)
            {
                vertices[1] = verticesHere[7];
                vertices[2] = verticesHere[6];
                vertices[3] = verticesHere[5];
                vertices[0] = verticesHere[4];
            }
            else if (adjacentToHere == Direction.right)
            {
                vertices[6] = verticesHere[7];
                vertices[5] = verticesHere[4];
                vertices[3] = verticesHere[0];
                vertices[2] = verticesHere[1];
            }
            else if (adjacentToHere == Direction.left)
            {
                vertices[7] = verticesHere[6];
                vertices[4] = verticesHere[5];
                vertices[0] = verticesHere[3];
                vertices[1] = verticesHere[2];
            }
            else if (adjacentToHere == Direction.up)
            {
                vertices[7] = verticesHere[4];
                vertices[6] = verticesHere[5];
                vertices[2] = verticesHere[3];
                vertices[1] = verticesHere[0];
            }
            else if (adjacentToHere == Direction.down)
            {
                vertices[4] = verticesHere[7];
                vertices[5] = verticesHere[6];
                vertices[3] = verticesHere[2];
                vertices[0] = verticesHere[1];
            }
            else
            {
                Debug.LogError("Invalid direction");
            }

            return vertices;

        }

        static int[][][] CreateFaceOverlapLookupTable()
        {
            int numShapes = NumEnumerators<Primitive.Shape>();
            int numOrientations = GridOrientation.all.Length;

            int[][][] table = new int[faces.Length][][];

            foreach (Face face in faces)
            {
                table[face.id] = new int[numShapes][];
                for (int s = 0; s < numShapes; s++)
                {
                    table[face.id][s] = new int[numOrientations];
                    for (int o = 0; o < numOrientations; o++)
                    {
                        table[face.id][s][o] = FaceNotOverlappedByShape(face.id, (Primitive.Shape)s, GridOrientation.all[o]);
                    }
                }
            }

            return table;


            int FaceNotOverlappedByShape(int faceId, Primitive.Shape shape, GridOrientation shapeOrientation)
            {
                foreach (Face otherFace in facesForShapes[(int)shape][shapeOrientation.id])
                {
                    int visibleFaceId = DistinctPartOfFace(faceId, otherFace.id);
                    if (visibleFaceId != faceId)
                    {
                        return visibleFaceId;
                    }
                }

                return faceId;
            }
        }



        // Faces for the basic shapes in neutral orientations,
        // defined by the indices of their vertices.
        static int[][] VertexIndicesNeutralOrientation(Primitive.Shape shape)
        {
            if (shape == Primitive.Shape.CUBE)
            {
                return new int[][] {
                                        new int[] {0, 1, 2, 3 },
                                        new int[] {4, 5, 6, 7 },
                                        new int[] {3, 2, 6, 5 },
                                        new int[] {4, 7, 1, 0 },
                                        new int[] {2, 1, 7, 6 },
                                        new int[] {3, 5, 4, 0 }
                                   };
            }

            if (shape == Primitive.Shape.PRISM)
            {
                return new int[][] {
                                        new int[] {0, 1, 2, 3 },
                                        new int[] {3, 2, 5 },
                                        new int[] {4, 1, 0 },
                                        new int[] {2, 1, 4, 5 },
                                        new int[] {3, 5, 4, 0 }
                                   };
            }

            if (shape == Primitive.Shape.TETRAHEDRON)
            {
                return new int[][] {
                                        new int[] {0, 1, 3 },
                                        new int[] {4, 1, 0 },
                                        new int[] {4, 3, 1 },
                                        new int[] {3, 4, 0 }
                                   };

            }

            if (shape == Primitive.Shape.NOTCHED_CUBE)
            {
                return new int[][] {
                                        new int[] {0, 2, 3 },
                                        new int[] {4, 5, 6, 7 },
                                        new int[] {3, 2, 6, 5 },
                                        new int[] {4, 7, 0 },
                                        new int[] {2, 7, 6 },
                                        new int[] {3, 5, 4, 0 },
                                        new int[] {0, 7, 2 }
                                   };

            }

            Debug.LogError("Unknown shape.");
            return null;
        }



    }
}