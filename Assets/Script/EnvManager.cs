using UnityEngine;

public class EnvManager : MonoBehaviour
{
    [Header("Environment Settings")]
    [SerializeField] private GameObject envPrefab; // ����Ԥ����
    [SerializeField] private int numEnvironments = 8; // ��������
    [SerializeField] private float envSpacing = 50f; // �������

    void Start()
    {
        // ���ɶ������ʵ��
        for (int i = 0; i < numEnvironments; i++)
        {
            Vector3 spawnPos = new Vector3(i * envSpacing, 0, 0); // ���������
            Instantiate(envPrefab, spawnPos, Quaternion.identity);
        }
    }
}