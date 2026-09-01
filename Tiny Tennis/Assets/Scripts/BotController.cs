using UnityEngine;

public class BotController : MonoBehaviour
{
    [SerializeField] private float speed = 6f;
    [SerializeField] private float targetY = 3.5f; // Фиксированная линия Y бота
    [SerializeField] private float defaultX = 0f;  // Центр стола по X

    private Transform ballTransform;
    private Vector3 defaultPosition;

    private void Start()
    {
        defaultPosition = new Vector3(defaultX, targetY, transform.position.z);
        
        Ball ball = FindFirstObjectByType<Ball>();
        if (ball != null)
        {
            ballTransform = ball.transform;
        }
    }

    private void Update()
    {
        if (ballTransform == null) return;

        // Рассчитываем целевую X, но не даём боту выходить за пределы стола (например, от -2.5f до 2.5f)
        float clampedBallX = Mathf.Clamp(ballTransform.position.x, -2.5f, 2.5f);

        float targetX = Mathf.MoveTowards(transform.position.x, clampedBallX, speed * Time.deltaTime);
        transform.position = new Vector3(targetX, targetY, transform.position.z);
    }

    // Метод для сброса позиции в центр
    public void ResetPosition()
    {
        // Рандомный X строго для бота в пределах корта (подставь свои границы X)
        float randomX = Random.Range(-2.2f, 2.2f);
        transform.position = new Vector3(randomX, targetY, transform.position.z);
    }
}