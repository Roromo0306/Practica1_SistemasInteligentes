using System.Collections.Generic;

public static class TwoOpt
{
    public static List<MazeNode> Improve(List<MazeNode> route)
    {
        if (route == null || route.Count < 4) return route;

        List<MazeNode> best = new(route);
        float bestCost = PathUtils.PathCost(best);

        bool improved = true;
        while (improved)
        {
            improved = false;

            for (int i = 1; i < best.Count - 2; i++)
            {
                for (int k = i + 1; k < best.Count - 1; k++)
                {
                    var candidate = Swap(best, i, k);
                    float candidateCost = PathUtils.PathCost(candidate);

                    if (candidateCost < bestCost)
                    {
                        best = candidate;
                        bestCost = candidateCost;
                        improved = true;
                    }
                }
            }
        }

        return best;
    }

    private static List<MazeNode> Swap(List<MazeNode> route, int i, int k)
    {
        List<MazeNode> result = new();

        for (int c = 0; c < i; c++) result.Add(route[c]);
        for (int c = k; c >= i; c--) result.Add(route[c]);
        for (int c = k + 1; c < route.Count; c++) result.Add(route[c]);

        return result;
    }
}