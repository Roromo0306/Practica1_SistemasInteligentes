using System.Collections.Generic;
using UnityEngine;

public static class PathUtils
{
    public static float PathCost(List<MazeNode> path)
    {
        if (path == null || path.Count < 2) return float.MaxValue;

        float cost = 0f;

        for (int i = 0; i < path.Count - 1; i++)
        {
            bool found = false;
            foreach (var e in path[i].edges)
            {
                if (e.target == path[i + 1])
                {
                    cost += e.cost;
                    found = true;
                    break;
                }
            }

            if (!found)
                cost += Vector2.Distance(path[i].transform.position, path[i + 1].transform.position);
        }

        return cost;
    }
}