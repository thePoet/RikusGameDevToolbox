using UnityEngine;
using RikusGameDevToolbox.Discrete3d;
using RikusGameDevToolbox.GeneralUse;

namespace RikusGameDevToolbox.PrimitiveMeshKit
{
    public class PrimitiveMesh 
    {
        public enum Mode { SINGLE_MATERIAL, MULTI_MATERIAL, MULTI_MATERIAL_WITH_BOUNDARIES_BETWEEN };
        public Vector3 offset = Vector3.zero;

        Mode mode;
        VecBasedCollection<Primitive> primitives;
        PrimitiveFacesToMesh facesToMesh;

        // struct?
        class MeshFace
        {
            public Vec position;
            public int faceId;
            public int material;
        }

        public PrimitiveMesh(Mode mode)
        {
            if (mode == Mode.SINGLE_MATERIAL)
            {
                throw new System.Exception("Single Substance Mode not implemented.");
            }
            this.mode = mode;

            facesToMesh = new PrimitiveFacesToMesh();
            primitives = new VecBasedCollection<Primitive>();
        }

        public void Clear()
        {
            primitives.Clear();
        }

        public void AddPrimitive(Primitive primitive)
        {
            primitives.Add(primitive.position, primitive);
        }

        public void RemovePrimitive(Primitive primitive)
        {
            primitives.RemoveAt(primitive.position, primitive);
        }

        public Primitive[] PrmitivesAt(Vec position)
        {
            return primitives.ItemsAt(position);
        }

        public bool HasPrimitive(Primitive primitive)
        {
            return primitives.HasItem(primitive.position, primitive);
        }
        public void SaveTo(Mesh mesh)
        {
            CreateMesh();
            facesToMesh.SaveToMesh(mesh, offset);
        }


        // --------------------- Private functions -------------------


        private void CreateMesh()
        {
            Timer t = new Timer();
            facesToMesh.Clear();

            // Iterate through all primitives and faces and send them to processing:
            foreach (Primitive[] primitivesInSamePos in primitives)
            {
                foreach (Primitive primitive in primitivesInSamePos)
                {
                    for (int nFace = 0; nFace < primitive.NumFaces(); nFace++)
                    {
                        ProcessFace(nFace, primitive, primitivesInSamePos);
                    }
                }
            }

        }

        private void ProcessFace(int nFace, Primitive primitive, Primitive[] allPrimitivesInPos)
        {
            int visibleFaceId = primitive.Face(nFace).id;

            if (primitive.Face(nFace).facingPosition == Vec.zero)
            {
                foreach (Primitive other in allPrimitivesInPos)
                {
                    if (other != primitive && (mode != Mode.MULTI_MATERIAL_WITH_BOUNDARIES_BETWEEN || primitive.material != other.material) )
                    {
                        visibleFaceId = PrimitiveGeometry.FaceNotOverlappedByShape(visibleFaceId, other.shape, other.orientation);
                    }
                }
            }
            else
            {
                Vec pos = primitive.position + primitive.Face(nFace).facingPosition;
                Primitive[] neighbours = primitives.ItemsAt(pos);
                if (neighbours!=null)
                {
                    foreach (Primitive other in neighbours)
                    {
                        if (mode != Mode.MULTI_MATERIAL_WITH_BOUNDARIES_BETWEEN || primitive.material != other.material)
                        {
                            visibleFaceId = PrimitiveGeometry.FaceNotOverlappedByShape(visibleFaceId, other.shape, other.orientation);
                        }
                    }
                }
            }

            if (visibleFaceId != -1)
            {
                CreateFace(primitive, visibleFaceId);
            }

      
        }


        private void CreateFace(Primitive primitive, int faceId)
        {
            facesToMesh.AddFace(faceId, primitive.position, primitive.material);
        }
    }

    
}