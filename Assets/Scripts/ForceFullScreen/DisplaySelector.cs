using UnityEngine;

public class DisplaySelector : MonoBehaviour
{
    [Header("Display Settings")]
    [Tooltip("0 = 주 모니터, 1 = 보조 모니터")]
    [SerializeField] private int _targetDisplay = 0;

    [Tooltip("항상 주 모니터(왼쪽)에 표시")]
    [SerializeField] private bool _alwaysUsePrimaryDisplay = true;

    void Awake()
    {
        SetDisplay();
    }

    void SetDisplay()
    {
#if !UNITY_EDITOR
        if (_alwaysUsePrimaryDisplay)
        {
            // 주 모니터의 왼쪽 상단에 창 배치
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);

            // 창 위치를 주 모니터로 이동 (0, 0)
            // Unity 2021.2+ 에서는 아래 방법 사용
            if (Display.displays.Length > 0)
            {
                Display.displays[0].Activate();
            }
        }
        else if (_targetDisplay < Display.displays.Length)
        {
            Display.displays[_targetDisplay].Activate();
        }

        Debug.Log($"[DisplaySelector] 모니터 수: {Display.displays.Length}, 선택된 모니터: {(_alwaysUsePrimaryDisplay ? 0 : _targetDisplay)}");
#endif
    }
}
