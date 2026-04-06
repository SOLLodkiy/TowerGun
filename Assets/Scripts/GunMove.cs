using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

public class GunMove : MonoBehaviour
{
    public PistolMove playerScript; // Ссылка на скрипт игрока

    public GameObject gameOverPanel; // Панель "ты проиграл"
    public GameObject ContinueText; // Текст продолжить
    public GameObject StartText;
    public GameObject PauseButton;
    public GameObject playerWeapon; // Сюда вставляем объект пистолета из иерархии игрока
    public GameObject playerSprite; // Дочерний объект с SpriteRenderer
    public GameObject bloodEffectPrefab; // Префаб эффекта крови
    public GameObject coinEffectPrefab; // Префаб эффекта монет
    public Text distanceText; // Текст для отображения дистанции
    public Text finalDistanceText; // Текст для отображения финальной дистанции
    public Text recordText; // Текст для отображения рекорда
    public Text coinText; // Текст для отображения количества монет
    public int coinCount; // Количество собранных монет
    public Text sessionCoinText; // Новый текст для отображения количества монет за забег
    public float sessionCoinCount; // Количество монет за текущий забег

    public GameObject[] objectsToAppearAfterShot; // Массив объектов, которые появляются после выстрела
    public GameObject[] objectsToDisappearOnShot; // Массив объектов, которые исчезают при выстреле
    public GameObject[] objectsToAppearOnGameOver; // Массив объектов, которые появляются при проигрыше
    public GameObject[] objectsToDisappearOnGameOver; // Массив объектов, которые пропадают при проигрыше
    public GameObject[] objectsToDisappearIfRecordLessThanTen; // Массив объектов, которые исчезают при старте, если рекорд меньше 10

    private Rigidbody2D rb;

    public float currentDistance; // Текущая дистанция
    public float highestDistance; // Наивысшая дистанция за сессию
    public float recordDistance; // Рекордная дистанция

    public Animator animator; // Ссылка на Animator компонент

    // Новая переменная для ограничения скорости
    public float maxSpeed = 10f; // Максимальная скорость персонажа

    private float timeScaleIncrement = 0.3f; // Шаг увеличения времени каждую секунду
    private float baseTimeScale = 1f; // Базовая скорость времени
    private Coroutine timeAccelerationCoroutine; // Ссылка на корутину для ускорения времени

    private float startY; // Начальная позиция по Y

    // ИСПРАВЛЕНИЕ 5: флаг для обработки G-клавиши перенесён из FixedUpdate в Update
    private bool pendingDebugDie = false;

    void Start()
    {
        StopAllCoroutines();
        rb = GetComponent<Rigidbody2D>();
        gameOverPanel.SetActive(false); // Скрыть панель в начале
        ContinueText.SetActive(false);
        StartText.SetActive(true);
        PauseButton.SetActive(false);
        // Загрузка сохранённого количества монет
        coinCount = PlayerPrefs.GetInt("Coins", 0);
        UpdateCoinUI();
        startY = transform.position.y; // Сохранить начальную позицию
        recordDistance = PlayerPrefs.GetFloat("RecordDistance", 0f); // Загрузить рекорд из PlayerPrefs

        // Деактивируем объекты, которые появляются после выстрела
        foreach (GameObject obj in objectsToAppearAfterShot)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }

        // Активируем объекты, которые исчезают при выстреле
        foreach (GameObject obj in objectsToDisappearOnShot)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
         // Сброс количества монет за забег
        sessionCoinCount = 0;
        UpdateSessionCoinUI();
        // Деактивируем объекты, которые исчезают, если рекорд меньше 10
        if (recordDistance < 10)
        {
            foreach (GameObject obj in objectsToDisappearIfRecordLessThanTen)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }

