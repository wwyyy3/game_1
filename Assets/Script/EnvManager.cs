using UnityEngine;

public class EnvManager : MonoBehaviour
{
    [Header("Environment Settings")]
    [SerializeField] private GameObject envPrefab; // 环境预制体
    [SerializeField] private int numEnvironments = 8; // 环境数量
    [SerializeField] private float envSpacing = 50f; // 环境间距

    void Start()
    {
        // 生成多个环境实例
        for (int i = 0; i < numEnvironments; i++)
        {
            Vector3 spawnPos = new Vector3(i * envSpacing, 0, 0); // 按间距排列
            Instantiate(envPrefab, spawnPos, Quaternion.identity);
        }
    }
}