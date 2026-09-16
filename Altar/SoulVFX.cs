using UnityEngine;

public class SoulVFX : MonoBehaviour
{
    private Transform _target;
    private float _finalSpeed; // Максимальная скорость, до которой разгонимся

    // --- НОВЫЕ ПАРАМЕТРЫ ДЛЯ РАЗГОНА ---
    [Header("Movement Phases")]
    [Tooltip("Сколько секунд душа просто висит на месте после появления")]
    [SerializeField] private float pauseDuration = 0.5f;
    [Tooltip("Сколько секунд длится фаза разгона от 0 до полной скорости")]
    [SerializeField] private float accelerationDuration = 1.5f;

    // Внутренние переменные для отслеживания фаз
    private float _currentSpeed = 0f;
    private float _pauseTimer;
    private float _accelerationTimer = 0f;

    // Метод для инициализации (запускается из Алтаря)
    // Теперь Setup принимает финальную скорость
    public void Setup(Transform target, float finalSpeed)
    {
        _target = target;
        _finalSpeed = finalSpeed;

        // Инициализируем таймер паузы
        _pauseTimer = pauseDuration;
        _currentSpeed = 0f; // Начинаем без движения
    }

    private void Update()
    {
        // Если цель пропала, уничтожаем душу
        if (_target == null)
        {
            Destroy(gameObject);
            return;
        }

        // --- ЛОГИКА ДВИЖЕНИЯ ПО ФАЗАМ ---

        // Фаза 1: Пауза/Зависание
        if (_pauseTimer > 0)
        {
            _pauseTimer -= Time.deltaTime;
            // Пока таймер больше нуля, мы не двигаемся
            return;
        }

        // Фаза 2: Медленный разгон (если пауза закончилась)
        if (_accelerationTimer < accelerationDuration)
        {
            _accelerationTimer += Time.deltaTime;

            // Расчитываем текущую скорость.
            // Mathf.Lerp плавно интерполирует от 0 до _finalSpeed 
            // в зависимости от прогресса таймера разгона.
            float progress = _accelerationTimer / accelerationDuration;
            _currentSpeed = Mathf.Lerp(0f, _finalSpeed, progress);
        }
        else
        {
            // Фаза 3: Движение на полной скорости
            _currentSpeed = _finalSpeed;
        }

        // --- НЕПОСРЕДСТВЕННОЕ ДВИЖЕНИЕ ---
        // Двигаем душу с рассчитанной текущей скоростью
        transform.position = Vector3.MoveTowards(transform.position, _target.position, _currentSpeed * Time.deltaTime);

        // Проверяем, достигли ли мы цели
        if (Vector3.Distance(transform.position, _target.position) < 0.1f)
        {
            Destroy(gameObject); // Уничтожаем объект души
        }
    }
}