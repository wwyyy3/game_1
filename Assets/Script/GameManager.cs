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

    private Vector3 lastSpawnCenter;

    /// <summary>
    /// Spawns a specified number of monsters.
    /// </summary>
    public void SpawnMonsters(Vector3 center)
    {
        lastSpawnCenter = center;

        DestroyMonsters();

        for (int i = 0; i < monsterCount; i++)
        {
            Vector3 spawnPos = GetRandomNavMeshPosition(center, spawnRadius);
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
        const int maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 randomDirection = Random.insideUnitSphere * radius + center;
            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit navHit, radius, NavMesh.AllAreas))
            {
                return navHit.position;
            }
        }
        return new Vector3(float.NaN, float.NaN, float.NaN);
    }

    public void SpawnMonsterAtPosition(Vector3 position)
    {
        Instantiate(monsterPrefab, position, Quaternion.identity);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(lastSpawnCenter, spawnRadius);
    }
}