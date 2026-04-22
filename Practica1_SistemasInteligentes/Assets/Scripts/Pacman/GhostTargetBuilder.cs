using System.Collections.Generic;
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

    [Header("Opcional")]
    [SerializeField] private List<Transform> powerPellets = new();
    [SerializeField] private List<Transform> tunnelPoints = new();

    public List<MazeNode> BuildCandidateNodes(Transform ghost)
    {
        List<MazeNode> result = new();

        MazeNode ghostNode = MazeGraph.Instance.GetClosestNode(ghost.position);
        MazeNode pacNode = MazeGraph.Instance.GetClosestNode(pacman.position);

        Vector3 predicted = pacman.position + (Vector3)predictionOffset;
        MazeNode futurePacNode = MazeGraph.Instance.GetClosestNode(predicted);

        MazeNode cornerNode = null;
        if (assignedCorner != null)
            cornerNode = MazeGraph.Instance.GetClosestNode(assignedCorner.position);

        var nearbyToPacman = MazeGraph.Instance.nodes
            .OrderBy(n => Vector2.Distance(n.transform.position, pacman.position))
            .Take(6)
            .ToList();

        AddIfValid(result, ghostNode);

        switch (role)
        {
            case GhostRole.DirectChase:
                // Rojo: va a por Pacman casi directo
                AddIfValid(result, pacNode);

                foreach (var node in nearbyToPacman.Take(2))
                    AddIfValid(result, node);

                AddIfValid(result, cornerNode);
                break;

            case GhostRole.Ambush:
                // Rosa: intenta interceptar hacia delante
                AddIfValid(result, futurePacNode);

                foreach (var node in nearbyToPacman
                             .OrderBy(n => Vector2.Distance(n.transform.position, predicted))
                             .Take(2))
                {
                    AddIfValid(result, node);
                }

                AddIfValid(result, pacNode);
                AddIfValid(result, cornerNode);
                break;

            case GhostRole.SideCover:
                // Azul: cubre laterales/intersecciones cercanas, no tan directo
                foreach (var node in nearbyToPacman.Skip(1).Take(3))
                    AddIfValid(result, node);

                AddIfValid(result, futurePacNode);
                AddIfValid(result, cornerNode);
                break;

            case GhostRole.Blocker:
                // Naranja: bloquea rutas de escape
                AddIfValid(result, futurePacNode);
                AddIfValid(result, cornerNode);

                foreach (var node in nearbyToPacman.Skip(2).Take(2))
                    AddIfValid(result, node);

                MazeNode pelletNode = GetClosestSpecialNode(powerPellets, pacman.position);
                AddIfValid(result, pelletNode);

                MazeNode tunnelNode = GetClosestSpecialNode(tunnelPoints, pacman.position);
                AddIfValid(result, tunnelNode);
                break;
        }

        // Limpieza final de duplicados/null
        result = result
            .Where(n => n != null)
            .Distinct()
            .ToList();

        Debug.Log(
            $"{gameObject.name} | role: {role} | corner: {(assignedCorner != null ? assignedCorner.name : "NULL")} | candidates: {string.Join(", ", result.Select(n => n.id))}"
        );

        return result;
    }

    private void AddIfValid(List<MazeNode> list, MazeNode node)
    {
        if (node == null) return;
        if (!list.Contains(node))
            list.Add(node);
    }

    private MazeNode GetClosestSpecialNode(List<Transform> points, Vector3 referencePosition)
    {
        if (points == null || points.Count == 0)
            return null;

        Transform closest = points
            .Where(t => t != null)
            .OrderBy(t => Vector2.Distance(t.position, referencePosition))
            .FirstOrDefault();

        if (closest == null)
            return null;

        return MazeGraph.Instance.GetClosestNode(closest.position);
    }
}