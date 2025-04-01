using System;
using UnityEngine;
using RikusGameDevToolbox.VectorGraphics;
using Random = UnityEngine.Random;

namespace RikusGameDevToolbox.Geometry2d
{
    public static class PolygonRenderer
    {

        public static GameObject Render(PolygonMesh2 mesh, float lineWidth)
        {
            GameObject go = new GameObject("PolygonMesh");

            foreach ((Vector2 a, Vector2 b)  in mesh.Edges())
            {
                var edge = CreateEdge(a, b, 0 + .01f, Color.cyan);
                edge.transform.parent = go.transform;
            }

            foreach (var (a,b)  in mesh.DebugBorderEdges())
            {
                var edge = CreateEdge(a, b, lineWidth*2f, Color.red);
                edge.transform.parent = go.transform;
            }
/*
            foreach (var outline in mesh.Outlines())
            {
                Vector2 previus = outline[outline.Count - 1];
                foreach (Vector2 p in outline)
                {
                    var edge = CreateEdge(previus, p, lineWidth*2f, Color.magenta);
                    edge.transform.parent = go.transform;
                    previus = p;
                }
            }
            
            foreach (var hole in mesh.Holes())
            {
                Vector2 previus = hole[hole.Count - 1];
                foreach (Vector2 p in hole)
                {
                    var edge = CreateEdge(previus, p, lineWidth*2f, Color.yellow);
                    edge.transform.parent = go.transform;
                    previus = p;
                }
            }*/
            return go;
        }
        
        
        public static GameObject Render(Polygon polygon, float lineWidth)
        {
            GameObject go = new GameObject("Polygon");
          
            Color color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            float pointSize = Random.Range(lineWidth*3f, lineWidth*7f);
            float rotation = Random.Range(0f, 360f);
            
            LineRenderer lr = go.AddComponent<LineRenderer>();
            Setup(lr, lineWidth, color);
            
            lr.loop = true;
            lr.positionCount = polygon.Contour.Length;
      
            for (int i=0; i<polygon.Contour.Length; i++)
            {
                lr.SetPosition(i, polygon.Contour[i]);
                var point = CreatePoint(polygon.Contour[i], pointSize,  color);
                point.transform.parent = go.transform;
                point.transform.localEulerAngles = new Vector3(0, 0, rotation);
            }
            
            for (int holeIdx=0; holeIdx < polygon.NumHoles; holeIdx++)
            {
                var hole = polygon.Hole(holeIdx);
                GameObject h = new GameObject("PolygonHole");
                var lr2 = h.AddComponent<LineRenderer>();
                Setup(lr2, lineWidth, color);
                lr2.loop = true;
                lr2.positionCount = hole.Contour.Length;
                
                for (int i=0; i<hole.Contour.Length; i++)
                {
                    lr2.SetPosition(i, hole.Contour[i]);
                    var point = CreatePoint(hole.Contour[i], pointSize,  color);
                    point.transform.parent = h.transform;
                    point.transform.localEulerAngles = new Vector3(0, 0, rotation);
                }
            }
    
            
            return go;
        }

        private static GameObject CreateEdge(Vector2 a, Vector2 b, float lineWidth, Color color)
        {
            GameObject go = new GameObject();
            var lr = go.AddComponent<LineRenderer>();
            Setup(lr, lineWidth, color);
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
          
            return go;
        }

        
        private static GameObject CreatePoint(Vector2 position, float size, Color color)
        {
            GameObject go = new GameObject();
            go.transform.position = position;
            var rp = go.AddComponent<RegularPolygon>();
            rp.numSides = 3;
            rp.radius = size;
            rp.lineWidth = size / 10f;
            rp.color = color;
            rp.UpdateLineRenderer();
            
            return go;
        }
        
        
        private static void Setup(LineRenderer lineRenderer, float lineWidth, Color color)
        {
            lineRenderer.useWorldSpace = false;
            lineRenderer.material = DefaulMaterial();
            lineRenderer.hideFlags = HideFlags.HideInInspector;
            lineRenderer.loop = false;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
        
        
        private static Material DefaulMaterial()
        {
            string shaderName = "Legacy Shaders/Particles/Alpha Blended Premultiply";
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new ApplicationException("Cannot find " + shaderName);
            return new Material(shader);
        }
    }
}