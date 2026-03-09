using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 에너지가 다다다닥 연속 발사되면서 도착 시 팡팡 터지는 폭죽 효과
/// </summary>
public class EnergyFlowEffect : MonoBehaviour
{
    [Header("모드 설정")]
    [Tooltip("true: 에너지 폭포수 모드 (위로 올라가는 스트림), false: 기존 번개 모드")]
    [SerializeField] private bool _useFountainMode = false;

    [Header("폭포수 모드 - 파티클 설정")]
    [Tooltip("폭포수 파티클 수 (기존보다 많게)")]
    [SerializeField] private int _fountainParticleCount = 300;

    [Tooltip("폭포수 파티클 크기")]
    [SerializeField] private float _fountainDotSize = 6f;

    [Tooltip("폭포수 발사 지속 시간 (길수록 스트림 지속)")]
    [SerializeField] private float _fountainBurstDuration = 0.8f;

    [Tooltip("폭포수 파티클 이동 시간")]
    [SerializeField] private float _fountainTravelTime = 1.0f;

    [Header("폭포수 모드 - 스트림 형태")]
    [Tooltip("시작 영역 좌우 폭")]
    [SerializeField] private float _fountainStartWidth = 60f;

    [Tooltip("시작 영역 상하 범위")]
    [SerializeField] private float _fountainStartHeight = 20f;

    [Tooltip("도착 영역 좌우 폭")]
    [SerializeField] private float _fountainEndWidth = 40f;

    [Tooltip("도착 영역 상하 범위")]
    [SerializeField] private float _fountainEndHeight = 30f;

    [Tooltip("도달 비율 최소 (0~1, 시작→끝 거리의 몇 %까지 올라갈지)")]
    [SerializeField][Range(0.1f, 1f)] private float _fountainReachMin = 0.5f;

    [Tooltip("도달 비율 최대 (0~1)")]
    [SerializeField][Range(0.1f, 1f)] private float _fountainReachMax = 1.0f;

    [Header("폭포수 모드 - 사인파 물결")]
    [Tooltip("좌우 물결 흔들림 강도")]
    [SerializeField] private float _fountainWobbleStrength = 50f;

    [Tooltip("좌우 물결 속도")]
    [SerializeField] private float _fountainWobbleSpeed = 5f;

    [Tooltip("물결 파장 수 (시작~끝 사이 사인 반복 횟수)")]
    [SerializeField] private float _fountainWobbleCycles = 2f;

    [Header("폭포수 모드 - 페이드")]
    [Tooltip("도착 부근에서 페이드아웃 시작 비율 (0~1)")]
    [SerializeField] private float _fountainFadeStart = 0.65f;

    [Tooltip("파티클 최소/최대 크기 비율")]
    [SerializeField] private float _fountainMinSize = 0.3f;
    [SerializeField] private float _fountainMaxSize = 1.0f;

    [Header("폭포수 모드 - 시작/끝 스케일")]
    [Tooltip("파티클 시작 스케일 (1.0 = 원래 크기)")]
    [SerializeField] private float _fountainStartScale = 1.0f;

    [Tooltip("파티클 끝 스케일 (0에 가까울수록 작아짐)")]
    [SerializeField] private float _fountainEndScale = 0.2f;

    [Header("스프라이트 애니메이션 모드")]
    [Tooltip("true: 스프라이트 프레임 애니메이션, false: 기존 파티클 모드")]
    [SerializeField] private bool _useSpriteAnimation = false;

    [Header("스프라이트 애니메이션 - 게임별 대상 이미지")]
    [SerializeField] private Image _animTargetGame1;  // 태양광 - 캔버스 위 Image
    [SerializeField] private Image _animTargetGame2;  // 수력
    [SerializeField] private Image _animTargetGame3;  // 풍력

    [Header("스프라이트 애니메이션 - 게임별 프레임")]
    [SerializeField] private Sprite[] _animFramesGame1;  // 태양광
    [SerializeField] private Sprite[] _animFramesGame2;  // 수력
    [SerializeField] private Sprite[] _animFramesGame3;  // 풍력

