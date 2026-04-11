using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PistolMove : MonoBehaviour
{
    public GunMove playerScript;

    [Header("Recoil Feel Settings")]
    [SerializeField] private float recoilMultiplier = 0.7f;
    [SerializeField] private float horizontalRecoilMultiplier = 0.6f;
    [SerializeField] private float verticalRecoilMultiplier = 0.85f;
    [SerializeField] private float playerMass = 1.3f;
    [SerializeField] private float recoilForce = 7f;

    public GameObject bulletPrefab;
    public float bulletSpeed = 15f;
    public Transform bulletSpawn;

    [Header("Bullet Pool")]
    public int bulletPoolSize = 20;
    private Queue<GameObject> bulletPool = new Queue<GameObject>();
    private Queue<Rigidbody2D> bulletRbPool = new Queue<Rigidbody2D>();

    [Header("Intended velocity estimator")]
    public Vector2 intendedVelocity;
    public float intendedVelFollow = 10f;

    [Header("Slow-motion settings")]
    [Range(0.05f, 1f)] public float slowMotionMinScale = 0.4f;
    private float defaultSlowMotionMinScale = 0.4f;
    public bool slowMoModifierEnabled = false;
    public float slowMotionDelay = 0.1f;
    public float slowMotionBuildSpeed = 2f;
    private float defaultSlowMotionBuildSpeed = 2f;
    public float slowMotionReturnSpeed = 3f;

    private bool timeIsModified = false;
    private float holdTimer = 0.1f;
    private float originalFixedDeltaTime;

    // ── Патроны ───────────────────────────────────────────────────────────────
    public int maxAmmo = 8;
    public int currentAmmo;
    public float reloadInterval = 0.5f;
    public float boostedReloadInterval = 0.35f;
    public float boostDuration = 2f;
    private bool isBoosted = false;
    private float boostTimer = 0f;
    private bool isBoostCoroutineRunning = false;

    // ── Ammo UI ───────────────────────────────────────────────────────────────
    [Header("Ammo UI")]
    public GameObject ammo;
    public Sprite ammoSprite;
    public Color ammoLoadedColor    = Color.white;
    public Color ammoEmptyColor     = Color.black;
    public Color ammoReloadingColor = new Color(1f, 0.65f, 0.1f, 1f);
    public Color ammoBgOuterColor   = Color.black;
    public Color ammoBgInnerColor   = new Color(0.25f, 0.25f, 0.25f, 1f);
    public float ammoBgOuterPadding = 8f;
    public float ammoBgInnerPadding = 4f;
    public Vector2 ammoSlotSize     = new Vector2(28f, 40f);
    public float ammoSlotSpacing    = 4f;
    public int ammoMaxPerRow        = 15;

    public Slider reloadSlider;

    private Image[] ammoSlotImages;
    private Image[] ammoFillImages;
    private float reloadProgress  = 0f;
    private int lastChargingSlot  = -1;
    private float lastFillAmount  = 0f;

    private Rigidbody2D rb;
    public Animator animator;
    public float lastAngle = 0f;
    public bool canShoot = false;

    public CameraFollow cameraFollow;

    // ── Виньетка (UI Image, без пост-процессинга) ─────────────────────────────
    [Header("Vignette UI")]
    [Tooltip("Растянутый на весь экран Image с чёрным цветом. Raycast Target — выключить.")]
    public Image vignetteImage;
    [Tooltip("Альфа виньетки когда магазин полный")]
    public float vignetteMinAlpha  = 0.05f;
    [Tooltip("Альфа виньетки когда магазин пустой")]
    public float vignetteMaxAlpha  = 0.35f;
    [Tooltip("Скорость плавного изменения альфы")]
    public float vignetteSmoothing = 8f;
    // Целевое значение альфы — пересчитывается при выстреле/перезарядке,
    // сама Image обновляется плавно в Update — один Lerp вместо записи Color каждый кадр
    private float vignetteTargetAlpha = 0f;

    public bool isWaitingToShoot = false;

    private float noFireTimer = 0f;
    public float noFireBoostThreshold = 2f;

    private float fireCooldown = 0.14f;
    private float lastFireTime = 0f;

    [Header("Trap slow-mo")]
    [Range(0.02f, 1f)] public float trapMaxSlowScale = 0.15f;
    public float trapSlowRadius  = 6f;
    public float trapMinDistance = 1f;

    private static readonly Collider2D[] trapHitBuffer = new Collider2D[32];
    private Collider2D selfCollider;

    [Tooltip("Как часто (сек) проверять близость ловушек. 0.05 = 20 раз/сек")]
    public float trapCheckInterval = 0.05f;
    private float trapCheckTimer   = 0f;
    private float cachedTrapScale  = 1f;

    // ── Горизонтальное трение ─────────────────────────────────────────────────
    [Header("Horizontal Friction")]
    [Tooltip("Тормозит боковое скольжение. 0 = нет трения, 1-3 = аркадное ощущение.")]
    public float horizontalDrag = 1.5f;

    public GameObject Saw;
    public bool BabyMode = false;

    public float sessionStartTime = 0f;
    public bool sessionStarted    = false;
    public event System.Action OnSessionStarted;

    private Camera mainCamera;

    private int pendingShotCount = 0;

    private Vector2 aimDirection = Vector2.right;
    private bool isPressingFire  = false;

    // ═════════════════════════════════════════════════════════════════════════
    void Start()
    {
        StopAllCoroutines();
        Input.ResetInputAxes();

        rb           = GetComponent<Rigidbody2D>();
        selfCollider = GetComponent<Collider2D>();
        mainCamera   = Camera.main;

        ammo.SetActive(false);
        if (reloadSlider != null) reloadSlider.gameObject.SetActive(false);
        Saw.SetActive(false);

        BuildAmmoUI();
        InitBulletPool();

        currentAmmo    = maxAmmo;
        reloadProgress = 0f;

        if (vignetteImage != null) vignetteImage.enabled = false;

        UpdateAmmoUI();
        StartCoroutine(Reload());

        aimDirection = Vector2.right;
        ApplyAimRotation(aimDirection);

        canShoot = true;
        originalFixedDeltaTime = Time.fixedDeltaTime;

        rb.mass = playerMass;
        ApplyBabyMode();
    }

    void OnDestroy()
    {
        FlushPendingShots();
    }

    public void FlushPendingShots()
    {
        if (pendingShotCount <= 0) return;
        PlayerPrefs.SetInt("TotalShots", PlayerPrefs.GetInt("TotalShots", 0) + pendingShotCount);
        pendingShotCount = 0;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ПОСТРОЕНИЕ UI ПАТРОННИКА
    // ═══════════════════════════════════════════════════════════════════════════

    void BuildAmmoUI()
    {
        if (ammo == null) { Debug.LogError("PistolMove: назначь ammo!"); return; }

        foreach (Transform child in ammo.transform)
            Destroy(child.gameObject);

        int perRow   = Mathf.Min(maxAmmo, ammoMaxPerRow);
        int rowCount = Mathf.CeilToInt((float)maxAmmo / perRow);

        float slotsW = perRow   * ammoSlotSize.x + (perRow   - 1) * ammoSlotSpacing;
        float slotsH = rowCount * ammoSlotSize.y  + (rowCount - 1) * ammoSlotSpacing;

        float outerW = slotsW + ammoBgOuterPadding * 2f;
        float outerH = slotsH + ammoBgOuterPadding * 2f;
        CreateUIRect("AmmoBg_Outer", ammo.transform, outerW, outerH, ammoBgOuterColor);
        CreateUIRect("AmmoBg_Inner", ammo.transform,
                     outerW - ammoBgInnerPadding * 2f,
                     outerH - ammoBgInnerPadding * 2f, ammoBgInnerColor);

        GameObject slotsRoot = new GameObject("AmmoSlotsRoot", typeof(RectTransform));
        slotsRoot.transform.SetParent(ammo.transform, false);
        RectTransform slotsRt = slotsRoot.GetComponent<RectTransform>();
        slotsRt.sizeDelta        = new Vector2(slotsW, slotsH);
        slotsRt.anchoredPosition = Vector2.zero;
        slotsRt.anchorMin = slotsRt.anchorMax = slotsRt.pivot = new Vector2(0.5f, 0.5f);

        ammoSlotImages = new Image[maxAmmo];
        ammoFillImages = new Image[maxAmmo];

        for (int i = 0; i < maxAmmo; i++)
        {
            int col = i % perRow;
            int row = i / perRow;

            // ИСПРАВЛЕНИЕ 1: центрирование — патроны симметричны относительно центра контейнера.
            // Формула: позиция ячейки = её индекс * шаг, смещённый так чтобы
            // первая ячейка и последняя были на одинаковом расстоянии от краёв.
            float x =  col * (ammoSlotSize.x + ammoSlotSpacing) - (slotsW - ammoSlotSize.x) * 0.5f;
            float y = -(row * (ammoSlotSize.y + ammoSlotSpacing) - (slotsH - ammoSlotSize.y) * 0.5f);

            GameObject slotGo = new GameObject("S" + i, typeof(RectTransform));
            slotGo.transform.SetParent(slotsRoot.transform, false);
            RectTransform slotRt = slotGo.GetComponent<RectTransform>();
            slotRt.sizeDelta        = ammoSlotSize;
            slotRt.anchoredPosition = new Vector2(x, y);
            slotRt.anchorMin = slotRt.anchorMax = slotRt.pivot = new Vector2(0.5f, 0.5f);

            GameObject baseGo = new GameObject("B", typeof(RectTransform), typeof(Image));
            baseGo.transform.SetParent(slotGo.transform, false);
            Image baseImg = baseGo.GetComponent<Image>();
            baseImg.sprite = ammoSprite;
            baseImg.color  = ammoLoadedColor;
            StretchToParent(baseGo.GetComponent<RectTransform>());
            ammoSlotImages[i] = baseImg;

            GameObject fillGo = new GameObject("F", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(slotGo.transform, false);
            Image fillImg = fillGo.GetComponent<Image>();
            fillImg.sprite     = ammoSprite;
            fillImg.color      = ammoReloadingColor;
            fillImg.type       = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Vertical;
            fillImg.fillOrigin = (int)Image.OriginVertical.Bottom;
            fillImg.fillAmount = 0f;
            StretchToParent(fillGo.GetComponent<RectTransform>());
            ammoFillImages[i] = fillImg;
        }
    }

    GameObject CreateUIRect(string name, Transform parent, float w, float h, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta        = new Vector2(w, h);
        rt.anchoredPosition = Vector2.zero;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        go.GetComponent<Image>().color = color;
        return go;
    }

    void StretchToParent(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ВИЗУАЛ ПАТРОНОВ
    // ═══════════════════════════════════════════════════════════════════════════

    void UpdateAmmoUI()
    {
        if (ammoSlotImages == null) return;
        for (int i = 0; i < ammoSlotImages.Length; i++)
        {
            if (ammoSlotImages[i] == null) continue;
            ammoSlotImages[i].color      = (i < currentAmmo) ? ammoLoadedColor : ammoEmptyColor;
            ammoFillImages[i].fillAmount = 0f;
        }
        lastChargingSlot = -1;
        lastFillAmount   = 0f;

        // Синхронизируем виньетку
        UpdateVignetteTarget();
    }

    void SetSlotReloadProgress(int slotIndex, float progress)
    {
        if (ammoSlotImages == null) return;
        if (slotIndex < 0 || slotIndex >= ammoSlotImages.Length) return;

        if (lastChargingSlot != slotIndex && lastChargingSlot >= 0
            && lastChargingSlot < ammoFillImages.Length)
            ammoFillImages[lastChargingSlot].fillAmount = 0f;

        if (Mathf.Abs(progress - lastFillAmount) > 0.005f || lastChargingSlot != slotIndex)
        {
            ammoSlotImages[slotIndex].color      = ammoEmptyColor;
            ammoFillImages[slotIndex].fillAmount = progress;
            lastChargingSlot = slotIndex;
            lastFillAmount   = progress;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ВИНЬЕТКА
    // ═══════════════════════════════════════════════════════════════════════════

    // Обновляет только целевое значение альфы — без записи в Image.color.
    // Реальное обновление происходит в Update через один Lerp.
    void UpdateVignetteTarget()
    {
        float t = maxAmmo > 0 ? 1f - (float)currentAmmo / maxAmmo : 0f;
        vignetteTargetAlpha = Mathf.Lerp(vignetteMinAlpha, vignetteMaxAlpha, t);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ПУЛ ПУЛЬ
    // ═══════════════════════════════════════════════════════════════════════════

    void InitBulletPool()
    {
        for (int i = 0; i < bulletPoolSize; i++)
        {
            GameObject b = Instantiate(bulletPrefab);
            b.SetActive(false);
            bulletPool.Enqueue(b);
            bulletRbPool.Enqueue(b.GetComponent<Rigidbody2D>());
        }
    }

    bool GetBulletFromPool(out GameObject bullet, out Rigidbody2D bulletRb)
    {
        if (bulletPool.Count > 0)
        {
            bullet   = bulletPool.Dequeue();
            bulletRb = bulletRbPool.Dequeue();
            bullet.SetActive(true);
            return true;
        }
        bullet   = Instantiate(bulletPrefab);
        bulletRb = bullet.GetComponent<Rigidbody2D>();
        return true;
    }

    public void ReturnBulletToPool(GameObject bullet)
    {
        bullet.SetActive(false);
        bulletPool.Enqueue(bullet);
        bulletRbPool.Enqueue(bullet.GetComponent<Rigidbody2D>());
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ПРИЦЕЛИВАНИЕ
    // ═══════════════════════════════════════════════════════════════════════════

    void UpdateAim()
    {
        Vector3 rawMousePos = Input.mousePosition;
        if (!IsMousePositionValid(rawMousePos)) return;

        Vector2 cursorWorld = mainCamera.ScreenToWorldPoint(rawMousePos);
        Vector2 dir = cursorWorld - (Vector2)transform.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        aimDirection = dir.normalized;
        ApplyAimRotation(aimDirection);
    }

    void ApplyAimRotation(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        FlipCharacter(angle);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  FIXED UPDATE — горизонтальное трение
    // ═══════════════════════════════════════════════════════════════════════════

    void FixedUpdate()
    {
        // ИСПРАВЛЕНИЕ 2: горизонтальное трение.
        // Гасим только X-компоненту скорости — вертикаль (гравитация + отдача вверх)
        // не трогаем. Экспоненциальное затухание: vx *= (1 - drag * dt) каждый шаг.
        if (horizontalDrag > 0f)
        {
            Vector2 vel = rb.linearVelocity;
            vel.x *= Mathf.Clamp01(1f - horizontalDrag * Time.fixedDeltaTime);
            rb.linearVelocity = vel;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  UPDATE
    // ═══════════════════════════════════════════════════════════════════════════

    void Update()
    {
        if (slowMoModifierEnabled)
        {
            slowMotionMinScale   = Mathf.Max(0.01f, defaultSlowMotionMinScale - 0.35f);
            slowMotionBuildSpeed = Mathf.Max(0.01f, defaultSlowMotionBuildSpeed + 1f);
        }
        else
        {
            slowMotionMinScale   = defaultSlowMotionMinScale;
            slowMotionBuildSpeed = defaultSlowMotionBuildSpeed;
        }

        HandleSlowMotion();

        if (canShoot && !isWaitingToShoot)
        {
            bool mouseDown = Input.GetMouseButtonDown(0);
            bool mouseHeld = Input.GetMouseButton(0);
            bool mouseUp   = Input.GetMouseButtonUp(0);

            if (mouseDown || mouseHeld)
                UpdateAim();

            if (mouseUp && Time.unscaledTime >= lastFireTime + fireCooldown)
            {
                Fire(aimDirection);
                lastFireTime = Time.unscaledTime;
                playerScript.StartText.SetActive(false);
                ammo.SetActive(true);
                playerScript.PauseButton.SetActive(true);
                Saw.SetActive(true);
                noFireTimer = 0f;
                playerScript.HandleObjectsOnShot();
            }
        }

        if (canShoot)
        {
            noFireTimer += Time.deltaTime;
            if (noFireTimer >= noFireBoostThreshold && !isBoosted)
                StartCoroutine(StartBoostReload());
        }

        trapCheckTimer += Time.unscaledDeltaTime;
        if (trapCheckTimer >= trapCheckInterval)
        {
            trapCheckTimer  = 0f;
            cachedTrapScale = GetTrapSlowScale();
        }

        intendedVelocity = Vector2.MoveTowards(
            intendedVelocity, rb.linearVelocity, intendedVelFollow * Time.deltaTime);

        // ИСПРАВЛЕНИЕ 3: плавное обновление виньетки — один Lerp в Update
        // вместо записи Color при каждом выстреле/перезарядке
        if (vignetteImage != null)
        {
            Color c = vignetteImage.color;
            float newAlpha = Mathf.Lerp(c.a, vignetteTargetAlpha,
                                         vignetteSmoothing * Time.deltaTime);
            if (Mathf.Abs(newAlpha - c.a) > 0.001f)
            {
                c.a = newAlpha;
                vignetteImage.color   = c;
                vignetteImage.enabled = c.a > 0.005f;
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  SLOW MOTION
    // ═══════════════════════════════════════════════════════════════════════════

    void HandleSlowMotion()
    {
        if (!canShoot) return;

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

        ApplyTimeScale(Mathf.Min(aimScale, cachedTrapScale));
    }

    public void ApplyBabyMode()
    {
        Vector3 scale = transform.localScale;
        float signY = Mathf.Sign(scale.y);
        if (BabyMode)
        {
            scale.x = 0.65f; scale.y = signY * 0.65f; scale.z = 0.65f;
            recoilForce = 15.5f; playerMass = 1.2f;
        }
        else
        {
            scale.x = 0.8f; scale.y = signY * 0.8f; scale.z = 0.8f;
            recoilForce = 12.5f; playerMass = 1.4f;
        }
        transform.localScale = scale;
    }

    float GetTrapSlowScale()
    {
        float minDist = trapSlowRadius + 0.01f;
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, trapSlowRadius, trapHitBuffer);

        for (int i = 0; i < count; i++)
        {
            Collider2D col = trapHitBuffer[i];
            if (!col.CompareTag("Dead") && !col.CompareTag("DeadlyBlock")) continue;

            float roughDist = Vector2.Distance(transform.position, col.transform.position);
            if (roughDist >= minDist + 2f) continue;

            float d = selfCollider != null
                ? selfCollider.Distance(col).distance
                : roughDist;
            if (d < minDist) minDist = d;
        }

        if (minDist > trapSlowRadius) return 1f;
        return Mathf.Lerp(1f, trapMaxSlowScale,
               Mathf.InverseLerp(trapSlowRadius, trapMinDistance, minDist));
    }

    void ApplyTimeScale(float value)
    {
        if (Mathf.Approximately(Time.timeScale, value)) return;
        Time.timeScale      = value;
        Time.fixedDeltaTime = originalFixedDeltaTime * value;
        timeIsModified      = !Mathf.Approximately(value, 1f);
    }

    public void ForceStopSlowMotion()
    {
        ApplyTimeScale(1f);
        timeIsModified = false;
        holdTimer      = 0f;
    }

    bool IsMousePositionValid(Vector3 p)
    {
        if (float.IsNaN(p.x)      || float.IsNaN(p.y))      return false;
        if (float.IsInfinity(p.x) || float.IsInfinity(p.y)) return false;
        if (p.x < 0 || p.y < 0)                              return false;
        if (p.x > Screen.width || p.y > Screen.height)       return false;
        return true;
    }

    void FlipCharacter(float angle)
    {
        bool facingLeft = angle > 90f || angle < -90f;
        bool wasLeft    = lastAngle > 90f || lastAngle < -90f;
        if (facingLeft != wasLeft)
            transform.localScale = new Vector3(
                transform.localScale.x, -transform.localScale.y, transform.localScale.z);
        lastAngle = angle;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ВЫСТРЕЛ
    // ═══════════════════════════════════════════════════════════════════════════

    void Fire(Vector2 direction)
    {
        if (!sessionStarted)
        {
            sessionStartTime = Time.time;
            sessionStarted   = true;
            OnSessionStarted?.Invoke();
        }

        if (currentAmmo <= 0) return;

        direction.Normalize();

        pendingShotCount++;
        if (pendingShotCount >= 10) FlushPendingShots();

        GetBulletFromPool(out GameObject bullet, out Rigidbody2D bulletRb);
        bullet.transform.SetPositionAndRotation(
            bulletSpawn.position,
            Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
        bulletRb.linearVelocity = direction * bulletSpeed;

        Vector2 recoilDir = -direction;
        float along = Vector2.Dot(rb.linearVelocity, recoilDir);
        if (along < 0) rb.linearVelocity -= recoilDir * along;

        Vector2 recoil = new Vector2(
            recoilDir.x * horizontalRecoilMultiplier,
            recoilDir.y * verticalRecoilMultiplier).normalized;
        rb.AddForce(recoil * recoilForce * recoilMultiplier, ForceMode2D.Impulse);

        Vector2 dv = recoilDir * recoilForce / rb.mass;
        intendedVelocity += dv;
        if (intendedVelocity.magnitude > playerScript.maxSpeed)
            intendedVelocity = intendedVelocity.normalized * playerScript.maxSpeed;
        if (rb.linearVelocity.magnitude > playerScript.maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * playerScript.maxSpeed;

        currentAmmo--;
        UpdateAmmoUI(); // внутри вызывает UpdateVignetteTarget

        if (currentAmmo == 0 && !isBoosted)
            StartCoroutine(StartBoostReload());

        cameraFollow.SetShake(Mathf.Lerp(0.1f, 0.16f, 1f - (float)currentAmmo / maxAmmo), 0.1f);

        if (animator != null) animator.Play("Shoot", 0, 0f);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ПЕРЕЗАРЯДКА
    // ═══════════════════════════════════════════════════════════════════════════

    IEnumerator Reload()
    {
        while (true)
        {
            if (currentAmmo < maxAmmo)
            {
                float interval = isBoosted ? boostedReloadInterval : reloadInterval;
                float elapsed  = reloadProgress * interval;

                while (elapsed < interval)
                {
                    int chargingSlot = currentAmmo;
                    elapsed       += Time.deltaTime;
                    reloadProgress = Mathf.Clamp01(elapsed / interval);
                    SetSlotReloadProgress(chargingSlot, reloadProgress);

                    float newInterval = isBoosted ? boostedReloadInterval : reloadInterval;
                    if (!Mathf.Approximately(newInterval, interval))
                    {
                        elapsed  = reloadProgress * newInterval;
                        interval = newInterval;
                    }

                    yield return null;
                }

                reloadProgress = 0f;
                currentAmmo++;
                UpdateAmmoUI(); // внутри вызывает UpdateVignetteTarget
            }
            else
            {
                reloadProgress = 0f;
            }

            if (isBoosted)
            {
                float interval = isBoosted ? boostedReloadInterval : reloadInterval;
                boostTimer += interval;
                if (boostTimer >= boostDuration) { isBoosted = false; boostTimer = 0f; }
            }

            yield return null;
        }
    }

    IEnumerator StartBoostReload()
    {
        if (isBoostCoroutineRunning) yield break;
        isBoostCoroutineRunning = true;
        isBoosted  = true;
        boostTimer = 0f;
        yield return new WaitForSeconds(boostDuration);
        isBoosted  = false;
        boostTimer = 0f;
        isBoostCoroutineRunning = false;
    }
}