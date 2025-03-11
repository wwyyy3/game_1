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

    // 用于可视化调试的中心点
    private Vector3 lastSpawnCenter;

    /// <summary>
    /// Spawns a specified number of monsters.
    /// </summary>
    public void SpawnMonsters(Vector3 center)
    {
        // 更新中心点，方便 Gizmos 绘制
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
                // 如果找到可用点，就返回
                return navHit.position;
            }
        }

        // 如果多次尝试都失败，这里返回一个无效值
        Debug.LogWarning($"Failed to find valid NavMesh position after {maxAttempts} attempts.");
        return new Vector3(float.NaN, float.NaN, float.NaN);
    }

    public void SpawnMonsterAtPosition(Vector3 position)
    {
        Instantiate(monsterPrefab, position, Quaternion.identity);
    }

    // 只要在 Scene 中选中该物体或显示 Gizmos，就会显示线框球
    private void OnDrawGizmos()
    {
        // 选择你喜欢的颜色
        Gizmos.color = Color.yellow;

        // 画一个线框球，中心是 lastSpawnCenter，半径是 spawnRadius
        Gizmos.DrawWireSphere(lastSpawnCenter, spawnRadius);
    }
}