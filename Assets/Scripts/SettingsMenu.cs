using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsMenu : MonoBehaviour
{
    public GameObject settingsPanel;          // Панель с настройками
    public GameObject confirmDeletePanel;     // Панель подтверждения удаления данных

    public Button settingsButton;             // Кнопка открытия панели настроек
    public Button closeSettingsButton;        // Кнопка закрытия панели настроек
    public Button deleteProgressButton;       // Кнопка для открытия панели подтверждения удаления
    public Button confirmDeleteButton;        // Кнопка подтверждения удаления
    public Button cancelDeleteButton;         // Кнопка отмены удаления

    public PistolMove playerGunMove; // Ссылка на скрипт GunMove
    public GunMove playerGunMove1; // Ссылка на скрипт GunMove
    public PauseMenu Pause; // Ссылка на скрипт GunMove

    public Toggle shadowToggle;  // Галочка для управления объектом
    public GameObject lightGameObject; // Игровой объект игрока
    private bool isShadowEnabled;


    void Start()
    {
        // Изначально панели скрыты
        settingsPanel.SetActive(false);
        confirmDeletePanel.SetActive(false);

        // Загружаем сохраненное состояние
        isShadowEnabled = PlayerPrefs.GetInt("IsShadowEnabled", 1) == 1;
        lightGameObject.SetActive(isShadowEnabled);
        
        // Устанавливаем начальное состояние галочки
        if (shadowToggle != null)
        {
            shadowToggle.isOn = isShadowEnabled;
            shadowToggle.onValueChanged.AddListener(ToggleShadow);
        }
    }

    // Открывает панель настроек
    public void OpenSettingsPanel()
    {
        settingsPanel.SetActive(true);
        Time.timeScale = 0f; // Останавливаем время
        playerGunMove.canShoot = false; // Отключаем возможность стрелять
    }

    // Закрывает панель настроек
    public void CloseSettingsPanel()
    {
        settingsPanel.SetActive(false);
        if (!Pause.isPaused)
        {
            Time.timeScale = 1f; // Восстанавливаем время
            StartCoroutine(EnableShootingAfterDelay(0.1f)); // Запускаем корутину
        }
    }

    // Открывает панель подтверждения удаления данных
    public void OpenConfirmDeletePanel()
    {
        confirmDeletePanel.SetActive(true);
    }

    // Закрывает панель подтверждения удаления данных
    public void CloseConfirmDeletePanel()
    {
        confirmDeletePanel.SetActive(false);
    }

    // Удаляет все сохранения
    public void DeleteAllProgress()
    {
        PlayerPrefs.DeleteAll(); // Удаляем все данные
        PlayerPrefs.Save();

        Time.timeScale = 1f; // Восстанавливаем время
        StartCoroutine(EnableShootingAfterDelay(0.1f)); // Запускаем корутину
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // Перезагрузить сцену
    }

    private IEnumerator EnableShootingAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay); // Ждем указанное время
        playerGunMove.canShoot = true; // Включаем возможность стрелять
    }

    private void ToggleShadow(bool isOn)
    {
        isShadowEnabled = isOn;
        lightGameObject.SetActive(isShadowEnabled);
        PlayerPrefs.SetInt("IsShadowEnabled", isShadowEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}
