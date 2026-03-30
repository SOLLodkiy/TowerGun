using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LevelPart
{
    public Transform partTransform; // Ссылка на трансформ части уровня
    public float minDistanceToSpawn; // Минимальная дистанция в метрах для появления
    public float spawnChance; // Шанс появления (0-100%)

    public List<Transform> coinPlacementPrefabs; // Список префабов для расположения монеток на уровне

    // Новый флаг, указывающий, может ли этот элемент быть отзеркален
    public bool canBeMirrored = true;
}

public class LevelGenerator : MonoBehaviour
{
    private const float PLAYER_DISTANCE_SPAWN_LEVEL_PART = 30f;
    private const float PLAYER_DISTANCE_REMOVE_LEVEL_PART = 50f;

    [SerializeField] private Transform levelPart_Start;
    [SerializeField] private List<LevelPart> levelPartList; // Массив, включающий метры и шанс появления
    [SerializeField] private GameObject player;

    private Vector3 lastEndPosition;
    private List<Transform> spawnedLevelParts = new List<Transform>(); // Список для хранения всех появившихся частей уровня
    private LevelPart lastSpawnedPart; // Хранит последнюю заспавненную часть уровня

    private void Awake()
    {
        lastEndPosition = levelPart_Start.Find("EndPosition").position;

        int startingSpawnLevelParts = 5;
        for (int i = 0; i < startingSpawnLevelParts; i++)
        {
            SpawnLevelPart();
        }
    }

    private void Update()
    {
        if (Vector3.Distance(player.transform.position, lastEndPosition) < PLAYER_DISTANCE_SPAWN_LEVEL_PART)
        {
            // Spawn another level part
            SpawnLevelPart();
        }

        // Удаление старых частей уровня, находящихся слишком далеко ниже игрока
        RemoveOldLevelParts();
    }

    private void SpawnLevelPart()
    {
        LevelPart chosenLevelPart = GetRandomLevelPart();

        if (chosenLevelPart != null)
        {
            // Определяем, нужно ли зеркалить уровень
            bool isMirrored = Random.value > 0.5f && chosenLevelPart.canBeMirrored; // 50% шанс на зеркалирование, если можно

            Transform levelPartToSpawn = isMirrored ? chosenLevelPart.partTransform : chosenLevelPart.partTransform;

            Transform lastLevelPartTransform = SpawnLevelPart(levelPartToSpawn, lastEndPosition, isMirrored);
            lastEndPosition = lastLevelPartTransform.Find("EndPosition").position;
            spawnedLevelParts.Add(lastLevelPartTransform); // Добавляем в список появившуюся часть уровня

            // Добавляем монетки на уровне, если для этого уровня есть префабы монеток
            if (chosenLevelPart.coinPlacementPrefabs.Count > 0)
            {
                SpawnCoinPlacement(chosenLevelPart, lastLevelPartTransform, isMirrored);
            }

            // Сохраняем последнюю заспавненную часть
            lastSpawnedPart = chosenLevelPart;
    }   
}

    private LevelPart GetRandomLevelPart()
    {
        // Определяем текущую пройденную дистанцию
        float currentDistance = player.transform.position.y;

        // Создаем список доступных частей уровня, которые могут появиться на текущей дистанции
        List<LevelPart> availableParts = new List<LevelPart>();
        foreach (LevelPart part in levelPartList)
        {
            if (currentDistance >= part.minDistanceToSpawn && part != lastSpawnedPart) // Проверяем, чтобы не заспавнить ту же часть
            {
                availableParts.Add(part);
            }
        }

        // Если нет доступных частей уровня, возвращаем null
        if (availableParts.Count == 0) return null;

        // Выбираем случайную часть уровня с учетом шанса появления
        float totalChance = 0f;
        foreach (LevelPart part in availableParts)
        {
            totalChance += part.spawnChance;
        }

        float randomValue = Random.Range(0, totalChance);
        float cumulativeChance = 0f;
        foreach (LevelPart part in availableParts)
        {
            cumulativeChance += part.spawnChance;
            if (randomValue < cumulativeChance)
            {
                return part;
            }
        }

        return availableParts[availableParts.Count - 1]; // Если по каким-то причинам не выбралась, возвращаем последнюю
    }

    private Transform SpawnLevelPart(Transform levelPart, Vector3 spawnPosition, bool isMirrored)
    {
        Quaternion rotation = isMirrored ? Quaternion.Euler(0, 180, 0) : Quaternion.identity; // Зеркалирование
        Transform levelPartTransform = Instantiate(levelPart, spawnPosition, rotation);
        return levelPartTransform;
    }

    private void SpawnCoinPlacement(LevelPart levelPart, Transform levelPartTransform, bool isMirrored)
    {
        // Выбираем случайный префаб для расположения монеток
        int randomIndex = Random.Range(0, levelPart.coinPlacementPrefabs.Count);
        Transform chosenCoinPlacement = levelPart.coinPlacementPrefabs[randomIndex];

        // Инстанцируем префаб монеток с учетом зеркалирования
        Quaternion rotation = isMirrored ? Quaternion.Euler(0, 180, 0) : Quaternion.identity;
        Instantiate(chosenCoinPlacement, levelPartTransform.position, rotation, levelPartTransform);
    }

    private void RemoveOldLevelParts()
    {
        // Итерируем по списку появившихся частей уровня и проверяем расстояние от игрока
        for (int i = spawnedLevelParts.Count - 1; i >= 0; i--)
        {
            Transform levelPart = spawnedLevelParts[i];
            if (player.transform.position.y - levelPart.position.y > PLAYER_DISTANCE_REMOVE_LEVEL_PART)
            {
                Destroy(levelPart.gameObject); // Удаляем часть уровня
                spawnedLevelParts.RemoveAt(i); // Удаляем ее из списка
            }
        }
    }
}
