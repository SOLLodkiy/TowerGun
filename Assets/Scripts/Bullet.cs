using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Bullet : MonoBehaviour
{
    public float lifetime = 1f; // Время жизни пули
    public GameObject explosionEffect; // Префаб эффекта распада пули
    public GameObject coinEffectPrefab; // Префаб эффекта монет
    private GameObject selfshot; // Префаб UI самовыстрел
    private GunMove playerScript; // Ссылка на скрипт игрока
    private SelfShot selfshotScript; // Ссылка на скрипт игрока
    private static bool selfshotDisabled = false; // Флаг для проверки, выполнено ли действие

    void Start()
    {
        // Уничтожаем пулю через заданное время
        Destroy(gameObject, lifetime);

        // Находим объект игрока и получаем его скрипт
        GameObject player = GameObject.FindWithTag("Player");
        GameObject canvas = GameObject.FindWithTag("Canvas");
        if (player != null)
        {
            playerScript = player.GetComponent<GunMove>();
            selfshotScript = canvas.GetComponent<SelfShot>();
            if (selfshotScript != null)
            {
                selfshot = selfshotScript.selfshot;
            }   
        }

        if (!selfshotDisabled && selfshot != null)
        {
            selfshot.SetActive(false);
            selfshotDisabled = true; // Устанавливаем флаг, чтобы больше не выполнять
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("WoodenPlatform") || other.CompareTag("Dead") || other.CompareTag("Coin") || other.CompareTag("ExplosiveBarrel") || other.CompareTag("EditorOnly"))
        {
            // Игнорируем коллизию с платформами
            Physics2D.IgnoreCollision(other, GetComponent<Collider2D>());
            return; // Не уничтожаем пулю
        }
        // Если пуля сталкивается с врагом
        else if (other.CompareTag("Enemy"))
        {
            // Передаем событие для врага
            other.GetComponent<EnemyPlatform>().KilledByPlayer();  // Вызов метода смерти врага
            other.GetComponent<EnemyPlatform>().Die();
            // Удаляем пулю
            Destroy(gameObject);  // Или выключаем пулю
        }
        else if (other.CompareTag("Player") && gameObject.CompareTag("PlayerBullet"))
        {
            // Включаем надпись выстрел в себя
            selfshot.SetActive(true);
            Destroy(gameObject);
            // Получаем скрипт GunMove и отключаем игрока
            GunMove player = other.GetComponent<GunMove>();
            if (player != null)
            {
                selfshot.SetActive(true);
                player.DisablePlayer();
                selfshot.SetActive(true);
            }
            selfshot.SetActive(true);
        }
        else
        {
            // Создаем эффект распада пули при столкновении с другими объектами
            CreateExplosionEffect();
            Destroy(gameObject);
        }
    }

    void CreateExplosionEffect()
    {
        if (explosionEffect != null)
        {
        // Создаем эффект распада пули
        GameObject explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);
        
        // Получаем компонент системы частиц
        ParticleSystem particleSystem = explosion.GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            // Настройка направления частиц
            ParticleSystem.MainModule main = particleSystem.main;
            main.startRotation = Mathf.Atan2(GetComponent<Rigidbody2D>().linearVelocity.y, GetComponent<Rigidbody2D>().linearVelocity.x);
        }
        
        // Уничтожаем эффект через некоторое время
        Destroy(explosion, particleSystem.main.duration);
        }
    }
}