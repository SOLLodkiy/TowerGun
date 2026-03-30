using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisablePlayerBlock : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Проверяем, если столкновение с игроком
        if (collision.CompareTag("Player"))
        {
            // Получаем скрипт GunMove и отключаем игрока
            GunMove player = collision.GetComponent<GunMove>();
            if (player != null)
            {
                player.DisablePlayer();
            }
        }

        //if (collision.CompareTag("Enemy"))
        //{
            // Обрабатываем смерть врага
            //collision.GetComponent<EnemyPlatform>().Die();  // Вызов метода смерти врага
        //}
    }
}