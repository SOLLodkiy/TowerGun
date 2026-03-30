using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ShopUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject shopPanel;
    public GameObject darkBackground;
    public Button closeButton;
    public Button upgradesButton;
    public Button cursesButton;
    public Button weaponsButton;
    public Button specialWeaponsButton;
    public Image backgroundImage; // фон магазина
    public Text shopTitle; // Заголовок магазина

    [Header("Category Colors")]
    public Color upgradesColor = new Color(0f, 0.8f, 0f);
    public Color cursesColor = new Color(0.35f, 0f, 0.5f); // фиолетовый
    public Color weaponsColor = new Color(1f, 0.5f, 0f); // оранжевый
    public Color specialWeaponsColor = Color.yellow;

    [Header("Category Titles")]
    public string upgradesTitle = "Аксессуары";
    public string cursesTitle = "Проклятия";
    public string weaponsTitle = "Оружие";
    public string specialWeaponsTitle = "Особое оружие";

    [Header("Close Button Anchors")]
    public RectTransform shopCloseAnchor;  // CloseBtnAnchor_Shop
    public RectTransform infoCloseAnchor;  // CloseBtnAnchor_Info

    [Header("Category Content Panels")]
    public GameObject upgradesContent;       // Панель с предметами улучшений
    public GameObject cursesContent;         // Панель с предметами проклятий
    public GameObject weaponsContent;        // Панель с оружием
    public GameObject specialWeaponsContent; // Панель с особым оружием


    public Animator upgradesAnimator;
    public Animator cursesAnimator;
    public Animator weaponsAnimator;
    public Animator specialWeaponsAnimator;

    private int currentCategory = 0; // 0 - Улучшения (дефолт)
    private const string CategoryKey = "ShopCategory"; // ключ для PlayerPrefs

    public PistolMove playerGunMove; // Ссылка на скрипт GunMove

    private void Start()
    {
        StopAllCoroutines(); // Остановить все корутины на этом объекте

        shopPanel.SetActive(false);
        darkBackground.SetActive(false);

        // Загружаем категорию из PlayerPrefs (если есть)
        currentCategory = PlayerPrefs.GetInt(CategoryKey, 0);

        // навешиваем слушатели
        closeButton.onClick.AddListener(CloseShop);
        upgradesButton.onClick.AddListener(() => SetCategory(0));
        cursesButton.onClick.AddListener(() => SetCategory(1));
        weaponsButton.onClick.AddListener(() => SetCategory(2));
        specialWeaponsButton.onClick.AddListener(() => SetCategory(3));
    }

    public void OpenShop()
    {
        playerGunMove.canShoot = false; // Отключаем возможность стрелять
        shopPanel.SetActive(true);
        darkBackground.SetActive(true);

        // Устанавливаем сохранённую категорию
        SetCategory(currentCategory);

        Time.timeScale = 0f; // останавливаем игру
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        darkBackground.SetActive(false);
        Time.timeScale = 1f; // возобновляем игру
        StartCoroutine(EnableShootingAfterDelay(0.1f)); // Запускаем корутину
    }

    private IEnumerator EnableShootingAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay); // Ждем указанное время
        playerGunMove.canShoot = true; // Включаем возможность стрелять
    }

    public void SetCategory(int categoryIndex)
    {
        currentCategory = categoryIndex;

        // Сохраняем категорию
        PlayerPrefs.SetInt(CategoryKey, currentCategory);
        PlayerPrefs.Save();

        switch (categoryIndex)
        {
            case 0:
                backgroundImage.color = upgradesColor;
                shopTitle.text = upgradesTitle;
                break;
            case 1:
                backgroundImage.color = cursesColor;
                shopTitle.text = cursesTitle;
                break;
            case 2:
                backgroundImage.color = weaponsColor;
                shopTitle.text = weaponsTitle;
                break;
            case 3:
                backgroundImage.color = specialWeaponsColor;
                shopTitle.text = specialWeaponsTitle;
                break;
        }
        // Включаем только текущую панель, остальные скрываем
        upgradesContent.SetActive(categoryIndex == 0);
        cursesContent.SetActive(categoryIndex == 1);
        weaponsContent.SetActive(categoryIndex == 2);
        specialWeaponsContent.SetActive(categoryIndex == 3);

        // Обновляем выбранную кнопку
        UpdateButtonSelection(categoryIndex);
    }

    private void UpdateButtonSelection(int categoryIndex)
    {
        // Сбрасываем все кнопки
        upgradesAnimator.SetBool("IsSelected", categoryIndex == 0);
        cursesAnimator.SetBool("IsSelected", categoryIndex == 1);
        weaponsAnimator.SetBool("IsSelected", categoryIndex == 2);
        specialWeaponsAnimator.SetBool("IsSelected", categoryIndex == 3);
    }
}
