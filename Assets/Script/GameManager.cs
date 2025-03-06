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

    /// <summary>
    /// Spawns a specified number of monsters.
    /// </summary>
    public void SpawnMonsters(Vector3 center)
    {
        DestroyMonsters(); 

        for (int i = 0; i < monsterCount; i++)
        {
            Vector3 spawnPos = GetRandomNavMeshPosition(center, spawnRadius);
            GameObject monster = Instantiate(monsterPrefab, spawnPos, Quaternion.identity);
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
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += center;
        NavMeshHit navHit;
        NavMesh.SamplePosition(randomDirection, out navHit, radius, NavMesh.AllAreas);
        return navHit.position;
    }
}
