using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RikusGameDevToolbox.GeneralUse
{
    public static class UniqueIdGenerator
    {
        public static long largest { get; set; }

        static UniqueIdGenerator()
        {
            largest = 0;
        }

        public static long New()
        {
            largest++;
            return largest;
        }

    }
}
