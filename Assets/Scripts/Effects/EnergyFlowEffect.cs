using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 에너지가 다다다닥 연속 발사되면서 도착 시 팡팡 터지는 폭죽 효과
/// </summary>
public class EnergyFlowEffect : MonoBehaviour
{
    [Header("경로 설정")]
    [SerializeField] private RectTransform _startPoint;
    [SerializeField] private RectTransform _endPoint;

    [Header("에너지 점 설정")]
    [SerializeField] private Sprite _particleSprite;
    [SerializeField] private Color _energyColor = new Color(1f, 0.9f, 0.3f, 1f);
    [SerializeField] private float _dotSize = 12f;  // 더 작게
    [SerializeField] private float _travelTime = 0.5f;  // 더 빠르게

    [Header("연속 발사 설정")]
    [Tooltip("총 발사되는 파티클 수")]
    [SerializeField] private int _totalParticles = 100;  // 훨씬 많이!

    [Tooltip("발사 지속 시간 (이 시간 동안 다다다닥 발사)")]
    [SerializeField] private float _burstDuration = 0.25f;

    [Tooltip("시작점 퍼짐 범위")]
    [SerializeField] private float _startSpread = 50f;

    [Tooltip("도착점 퍼짐 범위")]
    [SerializeField] private float _endSpread = 180f;  // 더 넓게

    [Tooltip("곡선 휘어짐 강도")]
    [SerializeField] private float _curveStrength = 120f;

    [Header("도착 시 폭발 효과")]
    [Tooltip("폭발 시 퍼지는 파티클 수")]
    [SerializeField] private int _explosionParticles = 12;  // 더 많이

    [Tooltip("폭발 반경")]
    [SerializeField] private float _explosionRadius = 100f;  // 더 넓게

    [Tooltip("폭발 지속 시간")]
    [SerializeField] private float _explosionDuration = 0.3f;

    [Header("크기 변화")]
    [SerializeField] private float _minSize = 0.4f;  // 더 작게
    [SerializeField] private float _maxSize = 1.0f;

    [Header("Canvas 설정")]
    [SerializeField] private Canvas _targetCanvas;

    private List<GameObject> _activeDots = new List<GameObject>();

    void Awake()
    {
        if (_targetCanvas == null)
        {
            Debug.LogError("EnergyFlowEffect: Target Canvas를 설정하세요!");
        }
    }

    /// <summary>
    /// 폭죽 효과 재생
    /// </summary>
    public void PlayEffect()
    {
        if (_startPoint == null || _endPoint == null || _targetCanvas == null)
        {
            return;
        }

        StartCoroutine(BurstFireCoroutine());
    }

    /// <summary>
    /// 다다다닥 연속 발사
    /// </summary>
    IEnumerator BurstFireCoroutine()
    {
        float interval = _burstDuration / _totalParticles;

        for (int i = 0; i < _totalParticles; i++)
        {
            SpawnParticle();

            // 약간의 랜덤 간격으로 자연스럽게
            yield return new WaitForSeconds(interval * Random.Range(0.5f, 1.5f));
        }
    }

    void SpawnParticle()
    {
        GameObject dot = new GameObject("EnergyDot");
        dot.transform.SetParent(_targetCanvas.transform, false);

        RectTransform rt = dot.AddComponent<RectTransform>();
        float randomSize = _dotSize * Random.Range(_minSize, _maxSize);
        rt.sizeDelta = new Vector2(randomSize, randomSize);
        rt.localScale = Vector3.zero;

        Image img = dot.AddComponent<Image>();
        if (_particleSprite != null)
        {
            img.sprite = _particleSprite;
        }

        // 색상 약간 랜덤화 (밝기 변화)
        float brightness = Random.Range(0.8f, 1.2f);
        img.color = new Color(
            Mathf.Clamp01(_energyColor.r * brightness),
            Mathf.Clamp01(_energyColor.g * brightness),
            Mathf.Clamp01(_energyColor.b * brightness),
            _energyColor.a
        );
        img.raycastTarget = false;

        rt.SetAsLastSibling();

        // 시작 위치 (약간 랜덤하게 퍼짐)
        Vector2 startCenter = WorldToCanvasPosition(_startPoint.position);
        float startAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float startRadius = Random.Range(0f, _startSpread);
        Vector2 startPos = startCenter + new Vector2(
            Mathf.Cos(startAngle) * startRadius,
            Mathf.Sin(startAngle) * startRadius * 0.5f
        );
        rt.anchoredPosition = startPos;

        // 도착 위치 (넓게 퍼짐)
        Vector2 endCenter = WorldToCanvasPosition(_endPoint.position);
        float endOffsetX = Random.Range(-_endSpread, _endSpread);
        float endOffsetY = Random.Range(-_endSpread * 0.3f, _endSpread * 0.3f);
        Vector2 endPos = endCenter + new Vector2(endOffsetX, endOffsetY);

        // 곡선 (주로 위로 볼록하게)
        float curveOffset = Random.Range(-_curveStrength, _curveStrength);

        // 이동 시간 랜덤
        float duration = _travelTime * Random.Range(0.7f, 1.3f);

        _activeDots.Add(dot);
        StartCoroutine(AnimateParticle(dot, rt, img, startPos, endPos, curveOffset, duration));
    }

