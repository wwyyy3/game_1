using UnityEngine;

public class EnvManager : MonoBehaviour
{
    public GameObject trainingEnvPrefab;
    public int envCount = 8;
    public float envSpacing = 50f;

    void Start()
    {
        for (int i = 0; i < envCount; i++)
        {
            Vector3 spawnPos = new Vector3(i * envSpacing, 0, 0);
            Instantiate(trainingEnvPrefab, spawnPos, Quaternion.identity);
        }
    }
}