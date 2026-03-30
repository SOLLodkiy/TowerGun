using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Collections;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance;

    [Header("Info Panel")]
    public GameObject infoPanel;               // сам объект плашки
    public RectTransform infoPanelRect;        // RectTransform плашки
    public RectTransform infoShownAnchor;      // якорь видимой позиции
    public RectTransform infoHiddenAnchor;     // якорь скрытой позиции
    public float infoSlideDuration = 0.30f;    // сек, unscaled
    public AnimationCurve infoEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Info UI")]
    public Text titleText;
    public Text descriptionText;
    public Text quoteText;
    public Text costText;
    public Image itemPreview;
    public Button actionButton;

    [Header("Close Button Motion")]
    public float closeBtnMoveDuration = 0.25f; // сек, unscaled
    public AnimationCurve closeBtnEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button shopButton; // ссылка на ту самую кнопку

    private ShopItemUI currentItem;
    private ShopUIManager shopManager;
    private bool infoOpen = false;

    private Coroutine panelMoveCo;
    private Coroutine closeBtnMoveCo;

    private PistolMove player;

    void Start()
    {
        StopAllCoroutines(); // Остановить все корутины на этом объекте
    }

    private void Awake()
    {
        Instance = this;
        shopManager = FindObjectOfType<ShopUIManager>();

        player = FindObjectOfType<PistolMove>();

        // Старт — плашка скрыта
        if (infoPanel != null)
        {
            infoPanel.SetActive(true); // активна, чтобы мы могли двигать RectTransform
            if (infoPanelRect != null && infoHiddenAnchor != null)
                infoPanelRect.position = infoHiddenAnchor.position;
            infoPanel.SetActive(false);
        }

        RestoreEquippedEffectsOnStart();
    }

    /* ===================== ОТКРЫТИЕ/ЗАКРЫТИЕ ПАНЕЛИ ===================== */

    public void OpenInfoPanel()
    {
        if (infoOpen) return;
        infoOpen = true;

        infoPanel.SetActive(true);
        // Двигаем плашку снизу вверх
        if (panelMoveCo != null) StopCoroutine(panelMoveCo);
        panelMoveCo = StartCoroutine(MoveUI(infoPanelRect, infoHiddenAnchor, infoShownAnchor, infoSlideDuration, infoEase));

        // Переназначаем кнопку закрытия и двигаем её к плашке
        SwitchCloseButton(CloseInfoPanel, shopManager.shopCloseAnchor, shopManager.infoCloseAnchor);
    }

    public void CloseInfoPanel()
    {
        if (!infoOpen) return;
        infoOpen = false;

        // Увозим плашку вниз
        if (panelMoveCo != null) StopCoroutine(panelMoveCo);
        panelMoveCo = StartCoroutine(CloseInfoRoutine());

        // Возвращаем кнопку на место и поведение "закрыть магазин"
        SwitchCloseButton(shopManager.CloseShop, shopManager.infoCloseAnchor, shopManager.shopCloseAnchor);
    }

    private IEnumerator CloseInfoRoutine()
    {
        yield return MoveUI(infoPanelRect, infoShownAnchor, infoHiddenAnchor, infoSlideDuration, infoEase);
        infoPanel.SetActive(false);
    }

    // Универсальная корутина движения UI (в мировых координатах), не зависит от Time.timeScale
    private IEnumerator MoveUI(RectTransform target, RectTransform from, RectTransform to, float duration, AnimationCurve curve)
    {
        if (target == null || from == null || to == null)
            yield break;

        Vector3 start = (from == null) ? target.position : from.position;
        Vector3 end = to.position;

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
        if (shopManager != null && shopManager.closeButton != null)
        {
            var btn = shopManager.closeButton;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => newAction());

            if (closeBtnMoveCo != null) StopCoroutine(closeBtnMoveCo);
            closeBtnMoveCo = StartCoroutine(MoveUI(
                btn.GetComponent<RectTransform>(),
                from,
                to,
                closeBtnMoveDuration,
                closeBtnEase
            ));
        }
    }


    /* ===================== ЭКРАНЫ ИНФОРМАЦИИ ===================== */

    public void ShowLockedInfo(ShopItemData data)
    {
        OpenInfoPanel();

        titleText.text = "Заблокировано!";
        quoteText.text = "";
        costText.text = "";
        itemPreview.sprite = data.lockedSprite;
        actionButton.gameObject.SetActive(false);

        // Подбираем текст в зависимости от типа задания
        switch (data.unlockType)
        {
            case UnlockType.KillEnemies:
                descriptionText.text = $"Убей {data.unlockRequirement} врагов";
                break;

            case UnlockType.ReachDistance:
                descriptionText.text = $"Доберись до {data.unlockRequirement} метров";
                break;

            case UnlockType.TotalCoinsCollected:
                descriptionText.text = $"Собери {data.unlockRequirement} монет всего";
                break;

            case UnlockType.TotalCoinsSpent:
                descriptionText.text = $"Потрать {data.unlockRequirement} монет всего";
                break;

            case UnlockType.Losses:
                descriptionText.text = $"Проиграй {data.unlockRequirement} раз";
                break;

            case UnlockType.TimePlayedActive:
                descriptionText.text = $"Проведи в игре {data.unlockRequirement} секунд";
                break;

            case UnlockType.ShotsFired:
                descriptionText.text = $"Сделай {data.unlockRequirement} выстрелов";
                break;

            case UnlockType.TotalDistance:
                descriptionText.text = $"Пройди всего {data.unlockRequirement} метров";
                break;

            case UnlockType.ItemsPurchased:
                descriptionText.text = $"Купи {data.unlockRequirement} предметов";
                break;

            default:
                descriptionText.text = "Задание неизвестно";
                break;
        }
    }

    public void ShowPurchaseInfo(ShopItemData data, ShopItemUI item)
    {
        OpenInfoPanel();
        currentItem = item;

        titleText.text = data.itemName;
        descriptionText.text = data.description;
        quoteText.text = data.quote;
        costText.text = $"{data.cost}";
        itemPreview.sprite = data.itemSprite;
        itemPreview.preserveAspect = true;
        // Авто-пропорции через RectTransform
        RectTransform rt = itemPreview.GetComponent<RectTransform>();
        float spriteRatio = (float)itemPreview.sprite.rect.width / itemPreview.sprite.rect.height;
        float currentWidth = rt.rect.width;
        float currentHeight = rt.rect.height;
        
        if (currentWidth / currentHeight > spriteRatio)
        {
            // ширина слишком большая
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentHeight * spriteRatio);
        }
        else
        {
            // высота слишком большая
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentWidth / spriteRatio);
        }

        actionButton.gameObject.SetActive(true);
        actionButton.GetComponentInChildren<Text>().text = "Купить";
        actionButton.onClick.RemoveAllListeners();
        actionButton.onClick.AddListener(() =>
        {
            GunMove player = FindObjectOfType<GunMove>();
            if (player != null && player.coinCount >= data.cost)
            {
                player.coinCount -= data.cost;
                PlayerPrefs.SetInt("Coins", player.coinCount);
                PlayerPrefs.SetInt("TotalCoinsSpent", PlayerPrefs.GetInt("TotalCoinsSpent", 0) + data.cost);
                player.UpdateCoinUI();
                item.Purchase();
                ShowEquipInfo(data, item, false);
                // Обновляем прогресс всех ShopItemUI
                foreach (var item in FindObjectsOfType<ShopItemUI>())
                {
                    item.UpdateProgressFromPrefs();
                    item.TryAutoUnlock();
                }
            }
            else
            {
                Debug.Log("Недостаточно монет!");
            }
        });
    }

    public void ShowEquipInfo(ShopItemData data, ShopItemUI item, bool equipped)
    {
        OpenInfoPanel();
        currentItem = item;

        titleText.text = data.itemName;
        descriptionText.text = data.description;
        quoteText.text = data.quote;
        costText.text = "Куплено";
        itemPreview.sprite = data.itemSprite;

        actionButton.gameObject.SetActive(true);
        actionButton.onClick.RemoveAllListeners();

        if (!equipped)
        {
            actionButton.GetComponentInChildren<Text>().text = "Надеть";
            actionButton.onClick.AddListener(() =>
            {
                item.Equip();
                ApplyEffect(data, true);
                ShowEquipInfo(data, item, true);
                EquipmentManager.Instance.EquipItem(item.Data);
            });
        }
        else
        {
            actionButton.GetComponentInChildren<Text>().text = "Снять";
            actionButton.onClick.AddListener(() =>
            {
                item.Unequip();
                ApplyEffect(data, false);
                ShowEquipInfo(data, item, false);
                EquipmentManager.Instance.UnequipItem(item.Data);
            });
        }
    }

    /* ===================== ПРИМЕНЕНИЕ ЭФФЕКТОВ ===================== */

    private void ApplyEffect(ShopItemData data, bool enable)
    {
        var type = System.Type.GetType(data.scriptName);
        if (type == null) { Debug.LogWarning("S"); return; }

        var target = FindObjectOfType(type);
        if (target == null) { Debug.LogWarning("O"); return; }

        var field = type.GetField(data.boolTargetName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(bool))
        {
            field.SetValue(target, enable);
            player.ApplyBabyMode();
        }
        else
        {
            Debug.LogWarning("B");
        }

        PlayerPrefs.SetInt($"Effect_{data.name}", enable ? 1 : 0);
    }

    private void RestoreEquippedEffectsOnStart()
    {
        foreach (var item in FindObjectsOfType<ShopItemUI>())
        {
            var data = item.Data;
            if (PlayerPrefs.GetInt($"Effect_{data.name}", 0) == 1)
            {
                ApplyEffect(data, true);
            }
        }
    }
}
