using UnityEngine;

public class Ball : MonoBehaviour
{
    [SerializeField] private float serveSpeed = 6f;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform botTransform;

    private Rigidbody2D rb;
    private bool isServed = false;
    private bool isPlayerServing = true; // true — подает игрок, false — бот

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        ResetForServe(true); // Начинаем с подачи игрока
    }

    private void Update()
    {
        // Если мяч еще не введен в игру — он привязан к подающему
        if (!isServed)
        {
            Transform currentServer = isPlayerServing ? playerTransform : botTransform;

            // Мяч висит чуть впереди подающего (вдоль Y)
            float offsetYSide = isPlayerServing ? 0.6f : -0.6f;
            transform.position = currentServer.position + new Vector3(0f, offsetYSide, 0f);

            // Подача по кнопке Space (если подает бот — подает автоматически через 1 сек)
            if (isPlayerServing && Input.GetKeyDown(KeyCode.Space))
            {
                ExecuteServe();
            }
        }
    }

    public void ExecuteServe()
    {
        isServed = true;
        // Направление строго прямо: вверх (+1), если подает игрок, или вниз (-1), если бот
        Vector2 serveDirection = isPlayerServing ? Vector2.up : Vector2.down;
        rb.linearVelocity = serveDirection * serveSpeed;
    }

    public void ResetForServe(bool playerServes)
    {
        isServed = false;
        isPlayerServing = playerServes;
        rb.linearVelocity = Vector2.zero;
    }
}