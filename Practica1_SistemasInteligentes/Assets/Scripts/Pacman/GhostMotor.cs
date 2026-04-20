using System.Collections.Generic;
using UnityEngine;

public class GhostMotor : MonoBehaviour
{
    [SerializeField] private float speed = 3.5f;
    [SerializeField] private float arriveDistance = 0.05f;

    private List<MazeNode> currentPath = new();
    private int currentIndex = 0;

    public void SetPath(List<MazeNode> path)
    {
        currentPath = path ?? new List<MazeNode>();
        currentIndex = 0;
    }

    private void Update()
    {
        if (currentPath == null || currentPath.Count == 0 || currentIndex >= currentPath.Count)
            return;

        Vector3 target = currentPath[currentIndex].transform.position;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target) <= arriveDistance)
            currentIndex++;
    }

    public bool HasFinishedPath()
    {
        return currentPath == null || currentIndex >= currentPath.Count;
    }
}