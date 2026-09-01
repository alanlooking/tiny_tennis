using System.Collections;
using UnityEngine;

public class Ball : MonoBehaviour
{
    [Header("Настройки Скорости")]
    [SerializeField] private float serveSpeed = 5f;
    [SerializeField] private float initialHitSpeed = 7f;
    [SerializeField] private float speedIncrement = 0.5f;
    [SerializeField] private float maxSpeed = 16f;

    [Header("Задержка авто-подачи бота")]
    [SerializeField] private float botServeDelay = 1.2f;

    [Header("Ссылки на объекты")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform botTransform;

    [Header("Зоны приземления (дочерние объекты Table)")]
    [SerializeField] private Collider2D playerTargetArea; // Прямоугольник игрока (на стороне бота)
    [SerializeField] private Collider2D botTargetArea;    // Прямоугольник бота (на стороне игрока)

    [Header("Дистанция удара")]
    [SerializeField] private float playerHitRadius = 1.5f;
    [SerializeField] private float botHitRadius = 1f;

    [Header("Граница аута")]
    [SerializeField] private float outBoundsY = 7.5f;

    private Rigidbody2D rb;
    private float currentSpeed;
    private bool isServed = false;
    private bool isPlayerServing = true;
    private bool isHeadingToBot = true;
    private Coroutine botServeCoroutine;

    private bool isProcessingMiss = false;
    private bool lastHitWasOut = false; // Флаг: был ли последний удар заведомым аутом (мимо стола)

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // 1. Подача
        if (!isServed)
        {
            Transform currentServer = isPlayerServing ? playerTransform : botTransform;
            float offsetYSide = isPlayerServing ? 0.6f : -0.6f;
            transform.position = currentServer.position + new Vector3(0f, offsetYSide, 0f);

            if (isPlayerServing && Input.GetKeyDown(KeyCode.Space))
            {
                ExecuteServe();
            }
            return;
        }

        // 2. Удар Игрока
        if (!isHeadingToBot)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= playerHitRadius && Input.GetKeyDown(KeyCode.Space))
            {
                HitBallToCourt(true);
            }
        }

        // 3. Удар Бота
        if (isHeadingToBot)
        {
            float distanceToBot = Vector2.Distance(transform.position, botTransform.position);
            if (distanceToBot <= botHitRadius)
            {
                HitBallToCourt(false);
            }
        }

        // 4. Проверка на АУТ / Вылет за пределы
        if (Mathf.Abs(transform.position.y) > outBoundsY)
        {
            bool playerWonPoint;

            if (lastHitWasOut)
            {
                // Если игрок/бот ударил мимо стола (в аут), очко отдаем тому, кто ПРИНИМАЛ
                playerWonPoint = !isHeadingToBot;
            }
            else
            {
                // Стандарт: если мяч пролетел за спину принимающего
                playerWonPoint = transform.position.y > 0;
            }

            GameManager.Instance.ScorePoint(playerWonPoint);
        }
    }

    public void ExecuteServe()
    {
        isServed = true;
        currentSpeed = serveSpeed;
        HitBallToCourt(isPlayerServing);
    }

    private IEnumerator BotServeRoutine()
    {
        yield return new WaitForSeconds(botServeDelay);
        if (!isServed && !isPlayerServing)
        {
            ExecuteServe();
        }
    }

    private void HitBallToCourt(bool headingToBot)
    {
        isProcessingMiss = false;
        isHeadingToBot = headingToBot;
        lastHitWasOut = false;

        if (!isServed)
        {
            currentSpeed = serveSpeed;
        }
        else if (currentSpeed < initialHitSpeed)
        {
            currentSpeed = initialHitSpeed;
        }
        else
        {
            currentSpeed = Mathf.Min(currentSpeed + speedIncrement, maxSpeed);
        }

        float targetX = 0f;
        float targetY = 0f;

        Collider2D targetArea = isHeadingToBot ? playerTargetArea : botTargetArea;

        if (targetArea != null)
        {
            Bounds bounds = targetArea.bounds;

            // Если бьющий слишком далеко вбоку, мы гарантируем, что вектор всё равно идёт В СТОЛ
            float hitterX = transform.position.x;
            if (Mathf.Abs(hitterX) > bounds.extents.x * 1.5f)
            {
                // Принудительно целимся ближе к центру прямоугольника
                targetX = Random.Range(bounds.min.x + 0.3f, bounds.max.x - 0.3f);
            }
            else
            {
                targetX = Random.Range(bounds.min.x, bounds.max.x);
            }

            targetY = Random.Range(bounds.min.y, bounds.max.y);
        }

        Vector2 targetPosition = new Vector2(targetX, targetY);
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;

        rb.linearVelocity = direction * currentSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isServed || isProcessingMiss) return;

        // Если мяч задел тело ИГРОКА (не успел отбить)
        if (!isHeadingToBot && (collision.gameObject.CompareTag("Player") || collision.gameObject.name == "Player"))
        {
            StartCoroutine(DelayedBodyHitPoint(false)); // Очко боту
        }
        // Если мяч задел тело БОТА
        else if (isHeadingToBot && (collision.gameObject.CompareTag("Bot") || collision.gameObject.name == "Bot"))
        {
            StartCoroutine(DelayedBodyHitPoint(true)); // Очко игроку
        }
    }

    private IEnumerator DelayedBodyHitPoint(bool playerWonPoint)
    {
        isProcessingMiss = true;

        // Даём короткое окно на нажатие Пробела (0.2 сек)
        yield return new WaitForSeconds(0.2f);

        // Если очко все ещё не отбито — останавливаем мяч и завершаем раунд
        if (isProcessingMiss)
        {
            rb.linearVelocity = Vector2.zero; // Останавливаем медленное качение
            GameManager.Instance.ScorePoint(playerWonPoint);
        }
    }

    public void ResetForServe(bool playerServes)
    {
        if (botServeCoroutine != null) StopCoroutine(botServeCoroutine);

        isProcessingMiss = false;
        lastHitWasOut = false;

        BotController bot = FindObjectOfType<BotController>();
        if (bot != null) bot.ResetPosition();

        isServed = false;
        isPlayerServing = playerServes;
        isHeadingToBot = playerServes;
        currentSpeed = serveSpeed;
        rb.linearVelocity = Vector2.zero;

        if (!isPlayerServing)
        {
            botServeCoroutine = StartCoroutine(BotServeRoutine());
        }
    }
}