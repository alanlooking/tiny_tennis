using System.Collections;
using UnityEngine;

// Трясёт камеру при ударах. Сила зависит от скорости мяча:
// подача почти не трясёт, разгоненный мяч — заметно.
public class CameraShake : MonoBehaviour
{
    [Header("Сила (смещение в юнитах)")]
    [SerializeField] private float minIntensity = 0.06f;
    [SerializeField] private float maxIntensity = 0.25f;

    [Header("Длительность (сек)")]
    [SerializeField] private float minDuration = 0.08f;
    [SerializeField] private float maxDuration = 0.22f;

    [Header("Порог")]
    [Tooltip("Удары слабее этого (0..1) камеру не трясут — подача и лёгкие удары")]
    [SerializeField] private float powerThreshold = 0.15f;

    private Ball ball;
    private Vector3 basePosition;
    private Coroutine shakeRoutine;

    private void Start()
    {
        basePosition = transform.position;
        ball = FindFirstObjectByType<Ball>();
        if (ball != null) ball.OnRallyHit += HandleRallyHit;
    }

    private void OnDestroy()
    {
        if (ball != null) ball.OnRallyHit -= HandleRallyHit;
    }

    private void HandleRallyHit(int hitNumber, float power01)
    {
        if (power01 < powerThreshold) return;

        float intensity = Mathf.Lerp(minIntensity, maxIntensity, power01);
        float duration = Mathf.Lerp(minDuration, maxDuration, power01);

        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        transform.position = basePosition; // вернуть перед новой тряской
        shakeRoutine = StartCoroutine(ShakeRoutine(intensity, duration));
    }

    private IEnumerator ShakeRoutine(float intensity, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float damp = 1f - (elapsed / duration); // затухание к концу
            Vector2 offset = Random.insideUnitCircle * (intensity * damp);
            transform.position = basePosition + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }
        transform.position = basePosition;
    }
}