        int best = PlayerPrefs.GetInt("BestDistance", 0);
        if (recordDistance > best)
        {
            PlayerPrefs.SetInt("BestDistance", Mathf.RoundToInt(recordDistance));
            foreach (var item in FindObjectsOfType<ShopItemUI>())
            {
                item.UpdateProgressFromPrefs();
                item.TryAutoUnlock();
            }
        }
    }

    // ИСПРАВЛЕНИЕ 5: GetKeyDown перенесён в Update — в FixedUpdate он пропускает нажатия,
    // потому что FixedUpdate и Update работают на разных частотах
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            DiePlayer();
            StartCoroutine(GameOver());
        }
    }

    void FixedUpdate()
    {
        // Ограничение скорости
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }

        // Обновляем текст с дистанцией
        currentDistance = transform.position.y - startY;
        distanceText.text = Mathf.Max(0, currentDistance).ToString("0") + " м";

        // Обновляем рекордную дистанцию за сессию
        if (currentDistance > highestDistance)
        {
            highestDistance = currentDistance;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Dead"))
        {
            DiePlayer();
            StopAllCoroutines();
            StartCoroutine(GameOver());
        }
    }

    IEnumerator GameOver()
    {
        Debug.Log("Проигрыш!");
        PlayerPrefs.SetInt("TotalLosses", PlayerPrefs.GetInt("TotalLosses", 0) + 1);
        PlayerPrefs.SetFloat("TotalDistance", PlayerPrefs.GetFloat("TotalDistance", 0) + currentDistance);
        float sessionDuration = 0f;
        if (playerScript.sessionStarted) 
        {
            sessionDuration = Time.time - playerScript.sessionStartTime;
            PlayerPrefs.SetFloat("TotalActiveTime", PlayerPrefs.GetFloat("TotalActiveTime", 0f) + sessionDuration);
            PlayerPrefs.Save();
            Debug.Log("Время с первого выстрела до смерти: " + sessionDuration + " сек.");
        }
        Time.timeScale = 1f;
        playerScript.canShoot = false; // Отключить возможность стрельбы
        rb.linearVelocity = Vector2.zero; // Остановить движение

        playerScript.currentAmmo = playerScript.maxAmmo;
        
        PauseButton.SetActive(false);

        gameOverPanel.SetActive(true); // Показать панель "ты проиграл"

        // Обновляем текст финальной дистанции и рекорда
        finalDistanceText.text = "Твоя дистанция: " + Mathf.Max(0, currentDistance).ToString("F2") + " м";
        if (currentDistance > recordDistance)
        {
            recordDistance = currentDistance;
            PlayerPrefs.SetFloat("RecordDistance", recordDistance); // Сохранить новый рекорд
            PlayerPrefs.Save();
        }
        recordText.text = "ㅤㅤㅤРекорд:ㅤㅤㅤ " + recordDistance.ToString("F2") + " м";

        // Активируем объекты при проигрыше
        foreach (GameObject obj in objectsToAppearOnGameOver)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }

        foreach (GameObject obj in objectsToDisappearOnGameOver)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }

        // ИСПРАВЛЕНИЕ 7: убрана лишняя строка coinText.text до анимации —
        // UpdateCoinTexts() ниже и так корректно обновит оба текста
        yield return new WaitForSeconds(0.5f); // Подождать 0.5 секунд
        Time.timeScale = 1f;

        // Обновляем общее количество монет
        yield return StartCoroutine(UpdateCoinTexts());

        yield return new WaitForSeconds(0.5f); // Подождать 0.5 секунд
        Time.timeScale = 1f;

        ContinueText.SetActive(true); // Показать текст "Продолжить"

        // Ожидание нажатия кнопки
        yield return new WaitUntil(() => Input.GetMouseButtonUp(0) || Input.touchCount > 0);

        playerScript.isWaitingToShoot = true; // Установить флаг ожидания
        yield return new WaitForSeconds(0.05f); // Ждать еще 1 секунду
        playerScript.isWaitingToShoot = false; // Разрешить стрельбу

        foreach (var item in FindObjectsOfType<ShopItemUI>())
        {
            Destroy(item.gameObject);
        }
        // Загружаем сцену заново
        EquipmentManager.Instance.RestartScene();
        StopAllCoroutines();

        yield break; // корутина завершается, сцена будет перезапущена
    }

    private bool hasDroppedWeapon = false;

    public void DisablePlayer()
    {
        // Отключить возможность стрелять и двигаться
        playerScript.canShoot = false;
        GetComponent<DangerIndicator>().enableWarnings = false; // Отключить подсказки
        rb.linearVelocity = Vector2.zero; // Остановить движение

        // Создаем эффект крови
        CreateBloodEffect(transform.position);

        // Отключить коллизию игрока
        GetComponent<Collider2D>().enabled = false;

        // Изменить цвет спрайта игрока на красный
        if (playerSprite != null)
        {
            SpriteRenderer spriteRenderer = playerSprite.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.red;
            }
        }

        // Выбросить оружие, если оно еще не выброшено
        if (!hasDroppedWeapon)
        {
            DropWeapon();
            hasDroppedWeapon = true; // Устанавливаем флаг
        }
        // Отталкивание игрока в противоположную сторону от пистолета
        KnockbackPlayer();

        if (timeAccelerationCoroutine == null)
        {
            // Сохраняем текущую скорость времени и запускаем корутину
            baseTimeScale = Time.timeScale;
            timeAccelerationCoroutine = StartCoroutine(TimeAccelerationRoutine());
        }
    }

    public void DiePlayer()
    {
        if (timeAccelerationCoroutine != null)
        {
            // Останавливаем корутину и сбрасываем время
            StopCoroutine(timeAccelerationCoroutine);
            timeAccelerationCoroutine = null;
            Time.timeScale = baseTimeScale; // Возвращаем стандартное значение времени
        }

        // Отключить возможность стрелять и двигаться
        playerScript.canShoot = false;
        rb.linearVelocity = Vector2.zero; // Остановить движение

        // Создаем эффект крови
        CreateBloodEffect(transform.position);

        // ИСПРАВЛЕНИЕ 6: отключаем все коллайдеры через GetComponents —
        // GetComponent<Collider2D>() и GetComponent<BoxCollider2D>() могли вернуть
        // один и тот же коллайдер, оставив второй включённым
        foreach (var col in GetComponents<Collider2D>())
            col.enabled = false;

        // Изменить цвет спрайта игрока на красный
        if (playerSprite != null)
        {
            SpriteRenderer spriteRenderer = playerSprite.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.red;
            }
        }

        // Выбросить оружие, если оно еще не выброшено
        if (!hasDroppedWeapon)
        {
            DropWeapon();
            hasDroppedWeapon = true; // Устанавливаем флаг
        }
    }

    void DropWeapon()
    {
        if (playerWeapon != null)
        {
        // Открепляем оружие от игрока
        playerWeapon.transform.parent = null;

        // Включаем коллайдер для оружия, если отключен (чтобы можно было взаимодействовать)
        Collider2D weaponCollider = playerWeapon.GetComponent<Collider2D>();
        if (weaponCollider != null)
        {
            weaponCollider.enabled = true;
        }

        // Добавляем компонент Rigidbody2D для движения, если его нет
        Rigidbody2D weaponRb = playerWeapon.GetComponent<Rigidbody2D>();
        if (weaponRb == null)
        {
            weaponRb = playerWeapon.AddComponent<Rigidbody2D>();
        }

        // Устанавливаем скорость для выброса оружия в направлении, куда оно направлено
        weaponRb.linearVelocity = playerScript.bulletSpawn.right * 5f; // Скорость выброса
        }
    }

    void KnockbackPlayer()
    {
        // Добавить импульс игроку в противоположную сторону от выброса оружия
        Vector2 knockbackDirection = -playerScript.bulletSpawn.right; // Направление от пистолета в противоположную сторону
        float knockbackForce = 5f; // Сила отталкивания

        // Применить силу отталкивания к игроку
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
    }

    void CreateBloodEffect(Vector2 position)
    {
        if (bloodEffectPrefab != null)
        {
            Instantiate(bloodEffectPrefab, position, Quaternion.identity);
        }
    }

    private IEnumerator TimeAccelerationRoutine()
    {
        int elapsedSeconds = 0; // Счётчик секунд
        while (true)
        {
            elapsedSeconds++; // Увеличиваем счетчик секунд
            Time.timeScale = baseTimeScale + (timeScaleIncrement * elapsedSeconds); // Увеличиваем скорость времени

            yield return new WaitForSeconds(1f); // Ждём одну секунду реального времени
        }
    }

    public void ResetSession()    
    {
        sessionCoinCount = 0;
        highestDistance = 0;
    }

    public void UpdateCoinUI()
    {
        coinText.text = coinCount.ToString();
    }

    public void UpdateSessionCoinUI()
    {
        sessionCoinText.text = sessionCoinCount.ToString();

        // Запускаем тряску текста
        StartCoroutine(ShakeCoinText());
    }

    // Новая корутина для обновления текстов монет
    IEnumerator UpdateCoinTexts()
    {
        // Определяем время, за которое нужно уменьшить монеты до нуля
        float duration = 0.6f; // 1 секунда
        float initialSessionCoinCount = sessionCoinCount; // Начальное количество монет за сессию
        float decreaseAmount = initialSessionCoinCount > 0 ? initialSessionCoinCount / duration : 0; // Количество, на которое будем уменьшать за кадр
        float elapsedTime = 0f; // Время, прошедшее с начала уменьшения

        // Уменьшение монет за сессию
        while (sessionCoinCount > 0)
        {
            sessionCoinText.text = Mathf.Floor(sessionCoinCount).ToString(); // Приведение к целому числу для отображения
            // Уменьшаем количество монет на величину decreaseAmount за каждый кадр
            sessionCoinCount -= decreaseAmount * Time.deltaTime; 
            // Запускаем тряску текста
            StartCoroutine(ShakeCoinText());
            sessionCoinCount = Mathf.Max(sessionCoinCount, 0); // Убедимся, что значение не станет отрицательным
            coinText.text = (coinCount - Mathf.Floor(sessionCoinCount)).ToString(); // Приведение к целому числу для отображения
            elapsedTime += Time.deltaTime; // Увеличиваем время
            yield return null; // Ждем следующего кадра
        }
        
        sessionCoinText.text = "0"; // Убедимся, что текст будет равен 0
        coinText.text = (coinCount).ToString(); // Убедимся, что текст будет равен конечному значению
    }

    IEnumerator ShakeCoinText()
    {
    Vector3 originalPosition = sessionCoinText.transform.localPosition; // Сохраняем оригинальную позицию
    float shakeDuration = 0.33f; // Длительность тряски
    float shakeMagnitude = 3.4f; // Сила тряски

    for (float t = 0; t < shakeDuration; t += Time.deltaTime)
    {
        float xOffset = Random.Range(-shakeMagnitude, shakeMagnitude);
        float yOffset = Random.Range(-shakeMagnitude, shakeMagnitude);
        sessionCoinText.transform.localPosition = new Vector3(originalPosition.x + xOffset, originalPosition.y + yOffset, originalPosition.z);
        yield return null; // Ждем до следующего кадра
    }

    sessionCoinText.transform.localPosition = originalPosition; // Возвращаем текст в оригинальную позицию
    }

    public void HandleObjectsOnShot()
    {
        // Появление объектов после выстрела
        foreach (GameObject obj in objectsToAppearAfterShot)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }

        // Исчезновение объектов при выстреле
        foreach (GameObject obj in objectsToDisappearOnShot)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
    }

}