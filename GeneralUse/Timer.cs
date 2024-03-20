
namespace RikusGameDevToolbox.GeneralUse
{


    public class Timer
    {
        public float StartTime { get; private set; }

        // Returns the seconds since the Timer has been started/reset.
        public float Time => TimeSinceStart();


        public Timer()
        {
            Reset();
        }

        public void Reset()
        {
            StartTime = UnityEngine.Time.realtimeSinceStartup;
        }

        float TimeSinceStart()
        {
            return UnityEngine.Time.realtimeSinceStartup - StartTime;
        }

    }

}
