using System.Collections;
using UnityEngine;

// Тип удара — используется анимациями
public enum HitType { Primary, Alternate }

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

    [Header("Кручение (альтернативный удар)")]
    [Tooltip("Сила закрутки: больше = круче дуга. 0 = выключить")]
    [SerializeField] private float spinAcceleration = 8f;

    // События для анимаций ракеток
    public event System.Action<HitType> OnPlayerHit;
    public event System.Action<HitType> OnBotHit; // теперь с типом удара

    // Game feel: события для камеры, трейла и UI
    public event System.Action<int, float> OnRallyHit; // (номер удара, сила удара 0..1)
    public event System.Action OnRallyReset;           // розыгрыш начался заново

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private BotController bot;
    private float currentSpeed;
    private float lastHitTime;
    private float spin; // Текущая закрутка: 0 = мяч летит прямо
    private int hitsInRally; // Счётчик ударов в текущем розыгрыше

    private bool isServed = false;
    private bool isPlayerServing = true;
    private bool isHeadingToBot = true;
    private Coroutine botServeCoroutine;

    private bool isRoundEnding = false;
    private bool hasHitTargetArea = false;

    // Публичные свойства для BotController
    public bool IsServed => isServed;
    public bool IsHeadingToBot => isHeadingToBot;
    public Vector2 Velocity => rb.linearVelocity;
    public float Spin => spin; // текущая закрутка — бот использует её в прогнозе

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bot = FindFirstObjectByType<BotController>();
    }

    private void Update()
    {
        if (isRoundEnding) return;

        // 1. Подача (Space) — анимация выбирается случайно
        if (!isServed)
        {
            Transform currentServer = isPlayerServing ? playerTransform : botTransform;
            float offsetYSide = isPlayerServing ? 0.6f : -0.6f;
            transform.position = currentServer.position + new Vector3(0f, offsetYSide, 0f);

            if (isPlayerServing && Input.GetKeyDown(KeyCode.Space))
            {
                HitType serveType = Random.value > 0.5f ? HitType.Primary : HitType.Alternate;
                ExecuteServe(serveType);
            }
            return;
        }

        // 2. Удар Игрока (мяч летит к игроку)
        if (!isHeadingToBot)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= playerHitRadius)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                {
                    HitBallToCourt(true, HitType.Primary);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                {
                    HitBallToCourt(true, HitType.Alternate);
                }
            }
        }

        // 3. Удар Бота (мяч летит к боту)
        if (isHeadingToBot)
        {
            float distanceToBot = Vector2.Distance(transform.position, botTransform.position);
            if (distanceToBot <= botHitRadius)
            {
                // Бот сам решает, крутить или бить прямо
                HitType botHitType = (bot != null) ? bot.ChooseHitType() : HitType.Primary;
                HitBallToCourt(false, botHitType);
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

    private void FixedUpdate()
    {
        if (!isServed || isRoundEnding) return;
        if (Mathf.Abs(spin) < 0.01f) return;

        Vector2 vel = rb.linearVelocity;
        float speed = vel.magnitude;
        if (speed < 0.01f) return;

        // Перпендикуляр к текущей скорости (поворот на 90°)
        Vector2 perp = new Vector2(-vel.y, vel.x) / speed;

        // Поворачиваем вектор скорости, СОХРАНЯЯ его длину —
        // мяч летит по идеальной дуге, темп игры не меняется
        Vector2 newVel = vel + perp * (spin * Time.fixedDeltaTime);
        rb.linearVelocity = newVel.normalized * speed;
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
            playerWonPoint = isHeadingToBot;
        }
        else
        {
            playerWonPoint = !isHeadingToBot;
        }

        Debug.Log($"[Итог розыгрыша] Летел к боту: {isHeadingToBot} | Попадание в стол: {hasHitTargetArea} | Очко отдано: {(playerWonPoint ? "Игроку" : "Боту")}");

        GameManager.Instance.ScorePoint(playerWonPoint);
    }

    public void ExecuteServe(HitType hitType)
    {
        isServed = true;
        currentSpeed = serveSpeed;
        HitBallToCourt(isPlayerServing, hitType);
    }

    private IEnumerator BotServeRoutine()
    {
        yield return new WaitForSeconds(botServeDelay);
        if (!isServed && !isPlayerServing)
        {
            // Тип для бота не важен: его анимация завязана на событие OnBotHit
            ExecuteServe(HitType.Primary);
        }
    }

    private void HitBallToCourt(bool headingToBot, HitType hitType)
    {
        lastHitTime = Time.time;
        hasHitTargetArea = false;
        isHeadingToBot = headingToBot;

        // Альтернативный удар закручивает мяч в случайную сторону,
        // основной и удары бота — прямой полёт
        spin = (hitType == HitType.Alternate)
            ? spinAcceleration * (Random.value > 0.5f ? 1f : -1f)
            : 0f;

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

        // Уведомляем анимации, какой удар сыгран
        if (headingToBot) OnPlayerHit?.Invoke(hitType);
        else OnBotHit?.Invoke(hitType);

        // Game feel: камера, трейл и UI узнают об ударе
        hitsInRally++;
        float power = Mathf.InverseLerp(serveSpeed, maxSpeed, currentSpeed);
        OnRallyHit?.Invoke(hitsInRally, power);
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
        spin = 0f;
        hitsInRally = 0;
        OnRallyReset?.Invoke();

        if (spriteRenderer != null) spriteRenderer.enabled = true;

        if (!isPlayerServing)
        {
            botServeCoroutine = StartCoroutine(BotServeRoutine());
        }
    }
}