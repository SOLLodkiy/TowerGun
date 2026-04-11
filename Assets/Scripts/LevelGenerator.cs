using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelPart
{
    public Transform partTransform;
    public float minDistanceToSpawn;
    public float spawnChance;
    public List<Transform> coinPlacementPrefabs;
    public bool canBeMirrored = true;
}

public class LevelGenerator : MonoBehaviour
{
    private const float PLAYER_DISTANCE_SPAWN_LEVEL_PART  = 30f;
    private const float PLAYER_DISTANCE_REMOVE_LEVEL_PART = 50f;

    [SerializeField] private Transform levelPart_Start;
    [SerializeField] private List<LevelPart> levelPartList;
    [SerializeField] private GameObject player;

    private Vector3 lastEndPosition;
    private List<Transform> spawnedLevelParts = new List<Transform>();
    private LevelPart lastSpawnedPart;

    // ОПТИМИЗАЦИЯ: кэшируем transform игрока — обращение через player.transform
    // каждый Update/кадр имеет накладные расходы
    private Transform playerTransform;

    // ОПТИМИЗАЦИЯ: переиспользуемый список для GetRandomLevelPart вместо
    // new List<LevelPart>() каждый вызов — убирает GC alloc при каждом спавне
    private List<LevelPart> availablePartsBuffer = new List<LevelPart>();

    private void Awake()
    {
        playerTransform = player.transform;
        lastEndPosition = levelPart_Start.Find("EndPosition").position;

        for (int i = 0; i < 5; i++)
            SpawnLevelPart();
    }

    private void Update()
    {
        if (Vector3.Distance(playerTransform.position, lastEndPosition) < PLAYER_DISTANCE_SPAWN_LEVEL_PART)
            SpawnLevelPart();

        RemoveOldLevelParts();
    }

    private void SpawnLevelPart()
    {
        LevelPart chosen = GetRandomLevelPart();
        if (chosen == null) return;

        bool isMirrored = Random.value > 0.5f && chosen.canBeMirrored;

        Quaternion rotation = isMirrored ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
        Transform spawned = Instantiate(chosen.partTransform, lastEndPosition, rotation);
        lastEndPosition = spawned.Find("EndPosition").position;
        spawnedLevelParts.Add(spawned);

        if (chosen.coinPlacementPrefabs.Count > 0)
            SpawnCoinPlacement(chosen, spawned, isMirrored);

        lastSpawnedPart = chosen;
    }

    private LevelPart GetRandomLevelPart()
    {
        float currentDistance = playerTransform.position.y;

        // ОПТИМИЗАЦИЯ: очищаем переиспользуемый буфер вместо создания нового списка
        availablePartsBuffer.Clear();
        foreach (LevelPart part in levelPartList)
        {
            if (currentDistance >= part.minDistanceToSpawn && part != lastSpawnedPart)
                availablePartsBuffer.Add(part);
        }

        if (availablePartsBuffer.Count == 0) return null;

        float totalChance = 0f;
        foreach (LevelPart part in availablePartsBuffer)
            totalChance += part.spawnChance;

        float randomValue = Random.Range(0f, totalChance);
        float cumulative = 0f;
        foreach (LevelPart part in availablePartsBuffer)
        {
            cumulative += part.spawnChance;
            if (randomValue < cumulative) return part;
        }

        return availablePartsBuffer[availablePartsBuffer.Count - 1];
    }

    private void SpawnCoinPlacement(LevelPart levelPart, Transform parent, bool isMirrored)
    {
        int idx = Random.Range(0, levelPart.coinPlacementPrefabs.Count);
        Quaternion rotation = isMirrored ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
        Instantiate(levelPart.coinPlacementPrefabs[idx], parent.position, rotation, parent);
    }

    private void RemoveOldLevelParts()
    {
        float playerY = playerTransform.position.y;
        for (int i = spawnedLevelParts.Count - 1; i >= 0; i--)
        {
            Transform part = spawnedLevelParts[i];
            if (part == null)
            {
                spawnedLevelParts.RemoveAt(i);
                continue;
            }
            if (playerY - part.position.y > PLAYER_DISTANCE_REMOVE_LEVEL_PART)
            {
                Destroy(part.gameObject);
                spawnedLevelParts.RemoveAt(i);
            }
        }
    }
}