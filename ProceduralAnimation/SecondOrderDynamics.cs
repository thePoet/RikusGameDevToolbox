using RikusGameDevToolbox.GeneralUse;
using UnityEngine;



namespace RikusGameDevToolbox.ProceduralAnimation
{
    /// <summary>
    /// Simulates a system with an output that trails it's input. The dynamic behaviour e.g. damping and
    /// overshooting is configured by three dynamic constants (f, z, r).
    ///
    /// Equation for the system:
    /// y + k1*yd1 + k2*yd2 = x + k3*xd1
    /// where
    /// k1 = z / (PI * f)
    /// k2 = 1 / ((2 * PI * f) * (2 * PI * f))
    /// k3 = r * z / (f * PI * f)
    /// 
    /// Based on this excellent video on such systems:  https://www.youtube.com/watch?v=KPoeNZZ6H4s 
    /// </summary>
    public class SecondOrderDynamics
    {
        #region ------------------------------------- PUBLIC FIELDS & PROPERTIES ---------------------------------------

        public float Input
        {
            get => _x;
            set => _x = value;
        }
        
        public float Output => _y;
        public float OutputVelocity => _yd1;    
        public float OutputAcceleration => _yd2;

        /// <summary>
        /// If true, the system simulates a rotation, given in degrees. 
        /// </summary>
        public bool isRotation = false;

        /// <summary>
        /// If true time step can be split internally to keep the simulation stable.
        /// </summary>
        public bool splitTimeStepAllowed = true;
        
        /// <summary>
        /// Largest safe timestep for the system to remain stable. Depends on f, z and r.
        /// </summary>
        public float MaxStableTimeStep { get; private set; } 
       
        #endregion
        #region ------------------------------------- PRIVATE FIELDS & PROPERTIES --------------------------------------

        private float _x;             // Input
        private float _xPrevious;     // Input at previous update
        private float _y;             // Output
        private float _yd1;           // Output's first derivative
        private float _yd2;           // Output's second derivative
        private float _k1, _k2, _k3;    // Terms that define the dynamics of the system. Derived from f, z and r.
        

        #endregion
        #region ------------------------------------------- PUBLIC METHODS ---------------------------------------------
        public SecondOrderDynamics()  
        {
            SetAtRest(0f);
            SetDynamicConstants(1f, 1f, 1f);
        }
     
        /// <summary>
        /// Set the dynamic constants that define the behaviour of the system.
        /// </summary>
        /// <param name="f">Natural frequency. The bigger the value, the faster the system reacts to change.</param>
        /// <param name="z">Damping coefficient. The bigger the value, the faster the system settles down.</param>
        /// <param name="r">Initial tailMaxSpeed. If > 1f, the system overshoots. If negative, the system will anticipate.</param>
        public void SetDynamicConstants(float f, float z, float r)
        {
            // Derive parameters in more usable format
            _k1 = z /  (Mathf.PI * f);
            _k2 = 1f / ((2f * Mathf.PI * f) * (2f * Mathf.PI * f));
            _k3 = r * z / (2f * Mathf.PI * f);
            
            // Calculate longest safe time step for the system. Time steps greater than this may lead to instability.
            float safetyMargin = 0.8f;
            MaxStableTimeStep = safetyMargin * (Mathf.Sqrt(4f * _k2 + _k1 * _k1) - _k1);
        }

        /// <summary>
        /// Updates the system. 
        /// </summary>
        /// <param name="timeDelta">Time step</param>
        /// <returns>Returns the output of the system after the time step.</returns>
        public float Update(float timeDelta)
        {
            float xd1 = DeltaX();
            _xPrevious = _x;
          
            int numSteps = NumberOfTimeSteps();
            timeDelta = timeDelta / numSteps;
           
            for (int i = 0; i < numSteps; i++)
            {
                Integrate(timeDelta);
            }

            return _y;

            float DeltaX()
            {
                if (isRotation)
                {
                    return Angle.SignedDelta(_xPrevious, _x) / timeDelta;
                }
                return (_x - _xPrevious) / timeDelta;
            }

            int NumberOfTimeSteps()
            {
                if (!splitTimeStepAllowed || timeDelta >= MaxStableTimeStep) return 1;
                return Mathf.CeilToInt(timeDelta / MaxStableTimeStep);
            }
            
            void Integrate(float timeStep)
            {
                // Integrate using semi-implicit Euler method:
                if (isRotation)
                {
                    // For rotations we need to account for the angle "going around".
                    _y = Angle.Add(_y, timeStep * _yd1);
                    float yToX = Angle.SignedDelta(_y, _x);
                    _yd2 = (yToX + _k3 * xd1 - _k1 * _yd1) / _k2;
                    _yd1 += timeStep * _yd2;
                }
                else
                {
                    _y += timeStep * _yd1;
                    _yd2 = (_x + _k3 * xd1 - _y - _k1 * _yd1) / _k2;
                    _yd1 += timeStep * _yd2;
                }
            }

        }
        
        /// <summary>
        /// Resets the system to be at rest with output & input at the given value.
        /// </summary>
        public void SetAtRest(float value)
        {
            _y = value;
            _yd1 = 0f;
            _yd2 = 0f;
            _x = value;
            _xPrevious = value;
        }
        
        #endregion
    }
}
