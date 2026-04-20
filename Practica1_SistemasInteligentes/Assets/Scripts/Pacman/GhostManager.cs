using UnityEngine;
using System.Collections.Generic;
public class GhostManager : MonoBehaviour
{
    public static GhostManager Instance;

    private List<GhostBase> ghosts = new List<GhostBase>();

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterGhost(GhostBase ghost)
    {
        if (!ghosts.Contains(ghost))
            ghosts.Add(ghost);
    }

    public void SetFrightenedMode(float time)
    {
        foreach (var ghost in ghosts)
            ghost.SetFrightened(time);
    }
}