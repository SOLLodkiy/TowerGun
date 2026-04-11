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
    private bool iconsBuilt = false; // флаг — иконки уже построены, не нужно пересоздавать

    private PistolMove player;

    private void Start()
    {
        // ОПТИМИЗАЦИЯ: ищем один раз в Start, не в Update
        player = FindFirstObjectByType<PistolMove>();

        preSessionContainer.gameObject.SetActive(true);
        inSessionContainer.gameObject.SetActive(false);

        // Строим иконки один раз
        RefreshIcons();
        iconsBuilt = true;

        if (player != null)
            player.OnSessionStarted += HandleSessionStart;
    }

    private void OnDestroy()
    {
        // Отписываемся чтобы не накапливать подписчиков между сессиями
        if (player != null)
            player.OnSessionStarted -= HandleSessionStart;
    }

    void Update()
    {
        // ОПТИМИЗАЦИЯ: убран FindFirstObjectByType из Update (вызывался каждый кадр)
        // и убран RefreshIcons из Update (каждый кадр Destroy+Instantiate иконок)
        // Теперь переход контейнеров обрабатывается только через событие OnSessionStarted

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

    private void RefreshIcons()
    {
        foreach (Transform child in preSessionContainer)
            Destroy(child.gameObject);

        foreach (var item in EquipmentManager.Instance.GetEquippedItems())
        {
            GameObject icon = Instantiate(iconPrefab, preSessionContainer);
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