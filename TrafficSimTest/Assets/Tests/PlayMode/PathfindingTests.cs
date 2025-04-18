using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.TestTools;
using Utils;

namespace Tests.PlayMode
{
    public class PathfindingTests
    {
        /*
         * Tests the simple case of getting from point A to point B:
         *
         *    # _ #    # - junction, _ - road
         */
        [UnityTest]
        public IEnumerator ChoosesSinglePath()
        {
            var j1Obj = new GameObject();
            Junction j1 = j1Obj.AddComponent<Junction>();
            j1.transform.position = new Vector3(-GameManager.TileSize, 0f, 0f);
            
            var j2Obj = new GameObject();
            Junction j2 = j2Obj.AddComponent<Junction>();
            j2.transform.position = new Vector3(GameManager.TileSize, 0f, 0f);

            Road r = new Road(
                null,
                new List<Vector3> { Vector3.zero },
                j1,
                j2
            );
            
            j1.Initialize(new List<Road> { r });
            j2.Initialize(new List<Road> { r });
            
            var path = Pathfinding.FindBestPath(j1, j2);
            Assert.AreEqual(path.Count, 2);
            Assert.AreEqual(path[0], j1);
            Assert.AreEqual(path[1], j2);
            
            yield return null;
        }
        
        /*
         * Tests the case where there are multiple roads to get from junction A to junction B:
         * 
         *      _ 
         *    |   |    # - junction
         *    # _ #    _,| - road
         */
        [UnityTest]
        public IEnumerator ChoosesShorterRoad()
        {
            var j1Obj = new GameObject();
            Junction j1 = j1Obj.AddComponent<Junction>();
            j1.transform.position = new Vector3(-GameManager.TileSize, 0f, 0f);
            
            var j2Obj = new GameObject();
            Junction j2 = j2Obj.AddComponent<Junction>();
            j2.transform.position = new Vector3(GameManager.TileSize, 0f, 0f);

            Road r1 = new Road(
                null,
                new List<Vector3> { new Vector3(0f, 0f, 0f) },
                j1,
                j2
            );
            
            Road r2 = new Road(
                null,
                new List<Vector3>
                {
                    new Vector3(-GameManager.TileSize, 0f, GameManager.TileSize),
                    new Vector3(0f, 0f, GameManager.TileSize),
                    new Vector3(GameManager.TileSize, 0f, GameManager.TileSize),
                },
                j1,
                j2
            );
            
            j1.Initialize(new List<Road> { r1, r2 });
            j2.Initialize(new List<Road> { r1, r2 });
            
            var path = Pathfinding.FindBestPath(j1, j2);
            Assert.AreEqual(path.Count, 2);
            Assert.AreEqual(path[0], j1);
            Assert.AreEqual(path[1], j2);
            
            var roads = Pathfinding.JunctionToRoadPath(path);
            Assert.AreEqual(roads.Count, 1);
            Assert.AreEqual(roads[0], r1);
            
            yield return null;
        }
        
        /*
         * Tests the case where there are multiple ways to get from point A to point B:
         * 
         *    # _ #    
         *    |   |    # - junction
         *    # _ #    _,| - road
         */
        [UnityTest]
        public IEnumerator ChoosesShorterPath()
        {
            var j1Obj = new GameObject();
            Junction j1 = j1Obj.AddComponent<Junction>();
            j1.transform.position = new Vector3(-GameManager.TileSize, 0f, -GameManager.TileSize);
            
            var j2Obj = new GameObject();
            Junction j2 = j2Obj.AddComponent<Junction>();
            j2.transform.position = new Vector3(GameManager.TileSize, 0f, -GameManager.TileSize);
            
            var j3Obj = new GameObject();
            Junction j3 = j3Obj.AddComponent<Junction>();
            j1.transform.position = new Vector3(-GameManager.TileSize, 0f, GameManager.TileSize);
            
            var j4Obj = new GameObject();
            Junction j4 = j4Obj.AddComponent<Junction>();
            j2.transform.position = new Vector3(GameManager.TileSize, 0f, GameManager.TileSize);

            Road r12 = new Road(
                null,
                new List<Vector3> { new Vector3(0f, 0f, -GameManager.TileSize) },
                j1,
                j2
            );
            
            Road r24 = new Road(
                null,
                new List<Vector3> { new Vector3(GameManager.TileSize, 0f, 0f) },
                j2,
                j4
            );
            
            Road r34 = new Road(
                null,
                new List<Vector3> { new Vector3(0f, 0f, GameManager.TileSize) },
                j3,
                j4
            );
            
            Road r13 = new Road(
                null,
                new List<Vector3> { new Vector3(-GameManager.TileSize, 0f, 0f) },
                j1,
                j3
            );
            
            j1.Initialize(new List<Road> { r12, r13 });
            j2.Initialize(new List<Road> { r24, r12 });
            j3.Initialize(new List<Road> { r13, r34 });
            j4.Initialize(new List<Road> { r24, r34 });
            
            List<Junction> path;
            
            path = Pathfinding.FindBestPath(j1, j2);
            Assert.AreEqual(path.Count, 2);
            Assert.AreEqual(path[0], j1);
            Assert.AreEqual(path[1], j2);
            
            path = Pathfinding.FindBestPath(j2, j4);
            Assert.AreEqual(path.Count, 2);
            Assert.AreEqual(path[0], j2);
            Assert.AreEqual(path[1], j4);
            
            path = Pathfinding.FindBestPath(j3, j4);
            Assert.AreEqual(path.Count, 2);
            Assert.AreEqual(path[0], j3);
            Assert.AreEqual(path[1], j4);
            
            path = Pathfinding.FindBestPath(j1, j3);
            Assert.AreEqual(path.Count, 2);
            Assert.AreEqual(path[0], j1);
            Assert.AreEqual(path[1], j3);
            
            yield return null;
        }
    }
}
