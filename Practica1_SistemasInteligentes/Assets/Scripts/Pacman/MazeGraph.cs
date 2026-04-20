using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MazeGraph : MonoBehaviour
{
    public static MazeGraph Instance;
    public List<MazeNode> nodes = new();

    private void Awake()
    {
        Instance = this;
        nodes = FindObjectsOfType<MazeNode>().OrderBy(n => n.id).ToList();
    }

    public MazeNode GetClosestNode(Vector3 worldPos)
    {
        MazeNode best = null;
        float bestDist = float.MaxValue;

        foreach (var n in nodes)
        {
            float d = Vector2.Distance(worldPos, n.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = n;
            }
        }

        return best;
    }
}