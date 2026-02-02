using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class WebcamController : MonoBehaviour
{
    [Header("UI Reference")]
    public RawImage _webcamDisplay;

    [Header("Webcam Settings")]
    public int _targetWidth = 640;
    public int _targetHeight = 480;
    public int _targetFPS = 60;

    private WebCamTexture _webCamTexture;
    private bool _isPermissionGranted = false;
    private static string _logFilePath;

    void Awake()
    {
        // 빌드 파일 옆에 로그 파일 생성 (Application.dataPath의 상위 폴더 = exe 위치)
        string exeFolder = Directory.GetParent(Application.dataPath).FullName;
        _logFilePath = Path.Combine(exeFolder, "webcam_log.txt");
        try
        {
            File.WriteAllText(_logFilePath, $"=== WebcamController Log Started: {System.DateTime.Now} ===\n");
            File.AppendAllText(_logFilePath, $"Log path: {_logFilePath}\n");
            File.AppendAllText(_logFilePath, $"Application.dataPath: {Application.dataPath}\n");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"로그 파일 생성 실패: {e.Message}");
        }
    }

    private static void Log(string message)
    {
        Debug.Log(message);
        try
        {
            if (!string.IsNullOrEmpty(_logFilePath))
            {
                File.AppendAllText(_logFilePath, $"[{System.DateTime.Now:HH:mm:ss}] {message}\n");
            }
        }
        catch { }
    }

    private static void LogError(string message)
    {
        Debug.LogError(message);
        try
        {
            if (!string.IsNullOrEmpty(_logFilePath))
            {
                File.AppendAllText(_logFilePath, $"[{System.DateTime.Now:HH:mm:ss}] ERROR: {message}\n");
            }
        }
        catch { }
    }

    IEnumerator Start()
    {
        Log("[WebcamController] Start 시작");

        // Windows에서는 권한 요청 불필요, 바로 초기화
        yield return InitializeWebcam();
    }

    IEnumerator InitializeWebcam()
    {
        Log("[WebcamController] InitializeWebcam 시작");

        // 첫 프레임 대기 (Unity 초기화 완료 보장)
        yield return null;

        WebCamDevice[] devices = WebCamTexture.devices;
        Log($"[WebcamController] 감지된 카메라 수: {devices.Length}");

        if (devices.Length == 0)
        {
            LogError("[WebcamController] 웹캠을 찾을 수 없습니다!");
            yield break;
        }

        // 첫 번째 웹캠 사용
        string deviceName = devices[0].name;
        Log($"[WebcamController] 기본 웹캠: {deviceName}");

        // C920 찾기 시도
        foreach (var device in devices)
        {
            Log($"[WebcamController] 발견된 카메라: {device.name}");
            if (device.name.Contains("C920") || device.name.Contains("HD Pro"))
            {
                deviceName = device.name;
                Log($"[WebcamController] C920 발견! {deviceName}");
                break;
            }
        }

        // 웹캠 시작
        Log($"[WebcamController] WebCamTexture 생성 중: {deviceName}");
        _webCamTexture = new WebCamTexture(deviceName, _targetWidth, _targetHeight, _targetFPS);

        Log("[WebcamController] Play() 호출");
        _webCamTexture.Play();
        Log($"[WebcamController] Play() 완료, isPlaying: {_webCamTexture.isPlaying}");

        // 웹캠 초기화 완료 대기 (빌드에서 필수!)
        Log("[WebcamController] 웹캠 초기화 대기 중...");
        int timeoutFrames = 300; // 약 5초 (60fps 기준)
        int frameCount = 0;

        while (_webCamTexture.width <= 16 && frameCount < timeoutFrames)
        {
            frameCount++;
            if (frameCount % 60 == 0)
            {
                Log($"[WebcamController] 대기 중... {frameCount}프레임, width: {_webCamTexture.width}");
            }
            yield return null;
        }

        if (_webCamTexture.width <= 16)
        {
            LogError($"[WebcamController] 웹캠 초기화 타임아웃! width: {_webCamTexture.width}, isPlaying: {_webCamTexture.isPlaying}");
            yield break;
        }

        // 초기화 완료 후 텍스처 할당
        _webcamDisplay.texture = _webCamTexture;
        Log($"[WebcamController] 웹캠 시작 완료: {_webCamTexture.width}x{_webCamTexture.height}");
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
