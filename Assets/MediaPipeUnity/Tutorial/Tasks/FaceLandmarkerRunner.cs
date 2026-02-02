using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Mediapipe.Unity.Tutorial
{
  public class FaceLandmarkerRunner : MonoBehaviour
  {
    [SerializeField] private RawImage screen;
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private int fps;

    private WebCamTexture webCamTexture;

    private IEnumerator Start()
    {
      // 카메라 권한 요청
      yield return RequestCameraPermission();

      if (WebCamTexture.devices.Length == 0)
      {
        Debug.LogError("Web Camera devices are not found. 카메라 권한이 거부되었거나 카메라가 없습니다.");
        yield break;
      }
      var webCamDevice = WebCamTexture.devices[0];
      webCamTexture = new WebCamTexture(webCamDevice.name, width, height, fps);
      webCamTexture.Play();

      // NOTE: On macOS, the contents of webCamTexture may not be readable immediately, so wait until it is readable
      yield return new WaitUntil(() => webCamTexture.width > 16);

      screen.rectTransform.sizeDelta = new Vector2(width, height);
      screen.texture = webCamTexture;
    }

    private IEnumerator RequestCameraPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
      if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
      {
        Permission.RequestUserPermission(Permission.Camera);

        float timeout = 10f;
        float elapsed = 0f;

        while (!Permission.HasUserAuthorizedPermission(Permission.Camera) && elapsed < timeout)
        {
          elapsed += Time.deltaTime;
          yield return null;
        }
      }

      if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
      {
        Debug.LogError("카메라 권한이 거부되었습니다!");
      }

#elif UNITY_IOS && !UNITY_EDITOR
      if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
      {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
      }

      if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
      {
        Debug.LogError("카메라 권한이 거부되었습니다!");
      }

#else
      yield return null;
#endif
    }

    private void OnDestroy()
    {
      if (webCamTexture != null)
      {
        webCamTexture.Stop();
      }
    }
  }
}
