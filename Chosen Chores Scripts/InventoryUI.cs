using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class InventoryUI : MonoBehaviour
{
    [Header("UI Элементы")]
    public Image[] slotBackgrounds;
    public Image[] itemIcons;

    public Color activeColor = Color.yellow;
    public Color inactiveColor = new Color(0, 0, 0, 0.5f);

    private PlayerInventory localPlayerInventory;

    void Update()
    {
        if (!localPlayerInventory && NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
            if (playerObj != null)
            {
                localPlayerInventory = playerObj.GetComponent<PlayerInventory>();
                localPlayerInventory.OnInventoryUpdated += RefreshUI;
                RefreshUI();
            }
        }
        if (localPlayerInventory == null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            if (NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                localPlayerInventory = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerInventory>();
                localPlayerInventory.OnInventoryUpdated += RefreshUI;
                RefreshUI();
            }
        }

        // Защита от сетевой задержки: проверяем, не пропустил ли UI загрузку иконки
        if (localPlayerInventory != null)
        {
            for (int i = 0; i < PlayerInventory.INVENTORY_SIZE; i++)
            {
                ulong netId = localPlayerInventory.inventorySlots[i];
                // Если слот занят, но иконка выключена — значит объект только что заспавнился
                if (netId != 0 && !itemIcons[i].enabled)
                {
                    RefreshUI();
                    break;
                }
            }
        }
    }

    private void RefreshUI()
    {
        if (localPlayerInventory == null) return;

        int activeIndex = localPlayerInventory.activeSlotIndex.Value;

        for (int i = 0; i < PlayerInventory.INVENTORY_SIZE; i++)
        {
            slotBackgrounds[i].color = (i == activeIndex) ? activeColor : inactiveColor;

            ulong netId = localPlayerInventory.inventorySlots[i];

            // Читаем физический объект через SpawnManager
            if (netId != 0 && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out NetworkObject itemObj))
            {
                UniversalNetworkItem item = itemObj.GetComponent<UniversalNetworkItem>();

                itemIcons[i].sprite = item.inventoryIcon;
                itemIcons[i].enabled = true;
                itemIcons[i].color = Color.white;
            }
            else
            {
                itemIcons[i].sprite = null;
                itemIcons[i].enabled = false;
            }
        }
    }

    private void OnDestroy()
    {
        if (localPlayerInventory != null)
        {
            localPlayerInventory.OnInventoryUpdated -= RefreshUI;
        }
    }
}