using UnityEngine;

public class CarController2D : MonoBehaviour
{
    public float velocidad = 5f;

    private Rigidbody2D rb;
    private Vector2 movimiento;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        movimiento.x = Input.GetAxisRaw("Horizontal");
        movimiento.y = Input.GetAxisRaw("Vertical");
        movimiento = movimiento.normalized;
    }

    void FixedUpdate()
    {
        rb.velocity = movimiento * velocidad;
    }
}