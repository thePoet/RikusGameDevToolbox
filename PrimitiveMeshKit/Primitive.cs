using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RikusGameDevToolbox.Discrete3d;



namespace RikusGameDevToolbox.PrimitiveMeshKit
{

    public class Primitive
    {
        public enum Shape { CUBE, PRISM, TETRAHEDRON, NOTCHED_CUBE };
        public Shape shape;
        public GridOrientation orientation;
        public Vec position;
        public int material = 0;

        public int NumFaces()
        {
            return PrimitiveGeometry.GetNumFaces(shape);
        }

        public PrimitiveGeometry.Face Face(int nFace)
        {
            return PrimitiveGeometry.GetFace(nFace, shape, orientation);
        }

        public bool IntersectsWith(Primitive otherPrimitive)
        {
            Debug.LogError("IntersectsWith is not implemented.");
            return false;
        }

        public override bool Equals(System.Object obj)
        {
            if ((obj != null) && this.GetType().Equals(obj.GetType()))
            {
                Primitive other = (Primitive)obj;
                return other.position == position && other.shape == shape &&
                       other.orientation == orientation && other.material == material;
            }

            return false;
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 13;
                hash = (hash * 7) + shape.GetHashCode();
                hash = (hash * 7) + orientation.GetHashCode();
                hash = (hash * 7) + position.GetHashCode();
                hash = (hash * 7) + material.GetHashCode();
                return hash;
            }
        }
    }

}

