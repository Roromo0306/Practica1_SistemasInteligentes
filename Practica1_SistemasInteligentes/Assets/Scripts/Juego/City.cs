using UnityEngine;

public class City : MonoBehaviour
{
    public bool visitada = false;
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        // Siempre avisamos al manager, incluso si ya estaba visitada
        TSPManager_Game.Instance.RegistrarVisita(this);

        // Solo la marcamos visualmente la primera vez
        if (!visitada)
        {
            visitada = true;

            if (sr != null)
                sr.color = Color.green;
        }
    }

    public void ReiniciarCiudad()
    {
        visitada = false;

        if (sr != null)
            sr.color = Color.white;
    }
}