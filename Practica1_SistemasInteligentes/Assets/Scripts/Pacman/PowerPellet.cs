using UnityEngine;

public class PowerPellet : MonoBehaviour
{
    [SerializeField] private int points = 50;
    [SerializeField] private float frightenedTime = 8f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        GameManager.Instance.AddScore(points);
        GhostManager.Instance.SetFrightenedMode(frightenedTime);
        Destroy(gameObject);
    }
}