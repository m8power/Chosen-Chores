using UnityEngine;
using Unity.Netcode;

public class PlayerVoiceChat : NetworkBehaviour
{
    private PlayerInputActions inputActions;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        inputActions = new PlayerInputActions();
        inputActions.Enable();

        inputActions.Player.VoiceChat.started += ctx => StartTalking();
        inputActions.Player.VoiceChat.canceled += ctx => StopTalking();

        if (VivoxVoiceManager.Instance != null)
        {
            VivoxVoiceManager.Instance.InitializePushToTalk();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        if (inputActions != null)
        {
            inputActions.Player.VoiceChat.started -= ctx => StartTalking();
            inputActions.Player.VoiceChat.canceled -= ctx => StopTalking();
            inputActions.Disable();
        }
    }

    private void StartTalking()
    {
        if (VivoxVoiceManager.Instance != null)
        {
            VivoxVoiceManager.Instance.MuteMicrophone(false);
            Debug.Log("<color=cyan>[Рация]</color> ВЕЩАНИЕ (Микрофон включен)");
        }
    }

    private void StopTalking()
    {
        if (VivoxVoiceManager.Instance != null)
        {
            VivoxVoiceManager.Instance.MuteMicrophone(true);
            Debug.Log("<color=cyan>[Рация]</color> КОНЕЦ СВЯЗИ (Микрофон выключен)");
        }
    }
}