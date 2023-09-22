using System;
using UnityEngine;

namespace RikusGameDevToolbox.VectorGraphics
{
    public class DefaulLineRenderer
    {
        public static void Setup(LineRenderer lineRenderer)
        {
            lineRenderer.useWorldSpace = false;
            lineRenderer.material = DefaulMaterial();
            lineRenderer.hideFlags = HideFlags.HideInInspector;
            lineRenderer.loop = false;
        }
        
        
        static Material DefaulMaterial()
        {
            string shaderName = "Legacy Shaders/Particles/Alpha Blended Premultiply";
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new ApplicationException("Cannot find " + shaderName);
            return new Material(shader);
        }
    }
}