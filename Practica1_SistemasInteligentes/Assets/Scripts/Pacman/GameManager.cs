using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private int startingLives = 3;
    private int score;
    private int lives;

    public int Score => score;
    public int Lives => lives;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        lives = startingLives;
    }

    public void AddScore(int amount)
    {
        score += amount;
        UI_Manager.Instance.Refresh(score, lives);
    }

    public void LoseLife()
    {
        lives--;
        UI_Manager.Instance.Refresh(score, lives);

        if (lives <= 0)
        {
            SceneManager.LoadScene("GameOver");
            return;
        }

        StartCoroutine(RoundManager.Instance.ResetPositions());
    }
}