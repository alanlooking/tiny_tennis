using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private Vector2 minBounds = new Vector2(-5f, -6f);
    [SerializeField] private Vector2 maxBounds = new Vector2(5f, 1f);

    private Rigidbody2D rb;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(moveX, moveY).normalized;
    }

    private void FixedUpdate()
    {
        // Движение через физику, благодаря чему персонаж упирается в стол как в стену
        rb.linearVelocity = moveInput * moveSpeed;

        // Ограничение внешних границ корта (чтобы игрок не убегал за экран)
        float clampedX = Mathf.Clamp(transform.position.x, minBounds.x, maxBounds.x);
        float clampedY = Mathf.Clamp(transform.position.y, minBounds.y, maxBounds.y);

        rb.position = new Vector2(clampedX, clampedY);
    }
}