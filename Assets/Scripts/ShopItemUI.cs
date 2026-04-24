using UnityEngine;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Image slotImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private Slider progressBar;

    [Header("Data")]
    [SerializeField] private ShopItemData itemData;

    private enum ItemState { Locked, Unlocked, Purchased, Equipped }
    private ItemState state;

    private string PrefKey => "ShopItem_" + itemData.name;

    private void Awake()
    {
        LoadState();
        RefreshUI();
        UpdateProgressFromPrefs();
        TryAutoUnlock();
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
        GetComponentInParent<ShopGridSorter>()?.SortItems();
    }

    public void Equip()
    {
        state = ItemState.Equipped;
        SaveState(); RefreshUI();
        GetComponentInParent<ShopGridSorter>()?.SortItems();
    }

    public void Unequip()
    {
        state = ItemState.Purchased;
        SaveState(); RefreshUI();
        GetComponentInParent<ShopGridSorter>()?.SortItems();
    }

    public void TryAutoUnlock()
    {
        if (state != ItemState.Locked) return;

        int progress = GetProgressValue();
        if (progress >= itemData.unlockRequirement)
        {
            state = ItemState.Unlocked;
            SaveState(); RefreshUI();
            GetComponentInParent<ShopGridSorter>()?.SortItems();
        }
    }

    public void UpdateProgressFromPrefs()
    {
        int progress = GetProgressValue();

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
            case UnlockType.KillEnemies:         return PlayerPrefs.GetInt("TotalKills", 0);
            case UnlockType.ReachDistance:       return PlayerPrefs.GetInt("BestDistance", 0);
            case UnlockType.TotalCoinsCollected: return PlayerPrefs.GetInt("TotalCoinsCollected", 0);
            case UnlockType.TotalCoinsSpent:     return PlayerPrefs.GetInt("TotalCoinsSpent", 0);
            case UnlockType.Losses:              return PlayerPrefs.GetInt("TotalLosses", 0);
            case UnlockType.TimePlayedActive:    return Mathf.FloorToInt(PlayerPrefs.GetFloat("TotalActiveTime", 0f));
            case UnlockType.ShotsFired:          return PlayerPrefs.GetInt("TotalShots", 0);
            case UnlockType.TotalDistance:       return Mathf.FloorToInt(PlayerPrefs.GetFloat("TotalDistance", 0f));
            case UnlockType.ItemsPurchased:      return PlayerPrefs.GetInt("TotalPurchases", 0);
            default:                             return 0;
        }
    }

    private void RefreshUI()
    {
        // Иконка видна всегда — для заблокированных рисуем её чёрной
        iconImage.enabled = true;
        iconImage.sprite = itemData.itemSprite;
        iconImage.preserveAspect = true;

        // Заблокировано — чёрный силуэт, иначе нормальный цвет
        iconImage.color = (state == ItemState.Locked) ? Color.black : Color.white;

        // Пропорции применяем только для разблокированных предметов
        if (state != ItemState.Locked)
        {
            RectTransform rt = iconImage.GetComponent<RectTransform>();
            float spriteRatio   = (float)iconImage.sprite.rect.width / iconImage.sprite.rect.height;
            float currentWidth  = rt.rect.width;
            float currentHeight = rt.rect.height;

            if (currentWidth / currentHeight > spriteRatio)
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, currentHeight * spriteRatio);
            else
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, currentWidth / spriteRatio);
        }

        switch (state)
        {
            case ItemState.Locked:    slotImage.sprite = itemData.lockedSprite;    break;
            case ItemState.Unlocked:  slotImage.sprite = itemData.unlockedSprite;  break;
            case ItemState.Purchased: slotImage.sprite = itemData.purchasedSprite; break;
            case ItemState.Equipped:  slotImage.sprite = itemData.equippedSprite;  break;
        }
    }

    private void SaveState() => PlayerPrefs.SetInt(PrefKey, (int)state);
    private void LoadState() => state = (ItemState)PlayerPrefs.GetInt(PrefKey, 0);

    public bool IsEquipped  => state == ItemState.Equipped;
    public bool IsPurchased => state == ItemState.Purchased;
    public bool IsUnlocked  => state == ItemState.Unlocked;
    public ShopItemData Data => itemData;
}