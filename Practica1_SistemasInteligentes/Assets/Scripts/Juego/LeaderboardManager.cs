using System.Collections.Generic;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance;

    [System.Serializable]
    public class Entrada
    {
        public float distancia;
        public float eficiencia;
        public string rango;

        public Entrada(float d, float e, string r)
        {
            distancia = d;
            eficiencia = e;
            rango = r;
        }
    }

    public List<Entrada> ranking = new List<Entrada>();

    void Awake()
    {
        Instance = this;
    }

    public void AñadirPuntuacion(float distancia, float eficiencia, string rango)
    {
        ranking.Add(new Entrada(distancia, eficiencia, rango));
        Debug.Log("Puntuación guardada: " + distancia + " | " + eficiencia + "% | " + rango);
    }
}