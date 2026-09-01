using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Счёт Матча")]
    public int playerScore = 0;
    public int botScore = 0;
    public int pointsToWin = 11;

    [Header("Подачи")]
    public bool isPlayerServing;
    private int totalPointsPlayed = 0;

    [Header("UI Ссылки")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text serveIndicatorText; // Необязательно: покажет чья подача

    private Ball ball;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // ИСПРАВЛЕНО: Присваиваем значение полю класса, а не создаем локальную переменную
        ball = FindFirstObjectByType<Ball>();
    }

    private void Start()
    {
        StartNewMatch();
    }

    public void StartNewMatch()
    {
        playerScore = 0;
        botScore = 0;
        totalPointsPlayed = 0;

        // Жребий: случайно определяем, кто подает первым
        isPlayerServing = Random.value > 0.5f;

        UpdateUI();

        // Если при старте ball не успел найтись в Awake, пробуем найти ещё раз
        if (ball == null) ball = FindFirstObjectByType<Ball>();

        if (ball != null)
        {
            ball.ResetForServe(isPlayerServing);
        }
    }

    public void ScorePoint(bool playerWonPoint)
    {
        if (playerWonPoint) playerScore++;
        else botScore++;

        totalPointsPlayed++;

        // Логика смены подачи и правил "Больше/Меньше" (Deuce)
        bool isDeuce = (playerScore >= 10 && botScore >= 10);

        if (isDeuce)
        {
            // При 10:10 и выше — подача меняется КАЖДЫЙ ход
            isPlayerServing = !isPlayerServing;
        }
        else
        {
            // До 10:10 — подача меняется каждые 2 сыгранных очка
            if (totalPointsPlayed % 2 == 0)
            {
                isPlayerServing = !isPlayerServing;
            }
        }

        UpdateUI();

        // Проверка на победу с разницей в 2 очка
        if (CheckWinCondition())
        {
            StartCoroutine(ResetMatchWithDelay(1.5f));
        }
        else
        {
            StartCoroutine(ResetRoundWithDelay(1.0f));
        }
    }

    private IEnumerator ResetRoundWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (ball == null) ball = FindFirstObjectByType<Ball>();
        if (ball != null)
        {
            ball.ResetForServe(isPlayerServing);
        }
    }

    private IEnumerator ResetMatchWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartNewMatch();
    }

    private bool CheckWinCondition()
    {
        // Стандартная победа (например 11:9)
        if (playerScore >= pointsToWin && (playerScore - botScore) >= 2)
        {
            Debug.Log("ИГРОК ПОБЕДИЛ В МАТЧЕ!");
            return true;
        }
        if (botScore >= pointsToWin && (botScore - playerScore) >= 2)
        {
            Debug.Log("БОТ ПОБЕДИЛ В МАТЧЕ!");
            return true;
        }
        return false;
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"{playerScore} : {botScore}";
        }

        if (serveIndicatorText != null)
        {
            serveIndicatorText.text = isPlayerServing ? "Подача: Игрок" : "Подача: Бот";
        }
    }
}