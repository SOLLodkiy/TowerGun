using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteBackground : MonoBehaviour
{
    [Header("Center column (4 sprites)")]
    public GameObject[] centerBackgrounds;

    [Header("Left column (3 sprites)")]
    public GameObject[] leftBackgrounds;

    [Header("Right column (3 sprites)")]
    public GameObject[] rightBackgrounds;

    [Header("Main camera")]
    public Camera mainCamera;

    [Header("Scroll offset")]
    [Tooltip("На сколько высот спрайта ниже камеры спрайт считается вышедшим из кадра. " +
             "Увеличь значение если на длинных телефонах видно перемещение снизу. " +
             "Рекомендуется 1.5–2.0 для безопасного запаса.")]
    public float recycleOffsetMultiplier = 1.5f;

    private float backgroundHeight;

    void Start()
    {
        if (centerBackgrounds == null || centerBackgrounds.Length == 0)
        {
            Debug.LogError("Заполните массив centerBackgrounds.");
            enabled = false;
            return;
        }

        backgroundHeight = centerBackgrounds[0]
                           .GetComponent<SpriteRenderer>()
                           .bounds.size.y;
    }

    void Update()
    {
        ScrollColumn(centerBackgrounds);
        ScrollColumn(leftBackgrounds);
        ScrollColumn(rightBackgrounds);
    }

    void ScrollColumn(GameObject[] column)
    {
        if (column == null || column.Length == 0) return;

        for (int i = 0; i < column.Length; i++)
        {
            // ИСПРАВЛЕНИЕ: спрайт перемещается только когда его верхний край
            // опускается ниже нижней границы видимости камеры с запасом.
            // Раньше: camera.y > sprite.y + height  (перемещение у нижнего края экрана)
            // Теперь: camera.y > sprite.y + height * multiplier
            // При multiplier = 1.5 спрайт уходит на полтора своих роста ниже камеры
            // прежде чем переместиться — на длинных экранах это за пределами видимости.
            float recycleThreshold = column[i].transform.position.y
                                     + backgroundHeight * recycleOffsetMultiplier;

            if (mainCamera.transform.position.y > recycleThreshold)
            {
                GameObject highest = GetHighest(column);

                column[i].transform.position = new Vector3(
                    highest.transform.position.x,
                    highest.transform.position.y + backgroundHeight,
                    highest.transform.position.z);

                UpdateOrder(column, i);
            }
        }
    }

    GameObject GetHighest(GameObject[] column)
    {
        GameObject highest = column[0];
        for (int i = 1; i < column.Length; i++)
            if (column[i].transform.position.y > highest.transform.position.y)
                highest = column[i];
        return highest;
    }

    void UpdateOrder(GameObject[] column, int index)
    {
        GameObject temp = column[index];
        for (int i = index; i < column.Length - 1; i++)
            column[i] = column[i + 1];
        column[column.Length - 1] = temp;
    }
}