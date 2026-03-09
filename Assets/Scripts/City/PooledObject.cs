using UnityEngine;

/// <summary>
/// 풀링된 오브젝트에 부착하는 컴포넌트
/// 화면 밖으로 나가면 자동으로 풀로 반환
/// </summary>
namespace SeongWon
{

public class PooledObject : MonoBehaviour
{
    [Header("Auto Return Settings")]
    [Tooltip("Y 위치가 이 값보다 낮아지면 자동으로 풀로 반환")]
    public float returnYThreshold = -800f;
    
    [Tooltip("활성화 후 이 시간(초)이 지나면 자동으로 풀로 반환 (0 = 비활성화)")]
    public float autoReturnTime = 0f;
    
    private ObjectPool parentPool;
    private RectTransform rectTransform;
    private float activationTime;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }
    
    private void OnEnable()
    {
        activationTime = Time.time;
        
        // 부모 ObjectPool 찾기
        if (parentPool == null && transform.parent != null)
        {
            parentPool = transform.parent.GetComponent<ObjectPool>();
        }
    }
    
    private void Update()
    {
        // Y 위치 체크
        if (rectTransform != null && rectTransform.anchoredPosition.y < returnYThreshold)
        {
            ReturnToPool();
            return;
        }
        
        // 시간 제한 체크
        if (autoReturnTime > 0f && Time.time - activationTime >= autoReturnTime)
        {
            ReturnToPool();
        }
    }
    
    public void ReturnToPool()
    {
        if (parentPool != null)
        {
            parentPool.ReturnObject(gameObject);
        }
        else
        {
            // 풀이 없으면 그냥 비활성화
            gameObject.SetActive(false);
        }
    }
    
    public void SetPool(ObjectPool pool)
    {
        parentPool = pool;
    }
}
}
