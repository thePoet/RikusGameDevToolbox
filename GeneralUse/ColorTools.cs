using UnityEngine;
using Random = UnityEngine.Random;

namespace RikusGameDevToolbox.GeneralUse
{
    public class ColorTools
    {
        public static void RandomTest()
        {
            TestReverseBlend(RandomOpaqueColor(), RandomTransparentColor());

            Color RandomOpaqueColor()
            {
                return new Color(Random.Range(0f, 1f), 
                    Random.Range(0f, 1f), 
                    Random.Range(0f, 1f), 
                    1f);
            }

            Color RandomTransparentColor()
            {
                Color color = RandomOpaqueColor();
                color.a = Random.Range(0f, 1f);
                return color;
            }
        }

        public static void TempTest()
        {
            Color under = new Color(30f/255f, 30f/255f, 31f/255f);
            Color blend = new Color(92f/255f, 81f/255f, 45f/255f);
            Color over = ReverseBlend(under, blend);
            Debug.Log(under);
            Debug.Log(over);
            Debug.Log(blend);
            Debug.Log("OVER : " + over);
            Debug.Log(over.r*255f + " " + over.g*255f + " " + over.b*255f);
        }

        public static void TestReverseBlend(Color under, Color over)
        {
            Color blend = Blend(under, over);
            Color reverseOver = ReverseBlend(under, blend);
            Color blend2 = Blend(under, reverseOver);

            Debug.Log("Testing under: " + under + " over: " + over + " blend: " + blend);
            if (IsSame(blend, blend2)) 
                Debug.Log("PASS");
            else
                Debug.Log("FAIL: revOver: " + reverseOver + "blend: " + blend2);
            Debug.Log("-------");
        }
        
        /// <summary>
        /// Returns true if Colors are same within 8 bit precision per component.
        /// </summary>
        public static bool IsSame(Color a, Color b)
        {
            const float tolerance = 0.5f / 256f;
            for (int i = 0; i < 4; i++)
            {
                if (Mathf.Abs(a[i] - b[i]) > tolerance) return false;
            }
            return true;
        }
        
        
        /// <summary>
        /// Blends two colors together using alpha compositing.
        /// Similar effect to the Photoshop's "normal" layer blend mode.
        /// </summary>
        public static Color Blend(Color under, Color over)
        {
            Color blend = AlphaMultiplied(over) + AlphaMultiplied(under) * (1f - over.a);
            return AlphaDivided(blend);
        }

        /// <summary>
        /// Figures out which color needs to be blended over the "under" Color
        /// to get the "blend" Color. The color is chosen so that it's alpha is as
        /// low as possible.
        /// NOTE: Does not currently work for cases where inputs are transparent.
        /// </summary>
        public static Color ReverseBlend(Color under, Color blend)
        {
            Color underM = AlphaMultiplied(under);
            Color blendM = AlphaMultiplied(blend);
            

            // Minimum opacity to prevent RGB values going < 0f
            float opacityMin = Mathf.Min(
                   blendM.r / underM.r,
                                blendM.g / underM.g,
                                blendM.b / underM.b,
                                1f
                );
      
            // Minimum opacity to prevent RGB values going > 1f
            float opacityMin2 = Mathf.Min(
                (blendM.r-blendM.a)/(under.r-under.a),
                (blendM.g-blendM.a)/(under.g-under.a),
                (blendM.b-blendM.a)/(under.b-under.a)
                );

            float opacity = Mathf.Min(opacityMin, opacityMin2);
            Color over = blendM - underM * opacity;

            return AlphaDivided(over);
        }
        
        static Color AlphaMultiplied(Color color)
        {
            return new Color(color.r * color.a,
                color.g * color.a,
                color.b * color.a,
                color.a);
        }

        static Color AlphaDivided(Color color)
        {
            if (color.a == 0f) return color;
            return new Color(color.r / color.a,
                color.g / color.a,
                color.b / color.a,
                color.a);
        }
    }
}