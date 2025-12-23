using UnityEngine;

public class BounceY : MonoBehaviour
{
    [Header("위아래 높이 (진폭)")]
    public float amplitude = 0.5f;   // 얼마나 위로 튈지

    [Header("통통 속도")]
    public float frequency = 2f;     // 몇 번 통통거릴지(속도)

    private Vector3 _startPos;

    private void Start()
    {
        // 시작 위치 저장 (여기를 기준으로 위아래 움직임)
        _startPos = transform.localPosition;
    }

    private void Update()
    {
        // 시간에 따라 -1 ~ 1 사이로 왕복
        float offset = Mathf.Sin(Time.time * frequency) * amplitude;

        // Y만 위아래로 움직이게
        transform.localPosition = new Vector3(
            _startPos.x,
            _startPos.y + offset,
            _startPos.z
        );
    }
}
