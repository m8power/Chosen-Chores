using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioMixer mainMixer;
    [SerializeField] private Slider volumeSlider;
    private const string VolumePrefKey = "MasterVolume";

    [Header("Graphics Settings")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Voice Chat Settings")]
    [SerializeField] private TMP_Dropdown microphoneDropdown;
    private const string MicPrefKey = "SelectedMicrophone";

    [Header("Character Customization")]
    [SerializeField] private Slider skinColorSlider;
    [Tooltip("Картинка в UI, которая будет показывать выбранный цвет")]
    [SerializeField] private Image colorPreviewImage;
    [Tooltip("Градиент для палитры цветов скинов")]
    [SerializeField] private Gradient skinColorGradient;
    public const string SkinColorPrefKey = "PlayerSkinColor";

    private Resolution[] _resolutions;
    private string[] _micDevices;

    private void Start()
    {
        // ... (Инициализация звука, графики и микрофона остается без изменений) ...

        fullscreenToggle.isOn = Screen.fullScreen;
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        float savedVolume = PlayerPrefs.GetFloat(VolumePrefKey, 0.75f);
        volumeSlider.value = savedVolume;
        SetVolume(savedVolume);
        volumeSlider.onValueChanged.AddListener(SetVolume);

        SetupResolutions();
        SetupMicrophones();

        // Инициализация цвета кожи
        if (skinColorSlider != null)
        {
            float savedSkinColor = PlayerPrefs.GetFloat(SkinColorPrefKey, 1f);
            skinColorSlider.value = savedSkinColor;
            skinColorSlider.onValueChanged.AddListener(SetSkinColor);
        }
    }

    #region Звук
    public void SetVolume(float volume)
    {
        float dbVolume = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
        mainMixer.SetFloat("Master", dbVolume);
        PlayerPrefs.SetFloat(VolumePrefKey, volume);
    }
    #endregion

    #region Графика
    private void SetupResolutions()
    {
        Resolution[] allResolutions = Screen.resolutions;
        List<Resolution> filteredResolutions = new List<Resolution>();

        for (int i = 0; i < allResolutions.Length; i++)
        {
            int existingIndex = filteredResolutions.FindIndex(r => r.width == allResolutions[i].width && r.height == allResolutions[i].height);
            if (existingIndex != -1)
            {
                if (allResolutions[i].refreshRateRatio.value > filteredResolutions[existingIndex].refreshRateRatio.value)
                {
                    filteredResolutions[existingIndex] = allResolutions[i];
                }
            }
            else
            {
                filteredResolutions.Add(allResolutions[i]);
            }
        }

        _resolutions = filteredResolutions.ToArray();
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < _resolutions.Length; i++)
        {
            int hz = Mathf.RoundToInt((float)_resolutions[i].refreshRateRatio.value);
            options.Add($"{_resolutions[i].width} x {_resolutions[i].height} ({hz}Hz)");

            if (_resolutions[i].width == Screen.currentResolution.width &&
                _resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        int savedResIndex = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);
        resolutionDropdown.value = savedResIndex < _resolutions.Length ? savedResIndex : currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = _resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
    #endregion

    #region Устройства ввода (Микрофон)
    private void SetupMicrophones()
    {
        microphoneDropdown.ClearOptions();
        _micDevices = Microphone.devices;

        if (_micDevices.Length == 0)
        {
            microphoneDropdown.AddOptions(new List<string> { "Микрофон не найден" });
            microphoneDropdown.interactable = false;
            return;
        }

        List<string> options = new List<string>();
        int currentMicIndex = 0;
        string savedMic = PlayerPrefs.GetString(MicPrefKey, "");

        for (int i = 0; i < _micDevices.Length; i++)
        {
            options.Add(_micDevices[i]);
            if (_micDevices[i] == savedMic) currentMicIndex = i;
        }

        microphoneDropdown.AddOptions(options);
        microphoneDropdown.value = currentMicIndex;
        microphoneDropdown.RefreshShownValue();
        SetMicrophone(currentMicIndex);
        microphoneDropdown.onValueChanged.AddListener(SetMicrophone);
    }

    public void SetMicrophone(int micIndex)
    {
        if (_micDevices.Length > 0 && micIndex < _micDevices.Length)
        {
            PlayerPrefs.SetString(MicPrefKey, _micDevices[micIndex]);
        }
    }
    #endregion

    #region Кастомизация персонажа
    public void SetSkinColor(float value)
    {
        // 1. Сохраняем оригинальное значение (от 1 до 7)
        PlayerPrefs.SetFloat(SkinColorPrefKey, value);

        // 2. Обновляем картинку в UI
        if (colorPreviewImage != null && skinColorGradient != null)
        {
            // Переводим 1-7 в 0-1 только для того, чтобы градиент смог выдать цвет
            float normalizedValue = (value - skinColorSlider.minValue) / (skinColorSlider.maxValue - skinColorSlider.minValue);
            colorPreviewImage.color = skinColorGradient.Evaluate(normalizedValue);
        }

        // 3. Передаем на сервер оригинальное значение (1, 2, 3... 7), так как шейдер ждет именно его
        if (PlayerSkinController.LocalInstance != null)
        {
            PlayerSkinController.LocalInstance.UpdateSkinColor(value);
        }
    }
    #endregion

    private void OnDestroy()
    {
        PlayerPrefs.Save();
    }
}