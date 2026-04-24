using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Linq;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance;

    private HashSet<ShopItemData> equippedItems = new HashSet<ShopItemData>();
    private ShopItemData[] allShopItems;

    public GunMove playerScript;

    public event System.Action OnEquipmentChanged;

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

        // Только загружаем список предметов и equippedItems из сохранений —
        // без ApplyEffects, потому что другие объекты ещё не инициализированы
        allShopItems = Resources.FindObjectsOfTypeAll<ShopItemData>();
        LoadEquippedItems();
    }

    void Start()
    {
        // Все объекты сцены уже прошли Awake и Start — теперь безопасно применять эффекты
        ApplyAllEquippedEffects();
    }

    // Вызывается после каждой загрузки сцены чтобы переприменить эффекты
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // После загрузки сцены ждём один кадр — объекты должны пройти Awake
        StartCoroutine(ApplyEffectsNextFrame());
    }

    private IEnumerator ApplyEffectsNextFrame()
    {
        // Ждём конца кадра — к этому моменту все Awake и Start отработали
        yield return new WaitForEndOfFrame();
        ApplyAllEquippedEffects();
    }

    private void LoadEquippedItems()
    {
        equippedItems.Clear();
        string saved = PlayerPrefs.GetString("EquippedItems", "");
        if (string.IsNullOrEmpty(saved)) return;

        foreach (var itemName in saved.Split(';'))
        {
            if (string.IsNullOrEmpty(itemName)) continue;
            ShopItemData item = System.Array.Find(allShopItems, x => x.name == itemName);
            if (item != null)
                equippedItems.Add(item);
        }
    }

    private void ApplyAllEquippedEffects()
    {
        foreach (var item in equippedItems)
            ApplyEffects(item);
    }

    public void EquipItem(ShopItemData item)
    {
        equippedItems.Add(item);
        ApplyEffects(item);
        SaveEquippedItems();
        OnEquipmentChanged?.Invoke();
    }

    public void UnequipItem(ShopItemData item)
    {
        if (!equippedItems.Contains(item)) return;
        equippedItems.Remove(item);
        RemoveEffects(item);
        SaveEquippedItems();
        OnEquipmentChanged?.Invoke();
    }

    public IEnumerable<ShopItemData> GetEquippedItems() => equippedItems;

    private void ApplyEffects(ShopItemData item)
    {
        if (string.IsNullOrEmpty(item.scriptName) || string.IsNullOrEmpty(item.boolTargetName)) return;
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
        if (string.IsNullOrEmpty(item.scriptName) || string.IsNullOrEmpty(item.boolTargetName)) return;
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
        PlayerPrefs.Save();
    }

    public void RestartScene()
    {
        StartCoroutine(RestartSceneCoroutine());
    }

    private IEnumerator RestartSceneCoroutine()
    {
        if (playerScript == null)
            playerScript = FindFirstObjectByType<GunMove>();

        if (playerScript != null)
            playerScript.ResetSession();
        else
            Debug.LogError("GunMove не найден при перезапуске");

        Time.timeScale = 1f;
        yield return null;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(
            SceneManager.GetActiveScene().buildIndex);
        asyncLoad.allowSceneActivation = true;
        yield return asyncLoad;
    }
}