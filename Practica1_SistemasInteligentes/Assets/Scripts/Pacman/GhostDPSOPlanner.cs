using System.Collections.Generic;
using UnityEngine;

public class GhostDPSOPlanner : MonoBehaviour
{
    [SerializeField] private GhostMotor motor;
    [SerializeField] private GhostTargetBuilder targetBuilder;

    [Header("DPSO")]
    [SerializeField] private int swarmSize = 12;
    [SerializeField] private int iterations = 8;
    [SerializeField] private float keepVelocityProb = 0.4f;
    [SerializeField] private float pbestProb = 0.8f;
    [SerializeField] private float gbestProb = 0.9f;
    [SerializeField] private float replanInterval = 0.6f;

    private float timer;

    private class Particle
    {
        public List<MazeNode> route = new();
        public List<(int, int)> velocity = new();
        public List<MazeNode> pbest = new();
        public float pbestCost = float.MaxValue;
    }

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
        List<MazeNode> candidates = targetBuilder.BuildCandidateNodes(transform);
        if (candidates.Count < 2) return;

        List<Particle> swarm = InitializeSwarm(candidates);

        List<MazeNode> gbest = null;
        float gbestCost = float.MaxValue;

        foreach (var p in swarm)
        {
            float c = PathUtils.PathCost(p.route);
            p.pbest = new List<MazeNode>(p.route);
            p.pbestCost = c;

            if (c < gbestCost)
            {
                gbest = new List<MazeNode>(p.route);
                gbestCost = c;
            }
        }

        for (int it = 0; it < iterations; it++)
        {
            foreach (var p in swarm)
            {
                var toPbest = DiffAsSwaps(p.route, p.pbest);
                var toGbest = DiffAsSwaps(p.route, gbest);

                List<(int, int)> newVelocity = new();

                foreach (var s in p.velocity)
                    if (Random.value < keepVelocityProb) newVelocity.Add(s);

                foreach (var s in toPbest)
                    if (Random.value < pbestProb) newVelocity.Add(s);

                foreach (var s in toGbest)
                    if (Random.value < gbestProb) newVelocity.Add(s);

                p.velocity = newVelocity;
                ApplyVelocity(p.route, p.velocity);

                p.route = TwoOpt.Improve(p.route);

                float cost = PathUtils.PathCost(p.route);

                if (cost < p.pbestCost)
                {
                    p.pbest = new List<MazeNode>(p.route);
                    p.pbestCost = cost;
                }

                if (cost < gbestCost)
                {
                    gbest = new List<MazeNode>(p.route);
                    gbestCost = cost;
                }
            }
        }

        if (gbest != null)
            motor.SetPath(gbest);
    }

    private List<Particle> InitializeSwarm(List<MazeNode> candidates)
    {
        List<Particle> swarm = new();

        for (int i = 0; i < swarmSize; i++)
        {
            Particle p = new();
            p.route = new List<MazeNode>(candidates);

            for (int j = 1; j < p.route.Count; j++)
            {
                int r = Random.Range(j, p.route.Count);
                (p.route[j], p.route[r]) = (p.route[r], p.route[j]);
            }

            swarm.Add(p);
        }

        return swarm;
    }

    private List<(int, int)> DiffAsSwaps(List<MazeNode> from, List<MazeNode> to)
    {
        List<(int, int)> swaps = new();
        List<MazeNode> temp = new(from);

        for (int i = 0; i < temp.Count; i++)
        {
            if (temp[i] == to[i]) continue;

            int j = temp.FindIndex(i + 1, x => x == to[i]);
            if (j >= 0)
            {
                swaps.Add((i, j));
                (temp[i], temp[j]) = (temp[j], temp[i]);
            }
        }

        return swaps;
    }

    private void ApplyVelocity(List<MazeNode> route, List<(int, int)> velocity)
    {
        foreach (var s in velocity)
        {
            if (s.Item1 >= 0 && s.Item1 < route.Count && s.Item2 >= 0 && s.Item2 < route.Count)
                (route[s.Item1], route[s.Item2]) = (route[s.Item2], route[s.Item1]);
        }
    }
}