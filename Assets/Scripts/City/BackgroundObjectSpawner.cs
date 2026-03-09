using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 배경에 나무, 집 등 랜덤한 오브젝트를 생성하는 스크립트입니다.
/// 화면 상단에서 생성되어 ScrollingObject에 의해 아래로 내려옵니다.
/// [최적화] ObjectPool을 사용하여 Instantiate/Destroy 호출 최소화
/// </summary>
namespace SeongWon
{

public class BackgroundObjectSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("생성할 배경 오브젝트 프리팹 배열 (나무, 집 등)")]
    public GameObject[] backgroundPrefabs;

    [Tooltip("오브젝트가 생성될 부모 Transform")]
    public Transform spawnContainer;

    [Tooltip("최소 생성 간격 (초)")]
    public float minSpawnInterval = 2f;
    
    [Tooltip("최대 생성 간격 (초)")]
    public float maxSpawnInterval = 5f;

    [Tooltip("생성 위치 Y 좌표 (화면 상단, 캔버스 크기에 맞춰 조절)")]
    public float spawnYPosition = 600f;

    [Tooltip("생성 위치 X 범위 (좌우 랜덤 범위) - 예: 800이면 -400 ~ +400 사이")]
    public float spawnXRange = 800f;
    
    [Header("Object Pooling")]
    [Tooltip("각 프리팹 타입당 풀 크기")]
    public int poolSizePerPrefab = 10;
    
    [Tooltip("각 프리팹 타입당 최대 풀 크기")]
    public int maxPoolSizePerPrefab = 20;

    private float timer = 0f;
    private float nextSpawnTime = 0f;
    private List<ObjectPool> objectPools = new List<ObjectPool>();

    void Start()
    {
        SetNextSpawnTime();
        
        // 각 프리팹에 대한 오브젝트 풀 생성
        if (backgroundPrefabs != null && backgroundPrefabs.Length > 0)
        {
            for (int i = 0; i < backgroundPrefabs.Length; i++)
            {
                if (backgroundPrefabs[i] == null) continue;
                
                GameObject poolObject = new GameObject($"BackgroundPool_{i}");
                poolObject.transform.SetParent(spawnContainer != null ? spawnContainer : transform);
                
                ObjectPool pool = poolObject.AddComponent<ObjectPool>();
                pool.prefab = backgroundPrefabs[i];
                pool.initialPoolSize = poolSizePerPrefab;
                pool.maxPoolSize = maxPoolSizePerPrefab;
                
                objectPools.Add(pool);
                
                // 프리팹에 PooledObject 컴포넌트가 없으면 추가
                if (backgroundPrefabs[i].GetComponent<PooledObject>() == null)
                {
                    backgroundPrefabs[i].AddComponent<PooledObject>();
                }
            }
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= nextSpawnTime)
        {
            SpawnRandomObject();
            timer = 0f;
            SetNextSpawnTime();
        }
    }

    void SetNextSpawnTime()
    {
        nextSpawnTime = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    void SpawnRandomObject()
    {
        if (objectPools == null || objectPools.Count == 0) return;

        // 랜덤 풀 선택
        int randomIndex = Random.Range(0, objectPools.Count);
        ObjectPool selectedPool = objectPools[randomIndex];
        
        if (selectedPool == null) return;

        // 풀에서 오브젝트 가져오기 (Instantiate 대신)
        GameObject newObj = selectedPool.GetObject();
        if (newObj == null) return;

        // 랜덤 X 위치 계산
        float randomX = Random.Range(-spawnXRange / 2f, spawnXRange / 2f);
        
        // 위치 설정
        RectTransform objRect = newObj.GetComponent<RectTransform>();
        if (objRect != null)
        {
            objRect.anchoredPosition = new Vector2(randomX, spawnYPosition);
            
            // 배경 오브젝트이므로 맨 뒤로 보냄
            newObj.transform.SetAsFirstSibling();
        }
    }
}
}
