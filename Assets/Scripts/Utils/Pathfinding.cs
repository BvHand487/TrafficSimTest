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
            List<Road> commonRoads = start.adjacentRoads.Intersect(end.adjacentRoads).ToList();

            if (commonRoads.Count == 0)
            {
                Junction closestStartJunction = Building.GetClosestJunction(start);
                Junction closestEndJunction = Building.GetClosestJunction(end);

                List<Junction> junctionPath = FindBestPath(closestStartJunction, closestEndJunction);

                Road[] roadEnds = new Road[2];

                if (junctionPath.Count > 1)
                {
                    roadEnds[0] = Junction.GetCommonRoad(junctionPath.First(), junctionPath[1]);
                    roadEnds[1] = Junction.GetCommonRoad(junctionPath[junctionPath.Count - 2], junctionPath.Last());

                    if (end.adjacentRoads.Contains(roadEnds.Last()))
                        junctionPath.RemoveAt(junctionPath.Count - 1);

                    if (start.adjacentRoads.Contains(roadEnds.First()))
                        junctionPath.RemoveAt(0);
                };

                List<Vector3> path = JunctionToVectorPath(junctionPath);

                roadEnds[0] = start.adjacentRoads.Intersect(junctionPath.First().roads).First();
                roadEnds[roadEnds.Count() - 1] = end.adjacentRoads.Intersect(junctionPath.Last().roads).First();


                var buildingExitPoint = Math.GetClosestVector(start.obj.transform.position, roadEnds.First().path);

                List<Vector3> startToRoadPath = new List<Vector3>();
                startToRoadPath.Add(start.obj.transform.position);
                startToRoadPath.Add(buildingExitPoint);

                var pathToStartJunction = roadEnds.First().SplitPath(junctionPath.First(), buildingExitPoint);
                if (pathToStartJunction.Last() == buildingExitPoint)
                    pathToStartJunction.Reverse();
                startToRoadPath.AddRange(pathToStartJunction);


                buildingExitPoint = Math.GetClosestVector(end.obj.transform.position, roadEnds.Last().path);

                List<Vector3> endToRoadPath = new List<Vector3>();
                var pathToEndJunction = roadEnds.Last().SplitPath(junctionPath.Last(), buildingExitPoint);
                if (pathToEndJunction.First() == buildingExitPoint)
                    pathToEndJunction.Reverse();
                endToRoadPath.AddRange(pathToEndJunction);

                endToRoadPath.Add(buildingExitPoint);
                endToRoadPath.Add(end.obj.transform.position);


                path.InsertRange(0, startToRoadPath);
                path.AddRange(endToRoadPath);

                return path;
            }
            else
            {
                Road commonRoad = commonRoads.First();
                List<Vector3> path = new List<Vector3>();

                Vector3 roadStart = Math.GetClosestVector(start.obj.transform.position, commonRoad.path);
                Vector3 roadEnd = Math.GetClosestVector(end.obj.transform.position, commonRoad.path);

                List<Vector3> startToEndPath = new List<Vector3>();
                startToEndPath.Add(start.obj.transform.position);
                startToEndPath.Add(roadStart);

                var roadToRoadPath = commonRoad.SplitPath(roadStart, roadEnd);
                if (roadToRoadPath.First() == roadEnd)
                    roadToRoadPath.Reverse();
                startToEndPath.AddRange(roadToRoadPath);

                startToEndPath.Add(roadEnd);
                startToEndPath.Add(end.obj.transform.position);

                path.AddRange(startToEndPath);

                return path;
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
            Vector3 posA = a.obj.transform.position;
            Vector3 posB = b.obj.transform.position;

            return Vector3.Distance(posA, posB);
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
            List<Vector3> vectorPath = new List<Vector3>();

            for (int k = 0; k < road.path.Count; ++k)
                vectorPath.Add(road.path[k]);

            return vectorPath;
        }

        public static List<Vector3> DiscretizeJunction(Junction junction, Road from, Road to)
        {
            List<Vector3> vectorPath = new List<Vector3>();
            Vector3 junctionPos = junction.obj.transform.position;

            vectorPath.Add(junctionPos);

            return vectorPath;
        }

        public static List<Vector3> JunctionToVectorPath(List<Junction> junctionPath)
        {
            List<Vector3> vectorPath = new List<Vector3>();

            Road prevRoad = null, nextRoad;

            for (int i = 0; i < junctionPath.Count - 1; ++i)
            {
                nextRoad = Junction.GetCommonRoad(junctionPath[i], junctionPath[i + 1]);
                if (Vector3.Distance(junctionPath[i].obj.transform.position, nextRoad.path.First()) >
                    Vector3.Distance(junctionPath[i + 1].obj.transform.position, nextRoad.path.First()))
                    nextRoad.path.Reverse();

                vectorPath.AddRange(DiscretizeJunction(junctionPath[i], prevRoad, nextRoad));
                vectorPath.AddRange(DiscretizeRoad(nextRoad));

                prevRoad = nextRoad;
            }

            vectorPath.Add(junctionPath.Last().obj.transform.position);
            return vectorPath;
        }
    }

}
