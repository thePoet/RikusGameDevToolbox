using System;
using System.Collections.Generic;
using UnityEngine;

namespace RikusGameDevToolbox.VectorGraphics
{
    [ExecuteInEditMode]
    public class VectorGraphicsShape : MonoBehaviour
    {
        public float lineWidth = 0.1f;
        public Color color = new Color(0.5f, 0.5f, 0.5f, 1f);

        void Awake()
        {
            var lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            DefaulLineRenderer.Setup(lineRenderer);
            UpdateLineRenderer();
        }

        private void OnEnable()
        {
            GetComponent<LineRenderer>().enabled = true;
        }

        private void OnDisable()
        {
            GetComponent<LineRenderer>().enabled = false;
        }

        // This is called when changes are made in the Inspector.
        private void OnValidate()
        {
            UpdateLineRenderer();
        }

        protected void SetVertices(List<Vector3> vertices)
        {
            var line = GetComponent<LineRenderer>();
            line.positionCount = vertices.Count; 
            line.SetPositions( vertices.ToArray() );
        }

        
        // TODO: If the shape Component is removed, the hidden LineRenderer remains...
        
        private void OnDestroy()
        {/*
            var lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                if (Application.isEditor)
                {
                    DestroyImmediate(lineRenderer);
                }
                else Destroy(lineRenderer);
            }*/
        }
        
        protected virtual void UpdateLineRenderer()
        {
            var line = GetComponent<LineRenderer>();
            
            line.startColor = color;
            line.endColor = color;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
        }
    }
}