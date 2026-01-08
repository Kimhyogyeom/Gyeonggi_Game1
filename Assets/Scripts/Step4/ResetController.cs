using UnityEngine;
using System.Collections;

public class ResetController : MonoBehaviour
{
    [SerializeField] private FadeAnimatorController _fadeAnimatorController;
    [SerializeField] private HandPanelController _handPanelController;
    [SerializeField] private HandWaveController _handWaveController;
    [SerializeField] private HandWaveController2 _handWaveController2;
    [SerializeField] private HandSwingController _handSwingController;

    [SerializeField] private GameObject _currentPanel;
    [SerializeField] private GameObject _nextPanel;

    [Header("Auto Reset Timer")]
    [SerializeField] private float _autoResetDelay = 30f;  // 기본 30초, Inspector에서 변경 가능

    private Coroutine _autoResetCoroutine;

    void Update()
    {
        // 현재 패널(End 패널)이 활성화되면 자동 리셋 타이머 시작
        if (_currentPanel != null && _currentPanel.activeSelf)
        {
            if (_autoResetCoroutine == null)
            {
                _autoResetCoroutine = StartCoroutine(AutoResetCoroutine());
            }
        }
    }

    IEnumerator AutoResetCoroutine()
    {
        Debug.Log($"자동 리셋 타이머 시작: {_autoResetDelay}초 후 리셋됩니다.");

        yield return new WaitForSeconds(_autoResetDelay);

        Debug.Log("자동 리셋 실행! 페이드 전환 시작");

        // 페이드 애니메이션을 통해 리셋 (대기 시간 적용됨)
        _fadeAnimatorController.AnimatorFadeInPlay();
    }

    /// <summary>
    /// All Reset (FadeAnimatorController에서 호출됨)
    /// </summary>
    public void ResetAllControllers()
    {
        // 타이머 코루틴 정리
        if (_autoResetCoroutine != null)
        {
            StopCoroutine(_autoResetCoroutine);
            _autoResetCoroutine = null;
        }

        _handPanelController.ResetProgress();
        _handWaveController.ResetWaveController();
        _handWaveController2.ResetWaveController();
        _handSwingController.ResetSwingController();

        _currentPanel.SetActive(false);
        _nextPanel.SetActive(true);
    }
}