    [Header("스프라이트 애니메이션 설정")]
    [Tooltip("초당 프레임 수")]
    [SerializeField] private float _animationFPS = 12f;

    [Header("셰이더 Reveal 설정 (스프라이트 애니메이션 모드 사용 시)")]
    [Tooltip("Reveal 엣지 부드러움 (0에 가까울수록 선명한 경계)")]
    [SerializeField][Range(0.001f, 0.3f)] private float _revealEdgeSoftness = 0.05f;

    [Tooltip("한 단계 reveal 애니메이션 시간 (초)")]
    [SerializeField] private float _revealStepDuration = 0.3f;

    [Tooltip("전체 reveal에 필요한 PlayEffect 호출 횟수")]
    [SerializeField] private int _revealSteps = 12;

    [Header("경로 설정 - 시작점")]
    [SerializeField] private RectTransform _startPoint;

    [Header("경로 설정 - 게임별 도착점")]
    [SerializeField] private RectTransform _endPointGame1;  // 태양광
    [SerializeField] private RectTransform _endPointGame2;  // 수력
    [SerializeField] private RectTransform _endPointGame3;  // 풍력

    private RectTransform _currentEndPoint;

    [Header("게임별 스프라이트")]
    [SerializeField] private Sprite _spriteGame1;  // 태양광
    [SerializeField] private Sprite _spriteGame2;  // 수력
    [SerializeField] private Sprite _spriteGame3;  // 풍력

    [Header("게임별 색상")]
    [SerializeField] private Color _colorGame1 = new Color(1f, 0.9f, 0.3f, 1f);   // 노란색 (태양광)
    [SerializeField] private Color _colorGame2 = new Color(0.3f, 0.7f, 1f, 1f);   // 파란색 (수력)
    [SerializeField] private Color _colorGame3 = new Color(0.5f, 1f, 0.8f, 1f);   // 청록색 (풍력)

    [Header("게임별 파동 효과 오브젝트 (Image)")]
    [SerializeField] private Image _waveObjectGame1;  // 태양광 - 파동 효과 낼 이미지
    [SerializeField] private Image _waveObjectGame2;  // 수력
    [SerializeField] private Image _waveObjectGame3;  // 풍력

    [Header("파동 효과 설정")]
    [SerializeField] private float _waveMaxScale = 1.5f;    // 파동 최대 크기
    [SerializeField] private float _waveDuration = 0.4f;    // 파동 지속 시간

    private Image _currentWaveObject;

    [Header("에너지 점 설정")]
    [SerializeField] private float _dotSize = 12f;
    [SerializeField] private float _travelTime = 0.5f;

    [Header("연속 발사 설정")]
    [Tooltip("총 발사되는 파티클 수")]
    [SerializeField] private int _totalParticles = 100;

    [Tooltip("발사 지속 시간 (이 시간 동안 다다다닥 발사)")]
    [SerializeField] private float _burstDuration = 0.25f;

    [Tooltip("시작점 퍼짐 범위")]
    [SerializeField] private float _startSpread = 50f;

    [Tooltip("도착점 퍼짐 범위")]
    [SerializeField] private float _endSpread = 180f;

    [Tooltip("곡선 휘어짐 강도")]
    [SerializeField] private float _curveStrength = 120f;

    [Header("도착 시 폭발 효과")]
    [Tooltip("폭발 시 퍼지는 파티클 수")]
    [SerializeField] private int _explosionParticles = 12;

    [Tooltip("폭발 반경")]
    [SerializeField] private float _explosionRadius = 100f;

    [Tooltip("폭발 지속 시간")]
    [SerializeField] private float _explosionDuration = 0.3f;

    [Header("크기 변화")]
    [SerializeField] private float _minSize = 0.4f;
    [SerializeField] private float _maxSize = 1.0f;

    [Header("Canvas 설정")]
    [SerializeField] private Canvas _targetCanvas;

    private List<GameObject> _activeDots = new List<GameObject>();

    // 현재 사용 중인 스프라이트/색상
    private Sprite _currentSprite;
    private Color _currentColor;
    private Sprite[] _currentAnimFrames;
    private Image _currentAnimTarget;
    private Coroutine _spriteAnimCoroutine;

