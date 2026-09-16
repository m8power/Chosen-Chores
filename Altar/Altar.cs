using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.AI;

public class Altar : NetworkBehaviour
{
    public enum AltarState
    {
        Idle,
        Activating,
        Absorbing,
        Deactivated
    }

    [Header("Settings")]
    [Tooltip("Радиус захвата врагов")]
    [SerializeField] private float captureRadius = 4f;
    [Tooltip("Время совместного удержания Q (в сек)")]
    [SerializeField] private float timeToActivate = 3f;
    [Tooltip("Время поглощения парализованных врагов (в сек)")]
    [SerializeField] private float timeToAbsorb = 3f;

    [Header("References")]
    [SerializeField] private AltarActivationZone zone1;
    [SerializeField] private AltarActivationZone zone2;
    [SerializeField] private GameObject zonesVisuals;
    [SerializeField] private Animator animator;

    [Header("VFX")]
    [SerializeField] private SoulVFX soulPrefab;
    [SerializeField] private Transform altarCenter;

    [Tooltip("До какого масштаба (Scale) вырастет центр")]
    [SerializeField] private float maxCenterScale = 2f;
    [Tooltip("Скорость появления/затухания (1 = вырастет на 1 единицу за 1 секунду)")]
    [SerializeField] private float centerScaleSpeed = 0.5f;

    [SerializeField] private float soulSpeed = 5f;

    public NetworkVariable<AltarState> CurrentState = new NetworkVariable<AltarState>(AltarState.Idle);

    private List<GameObject> _paralyzedEnemies = new List<GameObject>();
    private List<GameObject> _fallingEnemies = new List<GameObject>();

    private Dictionary<GameObject, float> _enemyTargetHeights = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, float> _enemyStartHeights = new Dictionary<GameObject, float>();

    private float _timer = 0f;

    private void Awake()
    {
        if (altarCenter != null)
        {
            altarCenter.localScale = Vector3.zero;
            altarCenter.gameObject.SetActive(false);
        }
    }

    public override void OnNetworkSpawn()
    {
        CurrentState.OnValueChanged += OnStateChanged;
        UpdateAnimation(CurrentState.Value);
    }

    public override void OnNetworkDespawn()
    {
        CurrentState.OnValueChanged -= OnStateChanged;
    }

    private void OnStateChanged(AltarState previousState, AltarState newState)
    {
        UpdateAnimation(newState);
    }

    private void UpdateAnimation(AltarState state)
    {
        if (animator == null) return;

        switch (state)
        {
            case AltarState.Idle:
                animator.CrossFade("Altar_Idle", 0.1f);
                break;
            case AltarState.Activating:
                animator.CrossFade("Altar_Activating", 0.1f);
                break;
            case AltarState.Absorbing:
                animator.CrossFade("Altar_Activated", 0.1f);
                break;
            case AltarState.Deactivated:
                animator.CrossFade("Altar_Idle", 0.1f);
                break;
        }
    }

    private void Update()
    {
        if (altarCenter != null)
        {
            bool shouldBeVisible = (CurrentState.Value == AltarState.Activating || CurrentState.Value == AltarState.Absorbing);
            float targetScale = shouldBeVisible ? maxCenterScale : 0f;

            if (shouldBeVisible && !altarCenter.gameObject.activeSelf)
            {
                altarCenter.gameObject.SetActive(true);
            }

            altarCenter.localScale = Vector3.MoveTowards(altarCenter.localScale, Vector3.one * targetScale, Time.deltaTime * centerScaleSpeed);

            if (!shouldBeVisible && altarCenter.localScale.x <= 0.001f && altarCenter.gameObject.activeSelf)
            {
                altarCenter.localScale = Vector3.zero;
                altarCenter.gameObject.SetActive(false);
            }
        }

        if (!IsServer) return;

        if (CurrentState.Value == AltarState.Idle || CurrentState.Value == AltarState.Activating)
        {
            if (zone1.IsPlayerReadyAndCasting() && zone2.IsPlayerReadyAndCasting())
            {
                if (CurrentState.Value == AltarState.Idle)
                {
                    CurrentState.Value = AltarState.Activating;
                    _timer = 0f;
                }

                Collider[] hits = Physics.OverlapSphere(transform.position, captureRadius);
                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Enemy"))
                    {
                        GameObject enemy = hit.gameObject;

                        if (!_paralyzedEnemies.Contains(enemy))
                        {
                            _paralyzedEnemies.Add(enemy);

                            if (_fallingEnemies.Contains(enemy))
                            {
                                _fallingEnemies.Remove(enemy);
                            }
                            else
                            {
                                _enemyStartHeights[enemy] = enemy.transform.position.y;
                                var agent = enemy.GetComponent<NavMeshAgent>();
                                if (agent != null) agent.updatePosition = false;
                            }

                            _enemyTargetHeights[enemy] = _enemyStartHeights[enemy] + 1.2f;

                            var enemyScript = enemy.GetComponent<EnemyController>();
                            if (enemyScript != null) enemyScript.SetParalyzed(true);

                            var stateMachineEnemy = enemy.GetComponent<EnemyStateMachine>();
                            if (stateMachineEnemy != null) stateMachineEnemy.SetParalyzed(true);
                        }
                    }
                }

                _timer += Time.deltaTime;
                if (_timer >= timeToActivate)
                {
                    StartAbsorptionPhase();
                }
            }
            else
            {
                if (CurrentState.Value == AltarState.Activating)
                {
                    CurrentState.Value = AltarState.Idle;
                    _timer = 0f;

                    foreach (var enemy in _paralyzedEnemies)
                    {
                        if (enemy != null)
                        {
                            _fallingEnemies.Add(enemy);

                            // --- НОВОЕ: Если ритуал прервали, сбрасываем эффект растворения (на всякий случай) ---
                            var dissolve = enemy.GetComponent<EnemyDissolve>();
                            if (dissolve != null) dissolve.StopAndResetDissolveRpc();
                        }
                    }
                    _paralyzedEnemies.Clear();
                }
            }
        }
        else if (CurrentState.Value == AltarState.Absorbing)
        {
            _timer += Time.deltaTime;
            if (_timer >= timeToAbsorb)
            {
                CompleteRitual();
            }
        }

