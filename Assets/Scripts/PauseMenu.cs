using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenu; // Панель меню паузы
    public GameObject confirmRestartPanel; // Панель подтверждения перезапуска
    public Text randomText; // Текст для отображения случайной фразы
    public List<string> phrases; // Массив с фразами
    public bool isPaused = false;
    public PistolMove playerGunMove; // Ссылка на скрипт GunMove

    public GameObject[] objectsToAppearOnPause; // Объекты, которые появятся при активации паузы

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) // Или можно использовать кнопку на UI
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            /* Сначала гасим любой активный slow‑mo */
            playerGunMove.ForceStopSlowMotion();

            Time.timeScale = 0f; // Останавливаем время
            pauseMenu.SetActive(true); // Показываем меню паузы
            randomText.text = GetRandomPhrase(); // Устанавливаем случайную фразу
            playerGunMove.canShoot = false; // Отключаем возможность стрелять
            BlurBackground(true); // Включаем блюр


            // Активируем объекты
            HandlePauseObjects(true);
        }
        else
        {
            Resume(); // Возвращаем игру
        }
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f; // Восстанавливаем время
        pauseMenu.SetActive(false); // Скрываем меню паузы
        StartCoroutine(EnableShootingAfterDelay(0.1f)); // Запускаем корутину
        BlurBackground(false); // Выключаем блюр

        // Деактивируем объекты
        HandlePauseObjects(false);
    }

    public void Restart()
    {
        confirmRestartPanel.SetActive(true); // Показываем панель подтверждения
    }

    public void OnConfirmRestart(bool confirm)
    {
        if (confirm)
        {
            Time.timeScale = 1f; // Восстанавливаем время перед перезапуском
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // Перезапускаем сцену
        }
        else
        {
            confirmRestartPanel.SetActive(false); // Скрываем панель подтверждения
        }
    }

    private string GetRandomPhrase()
    {
        return phrases[Random.Range(0, phrases.Count)]; // Выбор случайной фразы
    }

    private void BlurBackground(bool enable)
    {
        // Здесь можно добавить код для включения/выключения блюра
    }

    private IEnumerator EnableShootingAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay); // Ждем указанное время
        playerGunMove.canShoot = true; // Включаем возможность стрелять
    }

    // Метод для активации/деактивации объектов
    private void HandlePauseObjects(bool isActive)
    {
        foreach (GameObject obj in objectsToAppearOnPause)
        {
            if (obj != null)
            {
                obj.SetActive(isActive);
            }
        }
    }
}
