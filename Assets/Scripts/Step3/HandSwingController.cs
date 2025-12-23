using UnityEngine;
using UnityEngine.UI;
using Mediapipe.Tasks.Vision.HandLandmarker;
using System.Reflection;
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

    [Header("Swing Detection Settings")]
    public float _swingThreshold = 400f;  // X축 좌우 이동 거리
    public int _totalSwingsNeeded = 12;   // 필요한 좌우 왕복 횟수

    [Header("Slider Settings")]
    public Slider _progressSlider;

    [Header("Hand Tracking")]
    public GameObject _solutionObject;

    private Component _annotationController;
    private FieldInfo _resultField;

    private float _lastX = -1f;
    private float _previousX = -1f;
    private bool _wasMovingRight = false;

    private float _peakX = 0f;
    private bool _hasPeak = false;

    private int _swingCount = 0;
    private float _currentProgress = 0f;
    private bool _isCompleted = false;

    private bool _isActive = false;
    [Header("Particle Sysyem_Light")]
    [SerializeField] private GameObject _light;
    void Start()
    {
        Debug.Log("=== HandSwingController 시작 ===");
        Debug.Log("필요한 좌우 흔들기 횟수: " + _totalSwingsNeeded);
        SetupHandTracking();
    }

    void Update()
    {
        if (_targetPanel != null && _targetPanel.activeSelf)
        {
            if (!_isActive)
            {
                StartSwingDetection();
            }

            if (!_isCompleted && _annotationController != null)
            {
                ProcessHandTracking();
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

    void StartSwingDetection()
    {
        Debug.Log(">>> 좌우 흔들기 감지 시작! (목표: " + _totalSwingsNeeded + "회)");
        _isActive = true;

        _lastX = -1f;
        _previousX = -1f;
        _wasMovingRight = false;
        _peakX = 0f;
        _hasPeak = false;
        _swingCount = 0;
        _currentProgress = 0f;
        _isCompleted = false;

        if (_object1 != null) _object1.SetActive(false);
        if (_object2 != null) _object2.SetActive(false);
        if (_object3 != null) _object3.SetActive(false);

        if (_progressSlider != null)
        {
            _progressSlider.value = 0f;
        }
    }

    void SetupHandTracking()
    {
        if (_solutionObject != null)
        {
            foreach (var comp in _solutionObject.GetComponents<Component>())
            {
                string typeName = comp.GetType().Name;

                if (typeName.Contains("HandLandmarker") || typeName.Contains("Runner"))
                {
                    var type = comp.GetType();

                    foreach (var field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (field.FieldType.Name.Contains("AnnotationController"))
                        {
                            _annotationController = field.GetValue(comp) as Component;

                            if (_annotationController != null)
                            {
                                var annoType = _annotationController.GetType();
                                foreach (var annoField in annoType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                                {
                                    if (annoField.FieldType.Name.Contains("HandLandmarkerResult"))
                                    {
                                        _resultField = annoField;
                                        Debug.Log("Hand Tracking 준비 완료!");
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
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("결과 접근 실패: " + e.Message);
        }
    }

    void ProcessHandData(HandLandmarkerResult result)
    {
        var indexTip = result.handLandmarks[0].landmarks[8];
        float currentX = indexTip.x * Screen.width;

        if (_lastX < 0)
        {
            _lastX = currentX;
            _previousX = currentX;
            _peakX = currentX;
            Debug.Log("초기 X 설정: " + currentX.ToString("F0"));
            return;
        }

        DetectSwing(currentX);

        _previousX = _lastX;
        _lastX = currentX;
    }

    void DetectSwing(float currentX)
    {
        float delta = currentX - _lastX;
        bool isMovingRight = delta > 0;

        // 방향 전환 감지: 오른쪽으로 가다가 왼쪽으로
        if (_wasMovingRight && !isMovingRight)
        {
            // 오른쪽 피크 발견!
            _peakX = _lastX;
            _hasPeak = true;

            if (Time.frameCount % 30 == 0)
            {
                Debug.Log("[오른쪽 피크] X: " + _peakX.ToString("F0"));
            }
        }
        // 방향 전환 감지: 왼쪽으로 가다가 오른쪽으로
        else if (!_wasMovingRight && isMovingRight)
        {
            // 왼쪽 골 발견!
            float valleyX = _lastX;

            // 피크가 있었다면 좌우 흔들기 완성 체크!
            if (_hasPeak)
            {
                float swingWidth = _peakX - valleyX;

                if (swingWidth >= _swingThreshold)
                {
                    Debug.Log("!!! 좌우 흔들기 감지! 너비: " + swingWidth.ToString("F0") + "px");
                    SwingDetected();
                    _hasPeak = false;
                }
                else
                {
                    Debug.Log("흔들기 너비 부족: " + swingWidth.ToString("F0") + "px (필요: " + _swingThreshold + "px)");
                }
            }

            if (Time.frameCount % 30 == 0)
            {
                Debug.Log("[왼쪽 골] X: " + valleyX.ToString("F0"));
            }
        }

        _wasMovingRight = isMovingRight;
    }

    void SwingDetected()
    {
        _swingCount++;

        _currentProgress = (float)_swingCount / _totalSwingsNeeded;

        if (_progressSlider != null)
        {
            _progressSlider.value = _currentProgress;
        }

        Debug.Log(">>> 좌우 흔들기 진행: " + _swingCount + "/" + _totalSwingsNeeded + " (" + (_currentProgress * 100f).ToString("F0") + "%)");

        if (_swingCount >= _totalSwingsNeeded)
        {
            Debug.Log("!!! 좌우 흔들기 완료! 다음 패널로 전환 준비!");
            _isCompleted = true;
            _fadeAnimatorController.AnimatorFadeInPlay();
        }
    }
    public void OnEventStartCoroutine()
    {
        StartCoroutine(TransitionToNextPanel());
    }
    void UpdateObjectActivation()
    {
        float progress = _currentProgress;

        // 1/3 완성 (33%)
        if (progress >= 0.33f && _object1 != null && !_object1.activeSelf)
        {
            _object1.SetActive(true);
            Debug.Log(">>> Object 1 활성화! (33% 달성)");
        }

        // 2/3 완성 (66%)
        if (progress >= 0.66f && _object2 != null && !_object2.activeSelf)
        {
            _object2.SetActive(true);
            Debug.Log(">>> Object 2 활성화! (66% 달성)");
        }

        // 완료 (100%)
        if (progress >= 1.0f && _object3 != null && !_object3.activeSelf)
        {
            _object3.SetActive(true);
            Debug.Log(">>> Object 3 활성화! (100% 달성)");
        }
    }

    IEnumerator TransitionToNextPanel()
    {
        Debug.Log("1초 후 다음 패널로 전환...");
        // yield return new WaitForSeconds(1f);

        Debug.Log(">>> 다음 패널로 전환 실행!");

        if (_targetPanel != null)
        {
            _targetPanel.SetActive(false);
            Debug.Log("현재 패널 비활성화 완료");
        }

        if (_nextPanel != null)
        {
            _nextPanel.SetActive(true);
            Debug.Log("다음 패널 활성화 완료");
        }
        yield return null;
    }

    public void ResetSwingController()
    {
        Debug.Log("HandSwingController 리셋!");

        _lastX = -1f;
        _previousX = -1f;
        _wasMovingRight = false;
        _peakX = 0f;
        _hasPeak = false;
        _swingCount = 0;
        _currentProgress = 0f;
        _isCompleted = false;
        _isActive = false;

        if (_object1 != null) _object1.SetActive(false);
        if (_object2 != null) _object2.SetActive(false);
        if (_object3 != null) _object3.SetActive(false);

        if (_progressSlider != null)
        {
            _progressSlider.value = 0f;
        }
    }
}
