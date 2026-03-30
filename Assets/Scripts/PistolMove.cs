using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PistolMove : MonoBehaviour
{
    public GunMove playerScript; // Ссылка на скрипт игрока

    public float recoilForce = 8f; // Сила отдачи при выстреле
    public GameObject bulletPrefab; // Префаб пули
    public float bulletSpeed = 15f; // Скорость пули
    public Transform bulletSpawn; // Точка появления пули

    [Header("Intended velocity estimator")]
    public Vector2 intendedVelocity;           // «свободная» скорость (без учёта стен)
    public float intendedVelFollow = 10f;      // насколько быстро тянем к факту


    [Header("Slow‑motion settings")]
    [Range(0.05f, 1f)] public float slowMotionMinScale = 0.4f; // минимальное TimeScale
    private float defaultSlowMotionMinScale = 0.4f;              // дефолтное значение
    public bool slowMoModifierEnabled = false;                  // булева переменная для изменения
    [Tooltip("Через сколько секунд удержания включаем плавное замедление")]
    public float slowMotionDelay = 0.1f;
    [Tooltip("Скорость, с которой TimeScale доходит до минимума (1/сек)")]
    public float slowMotionBuildSpeed = 2f;
    private float defaultSlowMotionBuildSpeed = 2f;
    [Tooltip("Скорость возвращения TimeScale к 1 (1/сек)")]
    public float slowMotionReturnSpeed = 3f;

    /*‑‑‑ Показатель визуального эффекта ‑‑‑*/
    [Header("Post‑process (edge blur)")]
    public Volume slowMoVolume;             // ссылка на Global Volume
    [Range(0f, 1f)] public float blurMaxIntensity = 0.7f;

    private bool timeIsModified = false;     // сейчас TimeScale ≠ 1?
    private float holdTimer = 0.1f;            // сколько держим кнопку
    private float originalFixedDeltaTime;    // «эталон» для физики
    private Vignette vignettePP;             // пост‑процессинг «размытие»


    //Переменные для патронов
    public int maxAmmo = 8;
    public int currentAmmo;
    public float reloadInterval = 0.5f;
    public float boostedReloadInterval = 0.35f;
    public float boostDuration = 2f;
    private bool isBoosted = false;
    private float boostTimer = 0f;

    // UI элементы для патронов
    public GameObject[] ammoSlots; // Массив слотов патронов (8 слотов)
    public Sprite fullAmmoSprite; // Спрайт заполненного патрона
    public Sprite emptyAmmoSprite; // Спрайт пустого патрона
    public GameObject ammo;
    public Slider reloadSlider; // Ссылка на слайдер перезарядки

    private Rigidbody2D rb;
    public Animator animator; // Ссылка на Animator компонент
    public float lastAngle = 0f; // Последний угол поворота
    public bool canShoot = false; // Может ли игрок стрелять

    public CameraFollow cameraFollow; // Компонент CameraShake
    public Image vignetteImage; // UI элемент для виньетки
    public Color vignetteColor = new Color(0f, 0f, 0f, 0.1f); // Цвет виньетки
    public float vignetteThreshold = 0.25f; // Порог для виньетки
    public float vignetteMinIntensity = 0.1f; // Минимальная интенсивность виньетки
    public float vignetteMaxIntensity = 0.3f; // Максимальная интенсивность виньетки

    public bool isWaitingToShoot = false; // Флаг ожидания стрельбы

    // Новые переменные для буста перезарядки
    private float noFireTimer = 0f; // Таймер времени без стрельбы
    public float noFireBoostThreshold = 2f; // Время без стрельбы для активации буста

    private float fireCooldown = 0.14f; // Время между выстрелами
    private float lastFireTime = 0f;

    [Header("Trap slow‑mo")]
    [Tooltip("Минимальный TimeScale, когда игрок почти касается ловушки")]
    [Range(0.02f,1f)] public float trapMaxSlowScale = 0.15f;
    [Tooltip("Дальность, с которой начинаем замедлять")]
    public float trapSlowRadius = 6f;          // м
    [Tooltip("Дальность, когда замедление максимальное")]
    public float trapMinDistance = 1f;         // м

    public GameObject Saw;

    public bool BabyMode = false; // Булевая переменная для режима «малыш»

    public float sessionStartTime = 0f; // Когда игрок сделал первый выстрел
    public bool sessionStarted = false; // Флаг, чтобы запускать только один раз
    public event System.Action OnSessionStarted;


    void Start()
    {
        StopAllCoroutines(); // Остановить все корутины на этом объекте
    
        Input.ResetInputAxes(); // Сбросить нажатия перед началом игры
        rb = GetComponent<Rigidbody2D>();
        ammo.SetActive(false);
        reloadSlider.gameObject.SetActive(false); // Скрыть слайдер в начале
        Saw.SetActive(false);

        currentAmmo = maxAmmo; // Инициализируем максимальным количеством патронов
        vignetteImage.enabled = false;
        UpdateAmmoUI(); // Обновляем UI патронов
        StartCoroutine(Reload()); // Запускаем перезарядку

        // Направляем курсор вправо от персонажа на старте
        Vector2 initialCursorPosition = new Vector2(transform.position.x + 1f, transform.position.y);
        Vector2 direction = initialCursorPosition - (Vector2)transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

        canShoot = true;

        // запоминаем дефолтный fixedDeltaTime
        originalFixedDeltaTime = Time.fixedDeltaTime;

        // достаём Vignette из профиля
        if (slowMoVolume != null)
        {
            slowMoVolume.profile.TryGet(out vignettePP);
        }

        // ---------- BabyMode handler ----------
        ApplyBabyMode();
    }

    void Update()
    {
        // Обновляем минимальный Slow Motion, если включен модификатор
        if (slowMoModifierEnabled)
        {
            slowMotionMinScale = defaultSlowMotionMinScale - 0.35f;
            if (slowMotionMinScale < 0.01f) slowMotionMinScale = 0.01f; // защита от отрицательного значения
            slowMotionBuildSpeed = defaultSlowMotionBuildSpeed + 1f;
            if (slowMotionBuildSpeed < 0.01f) slowMotionBuildSpeed = 0.01f; // защита от отрицательного значения
        }
        else
        {
            slowMotionMinScale = defaultSlowMotionMinScale;
            slowMotionBuildSpeed = defaultSlowMotionBuildSpeed;
        }

        // ---------- Slow‑motion handler (NEW) ----------
        HandleSlowMotion();
        // ------------------------------------------------


        if (canShoot && !isWaitingToShoot)
        {
            // Получаем позицию курсора
            Vector2 cursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // Вычисляем направление к курсору
            Vector2 direction = cursorPosition - (Vector2)transform.position;
            // Поворачиваем персонажа в направлении курсора
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

            // Отзеркаливаем персонажа относительно оси Y
            FlipCharacter(angle);

            if (Input.GetMouseButtonUp(0) && Time.unscaledTime >= lastFireTime + fireCooldown) // При отпускании кнопки
            {
                Fire(direction);
                lastFireTime = Time.unscaledTime; // важно считать в реальном времени
                playerScript.StartText.SetActive(false);
                ammo.SetActive(true);
                playerScript.PauseButton.SetActive(true);
                Saw.SetActive(true);

                // Сбрасываем таймер без стрельбы
                noFireTimer = 0f;

                // Обрабатываем объекты при выстреле
                playerScript.HandleObjectsOnShot();
            }
        }

        // Обновляем таймер без стрельбы
        if (canShoot)
        {
            noFireTimer += Time.deltaTime;
            if (noFireTimer >= noFireBoostThreshold && !isBoosted)
            {
                StartCoroutine(StartBoostReload());
            }
        }

        // немного «пришиваем» оценку к реальной скорости, чтобы не разъезжалась
        intendedVelocity = Vector2.MoveTowards(intendedVelocity, rb.linearVelocity, intendedVelFollow * Time.deltaTime);

    }

    void HandleSlowMotion()
    {
        if (!canShoot) return;               // пауза / меню

        /* A. Aim‑slow‑mo ------------------------------------------------*/
        float aimScale;
        if (Input.GetMouseButton(0))
        {
            if (Input.GetMouseButtonDown(0)) holdTimer = 0f;
            holdTimer += Time.unscaledDeltaTime;

            float t = 0f;
            if (holdTimer > slowMotionDelay)
                t = Mathf.Clamp01((holdTimer - slowMotionDelay) * slowMotionBuildSpeed);

            aimScale = Mathf.Lerp(1f, slowMotionMinScale, t);
        }
        else
        {
            aimScale = Mathf.MoveTowards(Time.timeScale, 1f,
                        Time.unscaledDeltaTime * slowMotionReturnSpeed);
        }

        /* B. Trap‑slow‑mo -----------------------------------------------*/
        float trapScale = GetTrapSlowScale();         // 1 … trapMaxSlowScale

        /* C. Итог --------------------------------------------------------*/
        float finalScale = Mathf.Min(aimScale, trapScale);
        ApplyTimeScale(finalScale);
    }

    public void ApplyBabyMode()
    {
        Vector3 scale = transform.localScale;

        float signY = Mathf.Sign(scale.y); // сохраняем флип

        if (BabyMode)
        {
            scale.x = 0.65f;
            scale.y = signY * 0.65f;
            scale.z = 0.65f;
            recoilForce = 9.5f;
        }
        else
        {
            scale.x = 0.8f;
            scale.y = signY * 0.8f;
            scale.z = 0.8f;
            recoilForce = 8f;
        }

        transform.localScale = scale;
    }

    float GetTrapSlowScale()
    {
        float minDist = trapSlowRadius + 0.01f;       // чуть > радиуса

        // берём все коллайдеры в круге; тег фильтруем вручную
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, trapSlowRadius);
        foreach (var col in hits)
        {
            if (col.CompareTag("Dead") || col.CompareTag("DeadlyBlock"))
            {
                float d = Vector2.Distance(transform.position, col.transform.position);
                if (d < minDist) minDist = d;
            }
        }

        if (minDist > trapSlowRadius) return 1f;      // ловушек поблизости нет

        float t = Mathf.InverseLerp(trapSlowRadius, trapMinDistance, minDist); // 0→1
        return Mathf.Lerp(1f, trapMaxSlowScale, t);   // 1 … trapMaxSlowScale
    }

    /* — вспомогательный: выставляет TimeScale и пост‑эффект — */
    void ApplyTimeScale(float value)
    {
        if (Mathf.Approximately(Time.timeScale, value)) return;

        Time.timeScale      = value;
        Time.fixedDeltaTime = originalFixedDeltaTime * value;
        timeIsModified      = !Mathf.Approximately(value, 1f);

        if (vignettePP != null)
        {
            float minPossible = Mathf.Min(slowMotionMinScale, trapMaxSlowScale);
            float k = Mathf.InverseLerp(1f, minPossible, value);   // 1→0
            vignettePP.intensity.value = Mathf.Lerp(0f, blurMaxIntensity, 1f - k);
            vignettePP.smoothness.value = 1f;
        }
    }

    public void ForceStopSlowMotion()
    {
        // Полностью обнуляем «буллет‑тайм», если он был
        ApplyTimeScale(1f);         // вернёт Time.timeScale и blur к 1
        timeIsModified = false;
        holdTimer      = 0f;
    }


    void FlipCharacter(float currentAngle)
    {
        if ((currentAngle > 90f || currentAngle < -90f) != (lastAngle > 90f || lastAngle < -90f))
        {
            transform.localScale = new Vector3(transform.localScale.x, -transform.localScale.y, transform.localScale.z);
        }
        lastAngle = currentAngle;
    }

    void Fire(Vector2 direction)
    {
        if (!sessionStarted)
        {
            sessionStartTime = Time.time; // Запоминаем время начала
            sessionStarted = true;
            OnSessionStarted?.Invoke();
        }

        if (currentAmmo > 0)
        {
            direction.Normalize();
            PlayerPrefs.SetInt("TotalShots", PlayerPrefs.GetInt("TotalShots", 0) + 1);

            GameObject bullet = Instantiate(bulletPrefab, bulletSpawn.position, Quaternion.identity);
            Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
            bulletRb.linearVelocity = direction * bulletSpeed;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            bullet.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

            //Отвечает за передвижение отталкиванием:
            rb.AddForce(-direction * recoilForce, ForceMode2D.Impulse);

            // Δv от импульса отдачи (импульс / масса)
            Vector2 dv = (-direction * recoilForce) / rb.mass;
            intendedVelocity += dv;

            // такой же кламп, как у реальной скорости
            if (intendedVelocity.magnitude > playerScript.maxSpeed)
                intendedVelocity = intendedVelocity.normalized * playerScript.maxSpeed;

            if (rb.linearVelocity.magnitude > playerScript.maxSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * playerScript.maxSpeed;
            }

            currentAmmo--;
            UpdateAmmoUI();

            if (currentAmmo == 0 && !isBoosted)
            {
                StartCoroutine(StartBoostReload());
            }

            float shakeIntensity = Mathf.Lerp(0.1f, 0.16f, 1 - (float)currentAmmo / maxAmmo);
            cameraFollow.SetShake(shakeIntensity, 0.1f);

            float vignetteIntensity = Mathf.Lerp(vignetteMinIntensity, vignetteMaxIntensity, 1 - (float)currentAmmo / maxAmmo);
            vignetteImage.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, vignetteIntensity);
            vignetteImage.enabled = true;

            if (animator != null)
            {
                animator.Play("Shoot", 0, 0f);
            }
        }
    }

    // ----- ниже код перезарядки, UI и т.д. без изменений -----
    IEnumerator Reload()
    {
        while (true)
        {
            float interval = isBoosted ? boostedReloadInterval : reloadInterval;

            if (currentAmmo < maxAmmo)
            {
                reloadSlider.gameObject.SetActive(true);
                reloadSlider.value = 0;
                float elapsedTime = 0f;

                while (elapsedTime < interval)
                {
                    elapsedTime += Time.deltaTime;
                    reloadSlider.value = elapsedTime / interval;
                    yield return null;
                }

                if (currentAmmo < maxAmmo)
                {
                    currentAmmo++;
                    UpdateAmmoUI();
                    float vignetteIntensity = Mathf.Lerp(vignetteMinIntensity, vignetteMaxIntensity, 1 - (float)currentAmmo / maxAmmo);
                    vignetteImage.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, vignetteIntensity);
                    vignetteImage.enabled = true;
                }
            }
            else
            {
                reloadSlider.gameObject.SetActive(false);
            }

            if (isBoosted)
            {
                boostTimer += interval;
                if (boostTimer >= boostDuration)
                {
                    isBoosted = false;
                    boostTimer = 0f;
                }
            }

            yield return null;
        }
    }

    IEnumerator StartBoostReload()
    {
        isBoosted = true;
        boostTimer = 0f;
        yield return new WaitForSeconds(boostDuration);
        isBoosted = false;
        boostTimer = 0f;
    }

    void UpdateAmmoUI()
    {
        for (int i = 0; i < ammoSlots.Length; i++)
        {
            if (i < currentAmmo)
            {
                ammoSlots[i].GetComponent<Image>().sprite = fullAmmoSprite;
            }
            else
            {
                ammoSlots[i].GetComponent<Image>().sprite = emptyAmmoSprite;
            }
        }
    }
}
