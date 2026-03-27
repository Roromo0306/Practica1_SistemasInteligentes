using UnityEngine;
using TMPro;

public class LeaderboardUI : MonoBehaviour
{
    public TextMeshProUGUI texto;

    public void ActualizarUI()
    {
        var scores = LeaderboardManager.Instance.scores;

        string t = "LEADERBOARD\n\n";

        for (int i = 0; i < scores.Count; i++)
        {
            var s = scores[i];

            t += (i + 1) + ". " +
                 s.eficiencia.ToString("F1") + "% | " +
                 s.rango + "\n";
        }

        texto.text = t;
    }
}