using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections; // Нужно для корутины

public class PlayerInventory : NetworkBehaviour
{
    public const int INVENTORY_SIZE = 5;

    [Header("Точки привязки")]
    public Transform handTransform;
    public Transform backpackTransform;

    [Header("База данных")]
    [SerializeField] public GameObject[] itemPrefabs; // Убедитесь, что список заполнен в Инспекторе!

    [Header("UI")]
    public GameObject playerUICanvas;

    public NetworkList<ulong> inventorySlots;
    public NetworkVariable<int> activeSlotIndex = new NetworkVariable<int>(0);

    public Action OnInventoryUpdated;

    void Awake()
    {
        inventorySlots = new NetworkList<ulong>(
            new ulong[] { 0, 0, 0, 0, 0 },
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
    }

    public override void OnNetworkSpawn()
    {
        if (playerUICanvas != null) playerUICanvas.SetActive(IsOwner);

        inventorySlots.OnListChanged += (changeEvent) => OnInventoryUpdated?.Invoke();
        activeSlotIndex.OnValueChanged += (oldVal, newVal) => OnInventoryUpdated?.Invoke();

        if (IsOwner)
        {
            // Если в буфере есть данные — запускаем восстановление
            if (InventoryBuffer.StoredItemIDs.Count > 0)
            {
                Debug.Log($"<color=green>[Инвентарь]</color> Нашел {InventoryBuffer.StoredItemIDs.Count} предметов в буфере. Начинаю восстановление...");
                StartCoroutine(RestoreRoutine());
            }
        }
    }

    private IEnumerator RestoreRoutine()
    {
        // Небольшая пауза, чтобы сервер успел полностью инициализировать игрока
        yield return new WaitForSeconds(0.2f);

        ClearInventoryServerRpc();
        RestoreItemsServerRpc(InventoryBuffer.StoredItemIDs.ToArray());

        // Очищаем буфер только ПОСЛЕ того, как отправили запрос серверу
        InventoryBuffer.Clear();
    }

    public void SaveToBuffer()
    {
        if (!IsOwner) return;

        InventoryBuffer.Clear();
        int savedCount = 0;

        for (int i = 0; i < INVENTORY_SIZE; i++)
        {
            ulong netId = inventorySlots[i];
            if (netId != 0 && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out NetworkObject obj))
            {
                var item = obj.GetComponent<UniversalNetworkItem>();
                if (item != null)
                {
                    InventoryBuffer.StoredItemIDs.Add(item.itemID);
                    savedCount++;
                    continue;
                }
            }
            InventoryBuffer.StoredItemIDs.Add(0);
        }
        Debug.Log($"<color=yellow>[Инвентарь]</color> Сохранено для перехода: {savedCount} предметов.");
    }

    [Rpc(SendTo.Server)]
    private void ClearInventoryServerRpc()
    {
        for (int i = 0; i < INVENTORY_SIZE; i++) inventorySlots[i] = 0;
    }

    [Rpc(SendTo.Server)]
    private void RestoreItemsServerRpc(int[] savedItemIDs)
    {
        for (int i = 0; i < savedItemIDs.Length; i++)
        {
            int id = savedItemIDs[i];
            if (id == 0) continue;

            GameObject prefabToSpawn = null;
            foreach (var prefab in itemPrefabs)
            {
                if (prefab != null && prefab.GetComponent<UniversalNetworkItem>().itemID == id)
                {
                    prefabToSpawn = prefab;
                    break;
                }
            }

            if (prefabToSpawn != null)
            {
                GameObject spawnedItem = Instantiate(prefabToSpawn, transform.position, Quaternion.identity);
                NetworkObject netObj = spawnedItem.GetComponent<NetworkObject>();

                netObj.SpawnWithOwnership(OwnerClientId);

                // Передаем предмет в TryAddItem, он сам найдет нужный слот
                TryAddItem(spawnedItem.GetComponent<UniversalNetworkItem>());
                Debug.Log($"<color=cyan>[Сервер]</color> Восстановлен предмет типа {id} для игрока {OwnerClientId}");
            }
            else
            {
                Debug.LogError($"<color=red>[Ошибка]</color> Сервер не нашел префаб с itemID {id} в списке itemPrefabs!");
            }
        }
    }

    public bool TryAddItem(UniversalNetworkItem item)
    {
        if (!IsServer) return false;

        int emptySlot = -1;
        for (int i = 0; i < INVENTORY_SIZE; i++)
        {
            if (inventorySlots[i] == 0)
            {
                emptySlot = i;
                break;
            }
        }

        if (emptySlot == -1) return false;

        inventorySlots[emptySlot] = item.NetworkObjectId;
        item.Pickup(NetworkObject, emptySlot == activeSlotIndex.Value);
        return true;
    }

    [Rpc(SendTo.Server)]
    public void ChangeSlotServerRpc(int newSlotIndex)
    {
        if (newSlotIndex == activeSlotIndex.Value) return;

        var currentItem = GetActiveItem();
        if (currentItem != null) currentItem.isEquipped.Value = false;

        activeSlotIndex.Value = newSlotIndex;

        var newItem = GetActiveItem();
        if (newItem != null) newItem.isEquipped.Value = true;
    }

    public UniversalNetworkItem GetActiveItem()
    {
        ulong netId = inventorySlots[activeSlotIndex.Value];
        if (netId != 0 && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out NetworkObject obj))
        {
            return obj.GetComponent<UniversalNetworkItem>();
        }
        return null;
    }
    [Rpc(SendTo.Server)]
    public void DropCurrentItemServerRpc()
    {
        // Используем наш надежный метод поиска активного предмета
        var currentItem = GetActiveItem();
        if (currentItem != null)
        {
            // Вызываем физический сброс на самом предмете
            currentItem.DropItem();
            // Очищаем слот в инвентаре
            inventorySlots[activeSlotIndex.Value] = 0;
        }
    }

    [Rpc(SendTo.Server)]
    public void ForcedStealItemServerRpc()
    {
        // Метод для ситуаций, когда враг выбивает предмет из рук
        inventorySlots[activeSlotIndex.Value] = 0;
        ClearLocalActiveItemClientRpc();
    }
    // Добавьте этот метод в PlayerInventory.cs
    public void RemoveItemFromInventory(ulong netId)
    {
        if (!IsServer) return;

        for (int i = 0; i < INVENTORY_SIZE; i++)
        {
            if (inventorySlots[i] == netId)
            {
                inventorySlots[i] = 0;
                Debug.Log($"<color=green>[Инвентарь]</color> Предмет {netId} удален из слота {i}");
                break;
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ClearLocalActiveItemClientRpc()
    {
        // Здесь можно добавить сброс анимаций рук, если нужно
    }
}