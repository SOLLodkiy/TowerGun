using UnityEngine;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image slotImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private Slider progressBar;

    [Header("Data")]
    [SerializeField] private ShopItemData itemData; // ← это поле ты будешь заполнять в инспекторе

    private enum ItemState { Locked, Unlocked, Purchased, Equipped }
    private ItemState state;

    private string PrefKey => "ShopItem_" + itemData.name;

    private void Awake()
    {
        LoadState();
        RefreshUI();
        UpdateProgressFromPrefs();   // чтобы прогрессбар сразу отобразился
        TryAutoUnlock();             // если условие уже выполнено — разблокировать
    }

    public void OnClick()
    {
        switch (state)
        {
            case ItemState.Locked:
                ShopUI.Instance.ShowLockedInfo(itemData);
                break;
            case ItemState.Unlocked:
                ShopUI.Instance.ShowPurchaseInfo(itemData, this);
                break;
            case ItemState.Purchased:
                ShopUI.Instance.ShowEquipInfo(itemData, this, false);
                break;
            case ItemState.Equipped:
                ShopUI.Instance.ShowEquipInfo(itemData, this, true);
                break;
        }
    }

    public void Purchase()
    {
        state = ItemState.Purchased;
        SaveState(); RefreshUI();
    }

    public void Equip()
    {
        state = ItemState.Equipped;
        SaveState(); RefreshUI();
    }

    public void Unequip()
    {
        state = ItemState.Purchased;
        SaveState(); RefreshUI();
    }

    public void TryAutoUnlock()
    {
        if (state != ItemState.Locked) return;

        int progress = GetProgressValue();
        if (progress >= itemData.unlockRequirement)
        {
            state = ItemState.Unlocked;
            SaveState(); RefreshUI();
        }
    }

    public void UpdateProgressFromPrefs()
    {
        int progress = GetProgressValue();
    
        // Список типов, для которых есть прогрессбар
        bool hasProgressBar = itemData.unlockType == UnlockType.KillEnemies
                        || itemData.unlockType == UnlockType.ReachDistance
                        || itemData.unlockType == UnlockType.TotalCoinsCollected
                        || itemData.unlockType == UnlockType.TotalCoinsSpent
                        || itemData.unlockType == UnlockType.Losses
                        || itemData.unlockType == UnlockType.TimePlayedActive
                        || itemData.unlockType == UnlockType.ShotsFired
                        || itemData.unlockType == UnlockType.TotalDistance
                        || itemData.unlockType == UnlockType.ItemsPurchased;

        if (hasProgressBar)
        {
            // Если прогресс >= требуемого, скрываем прогрессбар
            if (progress >= itemData.unlockRequirement)
            {
                progressBar.gameObject.SetActive(false);
            }
            else
            {
                progressBar.gameObject.SetActive(true);
                progressBar.value = Mathf.Clamp01((float)progress / itemData.unlockRequirement);
            }
        }
        else
        {
            progressBar.gameObject.SetActive(false);
        }
    }

    private int GetProgressValue()
    {
        switch (itemData.unlockType)
        {
            case UnlockType.KillEnemies:
                return PlayerPrefs.GetInt("TotalKills", 0);

            case UnlockType.ReachDistance:
                return PlayerPrefs.GetInt("BestDistance", 0);

            case UnlockType.TotalCoinsCollected:
                return PlayerPrefs.GetInt("TotalCoinsCollected", 0);

            case UnlockType.TotalCoinsSpent:
                return PlayerPrefs.GetInt("TotalCoinsSpent", 0);

            case UnlockType.Losses:
                return PlayerPrefs.GetInt("TotalLosses", 0);

            case UnlockType.TimePlayedActive:
                // секундами (требование тоже в секундах)
                return Mathf.FloorToInt(PlayerPrefs.GetFloat("TotalActiveTime", 0f));

            case UnlockType.ShotsFired:
                return PlayerPrefs.GetInt("TotalShots", 0);

            case UnlockType.TotalDistance:
                return Mathf.FloorToInt(PlayerPrefs.GetFloat("TotalDistance", 0f));

            case UnlockType.ItemsPurchased:
                return PlayerPrefs.GetInt("TotalPurchases", 0);

            default:
                return 0;
        }
    }

    private void RefreshUI()
    {
        // Иконка видна только, когда НЕ Locked
        iconImage.enabled = (state != ItemState.Locked);
        if (iconImage.enabled) 
        {
            iconImage.sprite = itemData.itemSprite;
            iconImage.preserveAspect = true;
            // Авто-пропорции через RectTransform
            RectTransform rt = iconImage.GetComponent<RectTransform>();
            float spriteRatio = (float)iconImage.sprite.rect.width / iconImage.sprite.rect.height;
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
        }

        switch (state)
        {
            case ItemState.Locked:      slotImage.sprite = itemData.lockedSprite;      break;
            case ItemState.Unlocked:    slotImage.sprite = itemData.unlockedSprite;    break;
            case ItemState.Purchased:   slotImage.sprite = itemData.purchasedSprite;   break;
            case ItemState.Equipped:    slotImage.sprite = itemData.equippedSprite;    break;
        }
    }

    private void SaveState() => PlayerPrefs.SetInt(PrefKey, (int)state);
    private void LoadState() => state = (ItemState)PlayerPrefs.GetInt(PrefKey, 0);

    // Чтобы ShopUI мог узнать текущее состояние при показе
    public bool IsEquipped => (int)state == 3;
    public ShopItemData Data => itemData;
}