    // Reveal 셰이더 머티리얼 (게임별 개별 인스턴스)
    private Material _revealMat1, _revealMat2, _revealMat3;
    private Material _currentRevealMat;
    private static readonly int PropProgress = Shader.PropertyToID("_Progress");
    private static readonly int PropEdgeSoftness = Shader.PropertyToID("_EdgeSoftness");

    void Awake()
    {
        if (_targetCanvas == null)
        {
            Debug.LogError("EnergyFlowEffect: Target Canvas를 설정하세요!");
        }
        // 기본값은 Game1
        _currentSprite = _spriteGame1;
        _currentColor = _colorGame1;

        // Reveal 셰이더 머티리얼 초기화
        if (_useSpriteAnimation)
        {
            InitRevealMaterials();
        }
    }

    void InitRevealMaterials()
    {
        Shader shader = Shader.Find("UI/RevealBottomToTop");
        if (shader == null)
        {
            Debug.LogError("EnergyFlowEffect: UI/RevealBottomToTop 셰이더를 찾을 수 없습니다!");
            return;
        }

        _revealMat1 = CreateRevealMaterial(shader);
        _revealMat2 = CreateRevealMaterial(shader);
        _revealMat3 = CreateRevealMaterial(shader);

        // 시작 시 이미지에 머티리얼 적용 (Progress=0 → 안 보임)
        if (_animTargetGame1 != null) _animTargetGame1.material = _revealMat1;
        if (_animTargetGame2 != null) _animTargetGame2.material = _revealMat2;
        if (_animTargetGame3 != null) _animTargetGame3.material = _revealMat3;
    }

    Material CreateRevealMaterial(Shader shader)
    {
        Material mat = new Material(shader);
        mat.SetFloat(PropProgress, 0f);
        mat.SetFloat(PropEdgeSoftness, _revealEdgeSoftness);
        return mat;
    }

    /// <summary>
    /// 게임 번호에 맞는 스프라이트/색상으로 효과 재생
    /// </summary>
    /// <param name="gameNumber">1, 2, 3 중 하나</param>
    public void PlayEffect(int gameNumber)
    {
        // 게임 번호에 따라 스프라이트/색상/도착점/파동오브젝트 설정
        switch (gameNumber)
        {
            case 1:
                _currentSprite = _spriteGame1;
                _currentColor = _colorGame1;
                _currentEndPoint = _endPointGame1;
                _currentWaveObject = _waveObjectGame1;
                _currentAnimFrames = _animFramesGame1;
                _currentAnimTarget = _animTargetGame1;
                _currentRevealMat = _revealMat1;
                break;
            case 2:
                _currentSprite = _spriteGame2;
                _currentColor = _colorGame2;
                _currentEndPoint = _endPointGame2;
                _currentWaveObject = _waveObjectGame2;
                _currentAnimFrames = _animFramesGame2;
                _currentAnimTarget = _animTargetGame2;
                _currentRevealMat = _revealMat2;
                break;
            case 3:
                _currentSprite = _spriteGame3;
                _currentColor = _colorGame3;
                _currentEndPoint = _endPointGame3;
                _currentWaveObject = _waveObjectGame3;
                _currentAnimFrames = _animFramesGame3;
                _currentAnimTarget = _animTargetGame3;
                _currentRevealMat = _revealMat3;
                break;
            default:
                _currentSprite = _spriteGame1;
                _currentColor = _colorGame1;
                _currentEndPoint = _endPointGame1;
                _currentWaveObject = _waveObjectGame1;
                _currentAnimFrames = _animFramesGame1;
                _currentAnimTarget = _animTargetGame1;
                _currentRevealMat = _revealMat1;
                break;
        }

        PlayEffectInternal();
        PlayWaveEffect();
    }

    /// <summary>
    /// 기본 효과 재생 (Game1 스프라이트/색상 사용)
    /// </summary>
    public void PlayEffect()
    {
        _currentSprite = _spriteGame1;
        _currentColor = _colorGame1;
        _currentEndPoint = _endPointGame1;
        _currentWaveObject = _waveObjectGame1;
        _currentAnimFrames = _animFramesGame1;
        _currentAnimTarget = _animTargetGame1;
        _currentRevealMat = _revealMat1;
        PlayEffectInternal();
        PlayWaveEffect();
    }

