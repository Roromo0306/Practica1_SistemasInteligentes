using UnityEngine;

public class PacmanController : MonoBehaviour
{
    [SerializeField] private float speed = 4f;
    private Vector2 currentDir = Vector2.left;
    private Vector2 nextDir = Vector2.left;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            nextDir = Vector2.up;
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            nextDir = Vector2.down;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            nextDir = Vector2.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            nextDir = Vector2.right;
    }

    private void FixedUpdate()
    {
        if (CanMove(nextDir))
            currentDir = nextDir;

        if (!CanMove(currentDir))
        {
            rb.velocity = Vector2.zero;
            return;
        }

        rb.velocity = currentDir * speed;
    }

    private bool CanMove(Vector2 dir)
    {
        float dist = 0.55f;
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.2f, dir, dist, LayerMask.GetMask("Wall"));
        return hit.collider == null;
    }
}