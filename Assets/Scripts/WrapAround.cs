using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WrapAround : MonoBehaviour
{
    private Camera mainCamera;
    private float screenWidth;

    public bool canWrapAround = false;
    public bool inWall = false;
    public float bounceFactor = 1.36f;
    [SerializeField] private float wallPushForce = 2f;

    private Rigidbody2D rb;

    public List<Collider2D> leftWalls;
    public List<Collider2D> rightWalls;

    private float wrapCooldown = 0.2f;
    private float lastWrapTime = -1f;

    // Кэшируем LayerMask чтобы не вызывать LayerMask.NameToLayer каждый OnTrigger
    private int wallLayer;
    private int blocksLayer;

    void Start()
    {
        mainCamera  = Camera.main;
        float height = 2f * mainCamera.orthographicSize;
        screenWidth  = height * mainCamera.aspect;
        rb           = GetComponent<Rigidbody2D>();
        if (rb == null)
            Debug.LogError("Rigidbody2D не найден на объекте " + gameObject.name);

        // Кэшируем слои один раз в Start
        wallLayer   = LayerMask.NameToLayer("Wall");
        blocksLayer = LayerMask.NameToLayer("Blocks");
    }

    // ── Крайние стены ────────────────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == wallLayer)
        {
            inWall = true;

            if (canWrapAround)
            {
                TeleportThroughWall(collision);
            }
            else
            {
                if (collision.transform.position.x < transform.position.x)
                    ApplyBounce(Vector2.right);
                else
                    ApplyBounce(Vector2.left);
            }
        }
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (rb == null) return;

        if (collision.gameObject.layer == wallLayer && !canWrapAround)
        {
            Vector2 pushDir = collision.transform.position.x < transform.position.x
                ? Vector2.right
                : Vector2.left;

            rb.linearVelocity = new Vector2(pushDir.x * wallPushForce, rb.linearVelocity.y);
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == wallLayer)
            inWall = false;
    }

    // ── Тайлы (Platform / Blocks) ─────────────────────────────────────────────
    // Используем OnCollisionEnter2D (не Trigger) потому что тайлы — твёрдые коллайдеры.
    // Направление отскока определяем по нормали контакта — это единственный
    // надёжный способ для Tilemap, у которого один большой общий коллайдер.

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer != blocksLayer) return;

        // Перебираем точки контакта и ищем горизонтальную нормаль
        // (нормаль.x значительная → это боковое столкновение)
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector2 normal = collision.GetContact(i).normal;

            // Если нормаль достаточно горизонтальная — это боковой удар о тайл
            // Порог 0.5 означает: угол нормали > 45° от вертикали
            if (Mathf.Abs(normal.x) > 0.5f)
            {
                // normal.x > 0 → стена слева от игрока → отскок вправо
                // normal.x < 0 → стена справа от игрока → отскок влево
                Vector2 bounceDir = normal.x > 0 ? Vector2.right : Vector2.left;
                ApplyBounce(bounceDir);
                break; // достаточно одного контакта
            }
        }
    }

    // OnCollisionStay2D — мягко выталкиваем из тайла если застряли
    void OnCollisionStay2D(Collision2D collision)
    {
        if (rb == null) return;
        if (collision.gameObject.layer != blocksLayer) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector2 normal = collision.GetContact(i).normal;
            if (Mathf.Abs(normal.x) > 0.5f)
            {
                Vector2 pushDir = normal.x > 0 ? Vector2.right : Vector2.left;
                rb.linearVelocity = new Vector2(pushDir.x * wallPushForce, rb.linearVelocity.y);
                break;
            }
        }
    }

    // ── Update — ограничение/wrap по краям экрана ────────────────────────────

    void Update()
    {
        Vector3 position = transform.position;
        float camX = mainCamera.transform.position.x;

        if (!canWrapAround)
        {
            if (position.x > camX + screenWidth * 0.5f)
            {
                position.x = camX + screenWidth * 0.5f;
                ApplyBounce(Vector2.left);
            }
            else if (position.x < camX - screenWidth * 0.5f)
            {
                position.x = camX - screenWidth * 0.5f;
                ApplyBounce(Vector2.right);
            }
        }
        else
        {
            if (position.x > camX + screenWidth * 0.5f)
                position.x = camX - screenWidth * 0.5f;
            else if (position.x < camX - screenWidth * 0.5f)
                position.x = camX + screenWidth * 0.5f;
        }

        transform.position = position;
    }

    // ── Телепорт сквозь стену (wrap) ─────────────────────────────────────────

    void TeleportThroughWall(Collider2D wall)
    {
        if (Time.time - lastWrapTime < wrapCooldown) return;

        Vector3 pos = transform.position;
        List<Collider2D> targetList = null;

        if (leftWalls.Contains(wall))
            targetList = rightWalls;
        else if (rightWalls.Contains(wall))
            targetList = leftWalls;

        if (targetList == null || targetList.Count == 0) return;

        Collider2D nearest = targetList[0];
        float minDist = Mathf.Abs(nearest.bounds.center.y - pos.y);

        foreach (var w in targetList)
        {
            float dist = Mathf.Abs(w.bounds.center.y - pos.y);
            if (dist < minDist) { minDist = dist; nearest = w; }
        }

        float safeOffset = 0.2f;

        if (leftWalls.Contains(wall))
            pos.x = nearest.bounds.min.x - safeOffset;
        else
            pos.x = nearest.bounds.max.x + safeOffset;

        transform.position = pos;
        lastWrapTime = Time.time;
    }

    // ── Отскок ───────────────────────────────────────────────────────────────

    void ApplyBounce(Vector2 direction)
    {
        if (rb == null) return;

        Vector2 velocity = rb.linearVelocity;
        float minHorizontalSpeed = 2f;

        velocity.x = direction.x * Mathf.Max(Mathf.Abs(velocity.x), minHorizontalSpeed) * bounceFactor;
        velocity.y = Mathf.Max(velocity.y, 1f);

        rb.linearVelocity = velocity;
    }
}