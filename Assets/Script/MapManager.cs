using UnityEngine;

public class MapManager : MonoBehaviour
{
    //[Header("Dynamic Settings")]
    public Vector2 MapSize = new Vector2(120f, 80f);

    //public bool IsPositionInMap(Vector3 position)
    //{
    //    Vector3 localPos = transform.InverseTransformPoint(position);
    //    return Mathf.Abs(localPos.x) < MapSize.x / 2 &&
    //           Mathf.Abs(localPos.z) < MapSize.y / 2;
    //}

    //// ������ͼ����ϵת������
    //public Vector3 MapToWorldPosition(Vector2 mapPosition)
    //{
    //    return transform.TransformPoint(
    //        new Vector3(mapPosition.x, 0, mapPosition.y));
    //}

    //public Vector3 GetGroundPosition(Vector3 position)
    //{
    //    if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f))
    //    {
    //        return hit.point;
    //    }
    //    return position;
    //}

    public bool CheckObstacleCollision(Vector3 position, float radius)
    {
        return Physics.CheckSphere(position, radius, LayerMask.GetMask("Obstacle"));
    }

    //public Bounds mapBounds;
    //void Awake()
    //{
    //    mapBounds = new Bounds(Vector3.zero, new Vector3(MapSize.x, 100f, MapSize.y));
    //}
    //public bool IsPositionInMap(Vector3 position)
    //{
    //    return mapBounds.Contains(position);
    //}


    [Header("Dynamic Settings")]
    [SerializeField] private Vector2 MapSizeXZ = new Vector2(120f, 80f);
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float raycastMaxDistance = 500f;

    private Bounds mapBounds;

    void Awake()
    {
        // ��ʼ����ͼ�߽磨�����ͼ������ԭ�㣩
        mapBounds = new Bounds(
            Vector3.zero,
            new Vector3(MapSizeXZ.x, 20f, MapSizeXZ.y) // �߶���Ϊ100�ף��ɸ���ʵ�ʵ�����
        );
    }

    public bool IsPositionInMap(Vector3 position)
    {
        return mapBounds.Contains(position);
    }

    public Vector3 MapToWorldPosition(Vector2 mapXZPosition)
    {
        return transform.TransformPoint(
            new Vector3(mapXZPosition.x, 0, mapXZPosition.y)
        );
    }

    public Vector3 GetGroundPosition(Vector3 position)
    {
        Vector3 rayStart = position + Vector3.up * 100f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastMaxDistance, groundLayer))
        {
            return hit.point;
        }

        Debug.LogError($"������ʧ�ܣ����꣺{position}");
        return position; // ���׳��쳣��throw new System.InvalidOperationException();
    }
}