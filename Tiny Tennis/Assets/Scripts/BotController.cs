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
    [SerializeField] private float reactionDelay = 0.3f;
    [Tooltip("Как часто бот пересматривает свой прогноз точки прилёта")]
    [SerializeField] private float thinkInterval = 0.2f;
    [Tooltip("Максимальная погрешность прогноза по X (юниты)")]
    [SerializeField] private float aimError = 0.5f;

    [Header("Чтение кручения")]
    [Tooltip("Насколько бот учитывает закрутку в прогнозе: 0 — не читает, 1 — читает идеально")]
    [SerializeField, Range(0f, 1f)] private float spinReading = 0f;

    [Header("Кручёные удары бота")]
    [Tooltip("Шанс, что бот ответит кручёным (Alternate) ударом")]
    [SerializeField, Range(0f, 1f)] private float spinChance = 0.3f;

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
                aimErrorOffset = Random.Range(-aimError, aimError);
            }

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

    // Прогноз точки прилёта мяча на линию Y бота
    private float PredictLandingX()
    {
        Vector2 pos = ball.transform.position;
        Vector2 vel = ball.Velocity;

        if (vel.y <= 0.01f) return pos.x;    // мяч не движется к боту
        if (pos.y >= targetY) return pos.x;  // мяч уже за линией бота

        // Насколько бот "видит" кручение (0 — полностью игнорирует)
        float effectiveSpin = ball.Spin * spinReading;

        if (Mathf.Abs(effectiveSpin) < 0.01f)
        {
            // Прямолинейный прогноз (как раньше)
            float timeToLine = (targetY - pos.y) / vel.y;
            return pos.x + vel.x * timeToLine;
        }

        // Бот "симулирует" полёт кручёного мяча в голове —
        // пошагово повторяя ту же физику, что делает FixedUpdate мяча
        float step = 0.02f;
        int maxSteps = 500; // страховка от вечного цикла
        Vector2 prevPos = pos;

        for (int i = 0; i < maxSteps; i++)
        {
            float spd = vel.magnitude;
            Vector2 perp = new Vector2(-vel.y, vel.x) / spd;
            vel = (vel + perp * (effectiveSpin * step)).normalized * spd;

            prevPos = pos;
            pos += vel * step;

            if (pos.y >= targetY)
            {
                // Точная точка пересечения — интерполяция между шагами
                float t = (targetY - prevPos.y) / (pos.y - prevPos.y);
                return Mathf.Lerp(prevPos.x, pos.x, t);
            }
        }

        return pos.x;
    }

    // Выбор типа удара: с шансом spinChance бот отвечает кручёным
    public HitType ChooseHitType()
    {
        return Random.value <= spinChance ? HitType.Alternate : HitType.Primary;
    }

    public void ResetPosition()
    {
        rb.position = new Vector2(defaultX, targetY);
        wasHeadingToBot = false;
        currentTargetX = defaultX;
    }
}