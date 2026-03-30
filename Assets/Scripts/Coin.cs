using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    private bool isCollected = false; // Флаг для проверки, собрана ли монета
    public GameObject coinEffectPrefab; // Префаб эффекта монет

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected) return; // Если монета уже собрана, ничего не делаем

        if (other.CompareTag("Player") || other.CompareTag("PlayerBullet"))
        {
            isCollected = true; // Помечаем монету как собранную

            // Выполняем действия по сбору монеты (например, эффект и удаление)
            CollectCoin(other);
        }
    }

    void Update()
    {
        // Добавляем монеты по кнопке K
        if (Input.GetKeyDown(KeyCode.K))
        {
            GunMove playerScript = GameObject.FindWithTag("Player").GetComponent<GunMove>();
            if (playerScript != null)
            {
                playerScript.coinCount += 1000;
                playerScript.sessionCoinCount += 1000;
                playerScript.UpdateCoinUI();
                playerScript.UpdateSessionCoinUI();
                PlayerPrefs.SetInt("Coins", playerScript.coinCount); // сохраняем
            }
        }
    }

    void CollectCoin(Collider2D collector)
    {
        // Создаем эффект на месте монеты
        if (coinEffectPrefab != null)
        {
            GameObject coinEffect = Instantiate(coinEffectPrefab, transform.position, Quaternion.identity);
            Destroy(coinEffect, 2f); // Уничтожаем эффект через 2 секунды
        }

        // Увеличиваем количество монет у игрока
        if (collector.CompareTag("Player"))
        {
            GunMove playerScript = collector.GetComponent<GunMove>();
            if (playerScript != null)
            {
                playerScript.coinCount++;
                playerScript.sessionCoinCount++;
                playerScript.UpdateCoinUI();
                playerScript.UpdateSessionCoinUI();
                PlayerPrefs.SetInt("Coins", playerScript.coinCount); // Сохраняем монеты
                PlayerPrefs.SetInt("TotalCoinsCollected", PlayerPrefs.GetInt("TotalCoinsCollected", 0) + 1);
            }
        }

        if (collector.CompareTag("PlayerBullet"))
        {
            GunMove playerScript = GameObject.FindWithTag("Player").GetComponent<GunMove>();
            if (playerScript != null)
            {
                playerScript.coinCount++;
                playerScript.sessionCoinCount++;
                playerScript.UpdateCoinUI();
                playerScript.UpdateSessionCoinUI();
                PlayerPrefs.SetInt("Coins", playerScript.coinCount); // Сохраняем монеты
                PlayerPrefs.SetInt("TotalCoinsCollected", PlayerPrefs.GetInt("TotalCoinsCollected", 0) + 1);
            }
        }

        // Удаляем монетку
        Destroy(gameObject);
    }
}
