using Unity.Netcode;
using UnityEngine;
using System;

public class PlayerStats : NetworkBehaviour
{
    [Header("Здоровье (Сетевое)")]
    // Писать может только сервер, читают все (чтобы видеть здоровье товарищей, если понадобится)
    public NetworkVariable<float> Health = new NetworkVariable<float>(
        100f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    [Header("Выносливость (Локальная)")]
    public float maxStamina = 100f;
    public float currentStamina;

    // События для обновления UI
    public Action<float> OnHealthChanged;
    public Action<float> OnStaminaChanged;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            currentStamina = maxStamina;
            // Вызываем ивент при старте, чтобы UI обновился
            OnHealthChanged?.Invoke(Health.Value);
            OnStaminaChanged?.Invoke(currentStamina);
        }

        // Подписываемся на изменение здоровья с сервера
        Health.OnValueChanged += (oldVal, newVal) => 
        {
            if (IsOwner) OnHealthChanged?.Invoke(newVal);
        };
    }
    public void ApplyDamage(float damage)
    {
        if (!IsServer) return; // Защита: менять NetworkVariable здоровья может ТОЛЬКО сервер

        Health.Value = Mathf.Clamp(Health.Value - damage, 0, 100f);
        if (Health.Value <= 0)
        {
            Die();
        }
    }

    // --- УПРАВЛЕНИЕ ЗДОРОВЬЕМ (Сервер) ---
    [Rpc(SendTo.Server)]
    public void TakeDamageServerRpc(float damage)
    {
        ApplyDamage(damage); // Просто перенаправляем вызов
    }

    private void Die()
    {
        Debug.Log($"[Сервер] Игрок {OwnerClientId} погиб!");
        // Здесь мы позже добавим рэгдолл или экран смерти
    }

    // --- УПРАВЛЕНИЕ СТАМИНОЙ (Клиент) ---
    public void UpdateStamina(float amount)
    {
        if (!IsOwner) return;

        float oldStamina = currentStamina;
        currentStamina = Mathf.Clamp(currentStamina + amount, 0, maxStamina);

        if (currentStamina != oldStamina)
        {
            OnStaminaChanged?.Invoke(currentStamina);
        }
    }
}