using UnityEngine;

public class Ball : MonoBehaviour
{
    [Header("Настройки Скорости и Ускорения")]
    [SerializeField] private float serveSpeed = 5f;          // Скорость при подаче
    [SerializeField] private float initialHitSpeed = 7f;     // Начальная скорость первого удара
    [SerializeField] private float speedIncrement = 0.6f;    // Прирост скорости за каждый удар
    [SerializeField] private float maxSpeed = 16f;           // Максимальный предел скорости

    [Header("Ссылки на объекты")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform botTransform;
    [SerializeField] private Collider2D tableCollider;

    [Header("Дистанция удара")]
    [SerializeField] private float playerHitRadius = 1.8f;
    [SerializeField] private float botHitRadius = 1.6f;

    private Rigidbody2D rb;
    private bool isServed = false;
    private bool isPlayerServing = true;
    private bool isHeadingToBot = true;

    // Текущая накопленная скорость ралли
    private float currentSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        ResetForServe(true);
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

        // 2. Удар ИГРОКА по Пробелу
        if (!isHeadingToBot)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= playerHitRadius && Input.GetKeyDown(KeyCode.Space))
            {
                HitBallToCourt(true); // Отправляем боту
            }
        }

        // 3. Автоматический удар БОТА
        if (isHeadingToBot)
        {
            float distanceToBot = Vector2.Distance(transform.position, botTransform.position);
            if (distanceToBot <= botHitRadius)
            {
                HitBallToCourt(false); // Отправляем игроку
            }
        }
    }

    public void ExecuteServe()
    {
        isServed = true;
        currentSpeed = serveSpeed; // Подача начинается с базовой скорости
        HitBallToCourt(isPlayerServing);
    }

    private void HitBallToCourt(bool headingToBot)
    {
        isHeadingToBot = headingToBot;

        // Расчет ускорения мяча
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
            // Увеличиваем скорость за каждый новый удар в ралли
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

    public void ResetForServe(bool playerServes)
    {
        isServed = false;
        isPlayerServing = playerServes;
        isHeadingToBot = playerServes;
        currentSpeed = serveSpeed;
        rb.linearVelocity = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerTransform.position, playerHitRadius);
        }
        if (botTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(botTransform.position, botHitRadius);
        }
    }
}