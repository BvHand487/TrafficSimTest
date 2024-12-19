using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utils
{
    public static class Pathfinding
    {
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
            {
                if (road.IsTurn(road.path[k]))
                    vectorPath.AddRange(
                        Math.GetBezier(
                            new List<Vector3>() {
                                road.path[k - 1],
                                road.path[k],
                                road.path[k + 1]
                            },
                            segments: 7
                       )
                    );
                else
                    vectorPath.Add(road.path[k]);
            }

            return vectorPath;
        }

        public static List<Vector3> DiscretizeJunction(Junction junction, Road from, Road to)
        {
            List<Vector3> vectorPath = new List<Vector3>();
            Vector3 junctionPos = junction.obj.transform.position;

            if (from != null &&
                to != null &&
                new Road(new List<Vector3>() { from.path.Last(), junctionPos, to.path.First() }).IsTurn(junctionPos))
                vectorPath.AddRange(
                    Math.GetBezier(
                        new List<Vector3>() {
                            Math.GetMidpointVector(from.path.Last(), junctionPos),
                            junctionPos,
                            Math.GetMidpointVector(junctionPos, to.path.First())
                        },
                        segments: 7
                    )
                );
            else
                vectorPath.Add(junctionPos);

            return vectorPath;
        }

        public static List<Vector3> JunctionToVectorPath(List<Junction> junctionPath)
        {
            List<Vector3> vectorPath = new List<Vector3>();

            Road prevRoad = null, nextRoad = null;

            for (int i = 0; i < junctionPath.Count - 1; ++i)
            {
                nextRoad = Junction.GetCommonRoad(junctionPath[i], junctionPath[i + 1]);
                if (Vector3.Distance(junctionPath[i].obj.transform.position, nextRoad.path[0]) >
                    Vector3.Distance(junctionPath[i + 1].obj.transform.position, nextRoad.path[0]))
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
