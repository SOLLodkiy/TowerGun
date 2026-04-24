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
    public Image backgroundImage;
    public Text shopTitle;

    [Header("Category Colors")]
    public Color upgradesColor = new Color(0f, 0.8f, 0f);
    public Color cursesColor = new Color(0.35f, 0f, 0.5f);
    public Color weaponsColor = new Color(1f, 0.5f, 0f);
    public Color specialWeaponsColor = Color.yellow;

    [Header("Category Titles")]
    public string upgradesTitle = "Аксессуары";
    public string cursesTitle = "Проклятия";
    public string weaponsTitle = "Оружие";
    public string specialWeaponsTitle = "Особое оружие";

    [Header("Close Button Anchors")]
    public RectTransform shopCloseAnchor;
    public RectTransform infoCloseAnchor;

    [Header("Category Content Panels")]
    public GameObject upgradesContent;
    public GameObject cursesContent;
    public GameObject weaponsContent;
    public GameObject specialWeaponsContent;

    public Animator upgradesAnimator;
    public Animator cursesAnimator;
    public Animator weaponsAnimator;
    public Animator specialWeaponsAnimator;

    private int currentCategory = 0;
    private const string CategoryKey = "ShopCategory";

    public PistolMove playerGunMove;

    private void Start()
    {
        StopAllCoroutines();

        shopPanel.SetActive(false);
        darkBackground.SetActive(false);

        currentCategory = PlayerPrefs.GetInt(CategoryKey, 0);

        closeButton.onClick.AddListener(CloseShop);
        upgradesButton.onClick.AddListener(() => SetCategory(0));
        cursesButton.onClick.AddListener(() => SetCategory(1));
        weaponsButton.onClick.AddListener(() => SetCategory(2));
        specialWeaponsButton.onClick.AddListener(() => SetCategory(3));
    }

    public void OpenShop()
    {
        playerGunMove.canShoot = false;
        shopPanel.SetActive(true);
        darkBackground.SetActive(true);

        SetCategory(currentCategory);

        Time.timeScale = 0f;

        // Сортируем все гриды при открытии магазина
        foreach (var sorter in FindObjectsByType<ShopGridSorter>(FindObjectsSortMode.None))
            sorter.SortItems();
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        darkBackground.SetActive(false);
        Time.timeScale = 1f;
        StartCoroutine(EnableShootingAfterDelay(0.1f));
    }

    private IEnumerator EnableShootingAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        playerGunMove.canShoot = true;
    }

    public void SetCategory(int categoryIndex)
    {
        currentCategory = categoryIndex;

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

        upgradesContent.SetActive(categoryIndex == 0);
        cursesContent.SetActive(categoryIndex == 1);
        weaponsContent.SetActive(categoryIndex == 2);
        specialWeaponsContent.SetActive(categoryIndex == 3);

        UpdateButtonSelection(categoryIndex);
    }

    private void UpdateButtonSelection(int categoryIndex)
    {
        upgradesAnimator.SetBool("IsSelected", categoryIndex == 0);
        cursesAnimator.SetBool("IsSelected", categoryIndex == 1);
        weaponsAnimator.SetBool("IsSelected", categoryIndex == 2);
        specialWeaponsAnimator.SetBool("IsSelected", categoryIndex == 3);
    }
}