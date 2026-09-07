using UnityEngine;

public class BotController : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float speed = 6f;
    [SerializeField] private float targetY = 3.5f;   // Линия Y бота
    [SerializeField] private float defaultX = 0f;    // "Дом" — центр стола

    [Header("Границы перемещения")]
    [SerializeField] private float minX = -2.5f;
    [SerializeField] private float maxX = 2.5f;

    [Header("Человечность (= сложность)")]
    [Tooltip("Задержка реакции: сколько секунд бот не реагирует на удар игрока")]
    [SerializeField] private float reactionDelay = 0.45f;
    [Tooltip("Как часто бот пересматривает свой прогноз точки прилёта")]
    [SerializeField] private float thinkInterval = 0.2f;
    [Tooltip("Максимальная погрешность прогноза по X (юниты)")]
    [SerializeField] private float aimError = 0.5f;

    private Rigidbody2D rb;
    private Ball ball;

    private bool wasHeadingToBot = false;  // Прошлое направление мяча
    private bool hasNoticedBall = false;   // Бот "заметил" мяч после задержки
    private float noticeTime;              // Момент, когда бот заметит мяч
    private float nextThinkTime;           // Следующий пересмотр прогноза
    private float aimErrorOffset;          // Погрешность "глаза" на текущий приём
    private float currentTargetX;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        ball = FindFirstObjectByType<Ball>();
    }

    private void FixedUpdate()
    {
        if (ball == null) return;

        float targetX;

        if (ball.IsServed && ball.IsHeadingToBot)
        {
            // Игрок только что ударил — детектим смену направления
            if (!wasHeadingToBot)
            {
                wasHeadingToBot = true;
                hasNoticedBall = false;
                noticeTime = Time.time + reactionDelay;
                // Ошибка глаза фиксируется ОДИН раз на приём,
                // чтобы бот не дёргался между пересчётами
                aimErrorOffset = Random.Range(-aimError, aimError);
            }

            // Пока не "заметил" мяч — едем к прошлой цели (обычно к дому)
            if (Time.time >= noticeTime)
            {
                if (!hasNoticedBall)
                {
                    hasNoticedBall = true;
                    nextThinkTime = 0f; // подумать немедленно
                }

                if (Time.time >= nextThinkTime)
                {
                    nextThinkTime = Time.time + thinkInterval;
                    float predicted = PredictLandingX() + aimErrorOffset;
                    currentTargetX = Mathf.Clamp(predicted, minX, maxX);
                }
            }

            targetX = currentTargetX;
        }
        else
        {
            // Мяч не подан или летит к игроку — возвращаемся домой
            wasHeadingToBot = false;
            targetX = defaultX;
            currentTargetX = defaultX;
        }

        float newX = Mathf.MoveTowards(rb.position.x, targetX, speed * Time.fixedDeltaTime);
        rb.MovePosition(new Vector2(newX, targetY));
    }

    // Линейная экстраполяция: где мяч с текущей скоростью
    // пересечёт линию Y бота
    private float PredictLandingX()
    {
        Vector2 pos = ball.transform.position;
        Vector2 vel = ball.Velocity;

        if (vel.y <= 0.01f) return pos.x;    // мяч не движется к боту
        if (pos.y >= targetY) return pos.x;  // мяч уже за линией бота

        float timeToLine = (targetY - pos.y) / vel.y;
        return pos.x + vel.x * timeToLine;
    }

    public void ResetPosition()
    {
        rb.position = new Vector2(defaultX, targetY);
        wasHeadingToBot = false;
        currentTargetX = defaultX;
    }
}