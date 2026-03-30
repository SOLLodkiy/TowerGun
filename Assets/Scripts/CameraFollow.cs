using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Цель, за которой будет следовать камера
    public float smoothSpeed = 0.125f; // Скорость сглаживания движения камеры

    private Vector3 offset;
    private Vector3 shakeOffset;
    public float shakeTime;
    public float shakeAmount;

    void Start()
    {
        // Расчет начального смещения камеры относительно цели
        offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        if (target != null)
        {
            // Получаем новое положение камеры
            Vector3 targetPosition = target.position + offset;

            // Ограничиваем движение камеры только вверх
            if (targetPosition.y < transform.position.y)
            {
                targetPosition.y = transform.position.y;
            }

            // Сглаживание движения камеры
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed);

            // Применение тряски
            if (shakeTime > 0)
            {
                Vector3 shake = Random.insideUnitSphere * shakeAmount;
                smoothedPosition += shake;
                shakeTime -= Time.deltaTime;
            }

            transform.position = new Vector3(transform.position.x, smoothedPosition.y, transform.position.z);
        }
    }

    public void SetShake(float amount, float duration)
    {
        shakeAmount = amount;
        shakeTime = duration;
    }
}