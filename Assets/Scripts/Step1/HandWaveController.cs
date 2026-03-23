using UnityEngine;
using UnityEngine.UI;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using System.Collections;

public class HandWaveController : MonoBehaviour
{
    [SerializeField] private FadeAnimatorController _fadeAnimatorController;

    [Header("Panel References")]
    public GameObject _panel2;
    public GameObject _panel3;

    [Header("Objects to Activate")]
    public GameObject _object1;
    public GameObject _object2;
    public GameObject _object3;

    [Header("Wave Detection Settings")]
    [Tooltip("점프 높이 배수 (코~어깨 거리 기준, 0.3 = 30%)")]
    public float _waveHeightMultiplier = 0.3f;
    public int _totalWavesNeeded = 12;

    [Header("Slider Settings")]
    public Slider _progressSlider;

    [Header("Particle System")]
    [SerializeField] private GameObject _particleA;

    [Header("Energy Flow Effect")]
    [SerializeField] private EnergyFlowEffect _energyFlowEffect;

    [Header("제스처 사운드")]
    [SerializeField] private AudioSource _gestureAudioSource;
    [SerializeField] private AudioClip _gestureSound;

    // Wave detection state
    private float _lastY = -1f;
    private bool _wasMovingUp = false;
    private float _peakY = 0f;
    private bool _hasPeak = false;

    private int _waveCount = 0;
    private float _currentProgress = 0f;
    private bool _isCompleted = false;
    private bool _isActive = false;

    // Pose 결과
    private PoseLandmarkerResult _latestResult;
    private bool _hasNewResult = false;

    void OnEnable()
    {
        SeongWon.PoseLandmarkerRunner.OnPoseResultEvent += OnPoseResult;
    }

    void OnDisable()
    {
        SeongWon.PoseLandmarkerRunner.OnPoseResultEvent -= OnPoseResult;
    }

    private void OnPoseResult(PoseLandmarkerResult result)
    {
        _latestResult = result;
        _hasNewResult = true;
    }

    void Start()
    {
        Debug.Log("=== HandWaveController 시작 (Pose 기반) ===");
        Debug.Log("필요한 웨이브 횟수: " + _totalWavesNeeded);
    }

    void Update()
    {
        if (_panel2 != null && _panel2.activeSelf)
        {
            if (!_isActive)
                StartWaveDetection();

            if (!_isCompleted && _hasNewResult)
            {
                _hasNewResult = false;
                ProcessPoseData(_latestResult);
                UpdateObjectActivation();
            }
        }
        else
        {
            if (_isActive)
            {
                _isActive = false;
                Debug.Log("Panel2 비활성화");
            }
        }
    }

    void StartWaveDetection()
    {
        Debug.Log(">>> 웨이브 감지 시작! (목표: " + _totalWavesNeeded + "회)");
        _isActive = true;

        _lastY = -1f;
        _wasMovingUp = false;
        _peakY = 0f;
        _hasPeak = false;
        _waveCount = 0;
        _currentProgress = 0f;
        _isCompleted = false;

        if (_object1 != null) _object1.SetActive(false);
        if (_object2 != null) _object2.SetActive(false);
        if (_object3 != null) _object3.SetActive(false);

        if (_progressSlider != null)
            _progressSlider.value = 0f;
    }

    void ProcessPoseData(PoseLandmarkerResult result)
    {
        if (result.poseLandmarks == null || result.poseLandmarks.Count == 0) return;

        var landmarks = result.poseLandmarks[PoseUtils.SelectCenterPlayer(result)].landmarks;
        if (landmarks.Count <= 16) return;

        float noseY = landmarks[0].y;
        float lShoulderY = landmarks[11].y;
        float rShoulderY = landmarks[12].y;
        float lWristY = landmarks[15].y;
        float rWristY = landmarks[16].y;

        // 신체 비율 기반 동적 임계값 (코~어깨 거리)
        float shoulderY = (lShoulderY + rShoulderY) / 2f;
        float bodyScale = Mathf.Abs(shoulderY - noseY);
        float dynamicThreshold = bodyScale * _waveHeightMultiplier;

        // 점프 감지: 어깨 평균 Y로 몸 전체 움직임 추적 (반전: 높을수록 큰 값)
        float currentY = 1.0f - shoulderY;

        if (_lastY < 0)
        {
            _lastY = currentY;
            _peakY = currentY;
            Debug.Log("초기 Y 설정: " + currentY.ToString("F4") + " (bodyScale: " + bodyScale.ToString("F4") + ", threshold: " + dynamicThreshold.ToString("F4") + ")");
            return;
        }

        DetectWave(currentY, dynamicThreshold);
        _lastY = currentY;
    }

