
using System.Runtime.Serialization;
using UnityEngine;
using System;

namespace RikusGameDevToolbox.Discrete3d
{
    /// <summary>
    /// This is same as Vector3Int but serializable.
    /// </summary> 
    public struct Vec : ISerializable, IComparable<Vec>
    {
        public int x;
        public int y;
        public int z;

        static public readonly Vec zero = new Vec(0, 0, 0);
        static public readonly Vec one = new Vec(1, 1, 1);

        public Vec(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vec(Vector3Int position)
        {
            x = position.x;
            y = position.y;
            z = position.z;
        }

        public static bool operator ==(Vec a, Vec b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z;
        }

        public static bool operator !=(Vec a, Vec b)
        {
            return a.x != b.x || a.y != b.y || a.z != b.z;
        }
        public override bool Equals(System.Object obj)
        {
            if (this.GetType().Equals(obj.GetType()))
            {
                return (Vec)obj == this;
            }
            return false;
        }
        public override int GetHashCode()
        {
            unchecked // Allow arithmetic overflow, numbers will just "wrap around"
            {
                int hashcode = 1430287;
                hashcode = hashcode * 7302013 ^ x.GetHashCode();
                hashcode = hashcode * 7302013 ^ y.GetHashCode();
                hashcode = hashcode * 7302013 ^ z.GetHashCode();
                return hashcode;
            }
        }

        public static Vec operator +(Vec a, Vec b)
        {
            return new Vec(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Vec operator -(Vec a, Vec b)
        {
            return new Vec(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static Vec operator -(Vec a)
        {
            return new Vec(-a.x, -a.y, -a.z);
        }


        public static Vec operator *(Vec a, int n)
        {
            return new Vec(a.x * n, a.y * n, a.z * n);
        }

        public static Vec operator *(int n, Vec a)
        {
            return new Vec(a.x * n, a.y * n, a.z * n);
        }

        public static Vec FromVector3(Vector3 vector)
        {
            return new Vec(Mathf.RoundToInt(vector.x), Mathf.RoundToInt(vector.y), Mathf.RoundToInt(vector.z));
        }

        public int CompareTo(Vec other)
        {

            if (this.x < other.x)
            {
                return 1;
            }
            else if (this.x > other.x)
            {
                return -1;
            }
            if (this.y < other.y)
            {
                return 1;
            }
            else if (this.y > other.y)
            {
                return -1;
            }
            if (this.z < other.z)
            {
                return 1;
            }
            else if (this.z > other.z)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        public Vec Rotate(Direction cardinalDirection)
        {
            if (cardinalDirection == Direction.forward)
                return new Vec(x, y, z);

            if (cardinalDirection == Direction.right)
                return new Vec(z, y, -x);

            if (cardinalDirection == Direction.back)
                return new Vec(-x, y, -z);

            if (cardinalDirection == Direction.left)
                return new Vec(-z, y, x);


            throw new System.Exception("Vec cannot be rotated by non cardinal direction");
        }


        public Vec InverseRotate(Direction cardinalDirection)
        {
            if (cardinalDirection == Direction.forward)
                return new Vec(x, y, z);

            if (cardinalDirection == Direction.left)
                return new Vec(-z, y, x);

            if (cardinalDirection == Direction.back)
                return new Vec(-x, y, -z);

            if (cardinalDirection == Direction.right)
                return new Vec(z, y, -x);


            throw new System.Exception("Vec cannot be rotated by non cardinal direction");
        }

        public bool IsInsideBox(Vec cornerA, Vec cornerB)
        {
            return ((x <= cornerA.x && x >= cornerB.x) || (x >= cornerA.x && x <= cornerB.x)) &&
                   ((y <= cornerA.y && y >= cornerB.y) || (y >= cornerA.y && y <= cornerB.y)) &&
                   ((z <= cornerA.z && z >= cornerB.z) || (z >= cornerA.z && z <= cornerB.z));

        }

        public Vector3Int ToVector3Int()
        {
            return new Vector3Int(x, y, z);
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }

        public float Magnitude()
        {
            return Mathf.Sqrt(x * x + y * y + z * z);
        }

        override public string ToString()
        {
            return "Vec :" + x + " " + y + " " + z;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            int[] coordinates = new int[3] { x, y, z };
            info.AddValue("Vec", coordinates, typeof(int[]));
        }

        public Vec WithoutY()
        {
            return new Vec(x, 0, z);
        }


        public Vec(SerializationInfo info, StreamingContext context)
        {
            int[] coordinates = (int[])info.GetValue("Vec", typeof(int[]));
            x = coordinates[0];
            y = coordinates[1];
            z = coordinates[2];
        }
    }
}