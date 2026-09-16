using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class PlayerStatsUI : MonoBehaviour
{
    [Header("UI Элементы")]
    [Tooltip("Текст для здоровья (TextMeshPro)")]
    public TMP_Text healthText;

    [Tooltip("Картинка стамины (Image Type: Filled)")]
    public Image staminaFill;

    [Header("Altar UI")]
    [SerializeField] private TextMeshProUGUI _sacrificedCounterText;

    // Внутренние ссылки
    private PlayerStats localPlayerStats;
    private float maxHealth = 100f;

    // Флаг, чтобы подписаться на Алтарь только один раз
    private bool _isSubscribedToAltar = false;

    private void Start()
    {
        // Start оставляем пустым, вся инициализация сети будет в Update
    }

    void Update()
    {
        // Ждем, пока клиент подключится и игрок появится на сцене
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            // 1. Инициализация локального игрока (если еще не нашли)
            if (localPlayerStats == null)
            {
                var playerObj = NetworkManager.Singleton.LocalClient.PlayerObject;
                if (playerObj != null)
                {
                    localPlayerStats = playerObj.GetComponent<PlayerStats>();

                    if (localPlayerStats != null)
                    {
                        localPlayerStats.OnHealthChanged += UpdateHealthUI;
                        localPlayerStats.OnStaminaChanged += UpdateStaminaUI;

                        UpdateHealthUI(localPlayerStats.Health.Value);
                        UpdateStaminaUI(localPlayerStats.currentStamina);
                    }
                }
            }

            // 2. Инициализация Алтаря (если еще не подписались и он уже существует на сцене)
            if (!_isSubscribedToAltar && AltarManager.Instance != null)
            {
                AltarManager.Instance.SacrificedCount.OnValueChanged += UpdateSacrificeUI;
                UpdateSacrificeUI(0, AltarManager.Instance.SacrificedCount.Value);

                _isSubscribedToAltar = true; // Запоминаем, что подписались
            }
        }
    }

    private void UpdateHealthUI(float currentHealth)
    {
        if (healthText != null)
        {
            int hp = Mathf.CeilToInt(currentHealth);
            healthText.text = $"{hp} / {maxHealth}";

            if (currentHealth <= 30f)
                healthText.color = Color.red;
            else
                healthText.color = Color.white;
        }
    }

    private void UpdateStaminaUI(float currentStamina)
    {
        if (staminaFill != null && localPlayerStats != null)
        {
            float fillPercentage = currentStamina / localPlayerStats.maxStamina;
            staminaFill.fillAmount = fillPercentage;
        }
    }

    private void OnDestroy()
    {
        if (localPlayerStats != null)
        {
            localPlayerStats.OnHealthChanged -= UpdateHealthUI;
            localPlayerStats.OnStaminaChanged -= UpdateStaminaUI;
        }

        // Отписываемся от Алтаря, только если успели подписаться
        if (_isSubscribedToAltar && AltarManager.Instance != null)
        {
            AltarManager.Instance.SacrificedCount.OnValueChanged -= UpdateSacrificeUI;
        }
    }

    private void UpdateSacrificeUI(int previousValue, int newValue)
    {
        if (_sacrificedCounterText != null)
        {
            _sacrificedCounterText.text = $"Жертв: {newValue}/5";
        }
    }
}