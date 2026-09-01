using UnityEngine;

public class TargetArea : MonoBehaviour
{
    [Header("Настройки зоны")]
    [Tooltip("Поставь галочку, если это зона НА СТОРОНЕ БОТА (куда бьёт игрок)")]
    [SerializeField] private bool isBotSideArea;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Проверяем наличие компонента Ball напрямую, без вызова CompareTag
        Ball ball = other.GetComponent<Ball>();
        if (ball != null)
        {
            ball.RegisterTargetHit(isBotSideArea);
        }
    }
}