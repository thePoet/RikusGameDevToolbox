using UnityEngine;
using System.Collections;
namespace RikusGameDevToolbox.GeneralUse
{
    public static class Math3d
    {
        public static Vector3 ForwardVecWithYRot(float rotation)
        {
            return Quaternion.Euler(0, rotation, 0) * Vector3.forward;
        }

        public static Vector3 DirectionTransform(Vector3 direction, Transform from, Transform to)
        {
            Vector3 inWorld = from.TransformDirection(direction);
            return to.InverseTransformDirection(inWorld);
        }

        // Gives rotation: from * result = to
        public static Quaternion RelativeRotation(Quaternion from, Quaternion to)
        {
            return Quaternion.Inverse(from) * to;
        }

        public static Vector3 RotatePointAroundAxis(Vector3 point, float angle, Vector3 axis)
        {
            Quaternion q = Quaternion.AngleAxis(angle, axis);
            return q * point;
        }

        // TODO: MAKE AN ANGLE CLASS AND MOVE THIS STUFF THERE

        // polar angle of vector in degrees 
        public static float PolarAngle(Vector3 vector)
        {
            return -Math2d.GetAngle(Vector2.up, new Vector2(vector.x, vector.z));
        }

        public static float PolarAngleFromTo(Vector3 startPosition, Vector3 endPosition)
        {
            return PolarAngle(endPosition - startPosition);
        }

        public static Vector3 DirectionPolarAngle(float polarAngle)
        {
            return ForwardVecWithYRot(polarAngle);
        }

        public static float AddPolarAngles(float angle1, float angle2)
        {
            float result = angle1 + angle2;
            while (result < 0f)
                result += 360f;
            while (result > 360f)
                result -= 360f;

            return result;

        }
    }
}
