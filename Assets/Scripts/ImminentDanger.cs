using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImminentDanger : MonoBehaviour
{
    [Header("Distance-based speed")]
    public float farDistance     = 10f;
    public float closeDistance   = 5f;
    public float soCloseDistance = 2f;

    [Header("Base speed values (by distance)")]
    public float speedFar     = 6f;
    public float speedMid     = 3f;
    public float speedNear    = 2f;
    public float speedSoClose = 1.5f;

    [Header("Progression (extra speed)")]
    public float extraStart      = 0f;
    public float extraGrowPerSec = 0.05f;
    public float extraMax        = 4f;

    [Header("Slow Saw")]
    [Tooltip("Если включено — пила движется медленнее чем обычно")]
    public bool slowSawEnabled = false;
    [Tooltip("Коэффициент замедления пилы (0.5 = вдвое медленнее)")]
    [Range(0.1f, 0.9f)]
    public float slowSawMultiplier = 0.6f;

    [Header("Links / UI")]
    public Transform player;
    public Camera mainCamera;

    [Header("Danger Indicator")]
    public Image dangerFill;
    public RectTransform playerIcon;
    public RectTransform sawIcon;
    public Color colorSafe   = new Color(0.39f, 0.90f, 0.13f);
    public Color colorMid    = new Color(0.94f, 0.82f, 0.15f);
    public Color colorDanger = new Color(0.89f, 0.29f, 0.29f);

    public float indicatorStartDistance = 40f;
    public float indicatorFullDistance  = 2f;

    public float iconBaseSize   = 36f;
    public float iconMaxSize    = 52f;
    public float pulseThreshold = 0.65f;
    public float pulseSpeed     = 3f;

    private float extraSpeed;
    public float speedSaw;

    private Transform selfTransform;
    private float smoothedFill = 0f;

    void Start()
    {
        extraSpeed    = extraStart;
        selfTransform = transform;
    }

    void Update()
    {
        extraSpeed = Mathf.Min(extraSpeed + extraGrowPerSec * Time.deltaTime, extraMax);

        float distance = Mathf.Abs(selfTransform.position.y - player.position.y);

        float baseSpeed;
        if      (distance >= farDistance)     baseSpeed = speedFar;
        else if (distance >= closeDistance)   baseSpeed = speedMid;
        else if (distance >= soCloseDistance) baseSpeed = speedNear;
        else                                  baseSpeed = speedSoClose;

        float currentSpeed = baseSpeed + extraSpeed;

        // Замедление пилы если включено
        if (slowSawEnabled)
            currentSpeed *= slowSawMultiplier;

        selfTransform.Translate(Vector3.up * currentSpeed * Time.deltaTime);
        speedSaw = currentSpeed;

        UpdateDangerIndicator(distance);
    }

    bool IsInCameraView()
    {
        Vector3 vp = mainCamera.WorldToViewportPoint(selfTransform.position);
        return vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f && vp.z > 0f;
    }

    private void UpdateDangerIndicator(float distance)
    {
        float linear     = Mathf.InverseLerp(indicatorStartDistance, indicatorFullDistance, distance);
        float targetFill = Mathf.Pow(linear, 2.5f);

        smoothedFill = Mathf.Lerp(smoothedFill, targetFill, Time.deltaTime * 4f);
        dangerFill.fillAmount = smoothedFill;

        Color targetColor;
        if      (smoothedFill < 0.4f) targetColor = colorSafe;
        else if (smoothedFill < 0.7f) targetColor = colorMid;
        else                          targetColor = colorDanger;
        dangerFill.color = Color.Lerp(dangerFill.color, targetColor, Time.deltaTime * 6f);

        UpdateIconScale();
    }

    private void UpdateIconScale()
    {
        float baseScale = Mathf.Lerp(iconBaseSize, iconMaxSize, smoothedFill);

        float pulse = 0f;
        if (smoothedFill >= pulseThreshold)
        {
            float pulseIntensity    = Mathf.InverseLerp(pulseThreshold, 1f, smoothedFill);
            float currentPulseSpeed = pulseSpeed + pulseIntensity * 4f;
            pulse = Mathf.Sin(Time.time * currentPulseSpeed) * pulseIntensity * 6f;
        }

        float   finalSize = baseScale + pulse;
        Vector2 size      = new Vector2(finalSize, finalSize);

        if (playerIcon != null) playerIcon.sizeDelta = size;
        if (sawIcon    != null) sawIcon.sizeDelta    = size;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerBullet")) Destroy(other.gameObject);
        if (other.CompareTag("EnemyBullet"))  Destroy(other.gameObject);
        if (other.CompareTag("Enemy"))        other.GetComponent<EnemyPlatform>()?.Die();
    }
}