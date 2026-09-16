using UnityEngine;
using UnityEngine.InputSystem;

public class PanelToggler : MonoBehaviour
{
    [Header("Настройки UI")]
    [Tooltip("Перетащите сюда панель (GameObject), которую нужно включать/отключать")]
    [SerializeField] private GameObject panelToToggle;

    // Ссылка на ваш сгенерированный C# класс инпутов
    private PlayerInputActions inputActions;

    private void Awake()
    {
        // Инициализируем наш кастомный класс
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        // Обязательно включаем отслеживание ввода
        inputActions.Enable();
    }

    private void OnDisable()
    {
        // Отключаем отслеживание, чтобы не было утечек памяти и ошибок, 
        // когда объект неактивен
        inputActions.Disable();
    }

    private void Update()
    {
        // ВНИМАНИЕ: Замените "UI" и "TogglePanel" на ваши реальные названия 
        // Action Map и Action из вашего ассета PlayerInputActions.
        if (inputActions.Player.TogglePanel.WasPressedThisFrame())
        {
            TogglePanel();
        }
    }

    private void TogglePanel()
    {
        if (panelToToggle != null)
        {
            // Меняем текущее состояние активности на противоположное
            panelToToggle.SetActive(!panelToToggle.activeSelf);
        }
        else
        {
            Debug.LogWarning("Панель для переключения не назначена в инспекторе!");
        }
    }
}