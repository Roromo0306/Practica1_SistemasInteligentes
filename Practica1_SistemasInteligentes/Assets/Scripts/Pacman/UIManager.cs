using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text livesText;

    private void Awake()
    {
        Instance = this;
    }

    public void Refresh(int score, int lives)
    {
        scoreText.text = $"SCORE: {score}";
        livesText.text = $"LIVES: {lives}";
    }
}