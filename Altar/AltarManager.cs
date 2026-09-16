using Unity.Netcode;
using UnityEngine;

public class AltarManager : NetworkBehaviour
{
    public static AltarManager Instance { get; private set; }

    // Глобальная переменная. Меняет сервер, видят все клиенты.
    public NetworkVariable<int> SacrificedCount = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Вызывает сервер при успешном жертвоприношении (из скрипта игрока)
    public void AddSacrifice()
    {
        if (IsServer)
        {
            SacrificedCount.Value++;
        }
    }
}