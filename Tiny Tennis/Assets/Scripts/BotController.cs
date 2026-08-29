using UnityEngine;

public class BotController : MonoBehaviour
{
    [Header("Настройки ИИ")]
    [SerializeField] private float baseMoveSpeed = 9f;
    [SerializeField] private Transform ballTransform;

    [Header("Границы Верхнего Корта")]
    [SerializeField] private Vector2 minBounds = new Vector2(-2.5f, 0.5f);
    [SerializeField] private Vector2 maxBounds = new Vector2(2.5f, 6f);

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (ballTransform == null) return;

        // 1. Целевая позиция: по X следим за мячом
        float targetX = ballTransform.position.x;

        // По Y: если мяч на стороне бота — ждем его ближе к сетке, иначе держимся в центре поля
        float targetY = ballTransform.position.y > 0 ? Mathf.Clamp(ballTransform.position.y, 1.5f, 4f) : 3.5f;

        Vector2 targetPosition = new Vector2(targetX, targetY);

        // 2. Ускоряемся, если мяч далеко по X, чтобы бот точно успевал
        float distanceX = Mathf.Abs(transform.position.x - targetX);
        float currentSpeed = baseMoveSpeed;
        if (distanceX > 2f)
        {
            currentSpeed *= 1.4f; // Динамическое ускорение для доставания боковых мячей
        }

        // 3. Плавное перемещение через MoveTowards к ригидбоди
        Vector2 newPos = Vector2.MoveTowards(rb.position, targetPosition, currentSpeed * Time.fixedDeltaTime);

        // 4. Ограничение границами
        newPos.x = Mathf.Clamp(newPos.x, minBounds.x, maxBounds.x);
        newPos.y = Mathf.Clamp(newPos.y, minBounds.y, maxBounds.y);

        rb.MovePosition(newPos);
    }
}