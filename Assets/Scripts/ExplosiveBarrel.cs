using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosiveBarrel : MonoBehaviour
{
    public float explosionRadius = 3f; // Радиус взрыва
    public float explosionForce = 500f; // Сила взрыва
    public GameObject explosionEffect; // Префаб взрыва (эффект частиц)
    public GameObject Enemy; // Префаб врага
    public float cameraShakeAmount = 0.3f; // Сила тряски экрана
    public float cameraShakeDuration = 0.5f; // Длительность тряски экрана
    private CameraFollow cameraFollow;

    private bool hasExploded = false;
    private bool hasExplodedByPlayer = false;

    private void Start() 
    {
        cameraFollow = FindFirstObjectByType<CameraFollow>(); // Находим камеру с классом CameraFollow
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((!hasExploded) &&
        (collision.CompareTag("PlayerBullet") || collision.CompareTag("EnemyBullet") ||
         collision.gameObject.layer == LayerMask.NameToLayer("Saw") ||
         collision.CompareTag("DeadlyBlock")))
        {
            // Если это пуля — уничтожаем её
            if (collision.CompareTag("PlayerBullet") || collision.CompareTag("EnemyBullet"))
            {
                Destroy(collision.gameObject);
                GunMove playerScript = GameObject.FindWithTag("Player").GetComponent<GunMove>();
                if (playerScript != null)
                {
                    playerScript.coinCount += 2;
                    playerScript.sessionCoinCount += 2;
                    playerScript.UpdateCoinUI();
                    playerScript.UpdateSessionCoinUI();
                    PlayerPrefs.SetInt("Coins", playerScript.coinCount); // Сохраняем монеты
                    PlayerPrefs.SetInt("TotalCoinsCollected", PlayerPrefs.GetInt("TotalCoinsCollected", 0) + 2);
                }
                hasExplodedByPlayer = true;
            }

            // Вызываем взрыв
            Explode();
        }
    }

    private void Explode()
    {
        hasExploded = true;

        // Воспроизведение эффекта взрыва
        if (explosionEffect)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }

        // Поиск всех объектов в радиусе взрыва
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D hit in hitColliders)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, (hit.transform.position - transform.position).normalized, explosionRadius);
            bool hasObstacle = false;
            foreach (RaycastHit2D h in hits)
            {
                if (h.collider != hit && h.collider.CompareTag("Platform")) // Проверяем, есть ли стена между бочкой и врагом
                {
                    hasObstacle = true;
                    break;
                }
            }
            if (hasObstacle) continue; // Если есть препятствие, игнорируем объект

            // Если попали в игрока, наносим урон
            GunMove player = hit.GetComponent<GunMove>();
            if (player != null)
            {
                player.DisablePlayer(); // Функция умирания
            }

            // Если попали во врага, наносим урон
            EnemyPlatform Enemy = hit.GetComponentInParent<EnemyPlatform>();
            if (Enemy != null)
            {
                if (hasExplodedByPlayer == true)
                {
                    GunMove playerScript = GameObject.FindWithTag("Player").GetComponent<GunMove>();
                    if (playerScript != null)
                    {
                        GameObject.FindWithTag("Enemy").GetComponent<EnemyPlatform>().KilledByPlayer();
                        PlayerPrefs.SetInt("TotalKills", PlayerPrefs.GetInt("TotalKills", 0) + 1);

                        // Обновить прогресс на слотах (если магазин открыт)
                        foreach (var item in FindObjectsByType<ShopItemUI>(FindObjectsSortMode.None))
                        {
                            item.UpdateProgressFromPrefs();
                            item.TryAutoUnlock();
                        }
                    }
                }
                Enemy.Die(); // Функция умирания
            }

            // Если попали в объект с Rigidbody2D, отталкиваем его
            Rigidbody2D rb = hit.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 forceDirection = (hit.transform.position - transform.position).normalized;
                rb.AddForce(forceDirection * explosionForce, ForceMode2D.Impulse);
            }
        }

        // Тряска экрана
        if (cameraFollow != null)
        {
            Debug.Log("Тряска от взрыва запущена!"); // Проверка
            cameraFollow.SetShake(cameraShakeAmount, cameraShakeDuration);
        }

        // Удаляем бочку
        Destroy(gameObject);
    }

    // Визуализация радиуса взрыва в редакторе
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}