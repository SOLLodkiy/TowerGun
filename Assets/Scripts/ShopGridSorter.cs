using UnityEngine;
using System.Linq;

public class ShopGridSorter : MonoBehaviour
{
    // Повесь этот скрипт на Content-объект внутри ScrollView каждой категории магазина.
    // Сортировка: Equipped → Purchased → Unlocked → Locked (слева-направо, сверху-вниз в GridLayoutGroup).

    public void SortItems()
    {
        var items = GetComponentsInChildren<ShopItemUI>(true);

        var sorted = items.OrderBy(item => GetSortPriority(item)).ToList();

        for (int i = 0; i < sorted.Count; i++)
            sorted[i].transform.SetSiblingIndex(i);
    }

    private int GetSortPriority(ShopItemUI item)
    {
        if (item.IsEquipped)  return 0; // Экипировано — первые
        if (item.IsPurchased) return 1; // Куплено — вторые
        if (item.IsUnlocked)  return 2; // Разблокировано — третьи
        return 3;                        // Заблокировано — последние
    }
}