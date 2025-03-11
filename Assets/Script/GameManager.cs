using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject monsterPrefab;
    [SerializeField] private int monsterCount = 5;
    [SerializeField] private float spawnRadius = 100f;

    // Stores the monster instances in the current scene
    private List<GameObject> spawnedMonsters = new List<GameObject>();

    // ���ڿ��ӻ����Ե����ĵ�
    private Vector3 lastSpawnCenter;

    /// <summary>
    /// Spawns a specified number of monsters.
    /// </summary>
    public void SpawnMonsters(Vector3 center)
    {
        // �������ĵ㣬���� Gizmos ����
        lastSpawnCenter = center;

        DestroyMonsters();

        for (int i = 0; i < monsterCount; i++)
        {
            Vector3 spawnPos = GetRandomNavMeshPosition(center, spawnRadius);
            if (float.IsNaN(spawnPos.x) || float.IsNaN(spawnPos.y) || float.IsNaN(spawnPos.z))
            {
                Debug.LogWarning("Got invalid spawn position, skip creating monster.");
                continue;
            }
            GameObject monster = Instantiate(monsterPrefab, spawnPos, Quaternion.identity,transform);
            spawnedMonsters.Add(monster);
        }
    }

    /// <summary>
    ///  Destroys all spawned monsters.
    /// </summary>
    public void DestroyMonsters()
    {
        foreach (GameObject monster in spawnedMonsters)
        {
            if (monster != null)
            {
                Destroy(monster);
            }
        }
        spawnedMonsters.Clear();
    }

    /// <summary>
    /// Gets a random position on the NavMesh.
    /// </summary>
    private Vector3 GetRandomNavMeshPosition(Vector3 center, float radius)
    {
        //Vector3 randomDirection = Random.insideUnitSphere * radius;
        //randomDirection += center;
        //NavMeshHit navHit;
        //NavMesh.SamplePosition(randomDirection, out navHit, radius, NavMesh.AllAreas);
        //return navHit.position;
        const int maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * radius + center;
            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit navHit, radius, NavMesh.AllAreas))
            {
                // ����ҵ����õ㣬�ͷ���
                return navHit.position;
            }
        }

        // �����γ��Զ�ʧ�ܣ����ﷵ��һ����Чֵ
        Debug.LogWarning($"Failed to find valid NavMesh position after {maxAttempts} attempts.");
        return new Vector3(float.NaN, float.NaN, float.NaN);
    }

    public void SpawnMonsterAtPosition(Vector3 position)
    {
        Instantiate(monsterPrefab, position, Quaternion.identity);
    }

    // ֻҪ�� Scene ��ѡ�и��������ʾ Gizmos���ͻ���ʾ�߿���
    private void OnDrawGizmos()
    {
        // ѡ����ϲ������ɫ
        Gizmos.color = Color.yellow;

        // ��һ���߿��������� lastSpawnCenter���뾶�� spawnRadius
        Gizmos.DrawWireSphere(lastSpawnCenter, spawnRadius);
    }
}