using System.Collections.Generic;
using UnityEngine;


namespace RikusGameDevToolbox.GeneralUse
{
    public static class UnityExtensionMethods
    {
     
        
        public static float Get(this Vector3 v, Dimension3d dimension)
        {
            if (dimension == Dimension3d.X) return v.x;
            if (dimension == Dimension3d.Y) return v.y;
            return v.z;
        }
        
        public static Vector2 Vec2XY(this Vector3 v)
        {
            return new Vector2(v.x, v.y);
        }

        public static Vector2 Vec2XZ(this Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }

        public static Vector2 Vec2YZ(this Vector3 v)
        {
            return new Vector2(v.y, v.z);
        }

        public static Vector2 ProjectionOn(this Vector2 v, Vector2 on)
        {
            // TODO: Optimize
            Vector3 result = Vector3.Project(v, on);
            return result;
        }
        
        public static Vector2 RejectionOn(this Vector2 v, Vector2 on)
        {
            // TODO: Optimize
            Vector3 result = Vector3.Project(v, on);
            return v - (Vector2)result;
        }
       
        public static Vector2 With(this Vector2 vector, float? x = null, float? y = null)
        {
            return new Vector2(x ?? vector.x, y ?? vector.y);
        }
        
        public static Vector3 With(this Vector3 vector, float? x = null, float? y = null, float? z = null)
        {
            return new Vector3(x ?? vector.x, y ?? vector.y, z ?? vector.z);
        }

     


        
        public static Vector3 Set(this Vector3 v, Dimension3d dimension, float value)
        {
            if (dimension == Dimension3d.X) return v.With(x: value);
            if (dimension == Dimension3d.Y) return v.With(y: value);
            return v.With(z: value);
        }


        public static Vector3 AddToX(this Vector3 v, float value)
        {
            return new Vector3(v.x + value, v.y, v.z);
        }

        public static Vector3 AddToY(this Vector3 v, float value)
        {
            return new Vector3(v.x, v.y + value, v.z);
        }

        public static Vector3 AddToZ(this Vector3 v, float value)
        {
            return new Vector3(v.x, v.y, v.z + value);
        }

        public static Vector3Int ToVector3Int(this Vector3 v)
        {
            return new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
        }

        public static void CopyLocalValuesFrom(this Transform t, Transform other)
        {
            t.localPosition = other.localPosition;
            t.localRotation = other.localRotation;
            t.localScale = other.localScale;
        }

        public static void ResetLocalValues(this Transform t)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }

        // Find children, grandchildren etc. of a GameObject with a given name
        public static GameObject RecursiveFind(this GameObject go, string name, bool prefix = false)
        {

            if (go.transform.Find(name) != null)
            {
                return go.transform.Find(name).gameObject;
            }
            else
            {
                int numChildren = go.transform.childCount;
                for (int i = 0; i < numChildren; i++)
                {
                    GameObject result = go.transform.GetChild(i).gameObject.RecursiveFind(name);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        public static GameObject RecursiveFindPrefix(this GameObject go, string name)
        {
            GameObject[] result = go.RecursiveFindAll(name, true);
            if (result == null || result.Length == 0)
                return null;

            return result[0];
        }

        // Find all children, grandchildren etc. of a GameObject with a given name or prefix in the name
        public static GameObject[] RecursiveFindAll(this GameObject go, string name, bool prefix = false)
        {
            List<GameObject> result = new List<GameObject>();

            if (go.name.Equals(name) || (prefix && go.name.StartsWith(name)))
                result.Add(go);

            int numChildren = go.transform.childCount;
            for (int i = 0; i < numChildren; i++)
                result.AddRange(go.transform.GetChild(i).gameObject.RecursiveFindAll(name, prefix));


            return result.ToArray();

        }



    }
}

