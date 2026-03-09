using UnityEngine;
using System.Collections;
namespace SeongWon
{


public class ObjectSizeManager : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField] float maxSize;

    WaitForSecondsRealtime waitForSecondsRealtime;

    private void Awake()
    {
        gameObject.transform.localScale = Vector3.zero;
    }

    public void StartIncrease() 
    {
        StartCoroutine(CoIncraseObj());
    }

    private IEnumerator CoIncraseObj() 
    {
        float currentScale = gameObject.transform.localScale.x;

        while (currentScale < maxSize)
        {
            // 프레임 속도에 관계없이 일정하게, 그리고 천천히 커지도록 Time.deltaTime 적용
            // speed 값이 너무 크면 Inspector에서 줄여주세요 (예: 0.5 ~ 1.0 정도 권장)
            currentScale += speed * Time.deltaTime;
            
            // maxScale을 초과하지 않도록 클램핑
            if (currentScale > maxSize) currentScale = maxSize;

            gameObject.transform.localScale = new Vector3(currentScale, currentScale, currentScale);
            yield return null;
        }
    }

    public void ResetSize() 
    {
        gameObject.transform.localScale = Vector3.zero;
    }
}
}
