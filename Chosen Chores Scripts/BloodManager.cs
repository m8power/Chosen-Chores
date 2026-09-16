using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BloodManager : NetworkBehaviour
{
    public static BloodManager Instance;

    public struct BloodNode
    {
        public Vector3 Position;
        public float Timestamp;
        public ulong PlayerId;
    }

    private List<BloodNode> activeBloodNodes = new List<BloodNode>();
    public float bloodLifespan = 30f; // Через сколько ИИ перестает чуять кровь

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Update()
    {
        if (!IsServer) return;

        // Удаляем старые следы, чтобы не засорять память
        float currentTime = Time.time;
        activeBloodNodes.RemoveAll(node => currentTime - node.Timestamp > bloodLifespan);
    }

    // ВЫЗЫВАЕТСЯ СЕРВЕРОМ: Добавить след
    public void AddBloodNode(Vector3 position, ulong playerId)
    {
        if (!IsServer) return;
        activeBloodNodes.Add(new BloodNode { Position = position, Timestamp = Time.time, PlayerId = playerId });
    }

    // ВЫЗЫВАЕТСЯ ИИ (Сервером): Найти самую свежую кровь в радиусе
    public bool TryGetFreshestBloodInRadius(Vector3 searchPosition, float radius, out Vector3 targetPos)
    {
        targetPos = Vector3.zero;
        if (!IsServer || activeBloodNodes.Count == 0) return false;

        float maxTime = -1f;
        bool found = false;

        foreach (var node in activeBloodNodes)
        {
            float dist = Vector3.Distance(searchPosition, node.Position);
            if (dist <= radius)
            {
                // Ищем самую СВЕЖУЮ каплю (с наибольшим Timestamp)
                if (node.Timestamp > maxTime)
                {
                    maxTime = node.Timestamp;
                    targetPos = node.Position;
                    found = true;
                }
            }
        }
        return found;
    }
  
    private void OnDrawGizmos()
    {
        if (activeBloodNodes == null || activeBloodNodes.Count == 0) return;

        Gizmos.color = Color.red;
        foreach (var node in activeBloodNodes)
        {
            // Рисуем красную сферу там, где сервер запомнил кровь
            Gizmos.DrawSphere(node.Position, 0.2f);
        }
    }
}