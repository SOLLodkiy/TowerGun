using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteBackground : MonoBehaviour
{
    [Header("Center column (4 sprites)")]
    public GameObject[] centerBackgrounds;     // 4 шт.

    [Header("Left column (3 sprites)")]
    public GameObject[] leftBackgrounds;       // 3 шт.

    [Header("Right column (3 sprites)")]
    public GameObject[] rightBackgrounds;      // 3 шт.

    [Header("Main camera")]
    public Camera mainCamera;

    private float backgroundHeight;            // Высота одного фона

    void Start()
    {
        // Без центральной колонки мы не узнаем высоту спрайта
        if (centerBackgrounds == null || centerBackgrounds.Length == 0)
        {
            Debug.LogError("Заполните массив centerBackgrounds.");
            enabled = false;
            return;
        }

        // Берём высоту первого спрайта (считаем, что у всех одинаковая)
        backgroundHeight = centerBackgrounds[0]
                           .GetComponent<SpriteRenderer>()
                           .bounds.size.y;
    }

    void Update()
    {
        // Скроллим каждую колонку
        ScrollColumn(centerBackgrounds);
        ScrollColumn(leftBackgrounds);
        ScrollColumn(rightBackgrounds);
    }

    /// <summary>
    /// Проверяет колонку и, если камера прошла нижний спрайт,
    /// перекидывает его наверх.
    /// </summary>
    void ScrollColumn(GameObject[] column)
    {
        if (column == null || column.Length == 0) return;

        for (int i = 0; i < column.Length; i++)
        {
            // Камера выше спрайта + высота => спрайт вышел из кадра
            if (mainCamera.transform.position.y >
                column[i].transform.position.y + backgroundHeight)
            {
                GameObject lowest = column[i];               // нижний спрайт
                GameObject highest = GetHighest(column);     // верхний спрайт

                // Ставим нижний точно над верхним
                lowest.transform.position = new Vector3(
                    highest.transform.position.x,
                    highest.transform.position.y + backgroundHeight,
                    highest.transform.position.z);

                UpdateOrder(column, i);                      // правим порядок
            }
        }
    }

    // Возвращает верхний спрайт в колонке
    GameObject GetHighest(GameObject[] column)
    {
        GameObject highest = column[0];
        for (int i = 1; i < column.Length; i++)
        {
            if (column[i].transform.position.y > highest.transform.position.y)
                highest = column[i];
        }
        return highest;
    }

    // Сдвигаем массив так, чтобы перемещённый спрайт стал последним (верхним)
    void UpdateOrder(GameObject[] column, int index)
    {
        GameObject temp = column[index];
        for (int i = index; i < column.Length - 1; i++)
        {
            column[i] = column[i + 1];
        }
        column[column.Length - 1] = temp;
    }
}
