using UnityEngine;

public class Pellet : MonoBehaviour
{
    [SerializeField] private int points = 10;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        GameManager.Instance.AddScore(points);
        Destroy(gameObject);
    }
}