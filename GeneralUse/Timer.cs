using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RikusGameDevToolbox.GeneralUse
{


    public class Timer
    {
        public float startTime { get; private set; }

        public float timeSinceStart
        {
            get { return TimeSinceStart(); }
            private set { }
        }


        public Timer()
        {
            Reset();
        }

        public void Reset()
        {
            startTime = Time.realtimeSinceStartup;
        }

        float TimeSinceStart()
        {
            return Time.realtimeSinceStartup - startTime;
        }

    }

}
