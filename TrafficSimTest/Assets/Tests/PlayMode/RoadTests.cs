using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class RoadTests
    {
        private (Junction, Junction, Road) SetupNormalRoad()
        {
            var j1Obj = new GameObject();
            Junction j1 = j1Obj.AddComponent<Junction>();
            j1.transform.position = new Vector3(-GameManager.TileSize, 0f, 0f);

            var j2Obj = new GameObject();
            Junction j2 = j2Obj.AddComponent<Junction>();
            j2.transform.position = new Vector3(GameManager.TileSize, 0f, 0f);

            Road r = new Road(null, new List<Vector3> { Vector3.zero }, j1, j2);

            j1.Initialize(new List<Road> { r });
            j2.Initialize(new List<Road> { r });

            return (j1, j2, r);
        }

        private (Junction, Junction, Road) SetupTurnRoad()
        {
            var j1Obj = new GameObject();
            Junction j1 = j1Obj.AddComponent<Junction>();
            j1.transform.position = new Vector3(-2f * GameManager.TileSize, 0f, 0f);

            var j2Obj = new GameObject();
            Junction j2 = j2Obj.AddComponent<Junction>();
            j2.transform.position = new Vector3(0f, 0f, 2f * GameManager.TileSize);

            Road r = new Road(null,
                new List<Vector3>
                {
                    new Vector3(-GameManager.TileSize, 0f, 0f),
                    new Vector3(0f, 0f, 0f),
                    new Vector3(0f, 0f, GameManager.TileSize),
                }, j1, j2);

            j1.Initialize(new List<Road> { r });
            j2.Initialize(new List<Road> { r });

            return (j1, j2, r);
        }

        private (Junction, Road) SetupCyclicRoad()
        {
            var jObj = new GameObject();
            Junction j = jObj.AddComponent<Junction>();
            j.transform.position = new Vector3(-GameManager.TileSize, 0f, -GameManager.TileSize);

            Road r = new Road(null,
                new List<Vector3>
                {
                    new Vector3(-GameManager.TileSize, 0f, 0f),
                    new Vector3(0f, 0f, 0f),
                    new Vector3(0f, 0f, -GameManager.TileSize),
                }, j, j);

            j.Initialize(new List<Road> { r });

            return (j, r);
        }

        [UnityTest]
        public IEnumerator IsConnectedTo_NormalRoad()
        {
            var (j1, j2, r) = SetupNormalRoad();

            Assert.IsTrue(r.IsConnectedTo(j1));
            Assert.IsTrue(r.IsConnectedTo(j2));

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConnectedTo_TurnRoad()
        {
            var (j1, j2, r) = SetupTurnRoad();

            Assert.IsTrue(r.IsConnectedTo(j1));
            Assert.IsTrue(r.IsConnectedTo(j2));

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsConnectedTo_CyclicRoad()
        {
            var (j, r) = SetupCyclicRoad();

            Assert.IsTrue(r.IsConnectedTo(j));

            yield return null;
        }

        [UnityTest]
        public IEnumerator GetsOtherJunction_NormalRoad()
        {
            var (j1, j2, r) = SetupNormalRoad();

            Assert.AreEqual(r.GetOtherJunction(j1), j2);
            Assert.AreEqual(r.GetOtherJunction(j2), j1);

            yield return null;
        }

        [UnityTest]
        public IEnumerator GetsOtherJunction_TurnRoad()
        {
            var (j1, j2, r) = SetupTurnRoad();

            Assert.AreEqual(r.GetOtherJunction(j1), j2);
            Assert.AreEqual(r.GetOtherJunction(j2), j1);

            yield return null;
        }

        [UnityTest]
        public IEnumerator GetsOtherJunction_CyclicRoad()
        {
            var (j, r) = SetupCyclicRoad();

            Assert.AreEqual(r.GetOtherJunction(j), j);

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsTurn_NormalRoad()
        {
            var (_, _, r) = SetupNormalRoad();

            Assert.IsFalse(r.IsTurn(Vector3.zero));

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsTurn_TurnRoad()
        {
            var (_, _, r) = SetupTurnRoad();

            Assert.IsTrue(r.IsTurn(Vector3.zero));

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsTurn_CyclicRoad()
        {
            var (_, r) = SetupCyclicRoad();

            Assert.IsTrue(r.IsTurn(Vector3.zero));

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsCyclic_NormalRoad()
        {
            var (_, _, r) = SetupNormalRoad();

            Assert.IsFalse(r.IsCyclic());

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsCyclic_TurnRoad()
        {
            var (_, _, r) = SetupTurnRoad();

            Assert.IsFalse(r.IsCyclic());

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsCyclic_CyclicRoad()
        {
            var (_, r) = SetupCyclicRoad();

            Assert.IsTrue(r.IsCyclic());

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsOnRoadPath_NormalRoad()
        {
            var (_, _, r) = SetupNormalRoad();

            foreach (var p in r.path) Assert.IsTrue(r.IsOnRoadPath(p));

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsOnRoadPath_TurnRoad()
        {
            var (_, _, r) = SetupTurnRoad();

            foreach (var p in r.path) Assert.IsTrue(r.IsOnRoadPath(p));

            yield return null;
        }

        [UnityTest]
        public IEnumerator IsOnRoadPath_CyclicRoad()
        {
            var (_, r) = SetupCyclicRoad();

            foreach (var p in r.path) Assert.IsTrue(r.IsOnRoadPath(p));

            yield return null;
        }

        [UnityTest]
        public IEnumerator SplitsRoad_NormalRoad()
        {
            var (j1, j2, r) = SetupNormalRoad();
            List<Vector3> result;

            result = r.SplitPath(j1, Vector3.zero);
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual(result[0], Vector3.zero);

            result = r.SplitPath(j2, Vector3.zero);
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual(result[0], Vector3.zero);

            yield return null;
        }

        [UnityTest]
        public IEnumerator SplitsRoad_TurnRoad()
        {
            var (j1, j2, r) = SetupTurnRoad();
            List<Vector3> result;

            result = r.SplitPath(j1, Vector3.zero);
            Assert.AreEqual(result.Count, 2);
            Assert.IsTrue(result[0] == r.path.First() || result[0] == r.path.Last());
            Assert.AreEqual(result[1], Vector3.zero);

            result = r.SplitPath(j2, Vector3.zero);
            Assert.AreEqual(result.Count, 2);
            Assert.AreEqual(result[0], Vector3.zero);
            Assert.IsTrue(result[1] == r.path.First() || result[1] == r.path.Last());

            yield return null;
        }

        [UnityTest]
        public IEnumerator SplitsRoad_CyclicRoad()
        {
            var (j, r) = SetupCyclicRoad();
            List<Vector3> result;

            result = r.SplitPath(j, Vector3.zero);
            Assert.AreEqual(result.Count, 2);
            Assert.IsTrue(result[0] == r.path.First() || result[0] == r.path.Last());
            Assert.AreEqual(result[1], Vector3.zero);

            yield return null;
        }

        [UnityTest]
        public IEnumerator GetClosestEndPoint_NormalRoad()
        {
            var (_, _, r) = SetupNormalRoad();

            Assert.AreEqual(r.GetClosestEndPoint(Vector3.right), r.path[0]);

            yield return null;
        }

        [UnityTest]
        public IEnumerator GetClosestEndPoint_TurnRoad()
        {
            var (_, _, r) = SetupTurnRoad();

            Assert.AreEqual(r.GetClosestEndPoint(Vector3.right), r.path[2]);
            Assert.AreEqual(r.GetClosestEndPoint(Vector3.back), r.path[0]);

            yield return null;
        }

        [UnityTest]
        public IEnumerator GetClosestEndPoint_CyclicRoad()
        {
            var (_, r) = SetupCyclicRoad();

            Assert.AreEqual(r.GetClosestEndPoint(Vector3.forward), r.path[2]);
            Assert.AreEqual(r.GetClosestEndPoint(Vector3.right), r.path[0]);

            yield return null;
        }
        
        [UnityTest]
        public IEnumerator GetClosestEndIndex_NormalRoad()
        {
            var (_, _, r) = SetupNormalRoad();

            Assert.AreEqual(r.GetClosestEndIndex(Vector3.right), 0);

            yield return null;
        }

        [UnityTest]
        public IEnumerator GetClosestEndIndex_TurnRoad()
        {
            var (_, _, r) = SetupTurnRoad();

            Assert.AreEqual(r.GetClosestEndIndex(Vector3.right), 2);
            Assert.AreEqual(r.GetClosestEndIndex(Vector3.back), 0);

            yield return null;
        }

        [UnityTest]
        public IEnumerator GetClosestEndIndex_CyclicRoad()
        {
            var (_, r) = SetupCyclicRoad();

            Assert.AreEqual(r.GetClosestEndIndex(Vector3.forward), 2);
            Assert.AreEqual(r.GetClosestEndIndex(Vector3.right), 0);

            yield return null;
        }
    }
}