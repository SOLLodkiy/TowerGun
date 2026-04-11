using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    [Tooltip("Время сглаживания. Чем меньше, тем резче камера догоняет игрока.")]
    public float smoothTime = 0.15f; 

    [Header("Shake Settings")]
    [Range(0f, 5f)]
    [Tooltip("Глобальный множитель тряски. 1 — стандарт, 0 — выключить, 2+ — безумие.")]
    public float shakeIntensity = 1.0f; 

    private Vector3 offset;
    private float shakeTime;
    private float shakeAmount;

    private Transform camTransform;
    
    private Vector3 currentCleanPos;
    private float currentVelocityY = 0f;
    private float highestY;
    private float startX;

    void Start()
    {
        camTransform = transform;
        offset = camTransform.position - target.position;

        startX = camTransform.position.x;
        highestY = target.position.y + offset.y;

        currentCleanPos = camTransform.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        float targetY = target.position.y + offset.y;

        if (targetY > highestY)
        {
            highestY = targetY;
        }

        float newY = Mathf.SmoothDamp(currentCleanPos.y, highestY, ref currentVelocityY, smoothTime);
        currentCleanPos = new Vector3(startX, newY, currentCleanPos.z);

        float shakeX = 0f;
        float shakeY = 0f;

        if (shakeTime > 0f)
        {
            // Умножаем базовую силу тряски на твой глобальный коэффициент
            float finalAmount = shakeAmount * shakeIntensity;
            
            shakeX = Random.Range(-finalAmount, finalAmount);
            shakeY = Random.Range(-finalAmount, finalAmount);
            shakeTime -= Time.unscaledDeltaTime; 
        }

        camTransform.position = currentCleanPos + new Vector3(shakeX, shakeY, 0f);
    }

    public void SetShake(float amount, float duration)
    {
        shakeAmount = amount;
        shakeTime = duration;
    }
}