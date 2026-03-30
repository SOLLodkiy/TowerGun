using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class EquippedItemsUI : MonoBehaviour
{
    [Header("Containers")]
    [SerializeField] private RectTransform preSessionContainer;
    [SerializeField] private RectTransform inSessionContainer;
    [SerializeField] private GameObject iconPrefab; // Простой Image с прозрачностью

    private bool isSessionStarted = false;
    private bool iconsMoved = false;

    private PistolMove player;

    private void Start()
    {
        RefreshIcons();

        // Изначально показываем preSession
        preSessionContainer.gameObject.SetActive(true);
        inSessionContainer.gameObject.SetActive(false);

        // Подписка на событие старта сессии
        player = FindObjectOfType<PistolMove>();
        if (player != null)
        {
            player.OnSessionStarted += HandleSessionStart;
        }
    }
    
    void Update()
    {
        player = FindObjectOfType<PistolMove>();

        if (!player.sessionStarted)
        {
            RefreshIcons();
        }

        if (player.sessionStarted && !iconsMoved)
        {
            MoveIconsTo(inSessionContainer);
            iconsMoved = true;
        }
    }

    private void HandleSessionStart()
    {
        if (isSessionStarted) return;

        isSessionStarted = true;
        preSessionContainer.gameObject.SetActive(false);
        inSessionContainer.gameObject.SetActive(true);

        // Переносим иконки
        MoveIconsTo(inSessionContainer);
    }

    private void RefreshIcons()
    {
        // Очищаем контейнер
        foreach (Transform child in preSessionContainer)
            Destroy(child.gameObject);

        // Добавляем все надетые предметы
        foreach (var item in EquipmentManager.Instance.GetEquippedItems())
        {
            GameObject icon = Instantiate(iconPrefab, preSessionContainer);
            icon.transform.localScale = Vector3.one * 0.5f; // уменьшенные и полупрозрачные

            // Назначаем спрайт предмета
            Image img = icon.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = item.itemSprite; // <- используем спрайт предмета
                img.preserveAspect = true;

                Color c = img.color;
                c.a = 0.6f; // полупрозрачность
                img.color = c;
            }
        }
    }

    private void CreateIcon(Sprite sprite, RectTransform parent)
    {
        GameObject iconGO = Instantiate(iconPrefab, parent);
        Image img = iconGO.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        Color c = img.color;
        c.a = 0.65f; // полупрозрачность
        img.color = c;
    }

    private void ClearContainer(RectTransform container)
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }

    private void MoveIconsTo(RectTransform newParent)
    {
        Transform[] children = preSessionContainer.Cast<Transform>().ToArray(); // копия
        foreach (Transform child in children)
        {
            child.SetParent(newParent, false);
        }
    }
}
