using System;
using UnityEngine;

namespace RikusGameDevToolbox.GeneralUse
{
        /// <summary>
        /// Represents a transformation from 2D to 2D 
        /// </summary>
        public readonly struct Transform2D
        {
            // TODO: would be neater to do with a matrix
            
            public Vector2 Origin { private get; init; }
            public Vector2 A { private get; init; }
            public Vector2 B { private get; init; }

            /// <summary>
            /// Creates a Transform2D based on a function.  
            /// </summary>
            public static Transform2D From(Func<Vector2, Vector2> f)
            {
                return new Transform2D
                {
                    Origin = f(Vector2.zero),
                    A = f(Vector2.right) - f(Vector2.zero),
                    B = f(Vector2.up) - f(Vector2.zero)
                };

            }
            
            public Vector2 TransformPoint(Vector2 point) => Origin + A * point.x + B * point.y;
        }
}