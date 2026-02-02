using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StartupLogger
{
    private static string _logFilePath;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // exe 파일 옆에 로그 파일 생성
        string exeFolder = Directory.GetParent(Application.dataPath).FullName;
        _logFilePath = Path.Combine(exeFolder, "webcam_log.txt");

        try
        {
            File.WriteAllText(_logFilePath, $"=== StartupLogger: {System.DateTime.Now} ===\n");
            Log($"Unity Version: {Application.unityVersion}");
            Log($"Platform: {Application.platform}");
            Log($"dataPath: {Application.dataPath}");
            Log($"Log file: {_logFilePath}");
            Log($"WebCamTexture.devices.Length: {WebCamTexture.devices.Length}");

            foreach (var device in WebCamTexture.devices)
            {
                Log($"  Camera: {device.name}");
            }

            // 씬 로드 이벤트 등록
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"StartupLogger 실패: {e.Message}");
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Log($"=== Scene Loaded: {scene.name} ===");
        Log($"Camera count: {WebCamTexture.devices.Length}");
    }

    public static void Log(string message)
    {
        Debug.Log($"[StartupLogger] {message}");
        try
        {
            if (!string.IsNullOrEmpty(_logFilePath))
            {
                File.AppendAllText(_logFilePath, $"[{System.DateTime.Now:HH:mm:ss}] {message}\n");
            }
        }
        catch { }
    }

    public static void LogError(string message)
    {
        Debug.LogError($"[StartupLogger] {message}");
        try
        {
            if (!string.IsNullOrEmpty(_logFilePath))
            {
                File.AppendAllText(_logFilePath, $"[{System.DateTime.Now:HH:mm:ss}] ERROR: {message}\n");
            }
        }
        catch { }
    }
}
