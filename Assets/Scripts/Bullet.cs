using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float lifetime = 1f;
    [Tooltip("Время (сек), в течение которого пуля не может убить игрока после вылета")]
    public float safeTime = 0.1f;

    public GameObject explosionEffect;
    public GameObject coinEffectPrefab;

    [Header("Ricochet")]
    public bool ricochetEnabled = false;
    public int maxRicochets = 3;
    public string ricochetTag = "Platform";

    private int ricochetCount = 0;
    private bool handlingHit = false;
    private float lastRicochetTime = -1f;
    private float ricochetCooldown = 0.05f;

    private bool selfshotDisabled = false;

    private float spawnTime;
    private GunMove playerScript;
    private SelfShot selfshotScript;
    private GameObject selfshot;

    private Rigidbody2D rb;
    private Collider2D selfCollider;
    private Collider2D playerCollider;

    private ContactFilter2D contactFilter;

    void Start()
    {
        spawnTime    = Time.time;
        rb           = GetComponent<Rigidbody2D>();
        selfCollider = GetComponent<Collider2D>();

        DangerIndicator di = FindFirstObjectByType<DangerIndicator>();
        if (di != null)
        {
            ricochetEnabled = di.ricochetEnabled;
            selfshotDisabled = di.selfshotDisabled;
        }

        lifetime = ricochetEnabled ? 2.75f : 0.67f;

        if (rb != null)
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        contactFilter = new ContactFilter2D();
        contactFilter.useTriggers = false;
        contactFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));

        Destroy(gameObject, lifetime);

        GameObject player = GameObject.FindWithTag("Player");
        GameObject canvas = GameObject.FindWithTag("Canvas");

        if (player != null)
        {
            playerScript  = player.GetComponent<GunMove>();
            playerCollider = player.GetComponent<Collider2D>();

            // Игнорируем коллайдер игрока на время safeTime
            if (playerCollider != null)
                StartCoroutine(IgnorePlayerRoutine());
        }

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

        // Игнорируем ВСЕ коллайдеры которые перекрываются с пулей при спавне,
        // кроме врагов — их убиваем сразу. Пулю при этом НЕ уничтожаем.
        Collider2D[] overlaps = new Collider2D[8];
        int overlapCount = selfCollider.Overlap(contactFilter, overlaps);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D other = overlaps[i];
            if (other == selfCollider) continue;

            if (other.CompareTag("Enemy"))
            {
                EnemyPlatform enemy = other.GetComponent<EnemyPlatform>();
                if (enemy != null) { enemy.KilledByPlayer(); enemy.Die(); }
                Destroy(gameObject);
                return;
            }

            // Для всех остальных — просто игнорируем коллизию, пуля летит дальше
            Physics2D.IgnoreCollision(selfCollider, other, true);
        }
    }

    private System.Collections.IEnumerator IgnorePlayerRoutine()
    {
        Physics2D.IgnoreCollision(selfCollider, playerCollider, true);
        yield return new WaitForSeconds(safeTime);
        if (selfCollider != null && playerCollider != null)
            Physics2D.IgnoreCollision(selfCollider, playerCollider, false);
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        Vector2 velocity = rb.linearVelocity;
        float   distance = velocity.magnitude * Time.fixedDeltaTime;

        if (distance <= 0f) return;

        RaycastHit2D[] hits = new RaycastHit2D[8];
        float castDistance = Mathf.Max(0f, distance - Physics2D.defaultContactOffset);
        int hitCount = selfCollider.Cast(velocity.normalized, contactFilter, hits, castDistance);

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D other = hits[i].collider;
            if (other == selfCollider) continue;

            if (IsIgnored(other))
            {
                Physics2D.IgnoreCollision(other, selfCollider);
                continue;
            }

            HandleHit(other, hits[i].normal);
            break;
        }

        handlingHit = false;
    }

    private bool IsIgnored(Collider2D other)
    {
        return other.CompareTag("WoodenPlatform") ||
               other.CompareTag("Dead")           ||
               other.CompareTag("Coin")           ||
               other.CompareTag("ExplosiveBarrel")||
               other.CompareTag("EditorOnly");
    }

    void HandleHit(Collider2D other, Vector2 hitNormal = default)
    {
        if (handlingHit) return;
        handlingHit = true;

        if (other.CompareTag("Enemy"))
        {
            EnemyPlatform enemy = other.GetComponent<EnemyPlatform>();
            if (enemy != null) { enemy.KilledByPlayer(); enemy.Die(); }
            CreateExplosionEffect();
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Player") && gameObject.CompareTag("PlayerBullet"))
        {
            if (Time.time - spawnTime < safeTime)
            {
                handlingHit = false;
                return;
            }
            if (selfshot != null) selfshot.SetActive(true);
            GunMove player = other.GetComponent<GunMove>();
            if (player != null) player.DisablePlayer();
            CreateExplosionEffect();
            Destroy(gameObject);
            return;
        }

        if (ricochetEnabled && other.CompareTag(ricochetTag))
        {
            if (Time.time - lastRicochetTime < ricochetCooldown)
            {
                handlingHit = false;
                return;
            }

            if (ricochetCount < maxRicochets)
            {
                ricochetCount++;
                lastRicochetTime = Time.time;

                if (rb != null && hitNormal != Vector2.zero)
                {
                    rb.linearVelocity = Vector2.Reflect(rb.linearVelocity, hitNormal);
                    float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }

                handlingHit = false;
                return;
            }
        }

        CreateExplosionEffect();
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.isTrigger && !IsIgnored(other))
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