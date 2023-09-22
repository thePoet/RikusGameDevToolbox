
using UnityEngine;

namespace RikusGameDevToolbox.GeneralUse
{
    /// <summary>
    /// This class is a wrapper for Unity's basic arithmetic operations for angles.
    /// The aim is to make their descriptions unambiguous since Unity's documentation for
    /// the said operations is lacking and a source of constant confusion for me. 
    /// </summary>
   public static class Angle
    {
        /// <param name="angleDeg">Angle in degrees</param>
        /// <returns>Returns the given angle in range 0f..360f</returns>
        public static float Normalize(float angle)
        {
            return Mathf.Repeat(angle, 360f);
        }

        /// <summary>
        /// Adds angle to another.
        /// </summary>
        /// <param name="angleA">Angle A in degrees</param>
        /// <param name="angleB">Angle B in degrees</param>
        /// <returns>Returns the sum of the angles in range 0f..360f</returns>
        public static float Add(float angleA, float angleB)
        {
            return Normalize(angleA + angleB);
        }
        
        /// <summary>
        /// Substracts angle B from angle A
        /// </summary>
        /// <param name="angleA">Angle A in degrees</param>
        /// <param name="angleB">Angle B in degrees</param>
        /// <returns>Returns the angle (A - B) in range 0f..360f</returns>
        public static float Subtract(float angleA, float angleB)
        {
            return Add(angleA, -angleB);
        }

        /// <summary>
        /// Returns how many degrees needs to be added or subtracted from
        /// angleFrom to get to angleTo. It goes the shorter way - direction of which
        /// is indicated by the sign of the result.
        ///
        /// For example Angle.SignedDelta(730f,-20f) returns -30f.
        /// </summary>
        /// <param name="angleFrom">Starting angle in degrees</param>
        /// <param name="angleTo">Target angle in degrees</param>
        /// <returns>Returns values in degrees in range -180f...180f</returns>
        public static float SignedDelta(float angleFrom, float angleTo)
        {
            return Mathf.DeltaAngle(angleFrom, angleTo);
        }

        // TODO: Move relevat stuff from Math2d to here
    }
}