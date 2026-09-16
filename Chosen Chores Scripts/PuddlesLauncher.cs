using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class BloodCollisionHandler : MonoBehaviour
{
    [Tooltip("Ссылка на глобальную систему частиц, которая рисует лужи")]
    public ParticleSystem puddleParticleSystem;

    [Tooltip("Ссылка на скрипт кровотечения этого игрока")]
    public PlayerBleeding playerBleeding;

    [Header("Сетевые настройки")]
    [Tooltip("Как часто отправлять координаты серверу (чтобы не спамить сеть)")]
    public float nodeNetworkInterval = 1f;

    private ParticleSystem dropParticleSystem;
    private List<ParticleCollisionEvent> collisionEvents;
    private float lastNodeTime;

    void Start()
    {
        dropParticleSystem = GetComponent<ParticleSystem>();
        collisionEvents = new List<ParticleCollisionEvent>();

        // Если забыли назначить в инспекторе, пытаемся найти на родителе
        if (playerBleeding == null)
        {
            playerBleeding = GetComponentInParent<PlayerBleeding>();
        }
    }

    void OnParticleCollision(GameObject other)
    {
        int numCollisionEvents = dropParticleSystem.GetCollisionEvents(other, collisionEvents);

        for (int i = 0; i < numCollisionEvents; i++)
        {
            Vector3 pos = collisionEvents[i].intersection;
            Vector3 normal = collisionEvents[i].normal;

            SpawnPuddle(pos, normal);
        }
    }

    private void SpawnPuddle(Vector3 position, Vector3 normal)
    {
        // 1. ВИЗУАЛ: Рисуем лужу локально для всех
        if (puddleParticleSystem != null)
        {
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
            emitParams.position = position + (normal * 0.01f);
            emitParams.rotation3D = Quaternion.LookRotation(normal).eulerAngles;
            emitParams.startSize = Random.Range(0.2f, 0.4f);
            puddleParticleSystem.Emit(emitParams, 1);
        }

        // 2. ЛОГИКА ИИ: Если это НАШ персонаж, сообщаем серверу точные координаты
        if (playerBleeding != null && playerBleeding.IsOwner)
        {
            if (Time.time - lastNodeTime >= nodeNetworkInterval)
            {
                lastNodeTime = Time.time;
                // Отправляем RPC с координатами визуальной лужи
                playerBleeding.PlaceBloodNodeServerRpc(position);
            }
        }
    }
}