using UnityEngine;
using Unity.Netcode;

public class PlayerHeadAim : NetworkBehaviour
{
    [Header("Связь")]
    public PlayerControllerMultiplayer controller;

    [Header("Кости")]
    public Transform headBone;
    public Transform leftEyeBone;
    public Transform rightEyeBone;

    [Header("Лимиты Головы")]
    public Vector2 headPitchLimit = new Vector2(-25f, 20f); // X (Вверх/Вниз)
    public Vector2 headYawLimit = new Vector2(-30f, 30f);   // Y (Влево/Вправо)
    public Vector2 headRollLimit = new Vector2(-20f, 20f);  // Z (Наклон)

    [Header("Лимиты Глаз (Локальные)")]
    public Vector2 eyePitchLimit = new Vector2(-120f, -60f); // X
    public Vector2 eyeYawLimit = new Vector2(-40f, 40f);     // Y
    public Vector2 eyeRollLimit = new Vector2(-20f, 20f);    // Z (Добавлен лимит по оси Z)
    public Vector3 eyeBaseRotation = new Vector3(90f, 0f, 0f); // Исходное положение 90, 0, 0

    private void LateUpdate()
    {
        if (controller == null || headBone == null) return;

        // 1. Получаем углы поворота от камеры (работает по сети для всех клиентов!)
        float targetPitch = controller.GetHeadPitch();
        float targetYaw = controller.GetHeadYaw();

        // Переменная для наклона головы влево/вправо (ось Z). 
        // В будущем сюда можно передавать угол из контроллера, если добавите наклоны камеры.
        float targetRoll = 0f;

        // --- ПОВОРОТ ГОЛОВЫ ---
        float clampHeadX = Mathf.Clamp(targetPitch, headPitchLimit.x, headPitchLimit.y);
        float clampHeadY = Mathf.Clamp(targetYaw, headYawLimit.x, headYawLimit.y);
        float clampHeadZ = Mathf.Clamp(targetRoll, headRollLimit.x, headRollLimit.y);

        // Умножаем текущий поворот кости (от аниматора) на наш новый угол
        headBone.localRotation *= Quaternion.Euler(clampHeadX, clampHeadY, clampHeadZ);

        if (leftEyeBone != null && rightEyeBone != null)
        {
            // 1. Инвертируем базу (90 становится -90), чтобы попасть в диапазон лимитов
            // 2. Используем + или -, в зависимости от того, куда должны вращаться глаза
            float eyeX = -eyeBaseRotation.x + targetPitch;
            float eyeY = eyeBaseRotation.y + targetYaw;
            float eyeZ = eyeBaseRotation.z; // Оставляем без изменений, если нет наклона

            // ПРОВЕРЬ В ИНСПЕКТОРЕ: 
            // eyePitchLimit.x должен быть -120 (минимум)
            // eyePitchLimit.y должен быть -60 (максимум)
            float clampEyeX = Mathf.Clamp(eyeX, eyePitchLimit.x, eyePitchLimit.y);
            float clampEyeY = Mathf.Clamp(eyeY, eyeYawLimit.x, eyeYawLimit.y);

            Quaternion eyeRot = Quaternion.Euler(clampEyeX, clampEyeY, eyeBaseRotation.z);

            leftEyeBone.localRotation = eyeRot;
            rightEyeBone.localRotation = eyeRot;
        }
    }
}