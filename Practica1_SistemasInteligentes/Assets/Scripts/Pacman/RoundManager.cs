using UnityEngine;
using System.Collections.Generic;
using System.Collections;
public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance;

    [SerializeField] private Transform pacman;
    [SerializeField] private Transform pacmanSpawn;
    [SerializeField] private List<GhostBase> ghosts;
    [SerializeField] private List<Transform> ghostSpawns;

    private void Awake()
    {
        Instance = this;
    }

    public IEnumerator ResetPositions()
    {
        yield return new WaitForSeconds(1f);

        pacman.position = pacmanSpawn.position;

        for (int i = 0; i < ghosts.Count && i < ghostSpawns.Count; i++)
        {
            ghosts[i].transform.position = ghostSpawns[i].position;
            ghosts[i].ResetGhostState();
        }
    }
}