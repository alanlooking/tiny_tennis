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

        // 4. Аут
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

        // Выбираем соответствующую зону удара
        Collider2D targetArea = isHeadingToBot ? playerTargetArea : botTargetArea;

        if (targetArea != null)
        {
            Bounds bounds = targetArea.bounds;

            // Выбираем случайную точку строго внутри границ прямоугольника
            targetX = Random.Range(bounds.min.x, bounds.max.x);
            targetY = Random.Range(bounds.min.y, bounds.max.y);
        }
        else
        {
            // Резервный вариант, если забыл перетащить ссылки в Инспектор
            targetY = isHeadingToBot ? 2f : -2f;
        }

        Vector2 targetPosition = new Vector2(targetX, targetY);
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;

        rb.linearVelocity = direction * currentSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isServed || isProcessingMiss) return;

        if (!isHeadingToBot && (collision.gameObject.CompareTag("Player") || collision.gameObject.name == "Player"))
        {
            StartCoroutine(DelayedBodyHitPoint(false));
        }
        else if (isHeadingToBot && (collision.gameObject.CompareTag("Bot") || collision.gameObject.name == "Bot"))
        {
            StartCoroutine(DelayedBodyHitPoint(true));
        }
    }

    private IEnumerator DelayedBodyHitPoint(bool playerWonPoint)
    {
        isProcessingMiss = true;

        yield return new WaitForSeconds(0.25f);

        if (isProcessingMiss)
        {
            GameManager.Instance.ScorePoint(playerWonPoint);
        }
    }

    public void ResetForServe(bool playerServes)
    {
        if (botServeCoroutine != null) StopCoroutine(botServeCoroutine);

        isProcessingMiss = false;

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