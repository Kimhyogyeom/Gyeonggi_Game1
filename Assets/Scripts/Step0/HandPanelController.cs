using UnityEngine;
using UnityEngine.UI;
using Mediapipe.Tasks.Vision.PoseLandmarker;

public class HandPanelController : MonoBehaviour
{
    [Header("Fade Animator")]
    [SerializeField] private FadeAnimatorController _fadeAnimatorController;

    [Header("Panel References")]
    public GameObject _panel1;
    public GameObject _panel2;

    [Header("Detection Settings")]
    [Tooltip("손목이 코보다 이 값만큼 더 위에 있어야 인정 (정규화 좌표, 0.05 = 5%)")]
    public float _handAboveHeadOffset = 0.05f;

    [Header("Slider Settings")]
    public Slider _progressSlider;
    public float _fillDuration = 3f;

    [Header("Particle Sysyem_Light")]
    [SerializeField] private GameObject _light;

    private bool _hasTransitioned = false;
    private float _currentProgress = 0f;
    private bool _isCharging = false;
    private bool _isFading = false;

    // Pose 결과 저장 (콜백은 다른 스레드에서 올 수 있으므로 Update에서 처리)
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
        _panel1.SetActive(true);
        _panel2.SetActive(false);
        _hasTransitioned = false;

        Debug.Log("=== HandPanelController 시작 (Pose 기반) ===");

        if (_progressSlider != null)
            _progressSlider.value = 0f;
    }

    void Update()
    {
        if (!_hasTransitioned && !_isFading)
        {
            if (_hasNewResult)
            {
                _hasNewResult = false;
                ProcessPoseData(_latestResult);
            }
        }

        UpdateSlider();
    }

    void ProcessPoseData(PoseLandmarkerResult result)
    {
        if (_hasTransitioned) return;

        if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
        {
            _isCharging = false;
            return;
        }

        var landmarks = result.poseLandmarks[PoseUtils.SelectCenterPlayer(result)].landmarks;
        if (landmarks.Count <= 16)
        {
            _isCharging = false;
            return;
        }

        float noseY = landmarks[0].y;       // 코
        float lWristY = landmarks[15].y;     // 왼쪽 손목
        float rWristY = landmarks[16].y;     // 오른쪽 손목

        // MediaPipe: Y값이 작을수록 위쪽 (0=화면상단, 1=화면하단)
        bool leftHandUp = lWristY < noseY - _handAboveHeadOffset;
        bool rightHandUp = rWristY < noseY - _handAboveHeadOffset;

        if (leftHandUp && rightHandUp)
        {
            if (!_isCharging)
                Debug.Log("충전 시작! (양손 머리 위)");
            _isCharging = true;
        }
        else
        {
            if (_isCharging)
                Debug.Log("충전 중지! 현재 진행도: " + (_currentProgress * 100f).ToString("F0") + "%");
            _isCharging = false;
        }
    }

    void UpdateSlider()
    {
        if (_hasTransitioned || _isFading) return;

        if (_isCharging)
        {
            _currentProgress += Time.deltaTime / _fillDuration;
            _currentProgress = Mathf.Clamp01(_currentProgress);

            if (_progressSlider != null)
            {
                _progressSlider.value = _currentProgress;
                if (!_light.activeSelf)
                    _light.SetActive(true);
            }

            // 활동 보고 (비활동 타임아웃 리셋)
            _fadeAnimatorController.ReportActivity();

            // 100% 도달하면 전환
            if (_currentProgress >= 1f)
            {
                _isFading = true;
                _isCharging = false;
                _light.SetActive(false);
                _fadeAnimatorController.AnimatorFadeInPlay();
            }
        }
        else
        {
            // 충전 중이 아니면 게이지 초기화
            if (_currentProgress > 0f)
            {
                Debug.Log("충전 중지 -> 게이지 초기화!");
                _currentProgress = 0f;
                if (_progressSlider != null)
                    _progressSlider.value = 0f;
            }

            if (_light.activeSelf)
                _light.SetActive(false);
        }
    }

    /// <summary>
    /// Fade Out Call
    /// </summary>
    public void TransitionToPanel2()
    {
        if (_hasTransitioned) return;

        Debug.Log(">>> Panel 2로 전환!");

        _panel1.SetActive(false);
        _panel2.SetActive(true);
        _hasTransitioned = true;
    }

    // 외부에서 호출할 리셋 함수
    public void ResetProgress()
    {
        Debug.Log("슬라이더 리셋!");
        _currentProgress = 0f;
        _isCharging = false;
        _hasTransitioned = false;
        _isFading = false;

        if (_progressSlider != null)
            _progressSlider.value = 0f;

        _panel1.SetActive(true);
        _panel2.SetActive(false);
    }
}
