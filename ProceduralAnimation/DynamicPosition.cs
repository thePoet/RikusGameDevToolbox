using System;
using RikusGameDevToolbox.GeneralUse;
using UnityEngine;

namespace RikusGameDevToolbox.ProceduralAnimation
{
    /// <summary>
    /// Makes the GameObject follow target's position in a dynamic fashion.
    /// </summary>
    public class DynamicPosition : MonoBehaviour
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
        public bool localPosition = true;
        
        public Vector3 Accelleration => dynamics.OutputAcceleration;
        public Vector3 Velocity => dynamics.OutputVelocity;
        
        #endregion
        #region ------------------------------------ PRIVATE FIELDS & PROPERTIES ---------------------------------------
        
        private SecondOrderDynamics3d dynamics;
        private Action _actionAfterArrival;
        private float _toleranceForArrival;

        private Vector3 CurrentPosition
        {
            get 
            {
                if (localPosition) return transform.localPosition;
                return transform.position;
            }
            set
            {
                if (localPosition) transform.localPosition = value;
                else transform.position = value;
            }
        }
        private Vector3 TargetPosition
        {
            get 
            {
                if (localPosition) return target.localPosition;
                return target.position;
            }
            set
            {
                if (localPosition) target.localPosition = value;
                else target.position = value;
            }
        }
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
            
            dynamics.Input = TargetPosition;
            dynamics.Update(Time.deltaTime);
            
            if (followTargetX) CurrentPosition = CurrentPosition.SetX(dynamics.Output.x);
            if (followTargetY) CurrentPosition = CurrentPosition.SetY(dynamics.Output.y); 
            if (followTargetZ) CurrentPosition = CurrentPosition.SetZ(dynamics.Output.z);

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
        #endregion
        #region ------------------------------------------ PUBLIC METHODS ---------------------------------------------
        public void GoTo(Vector3 position, Action actionAfterArrival = null, float toleranceForArrival=0.01f)
        {
            if (target == null)
            {
                var newTarget = new GameObject("Target");
                target = newTarget.transform;
                if (localPosition) target.parent = transform.parent;
            }

            TargetPosition = position;

            _actionAfterArrival = actionAfterArrival;
            _toleranceForArrival = toleranceForArrival;
        }
        
        
        #endregion
        #region ------------------------------------------ PRIVATE METHODS ---------------------------------------------
        private void UpdateDynamicConstants()
        {
            dynamics?.SetDynamicConstants(f, z, r);
        }

        bool HasArrived()
        {
            if (target==null) return false;
            return (CurrentPosition - TargetPosition).magnitude < _toleranceForArrival;
        }

        private void Reset()
        {
            dynamics.SetAtRest(CurrentPosition);
        }
        #endregion
    }
}