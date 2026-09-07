using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private Vector2 minBounds = new Vector2(-5f, -6f);
    [SerializeField] private Vector2 maxBounds = new Vector2(5f, 1f);

    [Header("Анимации")]
    [SerializeField] private Animator animator;
    [SerializeField] private string primaryHitTrigger = "HitPrimary";
    [SerializeField] private string alternateHitTrigger = "HitAlternate";

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Ball ball;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        ball = FindFirstObjectByType<Ball>();
        if (ball != null)
        {
            ball.OnPlayerHit += HandlePlayerHit;
        }
        else
        {
            Debug.LogWarning("[PlayerController] Мяч не найден на сцене — анимации ударов работать не будут");
        }

        if (animator == null)
        {
            Debug.LogWarning("[PlayerController] Animator не назначен — анимации ударов не будут воспроизводиться");
        }
    }

    private void OnDestroy()
    {
        if (ball != null)
        {
            ball.OnPlayerHit -= HandlePlayerHit;
        }
    }

    // Вызывается мячом при каждом ударе игрока (включая подачу)
    private void HandlePlayerHit(HitType hitType)
    {
        Debug.Log($"[Анимация] Пришёл удар: {hitType}");

        if (animator == null) return;

        animator.SetTrigger(hitType == HitType.Primary
            ? primaryHitTrigger
            : alternateHitTrigger);
    }

    private void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(moveX, moveY).normalized;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;

        float clampedX = Mathf.Clamp(transform.position.x, minBounds.x, maxBounds.x);
        float clampedY = Mathf.Clamp(transform.position.y, minBounds.y, maxBounds.y);

        rb.position = new Vector2(clampedX, clampedY);
    }
}