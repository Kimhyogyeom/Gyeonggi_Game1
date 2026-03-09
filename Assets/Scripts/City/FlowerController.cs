using UnityEngine;
using System.Collections;
namespace SeongWon
{


public class FlowerController : MonoBehaviour
{
    [Header("꽃, 나무 충돌처리 코루틴")]
    [SerializeField] float radius;
    WaitForSecondsRealtime waitForSecondsRealtime;
    Coroutine coroutine;

    private void OnEnable()
    {
        coroutine = StartCoroutine(CoCheckFlower());
    }

    private void OnDisable()
    {
        if(coroutine != null)
            StopCoroutine(coroutine);
    }

    private IEnumerator CoCheckFlower()
    {
        while (true)
        {
            Collider[] flowers = Physics.OverlapSphere(transform.position, radius);

            for(int i = 0; i < flowers.Length; i++) 
            {
                flowers[i].GetComponent<ObjectSizeManager>()?.StartIncrease();
            }

            yield return waitForSecondsRealtime;
        }

    }
}
}
