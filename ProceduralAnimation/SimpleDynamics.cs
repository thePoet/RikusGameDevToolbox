using System;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;



namespace RikusGameDevToolbox.ProceduralAnimation
{
    /// <summary>
    /// Simulates a system with an output that trails it's input. The exact behaviour is governed by
    /// maximum velocity and accelleration.


    /// </summary>
    public class SimpleDynamics
    {
        #region ------------------------------------- PUBLIC FIELDS & PROPERTIES ---------------------------------------

        public float Input
        {
            get => _x;
            set => _x = value;
        }
        
        public float MaxVelocity { get; set; }
        public float MaxAccelleration { get; set; }
        
        
        public float Output => _y;
        public float OutputVelocity => _v;    


        /// <summary>
        /// If true, the system simulates a rotation, given in degrees. 
        /// </summary>
        public bool isRotation = false;

        public bool suddenStop = false;
        
        #endregion
        #region ------------------------------------- PRIVATE FIELDS & PROPERTIES --------------------------------------

        private float _x;             // Input
        private float _y;             // Output
        private float _v;             // Velocity   
        private float _aMax, _vMax;
        

        #endregion
        #region ------------------------------------------- PUBLIC METHODS ---------------------------------------------
        public SimpleDynamics()  
        {
            SetAtRest(0f);
        }


        /// <summary>
        /// Updates the system. 
        /// </summary>
        /// <param name="timeDelta">Time step</param>
        /// <returns>Returns the output of the system after the time step.</returns>
        public float Update(float timeDelta)
        {
            if (isRotation || suddenStop) throw new NotImplementedException("rotation / suddenstop not implemented");
            
            float delta = _x - _y;
            float targetVelocity = delta * MaxAccelleration;
            targetVelocity = Mathf.Clamp(targetVelocity, -MaxVelocity, MaxVelocity);
            _v = Mathf.MoveTowards(_v, targetVelocity, MaxAccelleration * timeDelta );
            _y += _v * timeDelta;

            return _y;
        }
        
        /// <summary>
        /// Resets the system to be at rest with output & input at the given value.
        /// </summary>
        public void SetAtRest(float value)
        {
            _y = value;
            _x = value;
            _v = 0f;
        }
        
        #endregion
    }
}
