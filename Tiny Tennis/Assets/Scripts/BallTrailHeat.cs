using UnityEngine;

// Красит след мяча: чем длиннее розыгрыш, тем "горячее" цвет
// (от белого к красному). Вешать на объект мяча.
[RequireComponent(typeof(TrailRenderer))]
public class BallTrailHeat : MonoBehaviour
{
    [Header("Нагрев")]
    [Tooltip("Сколько ударов в розыгрыше нужно для максимального нагрева")]
    [SerializeField] private int maxHitsForFullHeat = 10;
    [SerializeField] private Color coldColor = new Color(0.8f, 0.9f, 1f, 0.55f);
    [SerializeField] private Color hotColor = new Color(1f, 0.3f, 0.1f, 0.85f);

    [Header("Форма следа")]
    [SerializeField] private float trailTime = 0.4f;   // длина хвоста, сек
    [SerializeField] private float startWidth = 0.2f;
    [SerializeField] private float endWidth = 0f;

    [Header("Материал (опционально)")]
    [Tooltip("Можно оставить пустым — скрипт создаст простой. Надёжнее создать материал с шейдером Sprites/Default и перетащить сюда")]
    [SerializeField] private Material trailMaterial;

    private TrailRenderer trail;
    private Ball ball;
    private Gradient gradient;

    private void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        gradient = new Gradient();

        trail.time = trailTime;
        trail.startWidth = startWidth;
        trail.endWidth = endWidth;
        trail.minVertexDistance = 0.05f;

        if (trailMaterial != null)
        {
            trail.material = trailMaterial;
        }
        else
        {
            trail.material = new Material(Shader.Find("Sprites/Default"));
        }
    }

    private void Start()
    {
        ball = FindFirstObjectByType<Ball>();
        if (ball != null)
        {
            ball.OnRallyHit += HandleRallyHit;
            ball.OnRallyReset += HandleRallyReset;
        }
        SetTrailColor(0);
    }

    private void OnDestroy()
    {
        if (ball != null)
        {
            ball.OnRallyHit -= HandleRallyHit;
            ball.OnRallyReset -= HandleRallyReset;
        }
    }

    // Пока мяч не подан, он таскается за подающим — след не рисуем
    private void Update()
    {
        trail.emitting = (ball != null && ball.IsServed);
    }

    private void HandleRallyHit(int hitNumber, float power01)
    {
        SetTrailColor(hitNumber);
    }

    private void HandleRallyReset()
    {
        trail.Clear();
        SetTrailColor(0);
    }

    private void SetTrailColor(int hits)
    {
        float t = Mathf.Clamp01((float)hits / maxHitsForFullHeat);
        Color color = Color.Lerp(coldColor, hotColor, t);

        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),        // хвост прозрачный
                new GradientAlphaKey(color.a, 0.3f), // тело следа
                new GradientAlphaKey(0f, 1f)
            });

        trail.colorGradient = gradient;
    }
}