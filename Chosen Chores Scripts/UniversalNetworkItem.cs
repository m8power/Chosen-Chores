using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components; // Обязательно для работы с NetworkTransform

[RequireComponent(typeof(Rigidbody))]
public class UniversalNetworkItem : NetworkBehaviour
{
    [Header("База данных")]
    [Tooltip("Уникальный номер предмета (например: 1 - Фонарик)")]
    public int itemID = 1;
    [Header("Данные для инвентаря")]
    public string itemName = "Предмет";
    public Sprite inventoryIcon;

    [Header("Компоненты предмета")]
    public GameObject visualModel;
    public Light itemLight;

    private Rigidbody rb;
    private Collider[] itemColliders;
    private NetworkTransform netTransform;
    private LightSource aiLightSource;
    public NetworkVariable<bool> isHeld = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isEquipped = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isLightOn = new NetworkVariable<bool>(false);

    public NetworkVariable<ulong> holderPlayerId = new NetworkVariable<ulong>(0);
    private Transform cachedHolderTransform;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Ищет все коллайдеры на самом объекте и на всех вложенных в него
        itemColliders = GetComponentsInChildren<Collider>(true);
        netTransform = GetComponent<NetworkTransform>();

        aiLightSource = GetComponent<LightSource>(); // Находим компонент
    }

    public override void OnNetworkSpawn()
    {
        // Подписываемся на изменения всех важных переменных
        isHeld.OnValueChanged += (oldValue, newValue) => UpdateVisibility();
        isEquipped.OnValueChanged += (oldValue, newValue) => UpdateVisibility();
        isLightOn.OnValueChanged += (oldValue, newValue) => UpdateVisibility();

        // Подписка на изменение ID владельца
        holderPlayerId.OnValueChanged += (oldValue, newValue) =>
        {
            Debug.Log($"[Предмет] Владелец изменился с {oldValue} на {newValue}. Сбрасываю кэш.");
            cachedHolderTransform = null; // Заставляем скрипт искать новую руку в следующем LateUpdate
        };

        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        bool shouldBeVisible = !isHeld.Value || isEquipped.Value;

        if (visualModel != null) visualModel.SetActive(shouldBeVisible);

        bool shouldLightBeOn = shouldBeVisible && isLightOn.Value;
        if (itemLight != null) itemLight.enabled = shouldLightBeOn;
        if (aiLightSource != null) aiLightSource.enabled = shouldLightBeOn;

        if (netTransform != null) netTransform.enabled = !isHeld.Value;
        if (rb != null)
        {
            rb.isKinematic = isHeld.Value;
            rb.useGravity = !isHeld.Value;
        }
        if (itemColliders != null)
        {
            foreach (var col in itemColliders)
            {
                col.enabled = !isHeld.Value;
            }
        }

        cachedHolderTransform = null;
    }

    void LateUpdate()
    {
        // Мягкая привязка каждый кадр
        if (isHeld.Value)
        {
            if (cachedHolderTransform == null)
            {
                FindHolderTransform();
            }

            if (cachedHolderTransform != null)
            {
                transform.position = cachedHolderTransform.position;
                transform.rotation = cachedHolderTransform.rotation;
            }
        }
    }

    private void FindHolderTransform()
    {
        if (holderPlayerId.Value == 0) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(holderPlayerId.Value, out NetworkObject holderObj))
        {
            // Проверка 1: Держит ли предмет Игрок?
            PlayerInventory inv = holderObj.GetComponent<PlayerInventory>();
            if (inv != null)
            {
                cachedHolderTransform = isEquipped.Value ? inv.handTransform : inv.backpackTransform;
                return;
            }

            // Проверка 2: Держит ли предмет Воришка?
            ThiefAI thief = holderObj.GetComponent<ThiefAI>();
            if (thief != null)
            {
                cachedHolderTransform = thief.handTransform;
                return;
            }
        }
    }

    public void Pickup(NetworkObject playerObject, bool equipImmediately)
    {
        if (!IsServer) return;

        NetworkObject.ChangeOwnership(playerObject.OwnerClientId);
        holderPlayerId.Value = playerObject.NetworkObjectId;

        isHeld.Value = true;
        isEquipped.Value = equipImmediately;
    }

    // Обычный метод, который сервер вызывает напрямую
    public void DropItem()
    {
        if (!IsServer) return; // Защита: выполняется только на сервере

        isHeld.Value = false;
        isEquipped.Value = false;
        holderPlayerId.Value = 0;
        NetworkObject.RemoveOwnership();

        if (rb != null) rb.AddForce(transform.forward * 3f, ForceMode.Impulse);
    }

    // --- НОВАЯ СИСТЕМА ИСПОЛЬЗОВАНИЯ ПРЕДМЕТОВ ---

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void UseItemServerRpc()
    {
        if (itemLight != null)
        {
            isLightOn.Value = !isLightOn.Value;
        }

        bool shouldDestroy = false;
        var behaviors = GetComponents<IItemBehavior>();
        foreach (var behavior in behaviors)
        {
            // Если хотя бы один скрипт сказал "я потрачен", ставим флаг на удаление
            if (behavior.OnItemUsedServer())
            {
                shouldDestroy = true;
            }
        }

        if (shouldDestroy)
        {
            // 1. Находим инвентарь владельца (если предмет кто-то держит)
            if (holderPlayerId.Value != 0 && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(holderPlayerId.Value, out NetworkObject holderObj))
            {
                PlayerInventory inv = holderObj.GetComponent<PlayerInventory>();
                if (inv != null)
                {
                    // Сообщаем инвентарю: "Меня больше нет, убери меня из списка!"
                    inv.RemoveItemFromInventory(NetworkObjectId);
                }
            }

            // 2. Теперь безопасно удаляем объект из сети
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
}

public interface IItemBehavior
{
    // Теперь метод возвращает bool (true = предмет потрачен)
    bool OnItemUsedServer();
}