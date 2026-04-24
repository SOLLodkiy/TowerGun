using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DangerIndicator : MonoBehaviour
{
    [Header("Accessory Flags")]
    public bool ricochetEnabled = false;
    public bool slowSawEnabled = false;
    public bool selfshotDisabled = false;

    public GameObject warningArcPrefab;  // Префаб полукруга
    public float detectionRadius = 5f;   // Радиус обнаружения блоков
    public float minScale = 0.5f;        // Минимальный размер полукруга
    public float maxScale = 1.5f;        // Максимальный размер полукруга
    public bool enableWarnings = true;   // Флаг включения/выключения полукругов

    private Dictionary<Vector2, GameObject> warningArcs = new Dictionary<Vector2, GameObject>();

    void Update()
    {
        if (!enableWarnings)
        {
            ClearWarnings();
            return;
        }

        UpdateWarnings();
    }

    void UpdateWarnings()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        List<Vector2> detectedDirections = new List<Vector2>();

        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("DeadlyBlock"))
            {
                Vector2 worldDirection = (hit.transform.position - transform.position).normalized;
                detectedDirections.Add(worldDirection);
            }
        }

        List<Vector2> uniqueDirections = GroupDirections(detectedDirections);

        foreach (var key in new List<Vector2>(warningArcs.Keys))
        {
            if (!uniqueDirections.Contains(key))
            {
                Destroy(warningArcs[key]);
                warningArcs.Remove(key);
            }
        }

        foreach (Vector2 worldDir in uniqueDirections)
        {
            float closestDistance = GetClosestBlockDistance(worldDir, hits);
            float scale = Mathf.Lerp(maxScale, minScale, closestDistance / detectionRadius);
            float alpha = Mathf.Lerp(1f, 0.2f, closestDistance / detectionRadius);

            if (!warningArcs.ContainsKey(worldDir))
            {
                GameObject newArc = Instantiate(warningArcPrefab);
                warningArcs[worldDir] = newArc;
            }

            GameObject arc = warningArcs[worldDir];

            Vector2 worldPosition = (Vector2)transform.position + worldDir * 1f;
            arc.transform.position = worldPosition;

            float angle = Mathf.Atan2(worldDir.y, worldDir.x) * Mathf.Rad2Deg - 90;
            arc.transform.rotation = Quaternion.Euler(0, 0, angle);

            arc.transform.localScale = Vector3.one * scale;

            SpriteRenderer sr = arc.GetComponent<SpriteRenderer>();
            Color color = sr.color;
            color.a = alpha;
            sr.color = color;
        }
    }

    void ClearWarnings()
    {
        foreach (var arc in warningArcs.Values)
        {
            Destroy(arc);
        }
        warningArcs.Clear();
    }

    float GetClosestBlockDistance(Vector2 worldDirection, Collider2D[] blocks)
    {
        float minDistance = detectionRadius;
        foreach (Collider2D block in blocks)
        {
            if (block.CompareTag("DeadlyBlock"))
            {
                Vector2 dirToBlock = (block.transform.position - transform.position).normalized;
                if (Vector2.Dot(worldDirection, dirToBlock) > 0.9f)
                {
                    float dist = Vector2.Distance(transform.position, block.transform.position);
                    minDistance = Mathf.Min(minDistance, dist);
                }
            }
        }
        return minDistance;
    }

    List<Vector2> GroupDirections(List<Vector2> directions)
    {
        List<Vector2> uniqueDirections = new List<Vector2>();

        foreach (Vector2 dir in directions)
        {
            bool merged = false;
            for (int i = 0; i < uniqueDirections.Count; i++)
            {
                if (Vector2.Dot(uniqueDirections[i], dir) > 0.8f)
                {
                    uniqueDirections[i] = (uniqueDirections[i] + dir).normalized;
                    merged = true;
                    break;
                }
            }
            if (!merged)
                uniqueDirections.Add(dir);
        }

        return uniqueDirections;
    }
}