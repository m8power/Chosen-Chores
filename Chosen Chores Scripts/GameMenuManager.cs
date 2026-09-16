using UnityEngine;
using UnityEngine.UI;

public class GameMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    private PlayerInputActions _inputActions;
    private bool _isMenuOpen = false;

    // ИСПРАВЛЕНИЕ: Теперь это публичное свойство, к которому может обращаться скрипт игрока
    public bool IsLocalPlayerSpawned { get; set; } = false;

    private void Awake()
    {
        _inputActions = new PlayerInputActions();
        _inputActions.Enable();

        continueButton.onClick.AddListener(CloseMenu);
        settingsButton.onClick.AddListener(OpenSettings);
        exitButton.onClick.AddListener(ExitGame);

        mainPanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    private void Update()
    {
        // Проверяем само нажатие Esc независимо от игрока
        if (_inputActions.Player.Pause.WasPressedThisFrame())
        {
            Debug.Log($"Esc нажат! Но разрешено ли открытие? IsLocalPlayerSpawned = {IsLocalPlayerSpawned}");

            if (IsLocalPlayerSpawned)
            {
                ToggleMenu();
            }
        }
    }

    private void ToggleMenu()
    {
        _isMenuOpen = !_isMenuOpen;

        if (_isMenuOpen)
        {
            mainPanel.SetActive(true);
            settingsPanel.SetActive(false);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            CloseMenu();
        }
    }

    private void CloseMenu()
    {
        _isMenuOpen = false;
        mainPanel.SetActive(false);
        settingsPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OpenSettings()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    private void ExitGame()
    {
        Debug.Log("Выход из игры...");
        Application.Quit();
    }

    private void OnDestroy()
    {
        if (_inputActions != null)
        {
            _inputActions.Disable();
        }
    }
}