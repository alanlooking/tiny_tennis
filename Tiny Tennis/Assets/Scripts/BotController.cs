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
        
        Ball ball = FindObjectOfType<Ball>();
        if (ball != null)
        {
            ballTransform = ball.transform;
        }
    }

    private void Update()
    {
        if (ballTransform == null) return;

        // Бот следит за мячом по горизонтали X
        float targetX = Mathf.MoveTowards(transform.position.x, ballTransform.position.x, speed * Time.deltaTime);
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