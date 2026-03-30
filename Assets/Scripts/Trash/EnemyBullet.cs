using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public GameObject explosionEffect; // Префаб эффекта распада пули

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("WoodenPlatform") || other.CompareTag("Dead") || other.CompareTag("Coin") || other.CompareTag("ExplosiveBarrel") || other.CompareTag("EditorOnly") || other.CompareTag("Enemy"))
        {
            // Игнорируем коллизию с платформами
            Physics2D.IgnoreCollision(other, GetComponent<Collider2D>());
            return; // Не уничтожаем пулю
        }
        else
        {
            // Создаем эффект распада пули при столкновении с другими объектами
            CreateEExplosionEffect();
            Destroy(gameObject);
        }
    }

    public void CreateEExplosionEffect()
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