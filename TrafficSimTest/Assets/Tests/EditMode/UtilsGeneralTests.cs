using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class UtilsGeneralTests
    {
        [Test]
        public void SelectsRandom()
        {
            Assert.AreEqual(Utils.Random.Select(new List<int>()), 0);
            Assert.AreEqual(Utils.Random.Select(new List<Vector3>()), Vector3.zero);
            
            List<int> list = new List<int>() { 1, 2, 3, 4, 5 };

            for (int i = 0; i < 10; i++)
            {
                int result = Utils.Random.Select(list);
                Assert.IsTrue(list.Contains(result));
            }
        }

        [Test]
        public void Flattens2DArray()
        {
            int[,] arrayMultidim = { { 1, 2, 3 }, { 4, 5, 6 } };
            int[][] arrayJagged = { new int[] { 1, 2, 3 }, new int [] { 4, 5, 6 } };

            int[] result;
            
            result = Utils.Arrays.Flatten2DArray(arrayMultidim);
            Assert.AreEqual(result.Length, 6);
            Assert.AreEqual(result[0], arrayMultidim[0, 0]);
            Assert.AreEqual(result[1], arrayMultidim[0, 1]);
            Assert.AreEqual(result[2], arrayMultidim[0, 2]);
            Assert.AreEqual(result[3], arrayMultidim[1, 0]);
            Assert.AreEqual(result[4], arrayMultidim[1, 1]);
            Assert.AreEqual(result[5], arrayMultidim[1, 2]);
            
            result = Utils.Arrays.Flatten2DArray(arrayJagged);
            Assert.AreEqual(result.Length, 6);
            Assert.AreEqual(result[0], arrayJagged[0][0]);
            Assert.AreEqual(result[1], arrayJagged[0][1]);
            Assert.AreEqual(result[2], arrayJagged[0][2]);
            Assert.AreEqual(result[3], arrayJagged[1][0]);
            Assert.AreEqual(result[4], arrayJagged[1][1]);
            Assert.AreEqual(result[5], arrayJagged[1][2]);
        }
    
        [Test]
        public void ConvertsTo2DArray()
        {
            int[] array = { 1, 2, 3, 4, 5, 6 };

            Assert.IsNull(Utils.Arrays.To2DArray(array, 0, 0));
            Assert.IsNull(Utils.Arrays.To2DArray(array, -1, 5));
            Assert.IsNull(Utils.Arrays.To2DArray(array, 3, 4));

            int[,] result = Utils.Arrays.To2DArray(array, 2, 3);
            Assert.AreEqual(result.GetLength(0), 2);
            Assert.AreEqual(result.GetLength(1), 3);
            Assert.AreEqual(result[0, 0], array[0]);
            Assert.AreEqual(result[0, 1], array[1]);
            Assert.AreEqual(result[0, 2], array[2]);
            Assert.AreEqual(result[1, 0], array[3]);
            Assert.AreEqual(result[1, 1], array[4]);
            Assert.AreEqual(result[1, 2], array[5]);
        }
    }
}
