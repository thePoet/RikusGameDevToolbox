using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using RikusGameDevToolbox.GeneralUse;
using static RikusGameDevToolbox.Tests.Helpers;
using static RikusGameDevToolbox.GeneralUse.RectExtensions;

namespace RikusGameDevToolbox.Tests
{
    public class TestRectExtensions
    {
       
        
        [Test]
        public void Encapsulate1()
        {
            Rect rect = new Rect(0f, 0f, 10f, 10f);
            Vector2 point = new Vector2(-1f, 0f);
            Rect expected = new Rect(-1f, 0f, 11f, 10f);
            rect.GrowToEncapsulate(point);
            Assert.IsTrue(IsSame(rect, expected));
        }
        
        [Test]
        public void Encapsulate2()
        {
            Vector2 p1 = new Vector2(-1f, 0f);
            Vector2 p2 = new Vector2(0f, 15f);
            Vector2 p3 = new Vector2(-5f, 5f);
            List<Vector2> points = new List<Vector2> {p1, p2, p3};
            Rect expected = new Rect(-5f, 0f, 5f, 15f);
            var result = CreateRectToEncapsulate(points);
            Assert.IsTrue(IsSame(result, expected));
        }
        
        [Test]
        public void Encapsulate3()
        {
            Rect r1 = new Rect(0f, 0f, 10f, 10f);
            Rect r2 = new Rect(-10f, -5f, 1f, 25f);

            Rect expected = new Rect(-10f, -5f, 20f, 25f);
            var result = CreateRectToEncapsulate(new List<Rect>{r1,r2});
            Assert.IsTrue(IsSame(result, expected));
        }
        
        [Test]
        public void Intersection()
        {
            Rect r1 = new Rect(0f, 0f, 2f, 3f);
            Rect r2 = new Rect(-2f, -2f, 10f, 3f);

            Rect expected = new Rect(0f, 0f, 2f, 1f);
            var result = r1.IntersectionWith(r2);
            Assert.IsTrue(IsSame(result, expected));
        }
        
        
        
        public bool IsSame(Rect a, Rect b)
        {
            return IsAlmostSame(a.x, b.x) && IsAlmostSame(a.y, b.y) && IsAlmostSame(a.width, b.width) && IsAlmostSame(a.height, b.height);
        }

    }
}