using UnityEngine;

namespace RikusGameDevToolbox.Tests
{
    public class Helpers
    {
        public static bool IsAlmostSame(float a, float b) => Mathf.Abs(a - b) < 0.001f;
    }
}