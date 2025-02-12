namespace RikusGameDevToolbox.GeneralUse
{
    /// <summary>
    /// A moving average that works with angles so there is no weirdness around the jump between 360 and 0 degrees.
    /// The averages it returns are normalized to 0..360 degrees.
    /// </summary>
    public class MovingCircularAverage
    {
        private readonly float[] _angles;
        private readonly int _bufferSize;
        private int _index;
        private int _count;

        /// <summary>
        /// Buffer size is the number of angles that will be stored and averaged.
        /// </summary>
        public MovingCircularAverage(int bufferSize)
        {
            _bufferSize = bufferSize;
            _angles = new float[bufferSize];
        }

        public void Add(float angle)
        {
            _angles[_index] = angle;
            _index = (_index + 1) % _bufferSize;
            if (_count < _bufferSize) _count++;
        }

        // Return the moving average of the angles normalized to 0..360 degrees
        public float Average()
        {
            if (_count == 0) return 0f;
            return Angle.CircularMean(_angles);
        }
    }
}