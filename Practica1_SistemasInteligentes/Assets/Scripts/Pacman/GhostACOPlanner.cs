using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GhostACOPlanner : MonoBehaviour
{
    [SerializeField] private GhostMotor motor;
    [SerializeField] private GhostTargetBuilder targetBuilder;

    [Header("ACO")]
    [SerializeField] private int antCount = 12;
    [SerializeField] private int iterations = 6;
    [SerializeField] private float alpha = 1.5f;
    [SerializeField] private float beta = 3f;
    [SerializeField] private float rho = 0.2f;
    [SerializeField] private float Q = 1f;
    [SerializeField] private float replanInterval = 0.5f;

    private float timer;
    private Dictionary<(int, int), float> pheromones = new();

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= replanInterval || motor.HasFinishedPath())
        {
            timer = 0f;
            Replan();
        }
    }

    private void Replan()
    {
        List<MazeNode> nodes = targetBuilder.BuildCandidateNodes(transform);
        if (nodes.Count < 2) return;

        InitializePheromones(nodes);

        List<MazeNode> bestPath = null;
        float bestCost = float.MaxValue;

        for (int it = 0; it < iterations; it++)
        {
            List<(List<MazeNode> path, float cost)> solutions = new();

            for (int k = 0; k < antCount; k++)
            {
                var path = BuildPath(nodes);
                float cost = PathUtils.PathCost(path);
                solutions.Add((path, cost));

                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestPath = path;
                }
            }

            Evaporate();
            Deposit(solutions);
        }

        if (bestPath != null)
        {
            bestPath = TwoOpt.Improve(bestPath);
            Debug.Log($"{gameObject.name} bestPath: {string.Join(" -> ", bestPath.Select(n => n.id))}"); motor.SetPath(bestPath);
        }
    }

    private void InitializePheromones(List<MazeNode> nodes)
    {
        foreach (var a in nodes)
        {
            foreach (var b in nodes)
            {
                if (a == b) continue;
                var key = (a.id, b.id);
                if (!pheromones.ContainsKey(key))
                    pheromones[key] = 1f;
            }
        }
    }

    private List<MazeNode> BuildPath(List<MazeNode> candidates)
    {
        List<MazeNode> unvisited = new(candidates);
        List<MazeNode> path = new();

        MazeNode current = unvisited[0];
        path.Add(current);
        unvisited.Remove(current);

        while (unvisited.Count > 0)
        {
            MazeNode next = SelectNext(current, unvisited);
            path.Add(next);
            unvisited.Remove(next);
            current = next;
        }

        return path;
    }

    private MazeNode SelectNext(MazeNode current, List<MazeNode> unvisited)
    {
        float total = 0f;
        List<(MazeNode node, float prob)> probs = new();

        foreach (var n in unvisited)
        {
            if (n == current || n.id == current.id)
                continue;

            if (!pheromones.TryGetValue((current.id, n.id), out float tau))
                tau = 1f;
            float dist = Vector2.Distance(current.transform.position, n.transform.position);
            float eta = 1f / Mathf.Max(dist, 0.001f);

            float value = Mathf.Pow(tau, alpha) * Mathf.Pow(eta, beta);
            probs.Add((n, value));
            total += value;
        }
        if (probs.Count == 0)
            return null;
        float r = Random.value * total;
        float acc = 0f;

        foreach (var p in probs)
        {
            acc += p.prob;
            if (r <= acc) return p.node;
        }

        return probs[^1].node;
    }

    private void Evaporate()
    {
        var keys = new List<(int, int)>(pheromones.Keys);
        foreach (var key in keys)
            pheromones[key] *= (1f - rho);
    }

    private void Deposit(List<(List<MazeNode> path, float cost)> solutions)
    {
        foreach (var s in solutions)
        {
            if (s.path == null || s.path.Count < 2) continue;

            float delta = Q / Mathf.Max(s.cost, 0.001f);
            for (int i = 0; i < s.path.Count - 1; i++)
            {
                var key = (s.path[i].id, s.path[i + 1].id);
                pheromones[key] += delta;
            }
        }
    }
}