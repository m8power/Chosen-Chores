using UnityEngine;
using Unity.Netcode;

public class PlayerFootstepController : NetworkBehaviour
{
    [Header("Audio Sources")]
    [Tooltip("Источник 2D-звука (слушает только владелец персонажа)")]
    [SerializeField] private AudioSource localAudioSource;

    [Tooltip("Источник 3D-звука (слушают все остальные игроки)")]
    [SerializeField] private AudioSource networkAudioSource;

    [Header("Audio Clips (Поверхности)")]
    [SerializeField] private AudioClip[] defaultSteps;

    [Header("Настройки")]
    [SerializeField] private Transform raycastPoint;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float volumeVariaton = 0.1f;
    [SerializeField] private float pitchVariaton = 0.1f;

    /// <summary>
    /// Этот метод вызывается через Animation Event.
    /// Так как анимация работает у всех клиентов (благодаря твоему PlayerControllerMultiplayer),
    /// этот метод вызовется локально на КАЖДОМ компьютере в нужный кадр.
    /// </summary>
    public void Step()
    {
        // Если объект еще не заспавнен в сети, игнорируем
        if (!IsSpawned) return;

        AudioClip clip = GetClipFromSurface();
        if (clip == null) return;

        // Главная магия разделения звука
        if (IsOwner)
        {
            // Это МОЙ персонаж. Играю звук себе "в голову" (2D)
            localAudioSource.pitch = 1f + Random.Range(-pitchVariaton, pitchVariaton);
            localAudioSource.PlayOneShot(clip, 1f - Random.Range(0f, volumeVariaton));
        }
        else
        {
            // Это ЧУЖОЙ персонаж на моем экране. Играю 3D звук от его ног
            networkAudioSource.pitch = 1f + Random.Range(-pitchVariaton, pitchVariaton);
            networkAudioSource.PlayOneShot(clip, 1f - Random.Range(0f, volumeVariaton));
        }
    }

    private AudioClip GetClipFromSurface()
    {
        // Простая проверка поверхности (можно расширить тегами, как обсуждали ранее)
        if (Physics.Raycast(raycastPoint.position, Vector3.down, out RaycastHit hit, 1.5f, groundLayer))
        {
            if (defaultSteps.Length > 0)
            {
                return defaultSteps[Random.Range(0, defaultSteps.Length)];
            }
        }
        return null;
    }
}