using UnityEngine;
using UnityEngine.UI;

public class WebcamController : MonoBehaviour
{
    [Header("UI Reference")]
    public RawImage _webcamDisplay;

    [Header("Webcam Settings")]
    public int _targetWidth = 640;
    public int _targetHeight = 480;
    public int _targetFPS = 60;

    private WebCamTexture _webCamTexture;

    void Start()
    {
        InitializeWebcam();
    }

    void InitializeWebcam()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.LogError("웹캠을 찾을 수 없습니다!");
            return;
        }

        // 첫 번째 웹캠 사용
        string deviceName = devices[0].name;
        Debug.Log($"사용할 웹캠: {deviceName}");

        // C920 찾기 시도
        foreach (var device in devices)
        {
            Debug.Log($"발견된 카메라: {device.name}");
            if (device.name.Contains("C920") || device.name.Contains("HD Pro"))
            {
                deviceName = device.name;
                Debug.Log($"C920 발견! {deviceName}");
                break;
            }
        }

        // 웹캠 시작
        _webCamTexture = new WebCamTexture(deviceName, _targetWidth, _targetHeight, _targetFPS);
        _webcamDisplay.texture = _webCamTexture;
        _webCamTexture.Play();

        Debug.Log($"웹캠 시작 완료: {_targetWidth}x{_targetHeight} @ {_targetFPS}fps");
    }

    void OnDestroy()
    {
        if (_webCamTexture != null && _webCamTexture.isPlaying)
        {
            _webCamTexture.Stop();
        }
    }

    // 외부에서 접근 가능
    public WebCamTexture GetWebcamTexture()
    {
        return _webCamTexture;
    }

    public bool IsPlaying()
    {
        return _webCamTexture != null && _webCamTexture.isPlaying;
    }
}
