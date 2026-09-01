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
    [SerializeField] private Collider2D tableCollider;

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

    // Флаг для защиты от дублирования очков и обработки задержки касания
    private bool isProcessingMiss = false;

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

            // Подача Игрока по Пробелу
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

        // 4. Аут (засчет очка только если мяч совсем улетел за пределы)
        if (Mathf.Abs(transform.position.y) > outBoundsY)
        {
            bool playerWonPoint = transform.position.y > 0;
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
        // Если игрок успел отбить мяч — отменяем зафиксированный промах при касании
        isProcessingMiss = false;

        isHeadingToBot = headingToBot;

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

        if (tableCollider != null)
        {
            Bounds bounds = tableCollider.bounds;
            float paddingX = bounds.size.x * 0.12f;
            targetX = Random.Range(bounds.min.x + paddingX, bounds.max.x - paddingX);

            if (isHeadingToBot)
            {
                targetY = Random.Range(bounds.center.y + 0.2f, bounds.max.y - 0.2f);
            }
            else
            {
                targetY = Random.Range(bounds.min.y + 0.2f, bounds.center.y - 0.2f);
            }
        }

        Vector2 targetPosition = new Vector2(targetX, targetY);
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;

        rb.linearVelocity = direction * currentSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isServed || isProcessingMiss) return;

        // Если мяч летел к Игроку и задел его
        if (!isHeadingToBot && (collision.gameObject.CompareTag("Player") || collision.gameObject.name == "Player"))
        {
            StartCoroutine(DelayedBodyHitPoint(false)); // Очко боту
        }
        // Если мяч летел к БОТУ и задел его
        else if (isHeadingToBot && (collision.gameObject.CompareTag("Bot") || collision.gameObject.name == "Bot"))
        {
            StartCoroutine(DelayedBodyHitPoint(true)); // Очко игроку
        }
    }

    // Небольшая задержка перед засчетом очка, чтобы дать игроку шанс нажать Пробел
    private IEnumerator DelayedBodyHitPoint(bool playerWonPoint)
    {
        isProcessingMiss = true;

        yield return new WaitForSeconds(0.25f);

        // Если за время задержки мяч так и не отбили (isProcessingMiss остался true) — отдаём очко
        if (isProcessingMiss)
        {
            GameManager.Instance.ScorePoint(playerWonPoint);
        }
    }

    public void ResetForServe(bool playerServes)
    {
        if (botServeCoroutine != null) StopCoroutine(botServeCoroutine);

        isProcessingMiss = false;

        // Вызов сброса позиции бота в случайную точку на его линии
        BotController bot = FindObjectOfType<BotController>();
        if (bot != null) bot.ResetPosition();

        isServed = false;
        isPlayerServing = playerServes;
        isHeadingToBot = playerServes;
        currentSpeed = serveSpeed;
        rb.linearVelocity = Vector2.zero;

        // Если подача бота — запускаем таймер автоматической подачи
        if (!isPlayerServing)
        {
            botServeCoroutine = StartCoroutine(BotServeRoutine());
        }
    }
}