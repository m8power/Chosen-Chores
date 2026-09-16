using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Collider))]
public class AltarActivationZone : NetworkBehaviour
{
    [Header("Debug")]
    [Tooltip("Включите это для тестов в одиночку. Зона будет всегда считаться активной.")]
    public bool debugForceActivate = false; // НОВОЕ

    private PlayerControllerMultiplayer _currentPlayerInZone;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerControllerMultiplayer>();
            if (player != null && _currentPlayerInZone == null)
            {
                _currentPlayerInZone = player;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerControllerMultiplayer>();
            if (player != null && _currentPlayerInZone == player)
            {
                _currentPlayerInZone = null;
            }
        }
    }

    public bool IsPlayerReadyAndCasting()
    {
        // НОВОЕ: Если включен дебаг, зона всегда говорит "Да, игрок тут и он кастует"
        if (debugForceActivate) return true;

        if (_currentPlayerInZone == null) return false;

        return _currentPlayerInZone.IsCasting;
    }
}