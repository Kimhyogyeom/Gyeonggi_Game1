using UnityEngine;

/// <summary>
/// 벌이 지나간 자리에 꽃을 생성하는 스크립트입니다.
/// 벌 오브젝트에 부착합니다.
/// [최적화] ObjectPool을 사용하여 Instantiate/Destroy 호출 최소화
/// </summary>
namespace SeongWon
{

public class BeeFlowerTrail : MonoBehaviour
{
    [Header("Flower Settings")]
    [Tooltip("생성할 꽃 프리팹 (ScrollingObject 스크립트가 붙어있어야 함)")]
    public GameObject flowerPrefab;

    [Tooltip("꽃이 생성될 부모 Transform (보통 배경 오브젝트들이 있는 컨테이너)")]
    public Transform flowerContainer;

    [Tooltip("꽃 생성 간격 (초 단위)")]
    public float spawnInterval = 0.5f;

    [Tooltip("꽃 생성 위치 오프셋 (벌의 중심에서 약간 뒤/아래로 조정)")]
    public Vector2 spawnOffset = Vector2.zero;
    
    [Header("Object Pooling")]
    [Tooltip("초기 풀 크기")]
    public int initialPoolSize = 30;
    
    [Tooltip("최대 풀 크기")]
    public int maxPoolSize = 50;

    private float timer = 0f;
    private RectTransform rectTransform;
    private ObjectPool flowerPool;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // 오브젝트 풀 생성
        if (flowerPrefab != null)
        {
            GameObject poolObject = new GameObject("FlowerPool");
            poolObject.transform.SetParent(flowerContainer != null ? flowerContainer : transform.parent);
            
            flowerPool = poolObject.AddComponent<ObjectPool>();
            flowerPool.prefab = flowerPrefab;
            flowerPool.initialPoolSize = initialPoolSize;
            flowerPool.maxPoolSize = maxPoolSize;
            
            // 프리팹에 PooledObject 컴포넌트가 없으면 추가
            if (flowerPrefab.GetComponent<PooledObject>() == null)
            {
                flowerPrefab.AddComponent<PooledObject>();
            }
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnFlower();
            timer = 0f;
        }
    }

    void SpawnFlower()
    {
        if (flowerPool == null || rectTransform == null) return;

        // 풀에서 꽃 가져오기 (Instantiate 대신)
        GameObject newFlower = flowerPool.GetObject();
        if (newFlower == null) return;
        
        // 위치 설정 (벌의 현재 위치 + 오프셋)
        RectTransform flowerRect = newFlower.GetComponent<RectTransform>();
        if (flowerRect != null)
        {
            flowerRect.anchoredPosition = rectTransform.anchoredPosition + spawnOffset;
            
            // 벌 보다 뒤에 그려지도록 형제 순서 조정
            newFlower.transform.SetAsFirstSibling();
        }
    }
}
}
