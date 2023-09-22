using System.Collections;
using System.Collections.Generic;
using RikusGameDevToolbox.GeneralUse;
using Unity.VisualScripting.ReorderableList;
using UnityEngine;

namespace RikusGameDevToolbox.TextureEditing
{

   
   public static class BrushStroke
   {
      public enum Effect
      {
         TEST,
         ERASE
      };
      
      
      public delegate void BeforePixelDraw(int x, int y, Color oldColor, Color newColor);
      
      public struct Point
      {
         public Vector2Int position;
         public float width;
         
         public Point(int x, int y, float width)
         {
            this.position = new Vector2Int(x, y);
            this.width = width;
         }
         
         public Point(Vector2Int position, float width)
         {
            this.position = position;
            this.width = width;
         }
         
         public int HalfWidth => (int)System.Math.Ceiling( width / 2f );
      }

      /// <summary>
      /// Draws a line with rounded caps in the ends.
      /// NOTE: Does not Apply the texture.
      /// </summary>
      /// <param name="targetTexture">The texture we draw on.</param>
      /// <param name="lineBegin"></param>
      /// <param name="lineEnd"></param>
      /// <param name="continuation">If true, the line is continuation from previous line and the cap in the
      /// beginning is not drawn.</param>
      public static void Draw(Texture2D targetTexture, Texture2D brushTexture,
                              Point lineBegin, Point lineEnd, 
                              Effect effect, bool continuation = false, 
                              float strenght = 1f, BeforePixelDraw beforeDraw = null)
      {
         Point[] strokePoints = { lineBegin, lineEnd };
         
         RectInt textureBounds = new RectInt(0, 0, targetTexture.width, targetTexture.height);
         RectInt strokeBounds = StrokeBounds(strokePoints);
         strokeBounds.ClampToBounds(textureBounds);
         

         int halfStrokeWidth = lineEnd.HalfWidth;


         float lenghtPixels = (lineEnd.position - lineBegin.position).magnitude;

         for (int x = strokeBounds.xMin; x <= strokeBounds.xMax; x++)
         {
            for (int y = strokeBounds.yMin; y <= strokeBounds.yMax; y++)
            {
               Vector2 point = new Vector2(x, y);
               float distanceFrom = DistanceFromLine(point, lineBegin.position, lineEnd.position);
               float distanceAlong = DistanceAlongLine(point, lineBegin.position, lineEnd.position);
               float distanceFromEnd = (lineEnd.position - point).magnitude;
               float distanceFromStart = (lineBegin.position - point).magnitude;

               bool onStrokeBody = Mathf.Abs(distanceFrom) < halfStrokeWidth && 
                                   distanceAlong >= 0f &&
                                   distanceAlong <= lenghtPixels;

               bool onBeginCap = distanceFromStart <= halfStrokeWidth;
               bool onEndCap = distanceFromEnd <= halfStrokeWidth;

               if (continuation && onEndCap) continue;
               
               if ( onBeginCap || onStrokeBody || onEndCap )
               {
                  var oldColor = targetTexture.GetPixel(x, y);
                  (float u, float v) = BrushUV(distanceAlong, distanceFrom, lineEnd.width, 
                     brushTexture.width, brushTexture.height);
                  Color brushColor = brushTexture.GetPixelBilinear(u, v);
                  
                  Color newColor = oldColor;
                  if (effect == Effect.TEST)
                  {
                     newColor = oldColor - brushColor * strenght;
                  }

                  if (effect == Effect.ERASE)
                  {
                     newColor.a -= brushColor.r * strenght;
                     if (newColor.a < 0) newColor.a = 0f;
                  }
                  
                  beforeDraw?.Invoke(x, y, oldColor, newColor);
                  targetTexture.SetPixel(x,y, newColor);
               }
            }
         }

         
         (float u, float v) BrushUV(float distanceAlongLine, float distanceFromLine, float strokeWidth,
            int texWidth, int texHeight)
         {
            float uvRatio = (float)texWidth / (float)texHeight;
            float u = (distanceAlongLine / strokeWidth)/uvRatio;
            float v = 0.5f + distanceFromLine / strokeWidth;
            return (u, v);
         }
      }
      
      


      /// <summary>
      /// Return points distance from line. If the point is on the left side of the line, the distance is negative. 
      /// </summary>
      static float DistanceFromLine(Vector2 point, Vector2 linePoint1, Vector2 linePoint2)
      {
         Vector2 rejection = (point - linePoint1).RejectionOn(linePoint2 - linePoint1);
         if (IsPointLeftSideOfLine()) return -rejection.magnitude;
         return rejection.magnitude;
         
         bool IsPointLeftSideOfLine() => (linePoint2.x - linePoint1.x)*(point.y - linePoint1.y) > 
                                         (linePoint2.y - linePoint1.y)*(point.x - linePoint1.x);
      }

      // Projects point on line and returns how far along the line it is. Negative if
      // before linePoint1.
      static float DistanceAlongLine(Vector2 point, Vector2 linePoint1, Vector2 linePoint2)
      {
         Vector2 lineDirection = linePoint2 - linePoint1;
         Vector2 projection = (point - linePoint1).ProjectionOn(lineDirection);
         float sign = Mathf.Sign(projection.x) * Mathf.Sign(lineDirection.x);
         return sign * projection.magnitude;
      }

      static RectInt StrokeBounds(Point[] points)
      {
         RectInt bounds = new RectInt(points[0].position, Vector2Int.zero);
         foreach (var point in points)
         {
            bounds = ExpandToContain(bounds, point);
         }
         
         return bounds;
     
         
         // Return RectInt that is expanded to contain the stroke point
         RectInt ExpandToContain(RectInt bounds, Point point)
         {
            Vector2Int min = bounds.min;
            Vector2Int max = bounds.max;
            int h = point.HalfWidth;
            
            if (point.position.x-h < min.x) min = new Vector2Int(point.position.x-h, min.y);
            if (point.position.x+h > max.x) max = new Vector2Int(point.position.x+h, max.y);
            if (point.position.y-h < min.y) min = new Vector2Int(min.x, point.position.y-h);
            if (point.position.y+h > max.y) max = new Vector2Int(max.x, point.position.y+h);

            bounds.SetMinMax(min, max);
            return bounds;
         }




      }
      
     

      


   }
   

}
