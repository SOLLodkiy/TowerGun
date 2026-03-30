using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImminentDanger : MonoBehaviour
{
    [Header("Distance‑based speed")]
    public float farDistance   = 10f;   // ≥ — самая высокая ступень
    public float closeDistance = 5f;
    public float soCloseDistance = 2f;  // ≤ — самая низкая ступень

    [Header("Base speed values (by distance)")]
    public float speedFar      = 6f;    // когда очень далеко
    public float speedMid      = 3f;    // средняя дистанция
    public float speedNear     = 2f;    // близко
    public float speedSoClose  = 1.5f;  // почти вплотную

    [Header("Progression (extra speed)")]
    public float extraStart    = 0f;    // начальный бонус
    public float extraGrowPerSec = 0.05f; // на сколько растёт каждую секунду
    public float extraMax      = 4f;    // «потолок» бонуса

    [Header("Links / UI")]
    public Transform player;
    public Image yellowWarning;
    public Image redWarning;
    public Camera mainCamera;

    private float extraSpeed;           // растущий бонус
    private float currentSpeed;         // итоговая скорость = base + extra
    public float speedSaw;

    void Start()
    {
        extraSpeed = extraStart;
    }

    void Update()
    {
        /* 1. растим бонус со временем */
        extraSpeed = Mathf.Min(extraSpeed + extraGrowPerSec * Time.deltaTime, extraMax);

        /* 2. выбираем базовую скорость по дистанции до игрока */
        float distance = Vector3.Distance(transform.position, player.position);

        float baseSpeed;
        if      (distance >= farDistance)                 baseSpeed = speedFar;
        else if (distance >= closeDistance)               baseSpeed = speedMid;
        else if (distance >= soCloseDistance)             baseSpeed = speedNear;
        else                                              baseSpeed = speedSoClose;

        /* 3. окончательная скорость пилы */
        currentSpeed = baseSpeed + extraSpeed;            // ← вся «магия» прогрессии
        transform.Translate(Vector3.up * currentSpeed * Time.deltaTime);

        /* 4. предупреждения на экране */
        HandleUI(distance);

        speedSaw = currentSpeed;
    }

    void HandleUI(float d)
    {
        if (d < soCloseDistance)
        {
            yellowWarning.gameObject.SetActive(false);
            redWarning.gameObject.SetActive(true);
        }
        else if (d < closeDistance)
        {
            yellowWarning.gameObject.SetActive(true);
            redWarning.gameObject.SetActive(false);
        }
        else
        {
            yellowWarning.gameObject.SetActive(false);
            redWarning.gameObject.SetActive(false);
        }

        if (redWarning.gameObject.activeSelf)
        {
            bool isInCamera = IsInCameraView();
            Color c = redWarning.color;
            c.a = isInCamera ? 0.5f : 1f;
            redWarning.color = c;
        }
    }

    bool IsInCameraView()
    {
        Vector3 vp = mainCamera.WorldToViewportPoint(transform.position);
        return vp.x > 0 && vp.x < 1 && vp.y > 0 && vp.y < 1 && vp.z > 0;
    }

    /* пуля / враг */
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerBullet"))  Destroy(other.gameObject);
        if (other.CompareTag("EnemyBullet"))  Destroy(other.gameObject);
        if (other.CompareTag("Enemy"))         other.GetComponent<EnemyPlatform>()?.Die();
    }
}
