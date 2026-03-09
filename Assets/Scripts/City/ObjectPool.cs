using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 재사용 가능한 오브젝트 풀 시스템
/// Instantiate/Destroy 호출을 최소화하여 성능 최적화
/// </summary>
namespace SeongWon
{

public class ObjectPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [Tooltip("풀링할 프리팹")]
    public GameObject prefab;
    
    [Tooltip("초기 풀 크기")]
    public int initialPoolSize = 20;
    
    [Tooltip("최대 풀 크기 (0 = 무제한)")]
    public int maxPoolSize = 50;
    
    private Queue<GameObject> availableObjects = new Queue<GameObject>();
    private List<GameObject> activeObjects = new List<GameObject>();
    
    private void Start()
    {
        // 초기 풀 생성
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewObject();
        }
    }
    
    private GameObject CreateNewObject()
    {
        if (prefab == null)
        {
            Debug.LogError("[ObjectPool] Prefab이 할당되지 않았습니다!");
            return null;
        }
        
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);
        availableObjects.Enqueue(obj);
        return obj;
    }
    
    /// <summary>
    /// 풀에서 오브젝트 가져오기
    /// </summary>
    public GameObject GetObject()
    {
        GameObject obj;
        
        // 사용 가능한 오브젝트가 있으면 재사용
        if (availableObjects.Count > 0)
        {
            obj = availableObjects.Dequeue();
        }
        else
        {
            // 풀이 비었고 최대 크기 제한이 없거나 아직 도달하지 않았으면 새로 생성
            if (maxPoolSize == 0 || (activeObjects.Count + availableObjects.Count) < maxPoolSize)
            {
                obj = CreateNewObject();
                if (obj == null) return null;
                availableObjects.Dequeue(); // 방금 추가된 오브젝트 가져오기
            }
            else
            {
                // 최대 크기 도달 - 가장 오래된 활성 오브젝트 재사용
                obj = activeObjects[0];
                activeObjects.RemoveAt(0);
                obj.SetActive(false);
            }
        }
        
        obj.SetActive(true);
        activeObjects.Add(obj);
        return obj;
    }
    
    /// <summary>
    /// 오브젝트를 풀로 반환
    /// </summary>
    public void ReturnObject(GameObject obj)
    {
        if (obj == null) return;
        
        if (activeObjects.Contains(obj))
        {
            activeObjects.Remove(obj);
            obj.SetActive(false);
            availableObjects.Enqueue(obj);
        }
    }
    
    /// <summary>
    /// 모든 활성 오브젝트를 풀로 반환
    /// </summary>
    public void ReturnAllObjects()
    {
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = activeObjects[i];
            obj.SetActive(false);
            availableObjects.Enqueue(obj);
        }
        activeObjects.Clear();
    }
}
}
