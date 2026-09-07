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
    [SerializeField] private Collider2D playerTargetArea; // Зона на стороне бота
    [SerializeField] private Collider2D botTargetArea;    // Зона на стороне игрока

    [Header("Дистанция удара")]
    [SerializeField] private float playerHitRadius = 1.6f;
    [SerializeField] private float botHitRadius = 1f;

    [Header("Границы аута")]
    [SerializeField] private float outBoundsY = 7.5f;
    [SerializeField] private float outBoundsX = 9f;

    [Header("Страховка от зависания розыгрыша")]
    [SerializeField] private float roundTimeout = 8f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private BotController bot;
    private float currentSpeed;
    private float lastHitTime;

    private bool isServed = false;
    private bool isPlayerServing = true;
    private bool isHeadingToBot = true;
    private Coroutine botServeCoroutine;

    private bool isRoundEnding = false;
    private bool hasHitTargetArea = false;

    // Публичные свойства для BotController
    public bool IsServed => isServed;
    public bool IsHeadingToBot => isHeadingToBot;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bot = FindFirstObjectByType<BotController>();
    }

    private void Update()
    {
        if (isRoundEnding) return;

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

        // 2. Удар Игрока (мяч летит к игроку)
        if (!isHeadingToBot)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= playerHitRadius && Input.GetKeyDown(KeyCode.Space))
            {
                HitBallToCourt(true);
            }
        }

        // 3. Удар Бота (мяч летит к боту)
        if (isHeadingToBot)
        {
            float distanceToBot = Vector2.Distance(transform.position, botTransform.position);
            if (distanceToBot <= botHitRadius)
            {
                HitBallToCourt(false);
            }
        }

        // 4. Проверка на вылет за границы ИЛИ зависание розыгрыша
        if (Mathf.Abs(transform.position.y) > outBoundsY ||
            Mathf.Abs(transform.position.x) > outBoundsX ||
            Time.time - lastHitTime > roundTimeout)
        {
            ProcessOutOrScore();
        }
    }

    /// <summary>
    /// Вызывается из TargetArea.cs при касании стола
    /// </summary>
    public void RegisterTargetHit(bool isBotSideArea)
    {
        if (isRoundEnding) return;

        if ((isHeadingToBot && isBotSideArea) || (!isHeadingToBot && !isBotSideArea))
        {
            hasHitTargetArea = true;
            Debug.Log($"[TargetArea] Мяч попал в стол {(isBotSideArea ? "Бота" : "Игрока")}!");
        }
    }

    private void ProcessOutOrScore()
    {
        if (isRoundEnding) return;
        isRoundEnding = true;

        rb.linearVelocity = Vector2.zero;
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        bool playerWonPoint;

        if (hasHitTargetArea)
        {
            // Мяч попал в стол, принимающий не отбил -> Очко бьющему
            playerWonPoint = isHeadingToBot;
        }
        else
        {
            // Аут -> Очко принимающему
            playerWonPoint = !isHeadingToBot;
        }

        Debug.Log($"[Итог розыгрыша] Летел к боту: {isHeadingToBot} | Попадание в стол: {hasHitTargetArea} | Очко отдано: {(playerWonPoint ? "Игроку" : "Боту")}");

        GameManager.Instance.ScorePoint(playerWonPoint);
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
        lastHitTime = Time.time;
        hasHitTargetArea = false;
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

        Collider2D targetArea = isHeadingToBot ? playerTargetArea : botTargetArea;

        if (targetArea != null)
        {
            Bounds bounds = targetArea.bounds;
            targetX = Random.Range(bounds.min.x, bounds.max.x);
            targetY = Random.Range(bounds.min.y, bounds.max.y);
        }

        Vector2 targetPosition = new Vector2(targetX, targetY);
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;

        rb.linearVelocity = direction * currentSpeed;
    }

    public void ResetForServe(bool playerServes)
    {
        if (botServeCoroutine != null) StopCoroutine(botServeCoroutine);

        isRoundEnding = false;
        hasHitTargetArea = false;
        lastHitTime = Time.time;

        if (bot != null) bot.ResetPosition();

        isServed = false;
        isPlayerServing = playerServes;
        isHeadingToBot = playerServes;
        currentSpeed = serveSpeed;
        rb.linearVelocity = Vector2.zero;

        if (spriteRenderer != null) spriteRenderer.enabled = true;

        if (!isPlayerServing)
        {
            botServeCoroutine = StartCoroutine(BotServeRoutine());
        }
    }
}