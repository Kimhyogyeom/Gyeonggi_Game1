using UnityEngine;

public class ForceFullScreen : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool _applyToChildren = true;

    private RectTransform _rectTransform;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        // 자기 자신을 전체 화면으로
        _rectTransform.anchorMin = Vector2.zero;
        _rectTransform.anchorMax = Vector2.one;
        _rectTransform.offsetMin = Vector2.zero;
        _rectTransform.offsetMax = Vector2.zero;

        // 자식들도 전체 화면으로
        if (_applyToChildren)
        {
            foreach (RectTransform child in _rectTransform)
            {
                child.anchorMin = Vector2.zero;
                child.anchorMax = Vector2.one;
                child.offsetMin = Vector2.zero;
                child.offsetMax = Vector2.zero;
            }
        }
    }
}
