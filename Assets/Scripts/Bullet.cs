using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class Bullet : MonoBehaviour
{
    public float lifetime = 1f;
    [Tooltip("Время (сек), в течение которого пуля не может убить игрока после вылета")]
    public float safeTime = 0.1f;

    public GameObject explosionEffect;
    public GameObject coinEffectPrefab;

    private static bool selfshotDisabled = false;

    private float spawnTime;
    private GunMove playerScript;
    private SelfShot selfshotScript;
    private GameObject selfshot;

    private Rigidbody2D rb;
    private Collider2D selfCollider;

    // 🔥 НОВОЕ: для рейкаста
    private Vector2 previousPosition;

    void Start()
    {
        spawnTime    = Time.time;
        rb           = GetComponent<Rigidbody2D>();
        selfCollider = GetComponent<Collider2D>();

        if (rb != null)
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        Destroy(gameObject, lifetime);

        previousPosition = transform.position;

        GameObject player = GameObject.FindWithTag("Player");
        GameObject canvas = GameObject.FindWithTag("Canvas");

        if (player != null)
            playerScript = player.GetComponent<GunMove>();

        if (canvas != null)
        {
            selfshotScript = canvas.GetComponent<SelfShot>();
            if (selfshotScript != null)
                selfshot = selfshotScript.selfshot;
        }

        if (!selfshotDisabled && selfshot != null)
        {
            selfshot.SetActive(false);
            selfshotDisabled = true;
        }
    }

    void FixedUpdate()
    {
        Vector2 currentPosition = transform.position;
        Vector2 direction = currentPosition - previousPosition;
        float distance = direction.magnitude;

        if (distance > 0f)
        {
            RaycastHit2D hit = Physics2D.Raycast(previousPosition, direction.normalized, distance);

            if (hit.collider != null && hit.collider != selfCollider)
            {
                HandleHit(hit.collider);
            }
        }

        previousPosition = currentPosition;
    }

    // 🔥 НОВОЕ: общая обработка попадания (чтобы не дублировать код)
    void HandleHit(Collider2D other)
    {
        if (other.CompareTag("WoodenPlatform") ||
            other.CompareTag("Dead")           ||
            other.CompareTag("Coin")           ||
            other.CompareTag("ExplosiveBarrel")||
            other.CompareTag("EditorOnly"))
        {
            Physics2D.IgnoreCollision(other, selfCollider);
            return;
        }

        if (other.CompareTag("Enemy"))
        {
            EnemyPlatform enemy = other.GetComponent<EnemyPlatform>();
            if (enemy != null)
            {
                enemy.KilledByPlayer();
                enemy.Die();
            }
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Player") && gameObject.CompareTag("PlayerBullet"))
        {
            if (Time.time - spawnTime < safeTime) return;

            if (selfshot != null) selfshot.SetActive(true);

            GunMove player = other.GetComponent<GunMove>();
            if (player != null) player.DisablePlayer();

            Destroy(gameObject);
            return;
        }

        CreateExplosionEffect();
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleHit(other);
    }

    void CreateExplosionEffect()
    {
        if (explosionEffect == null) return;

        GameObject explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);
        ParticleSystem ps = explosion.GetComponent<ParticleSystem>();

        if (ps != null)
        {
            ParticleSystem.MainModule main = ps.main;
            if (rb != null)
            {
                Vector2 vel = rb.linearVelocity;
                main.startRotation = Mathf.Atan2(vel.y, vel.x);
            }
            Destroy(explosion, main.duration);
        }
        else
        {
            Destroy(explosion, 1f);
        }
    }
}