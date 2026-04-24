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

    [Header("Recoil Modifiers")]
    [Tooltip("Если включено — recoilForce становится 14 вместо стандартного 12.5")]
    public bool boostedRecoilEnabled = false;
    [Tooltip("Стандартная сила отдачи (используется когда boostedRecoilEnabled = false)")]
    public float normalRecoilForce   = 12.5f;
    [Tooltip("Повышенная сила отдачи (используется когда boostedRecoilEnabled = true)")]
    public float boostedRecoilForce  = 14f;

    [Tooltip("Если включено — отдача вниз полностью убирается. Действует только на вертикальную составляющую вниз.")]
    public bool noDownwardRecoilEnabled = false;

    [Header("Accelerometer")]
    [Tooltip("Если включено — наклон телефона добавляет горизонтальную силу игроку")]
    public bool accelEnabled = false;
    [Tooltip("Сила горизонтального движения от акселерометра")]
    public float accelForce = 5f;
    [Tooltip("Мёртвая зона акселерометра — наклоны меньше этого значения игнорируются")]
    public float accelDeadZone = 0.1f;

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
    [Tooltip("Размер одного патрона при базовом количестве пуль (baseAmmoForScale)")]
    public Vector2 ammoSlotSize     = new Vector2(28f, 40f);
    [Tooltip("Отступ между патронами при базовом количестве пуль")]
    public float ammoSlotSpacing    = 4f;
    [Tooltip("Максимум патронов в одну строку")]
    public int ammoMaxPerRow        = 15;

    [Tooltip("Базовое количество пуль при котором размер слота = 100%")]
    public int baseAmmoForScale     = 8;
    [Tooltip("Минимальный масштаб слота (0.4 = 40% от базового размера)")]
    [Range(0.1f, 1f)]
    public float minSlotScale       = 0.4f;

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

    public Toggle SwipeToggle;

    // ── Виньетка ──────────────────────────────────────────────────────────────
    [Header("Vignette UI")]
    [Tooltip("Растянутый на весь экран Image с чёрным цветом. Raycast Target — выключить.")]
    public Image vignetteImage;
    [Tooltip("Альфа виньетки когда магазин полный")]
    public float vignetteMinAlpha  = 0.05f;
    [Tooltip("Альфа виньетки когда магазин пустой")]
    public float vignetteMaxAlpha  = 0.35f;
    [Tooltip("Скорость плавного изменения альфы")]
    public float vignetteSmoothing = 8f;
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
    // ══  SWIPE / SLINGSHOT УПРАВЛЕНИЕ  ═══════════════════════════════════════
    // ═════════════════════════════════════════════════════════════════════════

    [Header("Swipe / Slingshot Control")]
    [Tooltip("false = старое управление тапами, true = свайп-рогатка")]
    public bool swipeControlEnabled = false;

    [Tooltip("Минимальная длина свайпа в пикселях, чтобы засчитать выстрел")]
    public float swipeMinPixels = 30f;

    [Tooltip("Радиус мёртвой зоны старта в пикселях — очень короткие тапы игнорируются")]
    public float swipeDeadZone = 8f;

    [Tooltip("Сглаживание направления во время удержания (0 = мгновенно, 1 = очень плавно)")]
    [Range(0f, 0.98f)]
    public float swipeDirectionSmoothing = 0.82f;

    [Tooltip("Скорость поворота прицела во время удержания (градусов/сек).")]
    public float swipeAimRotationSpeed = 600f;

    [Tooltip("Порог скорости движения пальца (px/сек): ниже него считается 'пауза'")]
    public float swipePauseVelocityThreshold = 60f;

    [Tooltip("Время (сек) неподвижности пальца, после которого направление считается залоченным")]
    public float swipeLockDelay = 0.08f;

    [Tooltip("Автовыстрел если палец не двигался N секунд и направление залочено")]
    public float swipeAutoFireDelay = 0.35f;

    [Tooltip("Максимальная длина 'резинки' рогатки в пикселях — для визуального ограничения")]
    public float swipeMaxStretchPixels = 220f;

    [Tooltip("UI Transform стрелки-прицела (необязательно, можно оставить пустым)")]
    public RectTransform swipeArrowUI;

    [Tooltip("UI Image рогатки (необязательно)")]
    public Image swipeStretchIndicator;

    // Внутренние переменные свайпа
    private bool   swipeTouchActive      = false;
    private Vector2 swipeTouchStart      = Vector2.zero;
    private Vector2 swipePrevPos         = Vector2.zero;
    private Vector2 swipeRawDir          = Vector2.zero;
    private Vector2 swipeSmoothedDir     = Vector2.zero;
    private Vector2 swipeLockedDir       = Vector2.zero;
    private bool    swipeDirLocked       = false;
    private float   swipeLockTimer       = 0f;
    private float   swipeAutoFireTimer   = 0f;
    private float   swipeStretchAmount   = 0f;
    private float   swipeCurrentVelocity = 0f;

    private float swipeCurrentAngle = 0f;
    private float swipeTargetAngle  = 0f;

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

        swipeCurrentAngle = 0f;
        swipeTargetAngle  = 0f;
        SetSwipeUI(false, Vector2.right, 0f);

        swipeControlEnabled = PlayerPrefs.GetInt("SwipeControlEnabled", 0) == 1;
        if (SwipeToggle != null)
        {
            SwipeToggle.SetIsOnWithoutNotify(swipeControlEnabled);
            SwipeToggle.onValueChanged.AddListener(OnSwipeToggleChanged);
        }
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

    void OnSwipeToggleChanged(bool isOn)
    {
        swipeControlEnabled = isOn;
        PlayerPrefs.SetInt("SwipeControlEnabled", isOn ? 1 : 0);
        PlayerPrefs.Save();

        if (!isOn) ResetSwipeState(keepActive: false);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  ПОСТРОЕНИЕ UI ПАТРОННИКА
    // ═══════════════════════════════════════════════════════════════════════════

    void BuildAmmoUI()
    {
        if (ammo == null) { Debug.LogError("PistolMove: назначь ammo!"); return; }

        foreach (Transform child in ammo.transform)
            Destroy(child.gameObject);

        float rawScale   = (float)baseAmmoForScale / Mathf.Max(1, maxAmmo);
        float slotScale  = Mathf.Clamp(rawScale, minSlotScale, 1f);

        Vector2 scaledSlotSize    = ammoSlotSize    * slotScale;
        float   scaledSlotSpacing = ammoSlotSpacing * slotScale;

        int perRow   = Mathf.Min(maxAmmo, ammoMaxPerRow);
        int rowCount = Mathf.CeilToInt((float)maxAmmo / perRow);

        float slotsW = perRow   * scaledSlotSize.x + (perRow   - 1) * scaledSlotSpacing;
        float slotsH = rowCount * scaledSlotSize.y  + (rowCount - 1) * scaledSlotSpacing;

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

            float x =  col * (scaledSlotSize.x + scaledSlotSpacing) - (slotsW - scaledSlotSize.x) * 0.5f;
            float y = -(row * (scaledSlotSize.y + scaledSlotSpacing) - (slotsH - scaledSlotSize.y) * 0.5f);

            GameObject slotGo = new GameObject("S" + i, typeof(RectTransform));
            slotGo.transform.SetParent(slotsRoot.transform, false);
            RectTransform slotRt = slotGo.GetComponent<RectTransform>();
            slotRt.sizeDelta        = scaledSlotSize;
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
    //  ПРИЦЕЛИВАНИЕ (старый режим)
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
    //  FIXED UPDATE
    // ═══════════════════════════════════════════════════════════════════════════

    void FixedUpdate()
    {
        if (horizontalDrag > 0f)
        {
            Vector2 vel = rb.linearVelocity;
            vel.x *= Mathf.Clamp01(1f - horizontalDrag * Time.fixedDeltaTime);
            rb.linearVelocity = vel;
        }

        // Акселерометр — применяем силу в FixedUpdate для физической корректности
        if (accelEnabled)
        {
            float tilt = Input.acceleration.x;
            if (Mathf.Abs(tilt) > accelDeadZone)
                rb.AddForce(Vector2.right * tilt * accelForce, ForceMode2D.Force);
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

        if (swipeControlEnabled)
            HandleSwipeControl();
        else
            HandleTapControl();

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
    //  СТАРОЕ УПРАВЛЕНИЕ (ТАП)
    // ═══════════════════════════════════════════════════════════════════════════

    void HandleTapControl()
    {
        if (!canShoot || isWaitingToShoot) return;

        bool mouseDown = Input.GetMouseButtonDown(0);
        bool mouseHeld = Input.GetMouseButton(0);
        bool mouseUp   = Input.GetMouseButtonUp(0);

        if (mouseDown || mouseHeld)
            UpdateAim();

        if (mouseUp && Time.unscaledTime >= lastFireTime + fireCooldown)
        {
            Fire(aimDirection);
            lastFireTime = Time.unscaledTime;
            ActivateGameUI();
            noFireTimer = 0f;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  НОВОЕ УПРАВЛЕНИЕ (СВАЙП-РОГАТКА)
    // ═══════════════════════════════════════════════════════════════════════════

    void HandleSwipeControl()
    {
        if (!canShoot || isWaitingToShoot) return;

        bool  touchBegan  = false;
        bool  touchEnded  = false;
        bool  touchHeld   = false;
        Vector2 touchPos  = Vector2.zero;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))  { touchBegan = true; touchPos = Input.mousePosition; }
        else if (Input.GetMouseButton(0)) { touchHeld  = true; touchPos = Input.mousePosition; }
        else if (Input.GetMouseButtonUp(0)) { touchEnded = true; touchPos = Input.mousePosition; }
#else
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            touchPos = t.position;
            switch (t.phase)
            {
                case TouchPhase.Began:      touchBegan = true; break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary: touchHeld  = true; break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:   touchEnded = true; break;
            }
        }
#endif

        if (touchBegan && IsMousePositionValid(touchPos))
        {
            swipeTouchActive     = true;
            swipeTouchStart      = touchPos;
            swipePrevPos         = touchPos;
            swipeRawDir          = Vector2.zero;
            swipeSmoothedDir     = Vector2.zero;
            swipeDirLocked       = false;
            swipeLockTimer       = 0f;
            swipeAutoFireTimer   = 0f;
            swipeStretchAmount   = 0f;
            swipeCurrentVelocity = 0f;
            swipeCurrentAngle    = float.NaN;
            holdTimer            = 0f;
        }

        if (swipeTouchActive && (touchHeld || touchBegan))
        {
            if (!IsMousePositionValid(touchPos)) return;

            Vector2 delta     = touchPos - swipeTouchStart;
            float   stretchPx = delta.magnitude;

            swipeCurrentVelocity = (touchPos - swipePrevPos).magnitude / Time.unscaledDeltaTime;
            swipePrevPos         = touchPos;

            if (stretchPx > swipeDeadZone)
            {
                swipeRawDir = delta.normalized;

                if (swipeSmoothedDir.sqrMagnitude < 0.001f)
                {
                    swipeSmoothedDir = swipeRawDir;
                }
                else
                {
                    swipeSmoothedDir = Vector2.Lerp(swipeRawDir, swipeSmoothedDir,
                        Mathf.Pow(swipeDirectionSmoothing, Time.unscaledDeltaTime * 60f));
                    if (swipeSmoothedDir.sqrMagnitude < 0.001f)
                        swipeSmoothedDir = swipeRawDir;
                    swipeSmoothedDir.Normalize();
                }

                swipeTargetAngle = Mathf.Atan2(swipeSmoothedDir.y, swipeSmoothedDir.x) * Mathf.Rad2Deg;
                if (float.IsNaN(swipeCurrentAngle))
                    swipeCurrentAngle = swipeTargetAngle;
                else
                    swipeCurrentAngle = Mathf.MoveTowardsAngle(
                        swipeCurrentAngle, swipeTargetAngle,
                        swipeAimRotationSpeed * Time.unscaledDeltaTime);

                Vector2 rotatedDir = new Vector2(
                    Mathf.Cos(swipeCurrentAngle * Mathf.Deg2Rad),
                    Mathf.Sin(swipeCurrentAngle * Mathf.Deg2Rad));

                aimDirection = rotatedDir;
                ApplyAimRotation(aimDirection);

                swipeStretchAmount = Mathf.Clamp01(stretchPx / swipeMaxStretchPixels);
                SetSwipeUI(true, aimDirection, swipeStretchAmount);

                if (swipeCurrentVelocity < swipePauseVelocityThreshold)
                {
                    swipeLockTimer += Time.unscaledDeltaTime;
                    if (swipeLockTimer >= swipeLockDelay && !swipeDirLocked)
                    {
                        swipeDirLocked     = true;
                        swipeLockedDir     = aimDirection;
                        swipeAutoFireTimer = 0f;
                    }
                }
                else
                {
                    swipeDirLocked     = false;
                    swipeLockTimer     = 0f;
                    swipeAutoFireTimer = 0f;
                }

                if (swipeDirLocked)
                {
                    swipeAutoFireTimer += Time.unscaledDeltaTime;
                    if (swipeAutoFireTimer >= swipeAutoFireDelay
                        && Time.unscaledTime >= lastFireTime + fireCooldown)
                    {
                        DoSwipeFire();
                        ResetSwipeState(keepActive: true);
                    }
                }
            }
            else
            {
                swipeLockTimer = 0f;
            }
        }

        if (swipeTouchActive && touchEnded)
        {
            Vector2 totalDelta = touchPos - swipeTouchStart;
            float   totalLen   = totalDelta.magnitude;

            if (totalLen >= swipeMinPixels
                && Time.unscaledTime >= lastFireTime + fireCooldown)
            {
                Vector2 fireDir = swipeDirLocked ? swipeLockedDir : aimDirection;
                Fire(fireDir);
                lastFireTime = Time.unscaledTime;
                ActivateGameUI();
                noFireTimer = 0f;
            }

            ResetSwipeState(keepActive: false);
        }
    }

    void DoSwipeFire()
    {
        if (Time.unscaledTime < lastFireTime + fireCooldown) return;

        Vector2 fireDir = swipeDirLocked ? swipeLockedDir : aimDirection;
        Fire(fireDir);
        lastFireTime = Time.unscaledTime;
        ActivateGameUI();
        noFireTimer = 0f;
    }

    void ResetSwipeState(bool keepActive)
    {
        swipeTouchActive     = keepActive;
        swipeDirLocked       = false;
        swipeLockTimer       = 0f;
        swipeAutoFireTimer   = 0f;
        swipeStretchAmount   = 0f;
        swipeCurrentVelocity = 0f;

        if (!keepActive)
        {
            swipeTouchStart = Vector2.zero;
            swipePrevPos    = Vector2.zero;
            swipeRawDir     = Vector2.zero;
            SetSwipeUI(false, aimDirection, 0f);
        }
        else
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            swipeTouchStart = Input.mousePosition;
            swipePrevPos    = swipeTouchStart;
#else
            if (Input.touchCount > 0)
            {
                swipeTouchStart = Input.GetTouch(0).position;
                swipePrevPos    = swipeTouchStart;
            }
#endif
            swipeRawDir       = Vector2.zero;
            swipeSmoothedDir  = Vector2.zero;
            swipeCurrentAngle = float.NaN;
            SetSwipeUI(false, aimDirection, 0f);
        }
    }

    void SetSwipeUI(bool visible, Vector2 dir, float stretch)
    {
        if (swipeArrowUI != null)
        {
            swipeArrowUI.gameObject.SetActive(visible);
            if (visible)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                swipeArrowUI.rotation = Quaternion.Euler(0, 0, angle);
                float s = Mathf.Lerp(0.6f, 1.4f, stretch);
                swipeArrowUI.localScale = new Vector3(s, s, 1f);
            }
        }

        if (swipeStretchIndicator != null)
        {
            swipeStretchIndicator.gameObject.SetActive(visible);
            if (visible)
            {
                swipeStretchIndicator.color = Color.Lerp(
                    new Color(1f, 1f, 1f, 0.4f),
                    new Color(1f, 0.5f, 0f, 0.85f),
                    stretch);
                swipeStretchIndicator.fillAmount = stretch;
            }
        }
    }

    void ActivateGameUI()
    {
        playerScript.StartText.SetActive(false);
        ammo.SetActive(true);
        playerScript.PauseButton.SetActive(true);
        Saw.SetActive(true);
        playerScript.HandleObjectsOnShot();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  SLOW MOTION
    // ═══════════════════════════════════════════════════════════════════════════

    void HandleSlowMotion()
    {
        if (!canShoot) return;

        float aimScale;

        if (swipeControlEnabled)
        {
            if (swipeTouchActive)
            {
                holdTimer += Time.unscaledDeltaTime;
                float stretch = Mathf.Max(swipeStretchAmount, 0.15f);
                float t = Mathf.Clamp01((holdTimer - slowMotionDelay) * slowMotionBuildSpeed)
                          * stretch;
                aimScale = Mathf.Lerp(1f, slowMotionMinScale, t);
            }
            else
            {
                holdTimer = 0f;
                aimScale  = Mathf.MoveTowards(Time.timeScale, 1f,
                             Time.unscaledDeltaTime * slowMotionReturnSpeed);
            }
        }
        else
        {
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
            playerMass = 1.2f;
        }
        else
        {
            scale.x = 0.8f; scale.y = signY * 0.8f; scale.z = 0.8f;
            playerMass = 1.4f;
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

        // Выбираем силу отдачи в зависимости от boostedRecoilEnabled
        float currentRecoilForce = boostedRecoilEnabled ? boostedRecoilForce : normalRecoilForce;

        Vector2 recoil = new Vector2(
            recoilDir.x * horizontalRecoilMultiplier,
            recoilDir.y * verticalRecoilMultiplier).normalized;

        // Убираем отдачу вниз если включено noDownwardRecoilEnabled
        if (noDownwardRecoilEnabled && recoil.y < 0f)
            recoil.y = 0f;

        // Ренормализуем только если вектор не нулевой
        if (recoil.sqrMagnitude > 0.0001f)
            recoil.Normalize();

        rb.AddForce(recoil * currentRecoilForce * recoilMultiplier, ForceMode2D.Impulse);

        Vector2 dv = recoilDir * currentRecoilForce / rb.mass;
        intendedVelocity += dv;
        if (intendedVelocity.magnitude > playerScript.maxSpeed)
            intendedVelocity = intendedVelocity.normalized * playerScript.maxSpeed;
        if (rb.linearVelocity.magnitude > playerScript.maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * playerScript.maxSpeed;

        currentAmmo--;
        UpdateAmmoUI();

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
                UpdateAmmoUI();
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