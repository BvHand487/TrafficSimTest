using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework;


namespace Tests.EditMode
{
    [TestFixture]
    public class UtilsMathTests
    {
        /*
         * Tests whether GetsClosestVector returns the closest Point which isn't the target point.
         */
        [Test]
        public void GetsClosestVector()
        {
            Vector3 target = Vector3.one;
            List<Vector3> points;
            Vector3 result;

            points = new List<Vector3>() {
                Vector3.zero,
            };
            result = Utils.Math.GetClosestVector(target, points);
            Assert.AreEqual(result, points[0]);
            
            points = new List<Vector3>() {
                Vector3.zero,
                -Vector3.one,
            };
            result = Utils.Math.GetClosestVector(target, points);
            Assert.AreEqual(result, points[0]);
            
            points = new List<Vector3>() {
                Vector3.one,
                -Vector3.one,
            };
            result = Utils.Math.GetClosestVector(target, points);
            Assert.AreEqual(result, points[1]);
        }
        
        [Test]
        public void AreCollinear()
        {
            Vector3 v1 = 3 * Vector3.up;
            Vector3 v2 = 2 * Vector3.up;
            Vector3 v3 = Vector3.down;
            
            bool result = Utils.Math.AreCollinear(v1, v2, v3);
            Assert.IsTrue(result);
        }
        
        [Test]
        public void GetsMidpointVector()
        {
            Assert.AreEqual(Utils.Math.GetMidpointVector(-Vector3.one, Vector3.one), Vector3.zero);
            Assert.AreEqual(Utils.Math.GetMidpointVector(Vector3.zero, Vector3.zero), Vector3.zero);
            Assert.AreEqual(Utils.Math.GetMidpointVector(Vector3.one, Vector3.one), Vector3.one);
            Assert.AreEqual(Utils.Math.GetMidpointVector(Vector3.one, 2f * Vector3.one), 1.5f * Vector3.one);
        }
        
        /*
         * Tests whether OrderVectorPath returns a list of points where each pair of points are closest neighbours.
         */
        [Test]
        public void OrdersVectorPath()
        {
            Assert.IsNull(Utils.Math.OrderVectorPath(null));
            Assert.IsNull(Utils.Math.OrderVectorPath(new List<Vector3>() {}));
            
            List<Vector3> result;
            
            result = Utils.Math.OrderVectorPath(new List<Vector3>() {
                Vector3.up,
            });
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual(result[0], Vector3.up);
            
            result = Utils.Math.OrderVectorPath(new List<Vector3>() {
                Vector3.up,
                Vector3.down,
            });
            Assert.AreEqual(result.Count, 2);
            Assert.AreEqual(result[0], Vector3.up);
            Assert.AreEqual(result[1], Vector3.down);
            
            result = Utils.Math.OrderVectorPath(new List<Vector3>() {
                2f * Vector3.down,
                Vector3.up,
                Vector3.right,
            });
            Assert.AreEqual(result.Count, 3);
            Assert.AreEqual(result[0], 2f * Vector3.down);
            Assert.AreEqual(result[1], Vector3.right);
            Assert.AreEqual(result[2], Vector3.up);
            
            result = Utils.Math.OrderVectorPath(new List<Vector3>() {
                0.5f * Vector3.left,
                Vector3.up,
                0.5f * Vector3.right,
            });
            Assert.AreEqual(result.Count, 3);
            Assert.AreEqual(result[0], 0.5f * Vector3.left);
            Assert.AreEqual(result[1], 0.5f * Vector3.right);
            Assert.AreEqual(result[2], Vector3.up);
        }
    }
}
