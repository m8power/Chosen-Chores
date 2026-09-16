using UnityEngine;
using Unity.Netcode;

public class PlayerBleeding : NetworkBehaviour
{
    [Header("Визуал")]
    public ParticleSystem bloodParticleSystem;

    [Header("Настройки времени")]
    public float minBleedDuration = 5f;
    public float maxBleedDuration = 10f;

    public NetworkVariable<bool> isBleeding = new NetworkVariable<bool>(false);

    private float stopBleedingTimer;
    private PlayerInputActions inputActions;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            inputActions = new PlayerInputActions();
            inputActions.Enable();
        }

        isBleeding.OnValueChanged += (oldVal, newVal) =>
        {
            if (newVal) bloodParticleSystem.Play();
            else bloodParticleSystem.Stop();
        };

        if (isBleeding.Value) bloodParticleSystem.Play();
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && inputActions != null) inputActions.Disable();
    }

    void Update()
    {
        if (IsOwner && inputActions != null && inputActions.Player.Bleed.WasPressedThisFrame())
        {
            StartBleedingServerRpc();
        }

        // Сервер теперь отвечает ТОЛЬКО за общую длительность кровотечения
        if (IsServer && isBleeding.Value)
        {
            stopBleedingTimer -= Time.deltaTime;
            if (stopBleedingTimer <= 0)
            {
                isBleeding.Value = false;
                Debug.Log("[Сервер] Кровотечение игрока остановилось по таймеру.");
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void StartBleedingServerRpc()
    {
        isBleeding.Value = true;
        stopBleedingTimer = Random.Range(minBleedDuration, maxBleedDuration);
    }

    // НОВЫЙ МЕТОД: Клиент вызывает его, чтобы передать точные координаты удара капли
    [Rpc(SendTo.Server)]
    public void PlaceBloodNodeServerRpc(Vector3 exactPosition)
    {
        BloodManager.Instance.AddBloodNode(exactPosition, NetworkObject.OwnerClientId);
    }
}