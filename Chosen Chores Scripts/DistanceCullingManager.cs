using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DistanceCullingManager : MonoBehaviour
{
    [Header("Настройки дистанции")]
    [Tooltip("Радиус вокруг игроков, внутри которого объекты активны")]
    [Min(5f)]
    public float activeRadius = 50f;

    [Tooltip("Частота проверок (в секундах). Не делайте 0, чтобы не убить FPS!")]
    [Range(0.1f, 2f)]
    public float checkInterval = 0.5f;

    [Header("Игроки (Кооператив)")]
    [Tooltip("Перетащите сюда Transform всех локальных или сетевых игроков")]
    public List<Transform> players = new List<Transform>();

    [Header("Объекты для отключения")]
    [Tooltip("Добавьте сюда все объекты, которые нужно скрывать вдали")]
    public List<GameObject> targetObjects = new List<GameObject>();

    
    private float _sqrActiveRadius;

    private void Start()
    {
        
        _sqrActiveRadius = activeRadius * activeRadius;

        // Запускаем проверку в отдельном цикле
        StartCoroutine(CullingRoutine());
    }

    private IEnumerator CullingRoutine()
    {
        while (true)
        {
            if (players.Count == 0 || targetObjects.Count == 0)
            {
                yield return new WaitForSeconds(checkInterval);
                continue;
            }

            // Идем с конца списка на случай, если какой-то объект был удален (Destroy)
            for (int i = targetObjects.Count - 1; i >= 0; i--)
            {
                GameObject obj = targetObjects[i];

                if (obj == null)
                {
                    targetObjects.RemoveAt(i); // Очищаем мусор
                    continue;
                }

                bool isNearAnyPlayer = false;
                Vector3 objPosition = obj.transform.position;

                // Проверяем дистанцию до каждого игрока
                foreach (Transform player in players)
                {
                    if (player == null) continue;

                    // Если расстояние от хотя бы одного игрока до объекта меньше радиуса
                    if ((player.position - objPosition).sqrMagnitude <= _sqrActiveRadius)
                    {
                        isNearAnyPlayer = true;
                        break; // Дальше проверять нет смысла, выходим из внутреннего цикла
                    }
                }

                // Вызываем SetActive ТОЛЬКО если состояние изменилось (экономим ресурсы Unity)
                if (obj.activeSelf != isNearAnyPlayer)
                {
                    obj.SetActive(isNearAnyPlayer);
                }
            }

            // Ждем указанное время перед следующей проверкой
            yield return new WaitForSeconds(checkInterval);
        }
    }

    // Отрисовка зон в редакторе (Gizmos)
    private void OnDrawGizmos()
    {
        if (players == null || players.Count == 0) return;

        // Зеленый полупрозрачный цвет для наглядности
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);

        foreach (Transform player in players)
        {
            if (player != null)
            {
                Gizmos.DrawWireSphere(player.position, activeRadius);
            }
        }
    }
}