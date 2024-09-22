using System;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;

namespace RikusGameDevToolbox.ProceduralAnimation
{
    /// <summary>
    /// Makes the GameObject follow target's local scale in a dynamic fashion.
    /// </summary>
    public class DynamicScale : MonoBehaviour
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
        public bool followTargetX = true;
        public bool followTargetY = true;
        public bool followTargetZ = true;
        public Transform target;
        
        #endregion
        
        #region ------------------------------------ PRIVATE FIELDS & PROPERTIES ---------------------------------------

        private SecondOrderDynamics3d dynamics;
        private Action _actionAfterArrival;
        private float _toleranceForArrival;
        #endregion

        #region ------------------------------------------- UNITY METHODS ----------------------------------------------

        void Start()
        {
            dynamics = new SecondOrderDynamics3d();
            UpdateDynamicConstants();
            Reset();
        }

        void Update()
        {
            if (target == null) return;

            dynamics.Input = target.localScale;
            dynamics.Update(Time.deltaTime);
            
            if (followTargetX) transform.localScale = transform.localScale.With( x: dynamics.Output.x);
            if (followTargetY) transform.localScale = transform.localScale.With( y: dynamics.Output.y); 
            if (followTargetZ) transform.localScale = transform.localScale.With( z: dynamics.Output.z);
            
            if (HasArrived() && _actionAfterArrival != null)
            {
                _actionAfterArrival();
                _actionAfterArrival = null;
            }
        }

        // Called if values are changed in editor
        private void OnValidate()
        {
            UpdateDynamicConstants();
        }
        
        public void ScaleTo(Vector3 targetScale, Action actionAfterArrival = null, float toleranceForArrival=0.01f)
        {
            if (target == null)
            {
                var newTarget = new GameObject("ScaleTarget");
                target = newTarget.transform;
                target.parent = transform;
            }

            target.localScale = targetScale;

            _actionAfterArrival = actionAfterArrival;
            _toleranceForArrival = toleranceForArrival;
        }
        
        
        #endregion
        #region ------------------------------------------ PRIVATE METHODS ---------------------------------------------
        private void UpdateDynamicConstants()
        {
            dynamics?.SetDynamicConstants(f, z, r);
        }

        private void Reset()
        {
            dynamics.SetAtRest(transform.localScale);
        }
        bool HasArrived()
        {
            if (target==null) return false;
            return (transform.localScale - target.localScale).magnitude < _toleranceForArrival;
        }
        
        #endregion
    }
}