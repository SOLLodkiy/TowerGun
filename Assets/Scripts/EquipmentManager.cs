using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Linq;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance;

    private HashSet<ShopItemData> equippedItems = new HashSet<ShopItemData>();

    // ОПТИМИЗАЦИЯ: кэшируем все ShopItemData при старте один раз
    // вместо Resources.FindObjectsOfTypeAll при каждом рестарте
    private ShopItemData[] allShopItems;

    public GunMove playerScript;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // ОПТИМИЗАЦИЯ: загружаем один раз, кэшируем
        allShopItems = Resources.FindObjectsOfTypeAll<ShopItemData>();

        RestoreEquippedEffectsOnStart();
        SaveEquippedItems();
    }

    void Start()
    {
        StopAllCoroutines();
    }

    public void EquipItem(ShopItemData item)
    {
        equippedItems.Add(item);
        ApplyEffects(item);
        SaveEquippedItems();
    }

    public void UnequipItem(ShopItemData item)
    {
        if (equippedItems.Contains(item))
        {
            equippedItems.Remove(item);
            RemoveEffects(item);
            SaveEquippedItems();
        }
    }

    public IEnumerable<ShopItemData> GetEquippedItems() => equippedItems;

    private void ApplyEffects(ShopItemData item)
    {
        if (string.IsNullOrEmpty(item.scriptName) || string.IsNullOrEmpty(item.boolTargetName))
            return;

        var type = System.Type.GetType(item.scriptName);
        if (type == null) return;

        var target = FindFirstObjectByType(type);
        if (target == null) return;

        var field = type.GetField(item.boolTargetName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (field != null && field.FieldType == typeof(bool))
            field.SetValue(target, true);
    }

    private void RemoveEffects(ShopItemData item)
    {
        if (string.IsNullOrEmpty(item.scriptName) || string.IsNullOrEmpty(item.boolTargetName))
            return;

        var type = System.Type.GetType(item.scriptName);
        if (type == null) return;

        var target = FindFirstObjectByType(type);
        if (target == null) return;

        var field = type.GetField(item.boolTargetName,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);

        if (field != null && field.FieldType == typeof(bool))
            field.SetValue(target, false);
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

        // ОПТИМИЗАЦИЯ: используем кэшированный массив вместо повторного поиска
        string[] names = saved.Split(';');
        foreach (var itemName in names)
        {
            if (string.IsNullOrEmpty(itemName)) continue;

            ShopItemData item = System.Array.Find(allShopItems, x => x.name == itemName);
            if (item != null)
            {
                equippedItems.Add(item);
                ApplyEffects(item);
            }
        }
    }

    public void RestartScene()
    {
        StartCoroutine(RestartSceneCoroutine());
    }

    private IEnumerator RestartSceneCoroutine()
    {
        // ОПТИМИЗАЦИЯ: убран ручной Destroy всех объектов — LoadSceneAsync сам
        // уничтожает сцену. Ручной цикл Destroy создавал тысячи pending-вызовов
        // за один кадр прямо перед загрузкой и был причиной спайков GC.

        if (playerScript == null)
            playerScript = FindFirstObjectByType<GunMove>();

        if (playerScript != null)
            playerScript.ResetSession();
        else
            Debug.LogError("GunMove не найден при перезапуске");

        Time.timeScale = 1f;

        // Один кадр паузы чтобы timeScale успел сброситься
        yield return null;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(
            SceneManager.GetActiveScene().buildIndex);
        asyncLoad.allowSceneActivation = true;

        yield return asyncLoad;
    }
}