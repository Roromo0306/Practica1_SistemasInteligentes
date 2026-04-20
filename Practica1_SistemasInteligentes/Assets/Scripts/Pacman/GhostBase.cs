using UnityEngine;

public class GhostBase : MonoBehaviour
{
    public enum GhostMode { Chase, Scatter, Frightened, Eaten }
    public GhostMode mode = GhostMode.Chase;
    private void Start()
    {
        GhostManager.Instance.RegisterGhost(this);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (mode == GhostMode.Frightened)
        {
            GameManager.Instance.AddScore(200);
            mode = GhostMode.Eaten;
        }
        else if (mode != GhostMode.Eaten)
        {
            GameManager.Instance.LoseLife();
        }
    }

    public void SetFrightened(float time)
    {

    }
}