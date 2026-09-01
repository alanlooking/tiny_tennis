using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Счет")]
    public int playerScore = 0;
    public int botScore = 0;
    public int pointsToWin = 11;

    [Header("UI Ссылка (необязательно)")]
    [SerializeField] private Text scoreText; // Ссылка на UI Text, если есть

    private Ball ball;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        ball = FindObjectOfType<Ball>();
    }

    public void ScorePoint(bool playerWonPoint)
    {
        if (playerWonPoint)
        {
            playerScore++;
            Debug.Log($"Очко ИГРОКУ! Счёт: {playerScore} - {botScore}");
        }
        else
        {
            botScore++;
            Debug.Log($"Очко БОТУ! Счёт: {playerScore} - {botScore}");
        }

        UpdateUI();

        // Проверка на победу в матче
        if (playerScore >= pointsToWin)
        {
            Debug.Log("ИГРОК ПОБЕДИЛ В МАТЧЕ!");
            ResetMatch();
        }
        else if (botScore >= pointsToWin)
        {
            Debug.Log("БОТ ПОБЕДИЛ В МАТЧЕ!");
            ResetMatch();
        }
        else
        {
            // Подает тот, кто выиграл очко
            ball.ResetForServe(playerWonPoint);
        }
    }

    private void ResetMatch()
    {
        playerScore = 0;
        botScore = 0;
        UpdateUI();
        ball.ResetForServe(true);
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"{playerScore} : {botScore}";
        }
    }
}