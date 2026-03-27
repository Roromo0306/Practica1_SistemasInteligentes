using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance;

    public List<ScoreData> scores = new List<ScoreData>();

    public LeaderboardUI ui;

    void Awake()
    {
        Instance = this;
    }

    public void AñadirPuntuacion(float distancia, float eficiencia, string rango)
    {
        ScoreData nuevo = new ScoreData(distancia, eficiencia, rango);
        scores.Add(nuevo);

        // Ordenar por eficiencia (mejor primero)
        scores = scores.OrderByDescending(s => s.eficiencia).ToList();

        // Limitar a top 10
        if (scores.Count > 10)
            scores.RemoveAt(scores.Count - 1);

        MostrarLeaderboard();

        ui.ActualizarUI();
    }

    void MostrarLeaderboard()
    {
        Debug.Log("===== LEADERBOARD =====");

        for (int i = 0; i < scores.Count; i++)
        {
            var s = scores[i];
            Debug.Log(
                (i + 1) + ". " +
                "Dist: " + s.distancia.ToString("F1") +
                " | Eff: " + s.eficiencia.ToString("F1") + "%" +
                " | Rank: " + s.rango
            );
        }
    }
}