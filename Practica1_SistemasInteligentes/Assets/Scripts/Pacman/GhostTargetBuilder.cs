using System.Collections.Generic;
//using System.Diagnostics;
using System.Linq;
using UnityEngine;
public enum GhostRole
{
    DirectChase,
    Ambush,
    SideCover,
    Blocker
}
public class GhostTargetBuilder : MonoBehaviour
{
    [SerializeField] private Transform pacman;
    [SerializeField] private Vector2 predictionOffset = new Vector2(2f, 0f);
    [SerializeField] private Transform assignedCorner;
    [SerializeField] private GhostRole role;

    public List<MazeNode> BuildCandidateNodes(Transform ghost)
    {
        List<MazeNode> result = new();

        MazeNode ghostNode = MazeGraph.Instance.GetClosestNode(ghost.position);
        MazeNode pacNode = MazeGraph.Instance.GetClosestNode(pacman.position);

        result.Add(ghostNode);
        result.Add(pacNode);

        Vector3 predicted = pacman.position + (Vector3)predictionOffset;
        MazeNode futurePacNode = MazeGraph.Instance.GetClosestNode(predicted);
        if (!result.Contains(futurePacNode))
            result.Add(futurePacNode);

        if (assignedCorner != null)
        {
            MazeNode cornerNode = MazeGraph.Instance.GetClosestNode(assignedCorner.position);
            if (!result.Contains(cornerNode))
                result.Add(cornerNode);
        }

        var nearby = MazeGraph.Instance.nodes
            .OrderBy(n => Vector2.Distance(n.transform.position, pacman.position))
            .Take(3);

        foreach (var n in nearby)
            if (!result.Contains(n))
                result.Add(n);


        Debug.Log($"{gameObject.name} usa esquina: {(assignedCorner != null ? assignedCorner.name : "NULL")}");
       
        return result;
    }
}