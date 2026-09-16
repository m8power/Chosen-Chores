using Unity.Netcode;
using UnityEngine;

public class PlayerTeleporter : NetworkBehaviour
{
    // Атрибут Unity 6: Этот метод будет вызван сервером, 
    // но выполнится ТОЛЬКО на компьютере владельца этого персонажа
    [Rpc(SendTo.Owner)]
    public void TeleportPlayerRpc(Vector3 newPosition, Quaternion newRotation)
    {
        // 1. Отключаем CharacterController (если есть), иначе он заблокирует телепортацию
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // 2. Сбрасываем инерцию Rigidbody (если используется)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 3. Переносим персонажа
        transform.position = newPosition;
        transform.rotation = newRotation;

        // 4. Включаем физику обратно
        if (cc != null) cc.enabled = true;

        Debug.Log($"[Клиент] Я успешно телепортировался на точку: {newPosition}");
    }
}