using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public float shakeAmount = 0.1f; // Сила тряски
    public float shakeDuration = 0.1f; // Длительность тряски

    private float shakeTime;
    private Vector3 originalPosition;

    void Start()
    {
        originalPosition = transform.localPosition;
    }

    void Update()
    {
        if (shakeTime > 0)
        {
            Vector3 shakeOffset = Random.insideUnitSphere * shakeAmount;
            transform.localPosition = originalPosition + shakeOffset;
            shakeTime -= Time.deltaTime;
        }
        else
        {
            shakeTime = 0;
            transform.localPosition = originalPosition;
        }
    }

    public void Shake(float amount, float duration)
    {
        shakeAmount = amount;
        shakeDuration = duration;
        shakeTime = duration;
    }
}