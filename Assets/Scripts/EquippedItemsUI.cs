using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class EquippedItemsUI : MonoBehaviour
{
    [Header("Containers")]
    [SerializeField] private RectTransform preSessionContainer;
    [SerializeField] private RectTransform inSessionContainer;
    [SerializeField] private GameObject iconPrefab;

    private bool isSessionStarted = false;
    private bool iconsMoved = false;

    private PistolMove player;

    private void Start()
    {
        player = FindFirstObjectByType<PistolMove>();

        preSessionContainer.gameObject.SetActive(true);
        inSessionContainer.gameObject.SetActive(false);

        RefreshIcons();

        if (player != null)
            player.OnSessionStarted += HandleSessionStart;

        // Подписываемся — теперь при надевании/снятии иконки обновятся сразу
        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged += OnEquipmentChanged;
    }

    private void OnDestroy()
    {
        if (player != null)
            player.OnSessionStarted -= HandleSessionStart;

        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipmentChanged -= OnEquipmentChanged;
    }

    void Update()
    {
        if (player != null && player.sessionStarted && !iconsMoved)
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
        MoveIconsTo(inSessionContainer);
    }

    // Вызывается когда экипировка изменилась
    private void OnEquipmentChanged()
    {
        RefreshIcons();
    }

    private void RefreshIcons()
    {
        // Определяем в каком контейнере сейчас находятся иконки
        RectTransform activeContainer = iconsMoved ? inSessionContainer : preSessionContainer;

        // Очищаем активный контейнер
        foreach (Transform child in activeContainer)
            Destroy(child.gameObject);

        // Если иконки были перенесены — чистим и preSession на всякий случай
        if (iconsMoved)
        {
            foreach (Transform child in preSessionContainer)
                Destroy(child.gameObject);
        }

        // Создаём иконки для всех надетых предметов
        foreach (var item in EquipmentManager.Instance.GetEquippedItems())
        {
            GameObject icon = Instantiate(iconPrefab, activeContainer);
            icon.transform.localScale = Vector3.one * 0.5f;

            Image img = icon.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = item.itemSprite;
                img.preserveAspect = true;
                Color c = img.color;
                c.a = 0.6f;
                img.color = c;
            }
        }
    }

    private void MoveIconsTo(RectTransform newParent)
    {
        Transform[] children = preSessionContainer.Cast<Transform>().ToArray();
        foreach (Transform child in children)
            child.SetParent(newParent, false);
    }
}