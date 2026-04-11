using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImminentDanger : MonoBehaviour
{
    [Header("Distance-based speed")]
    public float farDistance    = 10f;
    public float closeDistance  = 5f;
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

    [Header("Links / UI")]
    public Transform player;
    public Image yellowWarning;
    public Image redWarning;
    public Camera mainCamera;

    private float extraSpeed;
    public float speedSaw;

    // ОПТИМИЗАЦИЯ: кэшируем transform пилы
    private Transform selfTransform;

    // ОПТИМИЗАЦИЯ: UI предупреждений меняется редко — отслеживаем состояние
    // чтобы не вызывать SetActive каждый кадр (SetActive дорогой вызов)
    private enum WarningState { None, Yellow, Red }
    private WarningState currentWarningState = WarningState.None;

    // ОПТИМИЗАЦИЯ: цвет redWarning меняется только при смене isInCamera —
    // кэшируем последнее состояние чтобы не писать в Image.color каждый кадр
    private bool lastInCameraState = false;
    private bool redWasActive = false;

    void Start()
    {
        extraSpeed    = extraStart;
        selfTransform = transform;
    }

    void Update()
    {
        extraSpeed = Mathf.Min(extraSpeed + extraGrowPerSec * Time.deltaTime, extraMax);

        // ОПТИМИЗАЦИЯ: sqrMagnitude дешевле чем Vector3.Distance (нет sqrt)
        // Используем только по Y так как пила движется вертикально
        float distance = Mathf.Abs(selfTransform.position.y - player.position.y);

        float baseSpeed;
        if      (distance >= farDistance)   baseSpeed = speedFar;
        else if (distance >= closeDistance) baseSpeed = speedMid;
        else if (distance >= soCloseDistance) baseSpeed = speedNear;
        else                                baseSpeed = speedSoClose;

        float currentSpeed = baseSpeed + extraSpeed;
        selfTransform.Translate(Vector3.up * currentSpeed * Time.deltaTime);

        HandleUI(distance);
        speedSaw = currentSpeed;
    }

    void HandleUI(float d)
    {
        WarningState needed;
        if      (d < soCloseDistance) needed = WarningState.Red;
        else if (d < closeDistance)   needed = WarningState.Yellow;
        else                          needed = WarningState.None;

        // ОПТИМИЗАЦИЯ: SetActive только при реальном изменении состояния
        if (needed != currentWarningState)
        {
            yellowWarning.gameObject.SetActive(needed == WarningState.Yellow);
            redWarning.gameObject.SetActive(needed == WarningState.Red);
            currentWarningState = needed;
            redWasActive = (needed == WarningState.Red);
        }

        // Обновляем альфу красного предупреждения только если оно активно
        if (redWasActive)
        {
            bool isInCamera = IsInCameraView();
            // ОПТИМИЗАЦИЯ: пишем в Image.color только при смене состояния
            if (isInCamera != lastInCameraState)
            {
                Color c = redWarning.color;
                c.a = isInCamera ? 0.5f : 1f;
                redWarning.color = c;
                lastInCameraState = isInCamera;
            }
        }
    }

    bool IsInCameraView()
    {
        Vector3 vp = mainCamera.WorldToViewportPoint(selfTransform.position);
        return vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f && vp.z > 0f;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerBullet")) Destroy(other.gameObject);
        if (other.CompareTag("EnemyBullet"))  Destroy(other.gameObject);
        if (other.CompareTag("Enemy"))        other.GetComponent<EnemyPlatform>()?.Die();
    }
}