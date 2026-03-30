using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPlatform : MonoBehaviour
{
    private Transform player; // Игрок
    public Transform visionZone; // Зона видимости врага
    private bool playerInRange = false; // Флаг, указывающий на нахождение игрока в зоне видимости
    public float sightDistance = 10f; // Дистанция, на которой враг может видеть игрока
    public LayerMask obstacleLayer; // Слой, через который враг не может видеть игрока (например, стены или другие объекты)
    public Transform gun; // Пистолет врага
    private SpriteRenderer gunSpriteRenderer; // Спрайт рендерер пистолета
    private float lastScaleX; // Для отслеживания текущего масштаба

    public GameObject bulletPrefab; // Префаб пули
    public Transform bulletSpawnPoint; // Точка, откуда будет вылетать пуля
    public float shootInterval = 1f; // Интервал между выстрелами
    public float bulletSpeed = 10f; // Скорость пули
    public float bulletLifetime = 5f; // Время жизни пули

    private float shootTimer = 0f; // Таймер для отслеживания времени между выстрелами
    private bool isDead = false; // Флаг смерти врага
    private bool killedByPlayer = false;
    private Rigidbody2D rb; // Ригидбоди врага
    private SpriteRenderer enemySpriteRenderer; // Спрайт рендерер врага
    private PolygonCollider2D gunCollider; // Коллайдер пистолета

    // Для эффекта частиц при смерти
    public ParticleSystem deathEffect; // Эффект частиц при смерти врага

    public GameObject bulletDecayEffect; // Префаб эффекта распада пули

    private void Start()
    {
        StopAllCoroutines(); // Остановить все корутины на этом объекте
        
        // Находим игрока в сцене
        player = FindObjectOfType<GunMove>().transform;

        // Инициализация
        lastScaleX = transform.localScale.x;
        gunSpriteRenderer = gun.GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        enemySpriteRenderer = GetComponent<SpriteRenderer>();
        gunCollider = gun.GetComponent<PolygonCollider2D>();

        // Изначально коллайдер пистолета выключен
        gunCollider.enabled = false;
    }

    private void Update()
    {
        if (!isDead)
        {
            // Если игрок находится в зоне видимости и не за препятствием
            if (playerInRange && CanSeePlayer())
            {
                // Поворачиваем пистолет в сторону игрока
                AimGunAtPlayer();

                // Определяем направление к игроку
                float directionToPlayer = player.position.x - transform.position.x;

                // Проверяем, куда должен смотреть враг
                bool shouldFaceRight = directionToPlayer > 0;

                // Проверяем, куда он сейчас смотрит (из-за возможной инверсии localScale)
                bool isFacingRight = transform.localScale.x > 0;

                // Если враг смотрит не в ту сторону, переворачиваем его
                if (shouldFaceRight != isFacingRight)
                {
                    FlipEnemy();
                    FlipGun();
                }

                // Обновляем таймер выстрела
                shootTimer += Time.deltaTime;

                // Если таймер больше или равен интервалу выстрела, стреляем
                if (shootTimer >= shootInterval)
                {
                    ShootBullet();
                    shootTimer = 0f; // Сбрасываем таймер после выстрела
                }
            }
            else
            {
                // Если игрок не в зоне видимости, пистолет не направлен
                //ResetGunAim();
            }
        }
    }

    private bool CanSeePlayer()
    {
        // Проверка, есть ли линия видимости от врага до игрока, не перекрытая препятствиями
        RaycastHit2D hit = Physics2D.Raycast(transform.position, (player.position - transform.position).normalized, sightDistance, obstacleLayer);

        // Если луч не столкнулся с препятствием, враг видит игрока
        if (hit.collider == null || hit.collider.CompareTag("Player"))
        {
            return true;
        }

        return false;
    }

    private void AimGunAtPlayer()
    {
        // Вычисляем направление от пистолета до игрока
        Vector2 directionToPlayer = player.position - gun.position;

        // Вычисляем угол между направлением пистолета и направлением к игроку
        float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;

        // Поворачиваем пистолет в сторону игрока
        gun.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }

    private void ResetGunAim()
    {
        // Восстановление пистолета в исходное положение (например, на правую сторону)
        gun.rotation = Quaternion.identity;
    }

    public void FlipEnemy()
    {
        // Меняем знак масштаба по оси X, что приведет к зеркалированию объекта
        lastScaleX = -lastScaleX; // Обновляем текущий масштаб по X
        transform.localScale = new Vector3(lastScaleX, transform.localScale.y, transform.localScale.z);
    }

    public void FlipGun()
    {
        // Флипаем спрайт пистолета по оси X, если враг смотрит влево
        if (transform.localScale.x < 0)
        {
            gunSpriteRenderer.flipX = true; // Пистолет смотрит влево
            gunSpriteRenderer.flipY = true; // Пистолет смотрит влево
        }
        else
        {
            gunSpriteRenderer.flipX = false; // Пистолет смотрит вправо
            gunSpriteRenderer.flipY = false; // Пистолет смотрит вправо
        }
    }

    private void ShootBullet()
    {
        // Создаем пулю
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);

        // Направляем пулю в сторону игрока
        Vector2 directionToPlayer = (player.position - gun.position).normalized;
        bullet.GetComponent<Rigidbody2D>().linearVelocity = directionToPlayer * bulletSpeed;

        // Запускаем корутину распада пули
        StartCoroutine(BulletDecay(bullet));
    }

    // Корутину распада пули
    private IEnumerator BulletDecay(GameObject bullet)
    {
        if (bullet == null) yield break;
        
        SpriteRenderer sr = bullet.GetComponent<SpriteRenderer>();
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();

        Vector3 lastPosition = bullet.transform.position;

        // Ждем жизни пули
        yield return new WaitForSeconds(bulletLifetime);

        if (bullet == null) yield break;

        // Останавливаем пулю
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (bulletDecayEffect != null)
            Instantiate(bulletDecayEffect, bullet.transform.position, Quaternion.identity);

        // Плавное исчезновение спрайта
        if (sr != null)
        {
            float fadeDuration = 0.3f; // время распада
            float timeElapsed = 0f;
            Color startColor = sr.color;

            while (timeElapsed < fadeDuration)
            {
                if (bullet == null) yield break;

                float alpha = Mathf.Lerp(startColor.a, 0f, timeElapsed / fadeDuration);
                sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                timeElapsed += Time.deltaTime;
                yield return null;
            }
        }

        // Удаляем пулю
        if (bullet != null)
            Destroy(bullet);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Когда игрок входит в зону видимости
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Когда игрок покидает зону видимости
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    public void KilledByPlayer()
    {
        killedByPlayer = true;
    }

    public void Die()
    {
        if (isDead) return;
        
        // Враг умирает
        isDead = true;

        // Отключаем все действия врага
        playerInRange = false;
        gunSpriteRenderer.enabled = true; // Прячем пистолет

        // Включаем коллайдер пистолета
        gunCollider.enabled = true;

        // Добавляем физику пистолету
        Rigidbody2D gunRb = gun.GetComponent<Rigidbody2D>();
        gunRb.isKinematic = false;
        gunRb.linearVelocity = new Vector2(gunRb.linearVelocity.x, gunRb.linearVelocity.y); // Применяем инерцию для пистолета

        // Включаем физику для самого врага и отбрасываем его
        rb.isKinematic = false;
        rb.freezeRotation = false; // Разблокируем вращение
        Vector2 direction = (transform.position - player.position).normalized;
        rb.AddForce(direction * 5f, ForceMode2D.Impulse); // Отбрасываем врага от пули

        // Меняем цвет врага на красноватый и делаем его полупрозрачным
        enemySpriteRenderer.color = new Color(1f, 0f, 0f, 0.5f);

        // Меняем цвет пистолета на красноватый и полупрозрачный
        gunSpriteRenderer.color = new Color(1f, 0f, 0f, 0.5f);

        // Запускаем эффект частиц
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity); // Создаем эффект частиц
        }

        if (killedByPlayer == true)
        {
            GunMove playerScript = GameObject.FindWithTag("Player").GetComponent<GunMove>();
            if (playerScript != null)
            {
                playerScript.coinCount += 5;
                playerScript.sessionCoinCount += 5;
                playerScript.UpdateCoinUI();
                playerScript.UpdateSessionCoinUI();
                PlayerPrefs.SetInt("Coins", playerScript.coinCount); // Сохраняем монеты

                PlayerPrefs.SetInt("TotalKills", PlayerPrefs.GetInt("TotalKills", 0) + 1);
                PlayerPrefs.SetInt("TotalCoinsCollected", PlayerPrefs.GetInt("TotalCoinsCollected", 0) + 5);
                // Обновить прогресс на слотах (если магазин открыт)
                foreach (var item in FindObjectsOfType<ShopItemUI>())
                {
                    item.UpdateProgressFromPrefs();
                    item.TryAutoUnlock();
                }
            }
        }

        // Запускаем корутину для постепенного исчезновения врага
        StartCoroutine(FadeOutAndDestroy());
        StartCoroutine(FadeOutGun());
    }

    private IEnumerator FadeOutAndDestroy()
    {
        float fadeDuration = 2f;
        float startAlpha = enemySpriteRenderer.color.a;
        float timeElapsed = 0f;

        while (timeElapsed < fadeDuration)
        {
            float newAlpha = Mathf.Lerp(startAlpha, 0f, timeElapsed / fadeDuration);
            enemySpriteRenderer.color = new Color(1f, 0f, 0f, newAlpha);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // Удаляем врага после исчезновения
        Destroy(gameObject);
    }
    // Корутину для исчезновения пистолета
    private IEnumerator FadeOutGun()
    {
    float fadeDuration = 2f;
    float startAlpha = gunSpriteRenderer.color.a;
    float timeElapsed = 0f;

    while (timeElapsed < fadeDuration)
    {
        float newAlpha = Mathf.Lerp(startAlpha, 0f, timeElapsed / fadeDuration);
        gunSpriteRenderer.color = new Color(1f, 0f, 0f, newAlpha); // Уменьшаем альфа-канал пистолета
        timeElapsed += Time.deltaTime;
        yield return null;
    }

    // После того как пистолет исчез, удаляем его
    Destroy(gun);
    }

    private void OnDrawGizmosSelected()
    {
        // Для визуализации зоны видимости и луча в редакторе
        if (player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }

        if (visionZone != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(visionZone.position, sightDistance);
        }
    }
}