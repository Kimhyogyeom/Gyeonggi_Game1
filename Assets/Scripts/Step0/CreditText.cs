using UnityEngine;
using System.Collections;

// STT 출처 : CLOVA Dubbing
namespace UI
{
    /// <summary>
    /// 시작 시 크레딧 텍스트 표시 후 자동으로 사라짐
    /// </summary>
    public class CreditText : MonoBehaviour
    {
        [Header("설정")]
        [Tooltip("표시 시간 (초)")]
        [SerializeField] private float displayDuration = 5f;

        [Header("대상")]
        [Tooltip("숨길 오브젝트 (비워두면 자기 자신)")]
        [SerializeField] private GameObject targetObject;

        private void Start()
        {
            if (targetObject == null)
            {
                targetObject = gameObject;
            }

            StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(displayDuration);
            targetObject.SetActive(false);
        }
    }
}
