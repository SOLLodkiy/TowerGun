using UnityEngine;

public enum UnlockType
{
    KillEnemies,
    ReachDistance,
    TotalCoinsCollected,  
    TotalCoinsSpent,      
    Losses,               
    TimePlayedActive,     
    ShotsFired,           
    TotalDistance,        
    ItemsPurchased
}

[CreateAssetMenu(fileName = "NewShopItem", menuName = "Shop/ShopItem")]
public class ShopItemData : ScriptableObject
{
    [Header("Общая информация")]
    public string itemName;
    [TextArea] public string description;
    [TextArea] public string quote;
    public int cost;
    public Sprite itemSprite;

    [Header("Слотные спрайты")]
    public Sprite lockedSprite;      // чёрный квадрат с вопросом
    public Sprite unlockedSprite;    // разблокированный слот
    public Sprite purchasedSprite;   // купленный
    public Sprite equippedSprite;    // экипированный

    [Header("Разблокировка")]
    public UnlockType unlockType;
    public int unlockRequirement; // сколько чего нужно сделать для открытия

    [Header("Эффект")]
    public string boolTargetName;   // имя переменной
    public string scriptName;       // имя скрипта, где эта переменная
}
