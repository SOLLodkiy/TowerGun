using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Bullet : MonoBehaviour
{
    public float lifetime = 1f; // Время жизни пули
    [Tooltip("Время (сек), в течение которого пуля не может убить игрока после вылета")]
    public float safeTime = 0.1f; 
    
    public GameObject explosionEffect; // Префаб эффекта распада пули
    public GameObject coinEffectPrefab; // Префаб эффекта монет
    private GameObject selfshot; // Префаб UI самовыстрел
    private GunMove playerScript; // Ссылка на скрипт игрока
    private SelfShot selfshotScript; // Ссылка на скрипт игрока
    private static bool selfshotDisabled = false; // Флаг для проверки, выполнено ли действие

    private float spawnTime; // Время появления пули

    void Start()
    {
        // Запоминаем точное время появления
        spawnTime = Time.time;

        // Уничтожаем пулю через заданное время
        Destroy(gameObject, lifetime);

        // Находим объект игрока и получаем его скрипт
        GameObject player = GameObject.FindWithTag("Player");
        GameObject canvas = GameObject.FindWithTag("Canvas");
        if (player != null)
        {
            playerScript = player.GetComponent<GunMove>();
            if (canvas != null)
            {
                selfshotScript = canvas.GetComponent<SelfShot>();
                if (selfshotScript != null)
                {
                    selfshot = selfshotScript.selfshot;
                }
            }
        }

        if (!selfshotDisabled && selfshot != null)
        {
            selfshot.SetActive(false);
            selfshotDisabled = true;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("WoodenPlatform") || other.CompareTag("Dead") || other.CompareTag("Coin") || other.CompareTag("ExplosiveBarrel") || other.CompareTag("EditorOnly"))
        {
            Physics2D.IgnoreCollision(other, GetComponent<Collider2D>());
            return;
        }
        else if (other.CompareTag("Enemy"))
        {
            other.GetComponent<EnemyPlatform>().KilledByPlayer();
            other.GetComponent<EnemyPlatform>().Die();
            Destroy(gameObject);
        }
        else if (other.CompareTag("Player") && gameObject.CompareTag("PlayerBullet"))
        {
            // ПРОВЕРКА: Если прошло меньше времени, чем safeTime — игнорируем столкновение
            if (Time.time - spawnTime < safeTime)
            {
                return; 
            }

            // Если пуля «старая» (вылетела давно) — убиваем игрока
            if (selfshot != null) selfshot.SetActive(true);
            
            GunMove player = other.GetComponent<GunMove>();
            if (player != null)
            {
                player.DisablePlayer();
            }
            
            Destroy(gameObject);
        }
        else
        {
            CreateExplosionEffect();
            Destroy(gameObject);
        }
    }

    void CreateExplosionEffect()
    {
        if (explosionEffect != null)
        {
            GameObject explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            ParticleSystem particleSystem = explosion.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                ParticleSystem.MainModule main = particleSystem.main;
                // Небольшая правка: используем velocity2D для направления частиц
                Vector2 vel = GetComponent<Rigidbody2D>().linearVelocity;
                main.startRotation = Mathf.Atan2(vel.y, vel.x);
                Destroy(explosion, main.duration);
            }
            else
            {
                Destroy(explosion, 1f);
            }
        }
    }
}