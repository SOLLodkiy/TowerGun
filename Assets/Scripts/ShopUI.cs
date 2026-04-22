using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Collections;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance;

    [Header("Info Panel")]
    public GameObject infoPanel;
    public RectTransform infoPanelRect;
    public RectTransform infoShownAnchor;
    public RectTransform infoHiddenAnchor;
    public float infoSlideDuration = 0.30f;
    public AnimationCurve infoEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Info UI")]
    public Text titleText;
    public Text descriptionText;
    public Text quoteText;
    public Text costText;
    public Image itemPreview;
    public Button actionButton;

    [Header("Close Button Motion")]
    public float closeBtnMoveDuration = 0.25f;
    public AnimationCurve closeBtnEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button shopButton;

    private ShopItemUI currentItem;
    private ShopUIManager shopManager;
    private bool infoOpen = false;

    private Coroutine panelMoveCo;
    private Coroutine closeBtnMoveCo;

    private PistolMove player;

    // Кэшируем RectTransform кнопки действия чтобы не вызывать GetComponent
    private RectTransform actionButtonRT;
    // Кэшируем Text кнопки действия
    private Text actionButtonText;

    void Start()
    {
        StopAllCoroutines();
    }

    private void Awake()
    {
        Instance = this;
        shopManager = FindFirstObjectByType<ShopUIManager>();
        player      = FindFirstObjectByType<PistolMove>();

        // Кэшируем при старте
        if (actionButton != null)
        {
            actionButtonRT   = actionButton.GetComponent<RectTransform>();
            actionButtonText = actionButton.GetComponentInChildren<Text>();
        }

        if (infoPanel != null)
        {
            infoPanel.SetActive(true);
            if (infoPanelRect != null && infoHiddenAnchor != null)
                infoPanelRect.position = infoHiddenAnchor.position;
            infoPanel.SetActive(false);
        }

        RestoreEquippedEffectsOnStart();
    }

    public void OpenInfoPanel()
    {
        if (infoOpen) return;
        infoOpen = true;

        infoPanel.SetActive(true);
        if (panelMoveCo != null) StopCoroutine(panelMoveCo);
        panelMoveCo = StartCoroutine(MoveUI(infoPanelRect, infoHiddenAnchor, infoShownAnchor, infoSlideDuration, infoEase));
        SwitchCloseButton(CloseInfoPanel, shopManager.shopCloseAnchor, shopManager.infoCloseAnchor);
    }

    public void CloseInfoPanel()
    {
        if (!infoOpen) return;
        infoOpen = false;

        if (panelMoveCo != null) StopCoroutine(panelMoveCo);
        panelMoveCo = StartCoroutine(CloseInfoRoutine());
        SwitchCloseButton(shopManager.CloseShop, shopManager.infoCloseAnchor, shopManager.shopCloseAnchor);
    }

    private IEnumerator CloseInfoRoutine()
    {
        yield return MoveUI(infoPanelRect, infoShownAnchor, infoHiddenAnchor, infoSlideDuration, infoEase);
        infoPanel.SetActive(false);
    }

    private IEnumerator MoveUI(RectTransform target, RectTransform from, RectTransform to,
                                float duration, AnimationCurve curve)
    {
        if (target == null || from == null || to == null) yield break;

        Vector3 start = from.position;
        Vector3 end   = to.position;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
            float e = curve != null ? curve.Evaluate(k) : k;
            target.position = Vector3.LerpUnclamped(start, end, e);
            yield return null;
        }
        target.position = end;
    }

    private void SwitchCloseButton(System.Action newAction, RectTransform from, RectTransform to)
    {
        if (shopManager == null || shopManager.closeButton == null) return;

        var btn = shopManager.closeButton;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => newAction());

        if (closeBtnMoveCo != null) StopCoroutine(closeBtnMoveCo);
        closeBtnMoveCo = StartCoroutine(MoveUI(
            btn.GetComponent<RectTransform>(), from, to, closeBtnMoveDuration, closeBtnEase));
    }

    public void ShowLockedInfo(ShopItemData data)
    {
        OpenInfoPanel();

        titleText.text   = "Заблокировано!";
        quoteText.text   = "";
        costText.text    = "";
        itemPreview.sprite = data.lockedSprite;
        actionButton.gameObject.SetActive(false);

        switch (data.unlockType)
        {
            case UnlockType.KillEnemies:         descriptionText.text = $"Убей {data.unlockRequirement} врагов"; break;
            case UnlockType.ReachDistance:       descriptionText.text = $"Доберись до {data.unlockRequirement} метров"; break;
            case UnlockType.TotalCoinsCollected: descriptionText.text = $"Собери {data.unlockRequirement} монет всего"; break;
            case UnlockType.TotalCoinsSpent:     descriptionText.text = $"Потрать {data.unlockRequirement} монет всего"; break;
            case UnlockType.Losses:              descriptionText.text = $"Проиграй {data.unlockRequirement} раз"; break;
            case UnlockType.TimePlayedActive:    descriptionText.text = $"Проведи в игре {data.unlockRequirement} секунд"; break;
            case UnlockType.ShotsFired:          descriptionText.text = $"Сделай {data.unlockRequirement} выстрелов"; break;
            case UnlockType.TotalDistance:       descriptionText.text = $"Пройди всего {data.unlockRequirement} метров"; break;
            case UnlockType.ItemsPurchased:      descriptionText.text = $"Купи {data.unlockRequirement} предметов"; break;
            default:                             descriptionText.text = "Задание неизвестно"; break;
        }
    }

    public void ShowPurchaseInfo(ShopItemData data, ShopItemUI item)
    {
        OpenInfoPanel();
        currentItem = item;

        titleText.text       = data.itemName;
        descriptionText.text = data.description;
        quoteText.text       = data.quote;
        costText.text        = $"{data.cost}";

        SetItemPreview(data.itemSprite);

        actionButton.gameObject.SetActive(true);
        // Используем кэшированный Text вместо GetComponentInChildren каждый раз
        actionButtonText.text = "Купить";
        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(() => OnBuyClicked(data, item));
    }

    private void OnBuyClicked(ShopItemData data, ShopItemUI item)
    {
        // Используем кэшированный player вместо FindFirstObjectByType в лямбде
        if (player == null) player = FindFirstObjectByType<PistolMove>();
        GunMove gunMove = player?.GetComponent<GunMove>() ?? FindFirstObjectByType<GunMove>();

        if (gunMove == null || gunMove.coinCount < data.cost)
        {
            Debug.Log("Недостаточно монет!");
            return;
        }

        gunMove.coinCount -= data.cost;
        PlayerPrefs.SetInt("Coins", gunMove.coinCount);
        PlayerPrefs.SetInt("TotalCoinsSpent", PlayerPrefs.GetInt("TotalCoinsSpent", 0) + data.cost);
        PlayerPrefs.SetInt("TotalPurchases", PlayerPrefs.GetInt("TotalPurchases", 0) + 1);
        gunMove.UpdateCoinUI();

        item.Purchase();
        ShowEquipInfo(data, item, false);

        // Обновляем прогресс предметов
        var shopItems = FindObjectsByType<ShopItemUI>(FindObjectsSortMode.None);
        foreach (var si in shopItems)
        {
            si.UpdateProgressFromPrefs();
            si.TryAutoUnlock();
        }
    }

    public void ShowEquipInfo(ShopItemData data, ShopItemUI item, bool equipped)
    {
        OpenInfoPanel();
        currentItem = item;

        titleText.text       = data.itemName;
        descriptionText.text = data.description;
        quoteText.text       = data.quote;
        costText.text        = "Куплено";

        SetItemPreview(data.itemSprite);

        actionButton.gameObject.SetActive(true);
        actionButton.onClick.RemoveAllListeners();

        if (!equipped)
        {
            actionButtonText.text = "Надеть";
            actionButton.onClick.AddListener(() =>
            {
                item.Equip();
                ApplyEffect(data, true);
                // EquipItem вызывает OnEquipmentChanged → EquippedItemsUI.RefreshIcons
                EquipmentManager.Instance.EquipItem(item.Data);
                ShowEquipInfo(data, item, true);
            });
        }
        else
        {
            actionButtonText.text = "Снять";
            actionButton.onClick.AddListener(() =>
            {
                item.Unequip();
                ApplyEffect(data, false);
                // UnequipItem вызывает OnEquipmentChanged → EquippedItemsUI.RefreshIcons
                EquipmentManager.Instance.UnequipItem(item.Data);
                ShowEquipInfo(data, item, false);
            });
        }
    }

    // Устанавливает спрайт превью с правильными пропорциями
    private void SetItemPreview(Sprite sprite)
    {
        itemPreview.sprite = sprite;
        itemPreview.preserveAspect = true;

        RectTransform rt = itemPreview.GetComponent<RectTransform>();
        float spriteRatio   = (float)sprite.rect.width / sprite.rect.height;
        float currentWidth  = rt.rect.width;
        float currentHeight = rt.rect.height;

        if (currentWidth / currentHeight > spriteRatio)
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentHeight * spriteRatio);
        else
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentWidth / spriteRatio);
    }

    private void ApplyEffect(ShopItemData data, bool enable)
    {
        var type = System.Type.GetType(data.scriptName);
        if (type == null) { Debug.LogWarning("Скрипт не найден: " + data.scriptName); return; }

        var target = FindFirstObjectByType(type);
        if (target == null) { Debug.LogWarning("Объект не найден для: " + data.scriptName); return; }

        var field = type.GetField(data.boolTargetName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (field != null && field.FieldType == typeof(bool))
        {
            field.SetValue(target, enable);
            // Применяем BabyMode если нужно
            if (player != null) player.ApplyBabyMode();
        }
        else
        {
            Debug.LogWarning("Поле не найдено: " + data.boolTargetName);
        }

        PlayerPrefs.SetInt($"Effect_{data.name}", enable ? 1 : 0);
    }

    private void RestoreEquippedEffectsOnStart()
    {
        var shopItems = FindObjectsByType<ShopItemUI>(FindObjectsSortMode.None);
        foreach (var item in shopItems)
        {
            var data = item.Data;
            if (PlayerPrefs.GetInt($"Effect_{data.name}", 0) == 1)
                ApplyEffect(data, true);
        }
    }
}