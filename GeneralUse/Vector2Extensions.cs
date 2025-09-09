using UnityEngine;

namespace RikusGameDevToolbox.GeneralUse
{
    public static class Vector2Extensions
    {
        /// <summary>
        /// Returns the clockwise angle (0f..360f) to the given vector
        /// </summary>
        public static float AngleClockwise(this Vector2 from, Vector2 vector)
        {
            float sa = Vector2.SignedAngle(from, vector);
            return sa < 0f ? -sa : 360f - sa;
        }
                
        /// <summary>
        /// Returns the counterclockwise angle (0f..360f) to the given vector
        /// </summary>
        public static float AngleCounterClockwise(this Vector2 from, Vector2 vector)
        {
            float sa = Vector2.SignedAngle(from, vector);
            return sa > 0f ? sa : 360f + sa;
        }
    }
}