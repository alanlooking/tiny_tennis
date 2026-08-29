using UnityEngine;

public class Ball : MonoBehaviour
{
    [Header("Настройки Скорости")]
    [SerializeField] private float serveSpeed = 6f;
    [SerializeField] private float hitSpeed = 9f;

    [Header("Ссылки на объекты")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform botTransform;
    [SerializeField] private Collider2D tableCollider;

    [Header("Дистанция удара")]
    [SerializeField] private float playerHitRadius = 1f;
    [SerializeField] private float botHitRadius = 1f;

    private Rigidbody2D rb;
    private bool isServed = false;
    private bool isPlayerServing = true;
    private bool isHeadingToBot = true; // Кто сейчас должен отбивать

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

        // 2. Удар ИГРОКА по Пробелу (только если мяч летит к игроку)
        if (!isHeadingToBot)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= playerHitRadius && Input.GetKeyDown(KeyCode.Space))
            {
                HitBallToCourt(true); // Отправляем боту
            }
        }

        // 3. Автоматический удар БОТА (когда мяч летит к боту и входит в его зону)
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
        HitBallToCourt(isPlayerServing);
    }

    private void HitBallToCourt(bool headingToBot)
    {
        isHeadingToBot = headingToBot;
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

        float currentSpeed = isServed ? hitSpeed : serveSpeed;
        rb.linearVelocity = direction * currentSpeed;
    }

    public void ResetForServe(bool playerServes)
    {
        isServed = false;
        isPlayerServing = playerServes;
        isHeadingToBot = playerServes; // Если подает игрок, мяч полетит к боту
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