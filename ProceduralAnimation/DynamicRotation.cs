using RikusGameDevToolbox.GeneralUse;
using UnityEditor;
using UnityEngine;

namespace RikusGameDevToolbox.ProceduralAnimation
{
    /// <summary>
    /// Makes the GameObject follow target's local rotation in a dynamic fashion.
    /// </summary>
    public class DynamicRotation : MonoBehaviour
    {
        #region ------------------------------------- PUBLIC FIELDS & PROPERTIES ---------------------------------------
        
        [Header("Natural frequency")]
        [Range(0.001f, 10f)]  
        public float f = 1f; 

        [Header("Damping coefficient")]
        [Range(0f, 5f)]  
        public float z = 1f; 
        
        [Header("Initial tailMaxSpeed.")]
        [Range(-5f, 5f)]  
        public float r = 1f;

        [Header("Target")]
        public Dimension3d rotationAxis;
        public Transform target;
        #endregion
        #region ------------------------------------ PRIVATE FIELDS & PROPERTIES ---------------------------------------
        
        private SecondOrderDynamics _dynamics;
        #endregion 
        #region ------------------------------------------- UNITY METHODS ----------------------------------------------

        void Start()
        {
            _dynamics = new SecondOrderDynamics
            {
                isRotation = true
            };
            UpdateDynamicConstants();
            Reset();
        }

        void Update()
        {  
            if (target == null) return;

            _dynamics.Input = target.localEulerAngles.Get(rotationAxis);
            _dynamics.Update(Time.deltaTime);
            
            transform.localEulerAngles = transform.localEulerAngles.Set(rotationAxis, _dynamics.Output);
        }

        // Called if values are changed in editor
        private void OnValidate()
        {
            UpdateDynamicConstants();
        }
        #endregion
        #region ------------------------------------------ PRIVATE METHODS ---------------------------------------------

        private void UpdateDynamicConstants()
        {
            _dynamics?.SetDynamicConstants(f, z, r);
        }

        private void Reset()
        {
            float rotation = transform.localEulerAngles.Get(rotationAxis);
            _dynamics.SetAtRest(rotation);
        }
        #endregion
    }
}