    IEnumerator AnimateParticle(GameObject dot, RectTransform rt, Image img,
        Vector2 startPos, Vector2 endPos, float curveOffset, float duration)
    {
        float elapsed = 0f;
        Color baseColor = img.color;

        // 시작 시 빠르게 커짐
        float popDuration = 0.08f;
        float popElapsed = 0f;
        while (popElapsed < popDuration)
        {
            if (dot == null) yield break;
            popElapsed += Time.deltaTime;
            float popT = popElapsed / popDuration;
            rt.localScale = Vector3.one * EaseOutBack(popT);
            yield return null;
        }
        rt.localScale = Vector3.one;

        // 이동
        while (elapsed < duration)
        {
            if (dot == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 이징: 빠르게 출발, 끝에서 감속
            float easedT = EaseOutQuad(t);

            // 베지어 곡선 (위로 볼록)
            Vector2 control = new Vector2(
                (startPos.x + endPos.x) * 0.5f + curveOffset,
                Mathf.Max(startPos.y, endPos.y) + Mathf.Abs(curveOffset) * 0.5f + 50f
            );
            Vector2 pos = QuadraticBezier(startPos, control, endPos, easedT);

            rt.anchoredPosition = pos;

            // 이동 중 살짝 깜빡임 (반짝반짝)
            float flicker = 0.9f + Mathf.Sin(elapsed * 30f) * 0.1f;
            img.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * flicker);

            yield return null;
        }

        // 도착 후 폭발!
        if (dot != null && rt != null && img != null)
        {
            Vector2 explosionCenter = rt.anchoredPosition;
            yield return ExplodeEffect(dot, rt, img, explosionCenter);
        }

        // 정리
        if (dot != null)
        {
            _activeDots.Remove(dot);
            Destroy(dot);
        }
    }

    /// <summary>
    /// 도착 시 원형으로 팡! 터지는 효과
    /// </summary>
    IEnumerator ExplodeEffect(GameObject mainDot, RectTransform mainRt, Image mainImg, Vector2 center)
    {
        // 메인 파티클 빠르게 커지면서 사라짐
        StartCoroutine(FadeOut(mainRt, mainImg, 0.1f));

        // 주변에 작은 파티클들 원형으로 퍼짐
        for (int i = 0; i < _explosionParticles; i++)
        {
            float angle = (360f / _explosionParticles) * i * Mathf.Deg2Rad;
            angle += Random.Range(-0.3f, 0.3f);  // 약간 불규칙하게

            SpawnExplosionParticle(center, angle);
        }

        yield return new WaitForSeconds(_explosionDuration);
    }

    void SpawnExplosionParticle(Vector2 center, float angle)
    {
        GameObject dot = new GameObject("ExplosionDot");
        dot.transform.SetParent(_targetCanvas.transform, false);

        RectTransform rt = dot.AddComponent<RectTransform>();
        float size = _dotSize * Random.Range(0.4f, 0.8f);
        rt.sizeDelta = new Vector2(size, size);
        rt.localScale = Vector3.one;
        rt.anchoredPosition = center;

        Image img = dot.AddComponent<Image>();
        if (_particleSprite != null)
        {
            img.sprite = _particleSprite;
        }
        img.color = _energyColor;
        img.raycastTarget = false;

        rt.SetAsLastSibling();

        _activeDots.Add(dot);

        // 바깥으로 퍼지는 위치
        float distance = _explosionRadius * Random.Range(0.6f, 1f);
        Vector2 targetPos = center + new Vector2(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance
        );

        StartCoroutine(AnimateExplosionParticle(dot, rt, img, center, targetPos));
    }

    IEnumerator AnimateExplosionParticle(GameObject dot, RectTransform rt, Image img,
        Vector2 start, Vector2 end)
    {
        float elapsed = 0f;
        Color baseColor = img.color;

        while (elapsed < _explosionDuration)
        {
            if (dot == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / _explosionDuration;

            // 빠르게 퍼지다가 감속
            float easedT = EaseOutQuad(t);

            rt.anchoredPosition = Vector2.Lerp(start, end, easedT);

            // 작아지면서 투명해짐
            float scale = Mathf.Lerp(1f, 0.3f, t);
            rt.localScale = Vector3.one * scale;

            float alpha = Mathf.Lerp(1f, 0f, t);
            img.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            yield return null;
        }

        if (dot != null)
        {
            _activeDots.Remove(dot);
            Destroy(dot);
        }
    }

    IEnumerator FadeOut(RectTransform rt, Image img, float duration)
    {
        float elapsed = 0f;
        Vector3 startScale = rt.localScale;
        Color startColor = img.color;

        while (elapsed < duration)
        {
            if (rt == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 커지면서 사라짐
            rt.localScale = startScale * (1f + t * 0.5f);

            float alpha = Mathf.Lerp(startColor.a, 0f, t);
            img.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            yield return null;
        }
    }

    Vector2 QuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    Vector2 WorldToCanvasPosition(Vector3 worldPos)
    {
        RectTransform canvasRect = _targetCanvas.GetComponent<RectTransform>();
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(_targetCanvas.worldCamera, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, _targetCanvas.worldCamera, out Vector2 localPoint);
        return localPoint;
    }

    public void ClearAllDots()
    {
        StopAllCoroutines();
        foreach (var dot in _activeDots)
        {
            if (dot != null) Destroy(dot);
        }
        _activeDots.Clear();
    }

    void OnDisable()
    {
        ClearAllDots();
    }
}
