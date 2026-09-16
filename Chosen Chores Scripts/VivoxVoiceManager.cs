using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Vivox;

public class VivoxVoiceManager : MonoBehaviour
{
    public static VivoxVoiceManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public async void InitializePushToTalk()
    {
        int attempts = 0;
        while (!VivoxService.Instance.IsLoggedIn && attempts < 50)
        {
            await Task.Delay(200);
            attempts++;
        }

        if (VivoxService.Instance.IsLoggedIn)
        {
            MuteMicrophone(true);
            Debug.Log("<color=magenta>[Рация]</color> Интеграция с лобби успешна. Готово!");
        }
    }

    public void MuteMicrophone(bool isMuted)
    {
        if (VivoxService.Instance != null && VivoxService.Instance.IsLoggedIn)
        {
            if (isMuted) VivoxService.Instance.MuteInputDevice();
            else VivoxService.Instance.UnmuteInputDevice();
        }
    }
}