using UnityEngine;
using UnityEngine.UI;
using Mediapipe.Tasks.Vision.HandLandmarker;
using System.Reflection;
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
    public float _minimumWaveHeight = 400f;
    public int _totalWavesNeeded = 12;  // 6 → 12로 2배 증가!

    [Header("Slider Settings")]
    public Slider _progressSlider;

    [Header("Hand Tracking")]
    public GameObject _solutionObject;

    private Component _annotationController;
    private FieldInfo _resultField;

    private float _lastY = -1f;
    private float _previousY = -1f;
    private bool _wasMovingUp = false;

    private float _peakY = 0f;
    private bool _hasPeak = false;

    private int _waveCount = 0;
    private float _currentProgress = 0f;
    private bool _isCompleted = false;

    private bool _isActive = false;

    [Header("Particle Sysyem_Light")]
    [SerializeField] private GameObject _light;
    void Start()
    {
        Debug.Log("=== HandWaveController 시작 ===");
        Debug.Log("필요한 웨이브 횟수: " + _totalWavesNeeded);
        SetupHandTracking();
    }

    void Update()
    {
        if (_panel2 != null && _panel2.activeSelf)
        {
            if (!_isActive)
            {
                StartWaveDetection();
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
                Debug.Log("Panel2 비활성화");
            }
        }
    }

    void StartWaveDetection()
    {
        Debug.Log(">>> 웨이브 감지 시작! (목표: " + _totalWavesNeeded + "회)");
        _isActive = true;

        _lastY = -1f;
        _previousY = -1f;
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
        float currentY = (1 - indexTip.y) * Screen.height;

        if (_lastY < 0)
        {
            _lastY = currentY;
            _previousY = currentY;
            _peakY = currentY;
            Debug.Log("초기 Y 설정: " + currentY.ToString("F0"));
            return;
        }

        DetectWave(currentY);

        _previousY = _lastY;
        _lastY = currentY;
    }

    void DetectWave(float currentY)
    {
        float delta = currentY - _lastY;
        bool isMovingUp = delta > 0;

        if (_wasMovingUp && !isMovingUp)
        {
            _peakY = _lastY;
            _hasPeak = true;

            if (Time.frameCount % 30 == 0)
            {
                Debug.Log("[피크] Y: " + _peakY.ToString("F0"));
            }
        }
        else if (!_wasMovingUp && isMovingUp)
        {
            float valleyY = _lastY;

            if (_hasPeak)
            {
                float waveHeight = _peakY - valleyY;

                if (waveHeight >= _minimumWaveHeight)
                {
                    Debug.Log("!!! 웨이브! 높이: " + waveHeight.ToString("F0") + "px");
                    WaveDetected();
                    _hasPeak = false;
                }
                else
                {
                    Debug.Log("높이 부족: " + waveHeight.ToString("F0") + "px");
                }
            }

            if (Time.frameCount % 30 == 0)
            {
                Debug.Log("[골] Y: " + valleyY.ToString("F0"));
            }
        }

        _wasMovingUp = isMovingUp;
    }

    void WaveDetected()
    {
        _waveCount++;

        _currentProgress = (float)_waveCount / _totalWavesNeeded;

        if (_progressSlider != null)
        {
            _progressSlider.value = _currentProgress;
        }

        Debug.Log(">>> 웨이브 진행: " + _waveCount + "/" + _totalWavesNeeded + " (" + (_currentProgress * 100f).ToString("F0") + "%)");


        // 완료 체크
        if (_waveCount >= _totalWavesNeeded)
        {
            Debug.Log("!!! 웨이브 완료! Panel 3로 전환 준비!");
            _isCompleted = true;
            _fadeAnimatorController.AnimatorFadeInPlay();
        }
    }

    void UpdateObjectActivation()
    {
        // 진행도에 따라 오브젝트 활성화
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
    public void OnEventStartCoroutine()
    {
        StartCoroutine(TransitionToPanel3());
    }
    IEnumerator TransitionToPanel3()
    {
        Debug.Log("1초 후 Panel 3로 전환...");
        //yield return new WaitForSeconds(1f);

        Debug.Log(">>> Panel 3로 전환 실행!");

        if (_panel2 != null)
        {
            _panel2.SetActive(false);
            Debug.Log("Panel 2 비활성화 완료");
        }

        if (_panel3 != null)
        {
            _panel3.SetActive(true);
            Debug.Log("Panel 3 활성화 완료");
        }
        yield return null;
    }

    public void ResetWaveController()
    {
        Debug.Log("HandWaveController 리셋!");

        _lastY = -1f;
        _previousY = -1f;
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
        {
            _progressSlider.value = 0f;
        }
    }
}