    /// <summary>
    /// 파동 효과 - 오브젝트 잔상이 커지면서 사라짐
    /// </summary>
    private void PlayWaveEffect()
    {
        if (_currentWaveObject == null) return;

        StartCoroutine(WaveEffectCoroutine(_currentWaveObject));
    }

    IEnumerator WaveEffectCoroutine(Image sourceImage)
    {
        // 원본 이미지의 복제본 생성 (잔상)
        GameObject waveObj = new GameObject("WaveEffect");
        waveObj.transform.SetParent(sourceImage.transform.parent, false);

        // 원본과 같은 위치/크기로 설정
        RectTransform waveRt = waveObj.AddComponent<RectTransform>();
        RectTransform sourceRt = sourceImage.rectTransform;

        waveRt.anchoredPosition = sourceRt.anchoredPosition;
        waveRt.sizeDelta = sourceRt.sizeDelta;
        waveRt.anchorMin = sourceRt.anchorMin;
        waveRt.anchorMax = sourceRt.anchorMax;
        waveRt.pivot = sourceRt.pivot;
        waveRt.localScale = sourceRt.localScale;
        waveRt.localRotation = sourceRt.localRotation;

        // 이미지 복사
        Image waveImg = waveObj.AddComponent<Image>();
        waveImg.sprite = sourceImage.sprite;
        waveImg.color = sourceImage.color;
        waveImg.raycastTarget = false;

        // 원본 뒤에 배치 (잔상이 뒤에 보이도록)
        waveRt.SetSiblingIndex(sourceRt.GetSiblingIndex());

        // 파동 애니메이션
        float elapsed = 0f;
        Vector3 startScale = waveRt.localScale;
        Color startColor = waveImg.color;

        while (elapsed < _waveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _waveDuration;

            // 부드럽게 커짐
            float scale = Mathf.Lerp(1f, _waveMaxScale, EaseOutQuad(t));
            waveRt.localScale = startScale * scale;

            // 부드럽게 투명해짐
            float alpha = Mathf.Lerp(startColor.a, 0f, EaseOutQuad(t));
            waveImg.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            yield return null;
        }

        Destroy(waveObj);
    }

    private void PlayEffectInternal()
    {
        if (_useSpriteAnimation)
        {
            if (_spriteAnimCoroutine != null) StopCoroutine(_spriteAnimCoroutine);
            _spriteAnimCoroutine = StartCoroutine(ShaderRevealCoroutine());
        }
        else
        {
            if (_startPoint == null || _currentEndPoint == null || _targetCanvas == null)
            {
                return;
            }
            StartCoroutine(BurstFireCoroutine());
        }
    }

    /// <summary>
    /// 셰이더 Reveal 모드 - 아래→위로 이미지가 한번 쭉 지나감
    /// </summary>
    IEnumerator ShaderRevealCoroutine()
    {
        if (_currentAnimTarget == null || _currentRevealMat == null)
        {
            Debug.LogWarning("[Reveal] AnimTarget 또는 RevealMat이 null!");
            yield break;
        }

        // 이미지 활성화 + progress 리셋
        _currentAnimTarget.enabled = true;
        _currentRevealMat.SetFloat(PropProgress, 0f);

        Debug.Log($"[Reveal] 시작! duration={_revealStepDuration}s, softness={_revealEdgeSoftness}");

        // 0 → 1 풀 애니메이션 (아래→위 reveal)
        float elapsed = 0f;
        while (elapsed < _revealStepDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutQuad(Mathf.Clamp01(elapsed / _revealStepDuration));
            _currentRevealMat.SetFloat(PropProgress, t);
            yield return null;
        }

        _currentRevealMat.SetFloat(PropProgress, 1f);

        // 잠깐 유지 후 리셋
        yield return new WaitForSeconds(0.1f);
        _currentRevealMat.SetFloat(PropProgress, 0f);
        _spriteAnimCoroutine = null;
    }

