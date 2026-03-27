using UnityEngine;

public class City : MonoBehaviour
{
    public bool visitada = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (visitada) return;

        if (other.CompareTag("Player"))
        {
            visitada = true;
            TSPManager_Game.Instance.RegistrarVisita(this);

            // Feedback visual
            GetComponent<Renderer>().material.color = Color.green;
        }
    }
}