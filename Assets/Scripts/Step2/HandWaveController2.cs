using UnityEngine;
using UnityEngine.UI;
using Mediapipe.Tasks.Vision.HandLandmarker;
using System.Reflection;
using System.Collections;

public class HandWaveController2 : MonoBehaviour
{
    [SerializeField] private FadeAnimatorController _fadeAnimatorController;
    [Header("Panel References")]
    public GameObject _panel3;
    public GameObject _panel4;

    [Header("Objects to Activate")]
    public GameObject _object1;
    public GameObject _object2;
    public GameObject _object3;

    [Header("Wave Detection Settings")]
    public float _minimumWaveHeight = 400f;
    public int _totalWavesNeeded = 12;

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
    private bool __isCompleted = false;

    private bool __isActive = false;

    [Header("Particle Sysyem_Light")]
    [SerializeField] private GameObject _light;
    void Start()
    {
        Debug.Log("=== HandWaveController3 시작 ===");
        Debug.Log("필요한 웨이브 횟수: " + _totalWavesNeeded);
        SetupHandTracking();
    }

    void Update()
    {
        if (_panel3 != null && _panel3.activeSelf)
        {
            if (!__isActive)
            {
                StartWaveDetection();
            }

            if (!__isCompleted && _annotationController != null)
            {
                ProcessHandTracking();
                UpdateObjectActivation();
            }
        }
        else
        {
            if (__isActive)
            {
                __isActive = false;
                Debug.Log("Panel3 비활성화");
            }
        }
    }

    void StartWaveDetection()
    {
        Debug.Log(">>> Panel 3 웨이브 감지 시작! (목표: " + _totalWavesNeeded + "회)");
        __isActive = true;

        _lastY = -1f;
        _previousY = -1f;
        _wasMovingUp = false;
        _peakY = 0f;
        _hasPeak = false;
        _waveCount = 0;
        _currentProgress = 0f;
        __isCompleted = false;

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
                                        Debug.Log("Panel 3 Hand Tracking 준비 완료!");
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
            Debug.Log("Panel 3 초기 Y 설정: " + currentY.ToString("F0"));
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
                Debug.Log("[Panel 3 피크] Y: " + _peakY.ToString("F0"));
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
                    Debug.Log("!!! Panel 3 웨이브! 높이: " + waveHeight.ToString("F0") + "px");
                    WaveDetected();
                    _hasPeak = false;
                }
                else
                {
                    Debug.Log("Panel 3 높이 부족: " + waveHeight.ToString("F0") + "px");
                }
            }

            if (Time.frameCount % 30 == 0)
            {
                Debug.Log("[Panel 3 골] Y: " + valleyY.ToString("F0"));
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

        Debug.Log(">>> Panel 3 웨이브 진행: " + _waveCount + "/" + _totalWavesNeeded + " (" + (_currentProgress * 100f).ToString("F0") + "%)");

        if (_waveCount >= _totalWavesNeeded)
        {
            Debug.Log("!!! Panel 3 웨이브 완료! Panel 4로 전환 준비!");
            __isCompleted = true;
            _fadeAnimatorController.AnimatorFadeInPlay();
        }
    }
    public void OnEventStartCoroutine()
    {
        StartCoroutine(TransitionToPanel4());
    }
    void UpdateObjectActivation()
    {
        float progress = _currentProgress;

        // 1/3 완성 (33%)
        if (progress >= 0.33f && _object1 != null && !_object1.activeSelf)
        {
            _object1.SetActive(true);
            Debug.Log(">>> Panel 3 Object 1 활성화! (33% 달성)");
        }

        // 2/3 완성 (66%)
        if (progress >= 0.66f && _object2 != null && !_object2.activeSelf)
        {
            _object2.SetActive(true);
            Debug.Log(">>> Panel 3 Object 2 활성화! (66% 달성)");
        }

        // 완료 (100%)
        if (progress >= 1.0f && _object3 != null && !_object3.activeSelf)
        {
            _object3.SetActive(true);
            Debug.Log(">>> Panel 3 Object 3 활성화! (100% 달성)");
        }
    }

    IEnumerator TransitionToPanel4()
    {
        Debug.Log("1초 후 Panel 4로 전환...");
        // yield return new WaitForSeconds(1f);

        Debug.Log(">>> Panel 4로 전환 실행!");

        if (_panel3 != null)
        {
            _panel3.SetActive(false);
            Debug.Log("Panel 3 비활성화 완료");
        }

        if (_panel4 != null)
        {
            _panel4.SetActive(true);
            Debug.Log("Panel 4 활성화 완료");
        }
        yield return null;
    }

    public void ResetWaveController()
    {
        Debug.Log("HandWaveController3 리셋!");

        _lastY = -1f;
        _previousY = -1f;
        _wasMovingUp = false;
        _peakY = 0f;
        _hasPeak = false;
        _waveCount = 0;
        _currentProgress = 0f;
        __isCompleted = false;
        __isActive = false;

        if (_object1 != null) _object1.SetActive(false);
        if (_object2 != null) _object2.SetActive(false);
        if (_object3 != null) _object3.SetActive(false);

        if (_progressSlider != null)
        {
            _progressSlider.value = 0f;
        }
    }
}
