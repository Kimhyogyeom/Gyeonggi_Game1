using UnityEngine;
using UnityEngine.UI;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using System.Collections;

public class HandSwingController : MonoBehaviour
{
    [SerializeField] private FadeAnimatorController _fadeAnimatorController;

    [Header("Panel References")]
    public GameObject _targetPanel;
    public GameObject _nextPanel;

    [Header("Objects to Activate")]
    public GameObject _object1;
    public GameObject _object2;
    public GameObject _object3;

    [Header("Wave Detection Settings")]
    [Tooltip("웨이브 높이 배수 (코~어깨 거리 기준, 0.8 = 80%)")]
    public float _waveHeightMultiplier = 0.8f;
    public int _totalWavesNeeded = 12;
    [Tooltip("웨이브 인식 후 쿨다운 (초)")]
    public float _waveCooldown = 0.4f;
    [Tooltip("Y값 스무딩 강도 (0~1, 낮을수록 부드러움)")]
    public float _smoothing = 0.3f;

    [Header("Slider Settings")]
    public Slider _progressSlider;

    [Header("Particle System")]
    [SerializeField] private GameObject _particleC;

    [Header("Energy Flow Effect")]
    [SerializeField] private EnergyFlowEffect _energyFlowEffect;

    [Header("제스처 사운드")]
    [SerializeField] private AudioSource _gestureAudioSource;
    [SerializeField] private AudioClip _gestureSound;

    // Wave detection state (Level 1과 동일한 Y축 웨이브 로직)
    private float _lastY = -1f;
    private float _smoothedY = -1f;
    private bool _wasMovingUp = false;
    private float _peakY = 0f;
    private bool _hasPeak = false;
    private float _lastWaveTime = -10f;

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
        Debug.Log("=== HandSwingController 시작 (Pose 기반, Y축 웨이브) ===");
        Debug.Log("필요한 웨이브 횟수: " + _totalWavesNeeded);
    }

    void Update()
    {
        if (_targetPanel != null && _targetPanel.activeSelf)
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
                Debug.Log("패널 비활성화");
            }
        }
    }

    void StartWaveDetection()
    {
        Debug.Log(">>> 웨이브 감지 시작! (목표: " + _totalWavesNeeded + "회)");
        _isActive = true;

        _lastY = -1f;
        _smoothedY = -1f;
        _wasMovingUp = false;
        _peakY = 0f;
        _hasPeak = false;
        _lastWaveTime = -10f;
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

        // 양 손목 평균 Y (반전: 높을수록 큰 값)
        float rawY = 1.0f - (lWristY + rWristY) / 2f;

        if (_lastY < 0)
        {
            _lastY = rawY;
            _smoothedY = rawY;
            _peakY = rawY;
            return;
        }

        // 스무딩 적용 (떨림 제거)
        _smoothedY = Mathf.Lerp(_smoothedY, rawY, _smoothing);

        DetectWave(_smoothedY, dynamicThreshold);
        _lastY = _smoothedY;
    }

    void DetectWave(float currentY, float threshold)
    {
        float delta = currentY - _lastY;
        bool isMovingUp = delta > 0;

        if (_wasMovingUp && !isMovingUp)
        {
            _peakY = _lastY;
            _hasPeak = true;
        }
        else if (!_wasMovingUp && isMovingUp)
        {
            float valleyY = _lastY;

            if (_hasPeak)
            {
                float waveHeight = _peakY - valleyY;

                if (waveHeight >= threshold && Time.time - _lastWaveTime >= _waveCooldown)
                {
                    Debug.Log("!!! 웨이브! 높이: " + waveHeight.ToString("F4") + " (임계값: " + threshold.ToString("F4") + ")");
                    _lastWaveTime = Time.time;
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

        _fadeAnimatorController.ReportActivity();

        if (_gestureAudioSource != null && _gestureSound != null)
            _gestureAudioSource.PlayOneShot(_gestureSound);

        PlayParticle();

        if (_energyFlowEffect != null)
            _energyFlowEffect.PlayEffect(3);  // Game3: 풍력

        Debug.Log(">>> 웨이브 진행: " + _waveCount + "/" + _totalWavesNeeded + " (" + (_currentProgress * 100f).ToString("F0") + "%)");

        if (_waveCount >= _totalWavesNeeded)
        {
            Debug.Log("!!! 웨이브 완료! 다음 패널로 전환 준비!");
            _isCompleted = true;
            _fadeAnimatorController.AnimatorFadeInPlay();
        }
    }

    void PlayParticle()
    {
        if (_particleC == null) return;

        if (_particleC.activeSelf)
            _particleC.SetActive(false);

        _particleC.SetActive(true);
    }

    void UpdateObjectActivation()
    {
        float progress = _currentProgress;

        if (progress >= 0.33f && _object1 != null && !_object1.activeSelf)
            _object1.SetActive(true);

        if (progress >= 0.66f && _object2 != null && !_object2.activeSelf)
            _object2.SetActive(true);

        if (progress >= 1.0f && _object3 != null && !_object3.activeSelf)
            _object3.SetActive(true);
    }

    public void OnEventStartCoroutine()
    {
        StartCoroutine(TransitionToNextPanel());
    }

    IEnumerator TransitionToNextPanel()
    {
        if (_targetPanel != null)
            _targetPanel.SetActive(false);

        if (_nextPanel != null)
            _nextPanel.SetActive(true);

        yield return null;
    }

    public void ResetSwingController()
    {
        Debug.Log("HandSwingController 리셋!");

        _lastY = -1f;
        _smoothedY = -1f;
        _wasMovingUp = false;
        _peakY = 0f;
        _hasPeak = false;
        _lastWaveTime = -10f;
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
