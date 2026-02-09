using UnityEngine;
using UnityEngine.UI;

public class ScrollingRawImage : MonoBehaviour
{
    [Header("스크롤 속도")]
    [SerializeField] private float _scrollSpeedX = 0.05f;
    [SerializeField] private float _scrollSpeedY = -0.05f;

    private RawImage _rawImage;

    void Start()
    {
        _rawImage = GetComponent<RawImage>();
    }

    void Update()
    {
        if (_rawImage == null) return;

        Rect uvRect = _rawImage.uvRect;
        uvRect.x += _scrollSpeedX * Time.deltaTime;
        uvRect.y += _scrollSpeedY * Time.deltaTime;
        _rawImage.uvRect = uvRect;
    }
}