    /// <summary>
    /// 다다다닥 연속 발사
    /// </summary>
    IEnumerator BurstFireCoroutine()
    {
        int count = _useFountainMode ? _fountainParticleCount : _totalParticles;
        float burst = _useFountainMode ? _fountainBurstDuration : _burstDuration;

        float elapsed = 0f;
        int spawned = 0;

        while (spawned < count)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / burst);

            // 폭포수 모드: ease-in (처음 느리게 → 끝에 몰아서)
            // 기존 모드: 선형 (균등 발사)
            float curve = _useFountainMode ? (t * t * t) : t;

            int shouldHaveSpawned = Mathf.Min(count, Mathf.CeilToInt(curve * count));

            // 이번 프레임에 부족한 만큼 한꺼번에 생성
            while (spawned < shouldHaveSpawned)
            {
                SpawnParticle();
                spawned++;
            }

            yield return null;
        }
    }

    void SpawnParticle()
    {
        GameObject dot = new GameObject("EnergyDot");
        dot.transform.SetParent(_targetCanvas.transform, false);

        RectTransform rt = dot.AddComponent<RectTransform>();
        float dotSize = _useFountainMode ? _fountainDotSize : _dotSize;
        float minSz = _useFountainMode ? _fountainMinSize : _minSize;
        float maxSz = _useFountainMode ? _fountainMaxSize : _maxSize;
        float randomSize = dotSize * Random.Range(minSz, maxSz);
        rt.sizeDelta = new Vector2(randomSize, randomSize);
        rt.localScale = Vector3.zero;

        Image img = dot.AddComponent<Image>();
        if (_currentSprite != null)
        {
            img.sprite = _currentSprite;
        }

        // 색상 약간 랜덤화 (밝기 변화)
        float brightness = Random.Range(0.8f, 1.2f);
        img.color = new Color(
            Mathf.Clamp01(_currentColor.r * brightness),
            Mathf.Clamp01(_currentColor.g * brightness),
            Mathf.Clamp01(_currentColor.b * brightness),
            _currentColor.a
        );
        img.raycastTarget = false;

        rt.SetAsLastSibling();

        // 시작/도착 위치 계산
        Vector2 startCenter = WorldToCanvasPosition(_startPoint.position);
        Vector2 endCenter = WorldToCanvasPosition(_currentEndPoint.position);
        Vector2 startPos, endPos;
        float curveOffset = 0f;

        if (_useFountainMode)
        {
            // 폭포수 모드: 시작/도착 영역 범위 지정
            float sx = Random.Range(-_fountainStartWidth * 0.5f, _fountainStartWidth * 0.5f);
            float sy = Random.Range(-_fountainStartHeight * 0.5f, _fountainStartHeight * 0.5f);
            startPos = startCenter + new Vector2(sx, sy);

            float ex = Random.Range(-_fountainEndWidth * 0.5f, _fountainEndWidth * 0.5f);
            float ey = Random.Range(-_fountainEndHeight * 0.5f, _fountainEndHeight * 0.5f);
            endPos = endCenter + new Vector2(ex, ey);

            // 도달 높이 랜덤: startPos~endPos 사이 reach% 지점까지만 이동
            float reach = Random.Range(_fountainReachMin, _fountainReachMax);
            endPos = Vector2.Lerp(startPos, endPos, reach);
        }
        else
        {
            // 기존 모드
            float startAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float startRadius = Random.Range(0f, _startSpread);
            startPos = startCenter + new Vector2(
                Mathf.Cos(startAngle) * startRadius,
                Mathf.Sin(startAngle) * startRadius * 0.5f
            );

            float endOffsetX = Random.Range(-_endSpread, _endSpread);
            float endOffsetY = Random.Range(-_endSpread * 0.3f, _endSpread * 0.3f);
            endPos = endCenter + new Vector2(endOffsetX, endOffsetY);

            curveOffset = Random.Range(-_curveStrength, _curveStrength);
        }

        // 이동 시간 랜덤
        float baseTravel = _useFountainMode ? _fountainTravelTime : _travelTime;
        float duration = baseTravel * Random.Range(0.7f, 1.3f);

        _activeDots.Add(dot);

        if (_useFountainMode)
        {
            StartCoroutine(AnimateFountainParticle(dot, rt, img, startPos, endPos, duration));
        }
        else
        {
            StartCoroutine(AnimateParticle(dot, rt, img, startPos, endPos, curveOffset, duration));
        }
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
    /// 폭포수 모드 - 에너지가 위로 올라가는 스트림
    /// </summary>
    IEnumerator AnimateFountainParticle(GameObject dot, RectTransform rt, Image img,
        Vector2 startPos, Vector2 endPos, float duration)
    {
        float elapsed = 0f;
        Color baseColor = img.color;

        // 시작 시 빠르게 나타남
        float popDuration = 0.05f;
        float popElapsed = 0f;
        while (popElapsed < popDuration)
        {
            if (dot == null) yield break;
            popElapsed += Time.deltaTime;
            float popT = popElapsed / popDuration;
            rt.localScale = Vector3.one * EaseOutQuad(popT);
            yield return null;
        }
        rt.localScale = Vector3.one;

        // 각 파티클마다 고유한 사인파 위상 오프셋 (겹침 방지)
        float wobblePhase = Random.Range(0f, Mathf.PI * 2f);
        // 개별 파티클 흔들림 강도 랜덤 (자연스러운 폭 변화)
        float wobbleAmp = _fountainWobbleStrength * Random.Range(0.5f, 1.2f);

        while (elapsed < duration)
        {
            if (dot == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 위로 올라가는 이동 (등속에 가까운 부드러운 이동)
            float easedT = EaseInOutQuad(t);

            // 기본 위치: 시작→끝 직선 보간
            Vector2 pos = Vector2.Lerp(startPos, endPos, easedT);

            // 사인파 물결: t 기반으로 _fountainWobbleCycles만큼 반복
            float sinValue = Mathf.Sin(t * _fountainWobbleCycles * Mathf.PI * 2f + wobblePhase);
            // 엔벨로프: 시작/끝은 좁고 중간이 넓음
            float envelope = Mathf.Sin(t * Mathf.PI);
            pos.x += sinValue * wobbleAmp * envelope;

            rt.anchoredPosition = pos;

            // 페이드아웃: 상단 도착 부근에서 서서히 사라짐
            float alpha = 1f;
            if (t > _fountainFadeStart)
            {
                float fadeT = (t - _fountainFadeStart) / (1f - _fountainFadeStart);
                alpha = 1f - fadeT * fadeT;  // 제곱으로 부드럽게
            }

            // 은은한 반짝임
            float flicker = 0.9f + Mathf.Sin(elapsed * 20f + wobblePhase) * 0.1f;
            img.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alpha * flicker);

            // 위로 갈수록 서서히 작아짐
            float scaleT = Mathf.Lerp(_fountainStartScale, _fountainEndScale, t * t);
            rt.localScale = Vector3.one * scaleT;

            yield return null;
        }

        // 정리
        if (dot != null)
        {
            _activeDots.Remove(dot);
            Destroy(dot);
        }
    }

    float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
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
        if (_currentSprite != null)
        {
            img.sprite = _currentSprite;
        }
        img.color = _currentColor;
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

        // 스프라이트 애니메이션 정리
        if (_spriteAnimCoroutine != null)
        {
            _spriteAnimCoroutine = null;
        }

        // Reveal 셰이더 progress 리셋
        if (_revealMat1 != null) _revealMat1.SetFloat(PropProgress, 0f);
        if (_revealMat2 != null) _revealMat2.SetFloat(PropProgress, 0f);
        if (_revealMat3 != null) _revealMat3.SetFloat(PropProgress, 0f);
    }

    void OnDisable()
    {
        ClearAllDots();
    }

    void OnDestroy()
    {
        if (_revealMat1 != null) Destroy(_revealMat1);
        if (_revealMat2 != null) Destroy(_revealMat2);
        if (_revealMat3 != null) Destroy(_revealMat3);
    }
}
