using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GunMove : MonoBehaviour
{
    public PistolMove playerScript;

    public GameObject gameOverPanel;
    public GameObject ContinueText;
    public GameObject StartText;
    public GameObject PauseButton;
    public GameObject playerWeapon;
    public GameObject playerSprite;
    public GameObject bloodEffectPrefab;
    public GameObject coinEffectPrefab;
    public Text distanceText;
    public Text finalDistanceText;
    public Text recordText;
    public Text coinText;
    public int coinCount;
    public Text sessionCoinText;
    public float sessionCoinCount;

    public GameObject[] objectsToAppearAfterShot;
    public GameObject[] objectsToDisappearOnShot;
    public GameObject[] objectsToAppearOnGameOver;
    public GameObject[] objectsToDisappearOnGameOver;
    public GameObject[] objectsToDisappearIfRecordLessThanTen;

    private Rigidbody2D rb;
    private SpriteRenderer playerSpriteRenderer;
    private Collider2D[] selfColliders;
    private DangerIndicator dangerIndicator;

    public float currentDistance;
    public float highestDistance;
    public float recordDistance;

    public Animator animator;
    public float maxSpeed = 10f;

    private float timeScaleIncrement = 0.3f;
    private float baseTimeScale = 1f;
    private Coroutine timeAccelerationCoroutine;

    private float startY;

    private ShopItemUI[] cachedShopItems;
    private bool isGameOverRunning = false;
    private bool isShakingCoinText = false;

    private int lastDisplayedDistance = -1;

    private Rigidbody2D weaponRb;
    private Collider2D weaponCollider;

    // ОПТИМИЗАЦИЯ: кэшируем WaitForSeconds — каждый new WaitForSeconds(x)
    // аллоцирует объект в heap, что видно как оранжевый скачок в профайлере.
    // Создаём один раз и переиспользуем.
    private WaitForSeconds wait05s;
    private WaitForSeconds wait005s;

    // ОПТИМИЗАЦИЯ: кэшируем WaitUntil с лямбдой — лямбда тоже аллоцируется
    // при каждом создании. Создаём один раз в Start.
    private WaitUntil waitForInput;

    // ОПТИМИЗАЦИЯ: System.Text.StringBuilder для строк с числами —
    // убирает аллокации от конкатенации в FixedUpdate и UpdateCoinTexts
    private System.Text.StringBuilder sb = new System.Text.StringBuilder(32);

    // ОПТИМИЗАЦИЯ: пул для эффекта крови — Instantiate каждую смерть
    // создаёт GC-мусор. Переиспользуем один объект.
    private GameObject bloodEffectInstance;

    void Start()
    {
        StopAllCoroutines();
        rb = GetComponent<Rigidbody2D>();
        ResetTime();
        Time.maximumDeltaTime = 0.1f; // Разрешает максимум ~5 шагов физики за кадр

        selfColliders   = GetComponents<Collider2D>();
        dangerIndicator = GetComponent<DangerIndicator>();
        if (playerSprite != null)
            playerSpriteRenderer = playerSprite.GetComponent<SpriteRenderer>();

        if (playerWeapon != null)
        {
            weaponCollider = playerWeapon.GetComponent<Collider2D>();
            weaponRb       = playerWeapon.GetComponent<Rigidbody2D>();
        }

        // Создаём кэшированные yield-объекты один раз
        wait05s      = new WaitForSeconds(0.5f);
        wait005s     = new WaitForSeconds(0.05f);
        waitForInput = new WaitUntil(() => Input.GetMouseButtonUp(0) || Input.touchCount > 0);

        // Создаём эффект крови заранее и прячем — будем показывать при смерти
        if (bloodEffectPrefab != null)
        {
            bloodEffectInstance = Instantiate(bloodEffectPrefab, Vector2.zero, Quaternion.identity);
            bloodEffectInstance.SetActive(false);
        }

        gameOverPanel.SetActive(false);
        ContinueText.SetActive(false);
        StartText.SetActive(true);
        PauseButton.SetActive(false);

        coinCount = PlayerPrefs.GetInt("Coins", 0);
        UpdateCoinUI();
        startY         = transform.position.y;
        recordDistance = PlayerPrefs.GetFloat("RecordDistance", 0f);

        foreach (GameObject obj in objectsToAppearAfterShot)
            if (obj != null) obj.SetActive(false);
        foreach (GameObject obj in objectsToDisappearOnShot)
            if (obj != null) obj.SetActive(true);

        sessionCoinCount = 0;
        UpdateSessionCoinUI();

        if (recordDistance < 10)
            foreach (GameObject obj in objectsToDisappearIfRecordLessThanTen)
                if (obj != null) obj.SetActive(false);

        int best = PlayerPrefs.GetInt("BestDistance", 0);
        if (recordDistance > best)
        {
            PlayerPrefs.SetInt("BestDistance", Mathf.RoundToInt(recordDistance));
            cachedShopItems = FindObjectsByType<ShopItemUI>(FindObjectsSortMode.None);
            foreach (var item in cachedShopItems)
            {
                item.UpdateProgressFromPrefs();
                item.TryAutoUnlock();
            }
        }

        if (cachedShopItems == null)
            cachedShopItems = FindObjectsByType<ShopItemUI>(FindObjectsSortMode.None);
    }

    void FixedUpdate()
    {
        // Оставляем здесь ТОЛЬКО физику
        if (rb.linearVelocity.sqrMagnitude > maxSpeed * maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G) && !isGameOverRunning)
        {
            DiePlayer();
            StartCoroutine(GameOver());
        }

        // Расчет дистанции перенесен сюда
        currentDistance = transform.position.y - startY;
        if (currentDistance > highestDistance)
            highestDistance = currentDistance;

        int displayDist = Mathf.Max(0, Mathf.FloorToInt(currentDistance));
        if (displayDist != lastDisplayedDistance)
        {
            lastDisplayedDistance = displayDist;
            sb.Clear();
            sb.Append(displayDist);
            sb.Append(" м");
            distanceText.text = sb.ToString();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Dead") && !isGameOverRunning)
        {
            DiePlayer();
            StopAllCoroutines();
            StartCoroutine(GameOver());
        }
    }

    IEnumerator GameOver()
    {
        isGameOverRunning = true;

        if (timeAccelerationCoroutine != null)
        {
            StopCoroutine(timeAccelerationCoroutine);
            timeAccelerationCoroutine = null;
        }
        ResetTime(); // Тот самый метод из Шага 1

        playerScript.FlushPendingShots();

        PlayerPrefs.SetInt("TotalLosses", PlayerPrefs.GetInt("TotalLosses", 0) + 1);
        PlayerPrefs.SetFloat("TotalDistance", PlayerPrefs.GetFloat("TotalDistance", 0) + currentDistance);

        if (playerScript.sessionStarted)
        {
            float sessionDuration = Time.time - playerScript.sessionStartTime;
            PlayerPrefs.SetFloat("TotalActiveTime",
                PlayerPrefs.GetFloat("TotalActiveTime", 0f) + sessionDuration);
            PlayerPrefs.Save();
        }

        ResetTime();
        playerScript.canShoot = false;
        rb.linearVelocity = Vector2.zero;
        playerScript.currentAmmo = playerScript.maxAmmo;

        PauseButton.SetActive(false);
        gameOverPanel.SetActive(true);

        // ОПТИМИЗАЦИЯ: string.Format вместо конкатенации с ToString("F2") —
        // меньше промежуточных строк в heap
        finalDistanceText.text = string.Format("Твоя дистанция: {0:F2} м",
                                               Mathf.Max(0, currentDistance));
        if (currentDistance > recordDistance)
        {
            recordDistance = currentDistance;
            PlayerPrefs.SetFloat("RecordDistance", recordDistance);
            PlayerPrefs.Save();
        }
        recordText.text = string.Format("ㅤㅤㅤРекорд:ㅤㅤㅤ {0:F2} м", recordDistance);

        foreach (GameObject obj in objectsToAppearOnGameOver)
            if (obj != null) obj.SetActive(true);
        foreach (GameObject obj in objectsToDisappearOnGameOver)
            if (obj != null) obj.SetActive(false);

        // ОПТИМИЗАЦИЯ: используем кэшированный WaitForSeconds вместо new каждый раз
        yield return wait05s;
        ResetTime();

        yield return StartCoroutine(UpdateCoinTexts());

        yield return wait05s;
        ResetTime();

        ContinueText.SetActive(true);

        // ОПТИМИЗАЦИЯ: используем кэшированный WaitUntil вместо new каждый раз
        yield return waitForInput;

        playerScript.isWaitingToShoot = true;
        yield return wait005s;
        playerScript.isWaitingToShoot = false;

        if (cachedShopItems != null)
            foreach (var item in cachedShopItems)
                if (item != null) Destroy(item.gameObject);

        EquipmentManager.Instance.RestartScene();
        StopAllCoroutines();
        yield break;
    }

    private bool hasDroppedWeapon = false;

    public void DisablePlayer()
    {
        playerScript.canShoot = false;
        if (dangerIndicator != null) dangerIndicator.enableWarnings = false;
        rb.linearVelocity = Vector2.zero;

        CreateBloodEffect(transform.position);

        GetComponent<Collider2D>().enabled = false;

        if (playerSpriteRenderer != null) playerSpriteRenderer.color = Color.red;

        if (!hasDroppedWeapon) { DropWeapon(); hasDroppedWeapon = true; }

        KnockbackPlayer();

        if (timeAccelerationCoroutine == null)
        {
            // ИСПРАВЛЕНИЕ: всегда стартуем с 1, не с текущего timeScale
            baseTimeScale = 1f;
            timeAccelerationCoroutine = StartCoroutine(TimeAccelerationRoutine());
        }
    }

    public void DiePlayer()
    {
        if (timeAccelerationCoroutine != null)
        {
            StopCoroutine(timeAccelerationCoroutine);
            timeAccelerationCoroutine = null;
            // ИСПРАВЛЕНИЕ: сбрасываем всегда в 1, не в baseTimeScale —
            // baseTimeScale мог быть уже завышен от предыдущей смерти
            ResetTime();
        }

        playerScript.canShoot = false;
        rb.linearVelocity = Vector2.zero;

        CreateBloodEffect(transform.position);

        foreach (var col in selfColliders)
            col.enabled = false;

        if (playerSpriteRenderer != null) playerSpriteRenderer.color = Color.red;

        if (!hasDroppedWeapon) { DropWeapon(); hasDroppedWeapon = true; }
    }

    void DropWeapon()
    {
        if (playerWeapon == null) return;

        playerWeapon.transform.parent = null;

        if (weaponCollider != null) weaponCollider.enabled = true;
        if (weaponRb == null) weaponRb = playerWeapon.AddComponent<Rigidbody2D>();
        weaponRb.linearVelocity = playerScript.bulletSpawn.right * 5f;
    }

    void KnockbackPlayer()
    {
        rb.AddForce(-playerScript.bulletSpawn.right * 5f, ForceMode2D.Impulse);
    }

    void CreateBloodEffect(Vector2 position)
    {
        if (bloodEffectInstance == null) return;

        // ОПТИМИЗАЦИЯ: переиспользуем один объект эффекта вместо Instantiate каждую смерть.
        // Перемещаем и активируем — ParticleSystem сам остановится после проигрывания.
        bloodEffectInstance.transform.position = position;
        bloodEffectInstance.SetActive(false); // сброс
        bloodEffectInstance.SetActive(true);  // воспроизведение заново
    }

    // Максимальный timeScale во время анимации смерти — не даём физике разогнаться
    private const float MAX_DEATH_TIMESCALE = 3f;

    private IEnumerator TimeAccelerationRoutine()
    {
        int elapsedSeconds = 0;
        while (true)
        {
            elapsedSeconds++;
            float target = baseTimeScale + timeScaleIncrement * elapsedSeconds;
            // ИСПРАВЛЕНИЕ: ограничиваем timeScale — без этого физика делала тысячи
            // шагов за кадр (2316 шагов в профайлере) и это была главная причина лагов.
            // При timeScale=7 физика работает в 7 раз интенсивнее — спираль смерти.
            Time.timeScale = Mathf.Min(target, MAX_DEATH_TIMESCALE);

            // ИСПРАВЛЕНИЕ: WaitForSecondsRealtime не зависит от timeScale —
            // WaitForSeconds ускорялся вместе с timeScale и разгонял сам себя
            yield return new WaitForSecondsRealtime(1f);
        }
    }

    public void ResetSession()
    {
        sessionCoinCount = 0;
        highestDistance  = 0;
    }

    public void UpdateCoinUI()
    {
        coinText.text = coinCount.ToString();
    }

    public void UpdateSessionCoinUI()
    {
        sessionCoinText.text = sessionCoinCount.ToString();
        StartCoroutine(ShakeCoinText());
    }

    IEnumerator UpdateCoinTexts()
    {
        float duration     = 0.6f;
        float initial      = sessionCoinCount;
        float decreaseRate = initial > 0f ? initial / duration : 0f;

        while (sessionCoinCount > 0f)
        {
            sessionCoinCount -= decreaseRate * Time.deltaTime;
            sessionCoinCount  = Mathf.Max(sessionCoinCount, 0f);

            int floored = Mathf.FloorToInt(sessionCoinCount);

            // ОПТИМИЗАЦИЯ: StringBuilder вместо ToString каждый кадр во время анимации
            sb.Clear();
            sb.Append(floored);
            sessionCoinText.text = sb.ToString();

            sb.Clear();
            sb.Append(coinCount - floored);
            coinText.text = sb.ToString();

            if (!isShakingCoinText)
                StartCoroutine(ShakeCoinText());

            yield return null;
        }

        sessionCoinText.text = "0";
        coinText.text = coinCount.ToString();
    }

    IEnumerator ShakeCoinText()
    {
        isShakingCoinText = true;

        Vector3 orig    = sessionCoinText.transform.localPosition;
        float duration  = 0.33f;
        float magnitude = 3.4f;

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            sessionCoinText.transform.localPosition = new Vector3(
                orig.x + Random.Range(-magnitude, magnitude),
                orig.y + Random.Range(-magnitude, magnitude),
                orig.z);
            yield return null;
        }

        sessionCoinText.transform.localPosition = orig;
        isShakingCoinText = false;
    }

    public void HandleObjectsOnShot()
    {
        foreach (GameObject obj in objectsToAppearAfterShot)
            if (obj != null) obj.SetActive(true);
        foreach (GameObject obj in objectsToDisappearOnShot)
            if (obj != null) obj.SetActive(false);
    }

    private void ResetTime()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f; // Стандартный шаг физики (50 FPS)
    }
}