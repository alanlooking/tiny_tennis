using UnityEngine;

public class BotController : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float speed = 6f;
    [SerializeField] private float targetY = 3.5f;   // Фиксированная линия Y бота
    [SerializeField] private float defaultX = 0f;    // "Дом" — центр стола

    [Header("Границы перемещения")]
    [SerializeField] private float minX = -2.5f;
    [SerializeField] private float maxX = 2.5f;

    private Rigidbody2D rb;
    private Ball ball;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        ball = FindFirstObjectByType<Ball>();
    }

    private void FixedUpdate()
    {
        if (ball == null) return;

        float targetX;

        if (ball.IsServed && ball.IsHeadingToBot)
        {
            // Мяч в игре и летит к боту — защищаемся
            targetX = Mathf.Clamp(ball.transform.position.x, minX, maxX);
        }
        else
        {
            // Мяч не подан или летит к игроку — возвращаемся "домой"
            targetX = defaultX;
        }

        // Двигаем риджидбоди через MovePosition — физика и transform
        // остаются синхронными, никакого конфликта
        float newX = Mathf.MoveTowards(rb.position.x, targetX, speed * Time.fixedDeltaTime);
        rb.MovePosition(new Vector2(newX, targetY));
    }

    // Мгновенный сброс позиции после розыгрыша
    public void ResetPosition()
    {
        rb.position = new Vector2(defaultX, targetY);
    }
}