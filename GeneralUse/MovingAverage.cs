

namespace RikusGameDevToolbox.GeneralUse
{

    public class MovingAverage
    {
        private float[] _values;
        private int _size;
        private int _index;
        private int _count = 0;

        public MovingAverage(int size)
        {
            _size = size;
            _values = new float[size];
        }

        public void Add(float value)
        {
            _values[_index] = value;
            _index = (_index + 1) % _size;
            if (_count < _size) _count++;
        }

        public float Average()
        {
            if (_count == 0) return 0f;

            float sum = 0;
            for (int i = 0; i < _count; i++)
            {
                sum += _values[i];
            }

            return sum / _count;
        }

    }
}