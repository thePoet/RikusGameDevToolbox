using System.Collections.Generic;
using UnityEngine;


namespace RikusGameDevToolbox.Discrete3d
{
    /// <summary>
    /// Defines a box-shaped volume
    /// </summary>
    public class Box
    {
        Vec cornerA;
        Vec cornerB;
        Vec cornerMin;
        Vec cornerMax;

        // These return the corners Box was constructed with.
        public Vec CornerA { get { return cornerA; } }
        public Vec CornerB { get { return cornerB; } }

        // These return the corners with smallest/largest coordinates (xMin, yMin, zMin) / (xMax, yMax, zMax) 
        public Vec CornerMin { get { return cornerMin; } }
        public Vec CornerMax { get { return cornerMax; } }
        public IEnumerable<Vec> Corners => CornersIterable();


        public int MinX { get { return cornerMin.x; } }
        public int MinY { get { return cornerMin.y; } }
        public int MinZ { get { return cornerMin.z; } }
        public int MaxX { get { return cornerMax.x; } }
        public int MaxY { get { return cornerMax.y; } }
        public int MaxZ { get { return cornerMax.z; } }

        public Vector3Int Size { get { return BoxSize(); } }


        // Box is defined by coordinates of any two opposing corners
        public Box(Vec cornerA, Vec cornerB)
        {
            this.cornerA = cornerA;
            this.cornerB = cornerB;
            Init();
        }

        // Box is defined by coordinates of any two opposing corners
        public Box(int x1, int y1, int z1, int x2, int y2, int z2)
        {
            cornerA = new Vec(x1, y1, z1);
            cornerB = new Vec(x2, y2, z2);
            Init();
        }

        override public string ToString()
        {
            return "Box :" + cornerMin + " -> " + cornerMax;
        }

        public bool IsInside(Vec position)
        {
            return (position.x >= cornerMin.x &&
                        position.y >= cornerMin.y &&
                        position.z >= cornerMin.z &&
                        position.x <= cornerMax.x &&
                        position.y <= cornerMax.y &&
                        position.z <= cornerMax.z);
        }

        public bool IsOutside(Vec position)
        {
            return !IsInside(position);
        }

        // Clamps the given position so that the result position will be inside the box.
        public Vec Clamp(Vec position)
        {
            return new Vec(Mathf.Clamp(position.x, MinX, MaxX),
                                 Mathf.Clamp(position.y, MinY, MaxY),
                                 Mathf.Clamp(position.z, MinZ, MaxZ));


        }

        public Box MakeCopy()
        {
            return new Box(cornerA, cornerB);
        }

        // Substract given box from this one and return
        // remaining volume as array of boxes.
        public Box[] Substract(Box box)
        {
            List<Box> result = new List<Box>();


            //TODO: Make this method work with all boxes
            if (!box.Size.Equals(Size))
                throw new System.NotImplementedException("Substracting non equal size boxes not implemented.");

            if (box.cornerMax.y != cornerMax.y)
                throw new System.NotImplementedException("Substracting boxes on y axis not implemented.");


            if (MaxX > box.MaxX)
            {
                result.Add(new Box(box.MaxX + 1, MinY, MinZ, MaxX, MaxY, MaxZ));
            }

            if (MinX < box.MinX)
            {
                result.Add(new Box(MinX, MinY, MinZ, box.MinX - 1, MaxY, MaxZ));
            }

            if (MaxZ > box.MaxZ)
            {
                result.Add(new Box(MinX, MinY, box.MaxZ + 1, MaxX, MaxY, MaxZ));
            }

            if (MinZ < box.MinZ)
            {
                result.Add(new Box(MinX, MinY, MinZ, MaxX, MaxY, box.MinZ - 1));
            }

            return result.ToArray();
        }

        public Box Expand(Vec amount)
        {
            return new Box(cornerMin - amount, cornerMax + amount);
        }

        public Box Expand(int amount)
        {
            return Expand(Vec.one * amount);
        }

        public IEnumerable<Vec> AllPositions()
        {
            for (int x = MinX; x <= MaxX; x++)
            {
                for (int y = MinY; y <= MaxY; y++)
                {
                    for (int z = MinZ; z <= MaxZ; z++)
                    {
                        yield return new Vec(x, y, z);
                    }
                }
            }
        }

        void Init()
        {


            cornerMin = new Vec((byte)Mathf.Min(cornerA.x, cornerB.x),
                                        (byte)Mathf.Min(cornerA.y, cornerB.y),
                                        (byte)Mathf.Min(cornerA.z, cornerB.z));

            cornerMax = new Vec((byte)Mathf.Max(cornerA.x, cornerB.x),
                                        (byte)Mathf.Max(cornerA.y, cornerB.y),
                                        (byte)Mathf.Max(cornerA.z, cornerB.z));

        }

        Vector3Int BoxSize()
        {
            return new Vector3Int(1 + CornerMax.x - CornerMin.x,
                                   1 + CornerMax.y - CornerMin.y,
                                   1 + CornerMax.z - CornerMin.z);
        }

        private IEnumerable<Vec> CornersIterable()
        {
            yield return new Vec(MinX,MinY,MinZ);
            yield return new Vec(MaxX,MinY,MinZ);
            yield return new Vec(MinX,MaxY,MinZ);
            yield return new Vec(MaxX,MaxY,MinZ);
            yield return new Vec(MinX,MinY,MaxZ);
            yield return new Vec(MaxX,MinY,MaxZ);
            yield return new Vec(MinX,MaxY,MaxZ);
            yield return new Vec(MaxX,MaxY,MaxZ);
        }

    }

}