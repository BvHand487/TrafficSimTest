using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace Utils
{
    public static class Pathfinding
    {
        public static List<Vector3> FindCarPath(Building start, Building end)
        {
            List<Road> commonRoads = start.spawnPoints.Keys.Intersect(end.spawnPoints.Keys).ToList();

            if (commonRoads.Count == 0)
            {
                List<Junction> junctionPath = FindBestPath(start.closestJunction, end.closestJunction);

                Road[] roadEnds = new Road[2];

                if (junctionPath.Count > 1)
                {
                    roadEnds[0] = Junction.GetCommonRoad(junctionPath.First(), junctionPath[1]);
                    roadEnds[1] = Junction.GetCommonRoad(junctionPath[junctionPath.Count - 2], junctionPath.Last());

                    if (end.spawnPoints.Keys.Contains(roadEnds.Last()))
                        junctionPath.RemoveAt(junctionPath.Count - 1);

                    if (start.spawnPoints.Keys.Contains(roadEnds.First()))
                        junctionPath.RemoveAt(0);
                };

                List<Vector3> path = JunctionToVectorPath(junctionPath);

                roadEnds[0] = start.spawnPoints.Keys.Intersect(junctionPath.First().roads).First();
                roadEnds[roadEnds.Count() - 1] = end.spawnPoints.Keys.Intersect(junctionPath.Last().roads).First();

                var buildingExitPoint = start.spawnPoints[roadEnds.First()];

                List<Vector3> startToRoadPath = new List<Vector3>();
                startToRoadPath.Add(start.obj.transform.position);

                var pathToStartJunction = roadEnds.First().SplitPath(junctionPath.First(), buildingExitPoint);
                if (!pathToStartJunction.Contains(buildingExitPoint))
                    if (Vector3.Distance(buildingExitPoint, pathToStartJunction.First()) <
                        Vector3.Distance(buildingExitPoint, pathToStartJunction.Last()))
                        pathToStartJunction.Insert(0, buildingExitPoint);
                    else
                        pathToStartJunction.Add(buildingExitPoint);

                if (pathToStartJunction.Last() == buildingExitPoint)
                    pathToStartJunction.Reverse();

                startToRoadPath.AddRange(pathToStartJunction);

                buildingExitPoint = end.spawnPoints[roadEnds.Last()];

                List<Vector3> endToRoadPath = new List<Vector3>();

                var pathToEndJunction = roadEnds.Last().SplitPath(junctionPath.Last(), buildingExitPoint);
                if (!pathToEndJunction.Contains(buildingExitPoint))
                    if (Vector3.Distance(buildingExitPoint, pathToEndJunction.First()) <
                        Vector3.Distance(buildingExitPoint, pathToEndJunction.Last()))
                        pathToEndJunction.Insert(0, buildingExitPoint);
                    else
                        pathToEndJunction.Add(buildingExitPoint);

                if (pathToEndJunction.First() == buildingExitPoint)
                    pathToEndJunction.Reverse();

                endToRoadPath.AddRange(pathToEndJunction);
                endToRoadPath.Add(end.obj.transform.position);

                path.InsertRange(0, startToRoadPath);
                path.AddRange(endToRoadPath);

                return Math.SmoothVectorPath(path, 5, 5);
            }
            else
            {
                Road commonRoad = commonRoads.First();

                Vector3 roadStart = Utils.Random.Select(start.spawnPoints.Values.Where(p => commonRoad.path.Contains(p)));
                Vector3 roadEnd = Utils.Random.Select(end.spawnPoints.Values.Where(p => commonRoad.path.Contains(p)));

                List<Vector3> startToEndPath = new List<Vector3>();
                startToEndPath.Add(start.obj.transform.position);

                var roadToRoadPath = commonRoad.SplitPath(roadStart, roadEnd);
                
                if (!roadToRoadPath.Contains(roadStart))
                    if (Vector3.Distance(roadStart, roadToRoadPath.First()) <
                        Vector3.Distance(roadStart, roadToRoadPath.Last()))
                        roadToRoadPath.Insert(0, roadStart);
                    else
                        roadToRoadPath.Add(roadStart);

                if (!roadToRoadPath.Contains(roadEnd))
                    if (Vector3.Distance(roadEnd, roadToRoadPath.First()) <
                        Vector3.Distance(roadEnd, roadToRoadPath.Last()))
                        roadToRoadPath.Insert(0, roadEnd);
                    else
                        roadToRoadPath.Add(roadEnd);

                if (roadToRoadPath.First() == roadEnd)
                    roadToRoadPath.Reverse();

                startToEndPath.AddRange(roadToRoadPath);
                startToEndPath.Add(end.obj.transform.position);

                return Math.SmoothVectorPath(startToEndPath, 5, 5);
            }
        }

        // A* algorithm for best path
        public static List<Junction> FindBestPath(Junction start, Junction end)
        {
            var openSet = new SortedSet<(float fScore, Junction junction)>(Comparer<(float fScore, Junction junction)>.Create((a, b) =>
            a.fScore == b.fScore ? a.junction.GetHashCode().CompareTo(b.junction.GetHashCode()) : a.fScore.CompareTo(b.fScore)));

            var cameFrom = new Dictionary<Junction, Junction>();
            var gScore = new Dictionary<Junction, float>();
            var fScore = new Dictionary<Junction, float>();

            gScore[start] = 0;
            fScore[start] = Heuristic(start, end);

            openSet.Add((fScore[start], start));

            while (openSet.Count > 0)
            {
                var current = openSet.Min.junction;
                if (current == end)
                    return ReconstructPath(cameFrom, current);

                openSet.Remove(openSet.Min);

                foreach (var road in current.roads)
                {
                    var neighbor = road.GetOtherJunction(current);
                    float tentativeGScore = gScore[current] + road.path.Count;

                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = tentativeGScore + Heuristic(neighbor, end);

                        if (!openSet.Any(item => item.junction == neighbor))
                        {
                            openSet.Add((fScore[neighbor], neighbor));
                        }
                    }
                }
            }

            return null; // No path found
        }

        // Heuristic function: Straight-line distance between two junctions
        private static float Heuristic(Junction a, Junction b)
        {
            return Junction.GetCommonRoad(a, b)?.length ?? Vector3.Distance(a.obj.transform.position, b.obj.transform.position);
        }

        // Reconstructs the path from end to start by tracing the cameFrom dictionary
        private static List<Junction> ReconstructPath(Dictionary<Junction, Junction> cameFrom, Junction current)
        {
            var path = new List<Junction>();
            while (current != null)
            {
                path.Add(current);
                cameFrom.TryGetValue(current, out current);
            }
            path.Reverse();
            return path;
        }

        public static List<Vector3> DiscretizeRoad(Road road)
        {
            return road.path;
        }

        public static List<Vector3> DiscretizeJunction(Junction junction)
        {
            return new List<Vector3> { junction.obj.transform.position };
        }

        public static List<Vector3> JunctionToVectorPath(List<Junction> junctionPath)
        {
            List<Vector3> vectorPath = new List<Vector3>();

            Road nextRoad;

            for (int i = 0; i < junctionPath.Count - 1; ++i)
            {
                nextRoad = Junction.GetCommonRoad(junctionPath[i], junctionPath[i + 1]);
                if (Vector3.Distance(junctionPath[i].obj.transform.position, nextRoad.path.First()) >
                    Vector3.Distance(junctionPath[i + 1].obj.transform.position, nextRoad.path.First()))
                    nextRoad.path.Reverse();

                vectorPath.AddRange(DiscretizeJunction(junctionPath[i]));
                vectorPath.AddRange(DiscretizeRoad(nextRoad));
            }

            vectorPath.Add(junctionPath.Last().obj.transform.position);
            return vectorPath;
        }
    }

}
