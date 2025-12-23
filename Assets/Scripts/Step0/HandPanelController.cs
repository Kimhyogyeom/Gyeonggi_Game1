using UnityEngine;
using UnityEngine.UI;
using Mediapipe.Tasks.Vision.HandLandmarker;
using System.Reflection;
/// <summary>
/// https://github.com/homuler/MediaPipeUnityPlugin/releases
/// </summary>
public class HandPanelController : MonoBehaviour
{
    [Header("Fade Animator")]
    [SerializeField] private FadeAnimatorController _fadeAnimatorController;

    [Header("Panel References")]
    public GameObject _panel1;
    public GameObject _panel2;

    [Header("Detection Settings")]
    public float _yThreshold = 1500f;

    [Header("Slider Settings")]
    public Slider _progressSlider;
    public float _fillDuration = 3f;  // 3초 동안 채우기

    [Header("Visual Feedback")]
    public Image _fingerIndicator;

    [Header("Hand Tracking")]
    public GameObject _solutionObject;

    private bool _hasTransitioned = false;
    private Component _handRunner;
    private Component _annotationController;
    private FieldInfo _resultField;

    private float _currentProgress = 0f;  // 현재 슬라이더 진행도 (0 ~ 1)
    private bool _isCharging = false;     // 충전 중인지 여부

    [Header("Particle Sysyem_Light")]
    [SerializeField] private GameObject _light;

    void Start()
    {
        _panel1.SetActive(true);
        _panel2.SetActive(false);
        _hasTransitioned = false;

        Debug.Log("=== HandPanelController 시작 ===");
        Debug.Log("Y Threshold: " + _yThreshold + "px");
        Debug.Log("Fill Duration: " + _fillDuration + "초");

        // 슬라이더 초기화
        if (_progressSlider != null)
        {
            _progressSlider.value = 0f;
        }

        if (_solutionObject != null)
        {
            foreach (var comp in _solutionObject.GetComponents<Component>())
            {
                string typeName = comp.GetType().Name;

                if (typeName.Contains("HandLandmarker") || typeName.Contains("Runner"))
                {
                    _handRunner = comp;
                    Debug.Log("Runner 발견: " + typeName);

                    var type = comp.GetType();

                    foreach (var field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (field.FieldType.Name.Contains("AnnotationController"))
                        {
                            _annotationController = field.GetValue(comp) as Component;
                            Debug.Log("Annotation Controller 획득!");

                            if (_annotationController != null)
                            {
                                var annoType = _annotationController.GetType();
                                foreach (var annoField in annoType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                                {
                                    if (annoField.FieldType.Name.Contains("HandLandmarkerResult"))
                                    {
                                        _resultField = annoField;
                                        Debug.Log("결과 필드 발견!");
                                        break;
                                    }
                                }
                            }
                            break;
                        }
                    }

                    break;
                }
            }
        }
    }

    void Update()
    {
        if (!_hasTransitioned && _annotationController != null)
        {
            ProcessHandTracking();
        }

        // 슬라이더 업데이트
        UpdateSlider();
    }

    void ProcessHandTracking()
    {
        if (_resultField == null) return;

        try
        {
            var obj = _resultField.GetValue(_annotationController);

            if (obj != null)
            {
                HandLandmarkerResult result = (HandLandmarkerResult)obj;

                if (result.handLandmarks != null && result.handLandmarks.Count > 0)
                {
                    ProcessHandData(result);
                    return;
                }
            }

            // 손이 없으면 충전 중지
            _isCharging = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError("결과 접근 실패: " + e.Message);
            _isCharging = false;
        }
    }

    void ProcessHandData(HandLandmarkerResult result)
    {
        if (_hasTransitioned) return;

        // 검지 손가락 끝 (8번 랜드마크)
        var indexTip = result.handLandmarks[0].landmarks[8];

        // 화면 좌표로 변환
        Vector2 fingerPos = new Vector2(
            indexTip.x * Screen.width,
            (1 - indexTip.y) * Screen.height
        );

        // Finger Indicator 업데이트
        if (_fingerIndicator != null)
        {
            _fingerIndicator.gameObject.SetActive(true);
            _fingerIndicator.rectTransform.position = fingerPos;
        }

        // Y좌표 체크
        CheckYPosition(fingerPos);
    }

    void CheckYPosition(Vector2 fingerPos)
    {
        Debug.Log("[손 추적] 검지 Y 위치: " + fingerPos.y + "px (임계값: " + _yThreshold + "px)");

        // Y좌표가 임계값 이상이면 충전 시작
        if (fingerPos.y >= _yThreshold)
        {
            if (!_isCharging)
            {
                Debug.Log("충전 시작!");
            }
            _isCharging = true;
        }
        else
        {
            if (_isCharging)
            {
                Debug.Log("충전 중지! 현재 진행도: " + (_currentProgress * 100f) + "%");
            }
            _isCharging = false;
        }
    }

    void UpdateSlider()
    {
        if (_hasTransitioned) return;

        if (_isCharging)
        {
            // 충전 중: 슬라이더 증가
            _currentProgress += Time.deltaTime / _fillDuration;
            _currentProgress = Mathf.Clamp01(_currentProgress);  // 0 ~ 1 사이로 제한

            if (_progressSlider != null)
            {
                _progressSlider.value = _currentProgress;
                if (!_light.activeSelf)
                {
                    _light.SetActive(true);
                }
            }

            // 100% 도달하면 전환
            if (_currentProgress >= 1f)
            {
                _fadeAnimatorController.AnimatorFadeInPlay();
                // Debug.Log("슬라이더 100% 도달! Panel 2로 전환!");
                // TransitionToPanel2();
            }
        }
        else
        {
            if (_light.activeSelf)
            {
                _light.SetActive(false);
            }
        }
        // 충전 중이 아니면 현재 값 유지 (멈춤)
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

        if (_fingerIndicator != null)
        {
            _fingerIndicator.gameObject.SetActive(false);
        }
    }

    // 외부에서 호출할 리셋 함수
    public void ResetProgress()
    {
        Debug.Log("슬라이더 리셋!");
        _currentProgress = 0f;
        _isCharging = false;
        _hasTransitioned = false;

        if (_progressSlider != null)
        {
            _progressSlider.value = 0f;
        }

        _panel1.SetActive(true);
        _panel2.SetActive(false);
    }
}
