using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInventory))]
public class PlayerInteraction : NetworkBehaviour
{
    public float interactionDistance = 3f;
    public Camera playerCamera;

    private PlayerInputActions inputActions;
    private PlayerInventory inventory;

   
    public override void OnNetworkSpawn()
    {
        inventory = GetComponent<PlayerInventory>();
        if (IsOwner)
        {
            inputActions = new PlayerInputActions();
            inputActions.Enable();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && inputActions != null) inputActions.Disable();
    }

    void Update()
    {
        if (!IsOwner || inputActions == null) return;

        // 1. ПОДБОР (E)
        if (inputActions.Player.Interact.WasPressedThisFrame())
        {
            TryPickup();
        }

        // 2. ИСПОЛЬЗОВАТЬ ПРЕДМЕТ (F - Фонарик и т.д.)
        if (inputActions.Player.ToggleFlashlight.WasPressedThisFrame())
        {
            UniversalNetworkItem activeItem = inventory.GetActiveItem();
            if (activeItem != null) activeItem.UseItemServerRpc();
        }

        // 3. ВЫБРОСИТЬ (G)
        if (inputActions.Player.Drop.WasPressedThisFrame())
        {
            inventory.DropCurrentItemServerRpc();
        }

        // 4. ПЕРЕКЛЮЧЕНИЕ СЛОТОВ (Нажатия 1-5)
        HandleSlotInput();
    }

    private void HandleSlotInput()
    {
        // Читаем нажатия из новой Input System абсолютно так же, как E, F и G
        if (inputActions.Player.Slot1.WasPressedThisFrame()) inventory.ChangeSlotServerRpc(0);
        if (inputActions.Player.Slot2.WasPressedThisFrame()) inventory.ChangeSlotServerRpc(1);
        if (inputActions.Player.Slot3.WasPressedThisFrame()) inventory.ChangeSlotServerRpc(2);
        if (inputActions.Player.Slot4.WasPressedThisFrame()) inventory.ChangeSlotServerRpc(3);
        if (inputActions.Player.Slot5.WasPressedThisFrame()) inventory.ChangeSlotServerRpc(4);
    }

    private void TryPickup()
    {
        if (playerCamera == null) return;

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, interactionDistance))
        {
            UniversalNetworkItem item = hit.collider.GetComponentInParent<UniversalNetworkItem>();
            if (item != null && !item.isHeld.Value)
            {
                PickupServerRpc(item.NetworkObjectId);
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void PickupServerRpc(ulong itemNetworkId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(itemNetworkId, out NetworkObject itemObj))
        {
            UniversalNetworkItem item = itemObj.GetComponent<UniversalNetworkItem>();

            // Пытаемся добавить в инвентарь. Если места нет - ничего не произойдет.
            bool success = inventory.TryAddItem(item);
            if (!success) Debug.Log("[Сервер] Инвентарь полон!");
        }
    }
   
}