    void DetectWave(float currentY, float threshold)
    {
        float delta = currentY - _lastY;
        bool isMovingUp = delta > 0;

        // 위로 가다가 아래로 → 피크 발견
        if (_wasMovingUp && !isMovingUp)
        {
            _peakY = _lastY;
            _hasPeak = true;
        }
        // 아래로 가다가 위로 → 골 발견 → 웨이브 체크
        else if (!_wasMovingUp && isMovingUp)
        {
            float valleyY = _lastY;

            if (_hasPeak)
            {
                float waveHeight = _peakY - valleyY;

                if (waveHeight >= threshold)
                {
                    Debug.Log("!!! 웨이브! 높이: " + waveHeight.ToString("F4") + " (임계값: " + threshold.ToString("F4") + ")");
                    WaveDetected();
                    _hasPeak = false;
                }
            }
        }

        _wasMovingUp = isMovingUp;
    }

    void WaveDetected()
    {
        _waveCount++;
        _currentProgress = (float)_waveCount / _totalWavesNeeded;

        if (_progressSlider != null)
            _progressSlider.value = _currentProgress;

        // 활동 보고
        _fadeAnimatorController.ReportActivity();

        // 제스처 사운드 재생
        if (_gestureAudioSource != null && _gestureSound != null)
            _gestureAudioSource.PlayOneShot(_gestureSound);

        // 파티클 활성화
        PlayParticle();

        // 에너지 흐름 효과
        if (_energyFlowEffect != null)
            _energyFlowEffect.PlayEffect(1);  // Game1: 태양광

        Debug.Log(">>> 웨이브 진행: " + _waveCount + "/" + _totalWavesNeeded + " (" + (_currentProgress * 100f).ToString("F0") + "%)");

        if (_waveCount >= _totalWavesNeeded)
        {
            Debug.Log("!!! 웨이브 완료! Panel 3로 전환 준비!");
            _isCompleted = true;
            _fadeAnimatorController.AnimatorFadeInPlay();
        }
    }

    void PlayParticle()
    {
        if (_particleA == null) return;

        if (_particleA.activeSelf)
            _particleA.SetActive(false);

        _particleA.SetActive(true);
    }

    void UpdateObjectActivation()
    {
        float progress = _currentProgress;

        if (progress >= 0.33f && _object1 != null && !_object1.activeSelf)
        {
            _object1.SetActive(true);
            Debug.Log(">>> Object 1 활성화! (33% 달성)");
        }

        if (progress >= 0.66f && _object2 != null && !_object2.activeSelf)
        {
            _object2.SetActive(true);
            Debug.Log(">>> Object 2 활성화! (66% 달성)");
        }

        if (progress >= 1.0f && _object3 != null && !_object3.activeSelf)
        {
            _object3.SetActive(true);
            Debug.Log(">>> Object 3 활성화! (100% 달성)");
        }
    }

    public void OnEventStartCoroutine()
    {
        StartCoroutine(TransitionToPanel3());
    }

    IEnumerator TransitionToPanel3()
    {
        Debug.Log(">>> Panel 3로 전환 실행!");

        if (_panel2 != null)
            _panel2.SetActive(false);

        if (_panel3 != null)
            _panel3.SetActive(true);

        yield return null;
    }

    public void ResetWaveController()
    {
        Debug.Log("HandWaveController 리셋!");

        _lastY = -1f;
        _wasMovingUp = false;
        _peakY = 0f;
        _hasPeak = false;
        _waveCount = 0;
        _currentProgress = 0f;
        _isCompleted = false;
        _isActive = false;

        if (_object1 != null) _object1.SetActive(false);
        if (_object2 != null) _object2.SetActive(false);
        if (_object3 != null) _object3.SetActive(false);

        if (_progressSlider != null)
            _progressSlider.value = 0f;
    }
}
