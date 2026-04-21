using System.Collections;
using UnityEngine;

public class GhostBase : MonoBehaviour
{
    public enum GhostMode { Chase, Scatter, Frightened, Eaten }
    public GhostMode mode = GhostMode.Chase;

    private Coroutine frightenedCoroutine;

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

            if (TryGetComponent<GhostMotor>(out var motor))
            {
                motor.SetPath(null);
            }
        }
        else if (mode != GhostMode.Eaten)
        {
            GameManager.Instance.LoseLife();
        }
    }

    public void ResetGhostState()
    {
        if (frightenedCoroutine != null)
        {
            StopCoroutine(frightenedCoroutine);
            frightenedCoroutine = null;
        }

        mode = GhostMode.Chase;

        if (TryGetComponent<GhostMotor>(out var motor))
        {
            motor.SetPath(null);
        }

        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.velocity = Vector2.zero;
        }
    }

    public void SetFrightened(float time)
    {
        if (mode == GhostMode.Eaten)
            return;

        if (frightenedCoroutine != null)
        {
            StopCoroutine(frightenedCoroutine);
        }

        frightenedCoroutine = StartCoroutine(FrightenedRoutine(time));
    }

    private IEnumerator FrightenedRoutine(float time)
    {
        mode = GhostMode.Frightened;

        yield return new WaitForSeconds(time);

        if (mode == GhostMode.Frightened)
        {
            mode = GhostMode.Chase;
        }

        frightenedCoroutine = null;
    }
}