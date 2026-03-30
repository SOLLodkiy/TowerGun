using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WrapAround : MonoBehaviour
{
    private Camera mainCamera;
    private float screenWidth;

    public bool canWrapAround = false;
    public bool inWall = false;
    public float bounceFactor = 1.4f;
    private float wallStayTimer = 0f;

    private Rigidbody2D rb;

    // Новые поля для указания стен вручную
    public List<Collider2D> leftWalls;
    public List<Collider2D> rightWalls;

    private float wrapCooldown = 0.2f; // Время блокировки повторного телепорта
    private float lastWrapTime = -1f;  // Время последнего телепорта

    void Start()
    {
        mainCamera = Camera.main;
        float height = 2f * mainCamera.orthographicSize;
        screenWidth = height * mainCamera.aspect;
        rb = GetComponent<Rigidbody2D>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            inWall = true;

            if (canWrapAround)
            {
                TeleportThroughWall(collision);
            }
            else
            {
                if (collision.transform.position.x < transform.position.x) // Стена слева
                {
                    ApplyBounce(Vector2.right); // Отбрасываем вправо
                }
                else if (collision.transform.position.x > transform.position.x) // Стена справа
                {
                    ApplyBounce(Vector2.left); // Отбрасываем влево
                }
            }
        }
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            wallStayTimer += Time.deltaTime;

            if (wallStayTimer >= 0.1f)
            {
                wallStayTimer = 0f;
                if (!canWrapAround)
                {
                    if (collision.transform.position.x < transform.position.x) // Стена слева
                    {
                        ApplyBounce(Vector2.right);
                    }
                    else if (collision.transform.position.x > transform.position.x) // Стена справа
                    {
                        ApplyBounce(Vector2.left);
                    }
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            inWall = false;
            wallStayTimer = 0f;
        }
    }

    void Update()
    {
        Vector3 position = transform.position;

        if (!canWrapAround)
        {
            if (position.x > mainCamera.transform.position.x + screenWidth / 2)
            {
                position.x = mainCamera.transform.position.x + screenWidth / 2;
                ApplyBounce(Vector2.left);
            }
            else if (position.x < mainCamera.transform.position.x - screenWidth / 2)
            {
                position.x = mainCamera.transform.position.x - screenWidth / 2;
                ApplyBounce(Vector2.right);
            }
        }
        else
        {
            if (position.x > mainCamera.transform.position.x + screenWidth / 2)
            {
                position.x = mainCamera.transform.position.x - screenWidth / 2;
            }
            else if (position.x < mainCamera.transform.position.x - screenWidth / 2)
            {
                position.x = mainCamera.transform.position.x + screenWidth / 2;
            }
        }

        transform.position = position;
    }

    void TeleportThroughWall(Collider2D wall)
    {
        // Проверяем таймер, чтобы не телепортировать слишком часто
        if (Time.time - lastWrapTime < wrapCooldown) return;

        Vector3 pos = transform.position;
        List<Collider2D> targetList = null;

        if (leftWalls.Contains(wall))
            targetList = rightWalls; // игрок касается левой стены
        else if (rightWalls.Contains(wall))
            targetList = leftWalls; // игрок касается правой стены

        if (targetList != null && targetList.Count > 0)
        {
            // Находим стену на той стороне, ближайшую по y
            Collider2D nearest = targetList[0];
            float minDist = Mathf.Abs(nearest.bounds.center.y - pos.y);

            foreach (var w in targetList)
            {
                float dist = Mathf.Abs(w.bounds.center.y - pos.y);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = w;
                }
            }

            float safeOffset = 0.2f; // безопасное смещение вне коллайдера

            // телепортируем игрока к противоположной стене с безопасным смещением
            if (leftWalls.Contains(wall))
                pos.x = nearest.bounds.min.x - safeOffset; // левый край правой стены
            else
                pos.x = nearest.bounds.max.x + safeOffset; // правый край левой стены

            transform.position = pos;

            // сохраняем время последнего телепорта
            lastWrapTime = Time.time;
        }
    }

    void ApplyBounce(Vector2 direction)
    {
        if (rb != null)
        {
            float speed = Mathf.Abs(rb.linearVelocity.x);
            float minBounce = 0.1f;
            float finalSpeed = Mathf.Max(speed, minBounce);
            rb.AddForce(direction * finalSpeed * bounceFactor, ForceMode2D.Impulse);
        }
    }
}
