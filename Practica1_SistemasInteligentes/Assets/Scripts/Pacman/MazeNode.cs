using System.Collections.Generic;
using UnityEngine;

public class MazeNode : MonoBehaviour
{
    public int id;
    public Vector2Int gridPosition;
    public List<MazeEdge> edges = new();
}

[System.Serializable]
public class MazeEdge
{
    public MazeNode target;
    public float cost;
}