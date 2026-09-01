using UnityEngine;

public class Ball : MonoBehaviour
{
    [Header("Настройки Скорости и Ускорения")]
    [SerializeField] private float serveSpeed = 5f;
    [SerializeField] private float initialHitSpeed = 7f;
    [SerializeField] private float speedIncrement = 0.6f;
    [SerializeField] private float maxSpeed = 16f;

    [Header("Ссылки на объекты")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform botTransform;
    [SerializeField] private Collider2D tableCollider;

    [Header("Дистанция удара")]
    [SerializeField] private float playerHitRadius = 1.8f;
    [SerializeField] private float botHitRadius = 1.6f;

    [Header("Границы для фиксации Аута")]
    [SerializeField] private float outBoundsY = 8f; // Расстояние по Y, дальше которого мяч уходит в аут

    private Rigidbody2D rb;
    private bool isServed = false;
    private bool isPlayerServing = true;
    private bool isHeadingToBot = true;

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

        // 2. Попытка удара ИГРОКА по Пробелу
        if (!isHeadingToBot)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= playerHitRadius && Input.GetKeyDown(KeyCode.Space))
            {
                HitBallToCourt(true); // Успешный удар
            }
        }

        // 3. Автоматический удар БОТА
        if (isHeadingToBot)
        {
            float distanceToBot = Vector2.Distance(transform.position, botTransform.position);
            if (distanceToBot <= botHitRadius)
            {
                HitBallToCourt(false); // Успешный удар бота
            }
        }

        // 4. Проверка на АУТ (мяч улетел за границы поля)
        if (Mathf.Abs(transform.position.y) > outBoundsY)
        {
            // Если мяч улетел за верхний край — очко игроку. За нижний — боту.
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

    private void HitBallToCourt(bool headingToBot)
    {
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

    // Фиксация касания ТЕЛОМ (когда мяч попадает в игрока/бота, но удар по кнопке не был сделан)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isServed) return;

        // Если мяч летит к ИГРОКУ и врезается в него телом — очко БОТУ
        if (!isHeadingToBot && (collision.gameObject.CompareTag("Player") || collision.gameObject.name == "Player"))
        {
            GameManager.Instance.ScorePoint(false); // Очко боту
        }
        // Если мяч летит к БОТУ и врезается в него — очко ИГРОКУ
        else if (isHeadingToBot && (collision.gameObject.CompareTag("Bot") || collision.gameObject.name == "Bot"))
        {
            GameManager.Instance.ScorePoint(true); // Очко игроку
        }
    }

    public void ResetForServe(bool playerServes)
    {
        isServed = false;
        isPlayerServing = playerServes;
        isHeadingToBot = playerServes;
        currentSpeed = serveSpeed;
        rb.linearVelocity = Vector2.zero;
    }
}