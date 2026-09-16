using Unity.Netcode;
using UnityEngine;

public class PlayerSkinController : NetworkBehaviour
{
    // Статическая ссылка на локального игрока для быстрого доступа из UI
    public static PlayerSkinController LocalInstance { get; private set; }

    [Tooltip("Перетащи сюда дочерний объект Skin (на котором висит SkinnedMeshRenderer)")]
    [SerializeField] private SkinnedMeshRenderer skinRenderer;

    // Свойство из Shader Graph. 
    // Если цвет не меняется, проверь Reference в шейдере, возможно там "_SkinColor"
    private readonly int _skinColorProp = Shader.PropertyToID("_SkinColor");

    // Синхронизируемая по сети переменная (NetworkVariable)
    // Право на изменение имеет только сервер, но читают все клиенты.
    private NetworkVariable<float> netSkinColor = new NetworkVariable<float>(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        // Если это наш локальный персонаж
        if (IsOwner)
        {
            LocalInstance = this;

            // Загружаем сохраненный цвет из PlayerPrefs и отправляем на сервер
            float savedColor = PlayerPrefs.GetFloat(SettingsMenu.SkinColorPrefKey, 1f);
            SetSkinColorServerRpc(savedColor);
        }

        // Применяем текущий цвет при спавне (для тех, кто уже был на сервере или только подключился)
        ApplyColorToMaterial(netSkinColor.Value);

        // Подписываемся на изменения переменной, чтобы цвет менялся в реальном времени у всех
        netSkinColor.OnValueChanged += (oldValue, newValue) =>
        {
            ApplyColorToMaterial(newValue);
        };
    }

    // Этот метод вызывается из UI SettingsMenu
    public void UpdateSkinColor(float newColor)
    {
        if (IsOwner)
        {
            SetSkinColorServerRpc(newColor);
        }
    }

    // RPC-запрос к серверу для изменения сетевой переменной
    [ServerRpc]
    private void SetSkinColorServerRpc(float newColor)
    {
        netSkinColor.Value = newColor;
    }

    // Визуальное применение материала
    private void ApplyColorToMaterial(float colorValue)
    {
        if (skinRenderer != null)
        {
            // Используем PropertyBlock для оптимизации, чтобы не плодить дубликаты материалов в памяти
            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
            skinRenderer.GetPropertyBlock(propBlock);
            propBlock.SetFloat(_skinColorProp, colorValue);
            skinRenderer.SetPropertyBlock(propBlock);
        }
    }

    public override void OnNetworkDespawn()
    {
        // Очищаем ссылку при выходе/отключении
        if (IsOwner && LocalInstance == this)
        {
            LocalInstance = null;
        }
    }
}