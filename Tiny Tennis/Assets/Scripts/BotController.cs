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

    [Header("Анимации")]
    [SerializeField] private Animator animator;
    [SerializeField] private string primaryHitTrigger = "HitPrimary";
    [SerializeField] private string alternateHitTrigger = "HitAlternate";
    [SerializeField] private string isMovingBool = "IsMoving";

    private Rigidbody2D rb;
    private Ball ball;

    private bool wasHeadingToBot = false;
    private bool hasNoticedBall = false;
    private float noticeTime;
    private float nextThinkTime;
    private float aimErrorOffset;
    private float currentTargetX;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        ball = FindFirstObjectByType<Ball>();
        if (ball != null)
        {
            ball.OnBotHit += HandleBotHit;
        }
    }

    private void OnDestroy()
    {
        if (ball != null)
        {
            ball.OnBotHit -= HandleBotHit;
        }
    }

    // Мяч сообщает: бот ударил (включая подачу)
    private void HandleBotHit(HitType hitType)
    {
        if (animator == null) return;

        animator.SetTrigger(hitType == HitType.Primary
            ? primaryHitTrigger
            : alternateHitTrigger);
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
                    nextThinkTime = 0f;
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

        // Анимация ходьбы: бот реально смещается в этом физическом шаге?
        if (animator != null)
        {
            animator.SetBool(isMovingBool, Mathf.Abs(newX - rb.position.x) > 0.001f);
        }

        rb.MovePosition(new Vector2(newX, targetY));
    }

    private float PredictLandingX()
    {
        Vector2 pos = ball.transform.position;
        Vector2 vel = ball.Velocity;

        if (vel.y <= 0.01f) return pos.x;
        if (pos.y >= targetY) return pos.x;

        // Насколько бот "видит" кручение (0 — полностью игнорирует)
        float effectiveSpin = ball.Spin * spinReading;

        if (Mathf.Abs(effectiveSpin) < 0.01f)
        {
            float timeToLine = (targetY - pos.y) / vel.y;
            return pos.x + vel.x * timeToLine;
        }

        float step = 0.02f;
        int maxSteps = 500;
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
                float t = (targetY - prevPos.y) / (pos.y - prevPos.y);
                return Mathf.Lerp(prevPos.x, pos.x, t);
            }
        }

        return pos.x;
    }

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