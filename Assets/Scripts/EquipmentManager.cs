using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance;

    private HashSet<ShopItemData> equippedItems = new HashSet<ShopItemData>();

    public GunMove playerScript;

    void Start()
    {
        StopAllCoroutines(); // Остановить все корутины на этом объекте
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject);

        RestoreEquippedEffectsOnStart(); // сразу при старте сцены
        SaveEquippedItems();
    }

    // Добавить предмет
    public void EquipItem(ShopItemData item)
    {
        equippedItems.Add(item);
        ApplyEffects(item);
        SaveEquippedItems();
    }

    // Снять предмет
    public void UnequipItem(ShopItemData item)
    {
        if (equippedItems.Contains(item))
        {
            equippedItems.Remove(item);
            RemoveEffects(item);
            SaveEquippedItems();
        }
    }

    // Вернуть список всех экипированных предметов
    public IEnumerable<ShopItemData> GetEquippedItems() => equippedItems;

    private void ApplyEffects(ShopItemData item)
    {
        // Применяем эффект по скрипту и полю, если задано
        if (!string.IsNullOrEmpty(item.scriptName) && !string.IsNullOrEmpty(item.boolTargetName))
        {
            var type = System.Type.GetType(item.scriptName);
            if (type != null)
            {
                var target = FindObjectOfType(type);
                if (target != null)
                {
                    var field = type.GetField(item.boolTargetName, 
                        System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.NonPublic);
                    if (field != null && field.FieldType == typeof(bool))
                    {
                        field.SetValue(target, true);
                    }
                }
            }
        }

        Debug.Log($"Эффект применён: {item.itemName}");
    }

    private void RemoveEffects(ShopItemData item)
    {
        if (!string.IsNullOrEmpty(item.scriptName) && !string.IsNullOrEmpty(item.boolTargetName))
        {
            var type = System.Type.GetType(item.scriptName);
            if (type != null)
            {
                var target = FindObjectOfType(type);
                if (target != null)
                {
                    var field = type.GetField(item.boolTargetName, 
                        System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.NonPublic);
                    if (field != null && field.FieldType == typeof(bool))
                    {
                        field.SetValue(target, false);
                    }
                }
            }
        }

        Debug.Log($"Эффект снят: {item.itemName}");
    }

    private void SaveEquippedItems()
    {
        string saveStr = string.Join(";", equippedItems.Select(i => i.name));
        PlayerPrefs.SetString("EquippedItems", saveStr);
    }

    private void RestoreEquippedEffectsOnStart()
    {
        string saved = PlayerPrefs.GetString("EquippedItems", "");
        if (string.IsNullOrEmpty(saved)) return;

        foreach (var itemName in saved.Split(';'))
        {
            if (string.IsNullOrEmpty(itemName)) continue;

            // Находим предмет через все ShopItemData на проекте
            ShopItemData item = Resources.FindObjectsOfTypeAll<ShopItemData>()
                .FirstOrDefault(x => x.name == itemName);
            
            if (item != null)
            {
                equippedItems.Add(item);
                ApplyEffects(item);
            }
        }
    }


    /// <summary>
    /// Чистый перезапуск сцены через 0.1 секунды
    /// </summary>
    public void RestartScene()
    {
        StartCoroutine(RestartSceneCoroutine());
    }

    private IEnumerator RestartSceneCoroutine()
    {
        if (playerScript == null)
        {
            playerScript = FindObjectOfType<GunMove>();
        }

        if (playerScript != null)
        {
            playerScript.ResetSession();
        }
        else
        {
            Debug.LogError("GunMove не найден при перезапуске сцены");
}

        // Сбрасываем статические данные, если есть (например, счетчики, рекорды за сессию)

        // Деактивируем все объекты с сцене кроме EquipmentManager и самого скрипта
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj != this.gameObject && obj.name != "EquipmentManager")
            {
                Destroy(obj);
            }
        }

        // Ждем один кадр, чтобы Unity обработал удаление объектов
        yield return null;

        // Сбрасываем Time.timeScale
        Time.timeScale = 1f;

        // Загрузка сцены с асинхронным методом, чтобы освободить память
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        asyncLoad.allowSceneActivation = true;

        yield return asyncLoad;
    }
}