        ProcessLevitation();
        ProcessFalling();
    }

    private void ProcessLevitation()
    {
        foreach (var enemy in _paralyzedEnemies)
        {
            if (enemy != null && _enemyTargetHeights.TryGetValue(enemy, out float targetY))
            {
                Vector3 pos = enemy.transform.position;
                pos.y = Mathf.MoveTowards(pos.y, targetY, 1f * Time.deltaTime);
                enemy.transform.position = pos;
            }
        }
    }

    private void ProcessFalling()
    {
        for (int i = _fallingEnemies.Count - 1; i >= 0; i--)
        {
            var enemy = _fallingEnemies[i];
            if (enemy == null)
            {
                _fallingEnemies.RemoveAt(i);
                continue;
            }

            if (_enemyStartHeights.TryGetValue(enemy, out float groundY))
            {
                Vector3 pos = enemy.transform.position;
                pos.y = Mathf.MoveTowards(pos.y, groundY, 2f * Time.deltaTime);
                enemy.transform.position = pos;

                if (Mathf.Abs(pos.y - groundY) < 0.01f)
                {
                    var agent = enemy.GetComponent<NavMeshAgent>();
                    if (agent != null) agent.updatePosition = true;

                    var enemyScript = enemy.GetComponent<EnemyController>();
                    if (enemyScript != null) enemyScript.SetParalyzed(false);

                    var stateMachineEnemy = enemy.GetComponent<EnemyStateMachine>();
                    if (stateMachineEnemy != null) stateMachineEnemy.SetParalyzed(false);

                    _fallingEnemies.RemoveAt(i);
                    _enemyStartHeights.Remove(enemy);
                    _enemyTargetHeights.Remove(enemy);
                }
            }
        }
    }

    private void StartAbsorptionPhase()
    {
        CurrentState.Value = AltarState.Absorbing;
        _timer = 0f;

        if (zonesVisuals != null) DisableZonesClientRpc();

        foreach (var enemy in _paralyzedEnemies)
        {
            if (enemy != null)
            {
                var dissolve = enemy.GetComponent<EnemyDissolve>();
                if (dissolve != null)
                {
                    // ФИКС: Умножаем время на 1.15f (замедляем шейдер на 15%).
                    // Теперь душа заспавнится ДО того, как враг полностью станет прозрачным!
                    dissolve.StartDissolveRpc(timeToAbsorb * 1.3f);
                }
            }
        }
    }

    private void CompleteRitual()
    {
        CurrentState.Value = AltarState.Deactivated;

        foreach (var enemy in _paralyzedEnemies)
        {
            if (enemy != null)
            {
                Vector3 enemyPos = enemy.transform.position;
                enemyPos.y += 0.5f;
                SpawnSoulVFXClientRpc(enemyPos);

                NetworkObject netObj = enemy.GetComponent<NetworkObject>();

                if (netObj != null && netObj.IsSpawned) netObj.Despawn(true);
                else Destroy(enemy);

                if (AltarManager.Instance != null) AltarManager.Instance.AddSacrifice();
            }
        }

        _paralyzedEnemies.Clear();
        _fallingEnemies.Clear();
        _enemyStartHeights.Clear();
        _enemyTargetHeights.Clear();

        Collider[] hits = Physics.OverlapSphere(transform.position, captureRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                GameObject sacrificedPlayer = hit.gameObject;

                Vector3 playerPos = sacrificedPlayer.transform.position;
                playerPos.y += 1.5f;
                SpawnSoulVFXClientRpc(playerPos);

                var stats = sacrificedPlayer.GetComponent<PlayerStats>();
                if (stats != null) stats.ApplyDamage(9999f);

                if (AltarManager.Instance != null) AltarManager.Instance.AddSacrifice();
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    private void DisableZonesClientRpc()
    {
        if (zonesVisuals != null) zonesVisuals.SetActive(false);
    }

    [Rpc(SendTo.Everyone)]
    private void SpawnSoulVFXClientRpc(Vector3 startPosition)
    {
        if (soulPrefab != null && altarCenter != null)
        {
            SoulVFX newSoul = Instantiate(soulPrefab, startPosition, Quaternion.identity);
            newSoul.Setup(altarCenter, soulSpeed);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.6f, 0.1f, 0.8f, 0.2f);
        Gizmos.DrawSphere(transform.position, captureRadius);
        Gizmos.color = new Color(0.6f, 0.1f, 0.8f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, captureRadius);
    }
}