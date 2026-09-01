using UnityEngine;

public class TargetArea : MonoBehaviour
{
    [Header("Настройки зоны")]
    [Tooltip("Поставь галочку, если это зона НА СТОРОНЕ БОТА (куда бьёт игрок)")]
    [SerializeField] private bool isBotSideArea;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Если в зону влетел мяч
        if (other.CompareTag("Ball") || other.GetComponent<Ball>() != null)
        {
            Ball ball = other.GetComponent<Ball>();
            if (ball != null)
            {
                ball.RegisterTargetHit(isBotSideArea);
            }
        }
    }
}