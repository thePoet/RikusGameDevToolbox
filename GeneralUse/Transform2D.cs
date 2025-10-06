using System;
using UnityEngine;

namespace RikusGameDevToolbox.GeneralUse
{
        /// <summary>
        /// Represents a transformation from 2D to 2D 
        /// </summary>
        public struct Transform2D
        {
            private Matrix4x4 _transformation;
            /// <summary>
            /// Creates a Transform2D based on a function that does the same transformation.
            /// </summary>
            public static Transform2D From(Func<Vector2, Vector2> f)
            {
                return new Transform2D
                {
                    _transformation = TransformationMatrix(f)
                };
                
                
                // Creates a transformation matrix from a function that does the same transformation
                Matrix4x4 TransformationMatrix(Func<Vector2, Vector2> f)
                {
                    Vector2 basisX = new Vector2(1, 0);
                    Vector2 basisY = new Vector2(0, 1);
                    Vector2 origin = new Vector2(0, 0);
                    Vector2 transformedX = f(basisX);
                    Vector2 transformedY = f(basisY);
                    Vector2 transformedOrigin = f(origin);

                    Matrix4x4 matrix = Matrix4x4.identity;
                    matrix[0, 0] = (transformedX-transformedOrigin).x;
                    matrix[0, 1] = (transformedY-transformedOrigin).x;
                    matrix[1, 0] = (transformedX-transformedOrigin).y;
                    matrix[1, 1] = (transformedY-transformedOrigin).y;
                    matrix[0, 3] = transformedOrigin.x;
                    matrix[1, 3] = transformedOrigin.y;

                    return matrix;
                }
                
            }
            
            public override string ToString()
            {
                return $"Transform2D Matrix:\n" +
                       $"[{_transformation[0,0]}, {_transformation[0,1]}, {_transformation[0,2]}, {_transformation[0,3]}]\n" +
                       $"[{_transformation[1,0]}, {_transformation[1,1]}, {_transformation[1,2]}, {_transformation[1,3]}]\n" +
                       $"[{_transformation[2,0]}, {_transformation[2,1]}, {_transformation[2,2]}, {_transformation[2,3]}]\n" +
                       $"[{_transformation[3,0]}, {_transformation[3,1]}, {_transformation[3,2]}, {_transformation[3,3]}]";
            }


            public Vector2 TransformPoint(Vector2 point) => _transformation.MultiplyPoint3x4(point);
        }
}