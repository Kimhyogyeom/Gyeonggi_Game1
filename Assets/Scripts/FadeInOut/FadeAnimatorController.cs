using Unity.VisualScripting;
using UnityEngine;
using System.Collections;

public enum State
{
    Step0,  // 대기 상태
    Step1,  // 태양광
    Step2,  // 수력
    Step3,  // 풍력
    Step4  // 마무리
}
public class FadeAnimatorController : MonoBehaviour
{
    [SerializeField] private HandPanelController _handPanelController;
    [SerializeField] private HandWaveController _handWaveController;
    [SerializeField] private HandWaveController2 _handWaveController2;
    [SerializeField] private HandSwingController _handSwingController;
    [SerializeField] private ResetController _resetController;

    public State _currentState = State.Step0;

    [SerializeField] private Animator _animator;

    [Header("패널 전환 대기 시간 설정")]
    [SerializeField] private float _readyToGame1Delay = 0f;   // Ready → Game1
    [SerializeField] private float _game1ToGame2Delay = 0f;   // Game1 → Game2
    [SerializeField] private float _game2ToGame3Delay = 0f;   // Game2 → Game3
    [SerializeField] private float _game3ToEndDelay = 0f;     // Game3 → End
    [SerializeField] private float _endToReadyDelay = 5f;     // End → Ready

    [Header("패널별 사운드 설정")]
    [SerializeField] private AudioSource _sfxAudioSource;  // 효과음용
    [SerializeField] private AudioClip _readySound;    // Ready 패널 사운드
    [SerializeField] private AudioClip _game1Sound;    // Game1 패널 사운드
    [SerializeField] private AudioClip _game2Sound;    // Game2 패널 사운드
    [SerializeField] private AudioClip _game3Sound;    // Game3 패널 사운드
    [SerializeField] private AudioClip _endSound1;     // End 패널 사운드 1
    [SerializeField] private AudioClip _endSound2;     // End 패널 사운드 2 (1번 끝나고 재생)

    [Header("페이드 진입 시 활성화할 오브젝트 (5개)")]
    [SerializeField] private GameObject[] _fadeObjects = new GameObject[5];

    [Header("페이드 진입 시 음성 설정")]
    [SerializeField] private AudioClip _fadeReadyToGame1Voice;    // Ready → Game1 페이드 시
    [SerializeField] private AudioClip _fadeGame1ToGame2Voice;    // Game1 → Game2 페이드 시
    [SerializeField] private AudioClip _fadeGame2ToGame3Voice;    // Game2 → Game3 페이드 시
    [SerializeField] private AudioClip _fadeGame3ToEndVoice;      // Game3 → End 페이드 시
    [SerializeField] private AudioClip _fadeEndToReadyVoice;      // End → Ready 페이드 시

    [Header("비활동 타임아웃 설정")]
    [SerializeField] private float _inactivityTimeout = 15f;  // 이 시간(초) 동안 진행 없으면 Ready로 리셋

    [Header("BGM 설정")]
    [SerializeField] private AudioClip _bgmClip;           // BGM 클립
    [SerializeField] private bool _playBgmOnStart = true;  // 시작 시 BGM 재생 여부
    [SerializeField] [Range(0f, 1f)] private float _bgmVolume = 0.5f;  // BGM 볼륨

    private AudioSource _bgmAudioSource;  // 자동 생성됨
    private float _lastActivityTime;  // 마지막 활동 시간

    void Start()
    {
        // BGM용 AudioSource 자동 생성
        CreateBGMAudioSource();

        // 게임 시작 시 Ready 사운드 재생
        PlaySound(_readySound);

        // BGM 재생
        if (_playBgmOnStart)
        {
            PlayBGM();
        }

        // 비활동 타이머 초기화
        _lastActivityTime = Time.time;
    }

    void Update()
    {
        // 비활동 타임아웃 체크 (Step4 제외, 페이드 중 제외)
        if (_currentState != State.Step4 && !_animator.GetBool("Fade"))
        {
            if (Time.time - _lastActivityTime > _inactivityTimeout)
            {
                ResetToReady();
            }
        }
    }

    /// <summary>
    /// 각 컨트롤러에서 활동(제스처 감지) 시 호출하여 타임아웃 리셋
    /// </summary>
    public void ReportActivity()
    {
        _lastActivityTime = Time.time;
    }

    /// <summary>
    /// 비활동 타임아웃 → Ready(Step0)로 전체 리셋
    /// </summary>
    private void ResetToReady()
    {
        Debug.Log($"비활동 타임아웃 ({_inactivityTimeout}초)! Ready로 리셋합니다.");

        // 타이머 즉시 리셋 (중복 호출 방지)
        _lastActivityTime = Time.time;

        // 진행 중인 코루틴 정리
        StopAllCoroutines();

        // 전체 컨트롤러 리셋 (패널 포함)
        _resetController.ResetAllControllers();

        // 상태 초기화
        _currentState = State.Step0;
        _animator.SetBool("Fade", false);

        // 페이드 오브젝트 정리
        DeactivateAllFadeObjects();

        // Ready 사운드 재생
        PlaySound(_readySound);
    }

    void CreateBGMAudioSource()
    {
        // BGM 전용 자식 오브젝트 생성
        GameObject bgmObj = new GameObject("BGM_AudioSource");
        bgmObj.transform.SetParent(this.transform);
        _bgmAudioSource = bgmObj.AddComponent<AudioSource>();
        _bgmAudioSource.playOnAwake = false;
        _bgmAudioSource.loop = true;
    }

    public void PlayBGM()
    {
        if (_bgmAudioSource == null || _bgmClip == null) return;

        _bgmAudioSource.clip = _bgmClip;
        _bgmAudioSource.loop = true;
        _bgmAudioSource.volume = _bgmVolume;
        _bgmAudioSource.Play();

        Debug.Log($"BGM 재생: {_bgmClip.name}");
    }

    public void StopBGM()
    {
        if (_bgmAudioSource == null) return;

        _bgmAudioSource.Stop();
        Debug.Log("BGM 정지");
    }

    public void SetBGMVolume(float volume)
    {
        if (_bgmAudioSource == null) return;

        _bgmVolume = Mathf.Clamp01(volume);
        _bgmAudioSource.volume = _bgmVolume;
    }

    public void AnimatorFadeInPlay()
    {
        // 스텝 전환 시 활동 타이머 리셋
        _lastActivityTime = Time.time;

        // 현재 상태에 맞는 페이드 오브젝트 활성화 + 음성 재생
        int stateIndex = (int)_currentState;
        ActivateFadeObject(stateIndex);

        switch (_currentState)
        {
            case State.Step0: PlaySound(_fadeReadyToGame1Voice); break;
            case State.Step1: PlaySound(_fadeGame1ToGame2Voice); break;
            case State.Step2: PlaySound(_fadeGame2ToGame3Voice); break;
            case State.Step3: PlaySound(_fadeGame3ToEndVoice); break;
            case State.Step4: PlaySound(_fadeEndToReadyVoice); break;
        }

        _animator.SetBool("Fade", true);
    }

    public void AnimatorFadeOutPlay()
    {
        switch (_currentState)
        {
            case State.Step0:
                StartCoroutine(DelayedTransition(_readyToGame1Delay, () =>
                {
                    _currentState = State.Step1;
                    _handPanelController.TransitionToPanel2();
                    PlaySound(_game1Sound);  // Game1 사운드 재생
                }));
                return;

            case State.Step1:
                StartCoroutine(DelayedTransition(_game1ToGame2Delay, () =>
                {
                    _currentState = State.Step2;
                    _handWaveController.OnEventStartCoroutine();
                    PlaySound(_game2Sound);  // Game2 사운드 재생
                }));
                return;

            case State.Step2:
                StartCoroutine(DelayedTransition(_game2ToGame3Delay, () =>
                {
                    _currentState = State.Step3;
                    _handWaveController2.OnEventStartCoroutine();
                    PlaySound(_game3Sound);  // Game3 사운드 재생
                }));
                return;

            case State.Step3:
                StartCoroutine(DelayedTransition(_game3ToEndDelay, () =>
                {
                    _currentState = State.Step4;
                    _handSwingController.OnEventStartCoroutine();
                    StartCoroutine(PlayEndSoundsSequence());  // End 사운드 순차 재생
                }));
                return;

            case State.Step4:
                StartCoroutine(DelayedTransition(_endToReadyDelay, () =>
                {
                    _currentState = State.Step0;
                    _resetController.ResetAllControllers();
                    PlaySound(_readySound);  // Ready 사운드 재생
                }));
                return;

            default:
                break;
        }
        _animator.SetBool("Fade", false);
    }

    void ActivateFadeObject(int index)
    {
        for (int i = 0; i < _fadeObjects.Length; i++)
        {
            if (_fadeObjects[i] != null)
                _fadeObjects[i].SetActive(i == index);
        }
    }

    void DeactivateAllFadeObjects()
    {
        for (int i = 0; i < _fadeObjects.Length; i++)
        {
            if (_fadeObjects[i] != null)
                _fadeObjects[i].SetActive(false);
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (_sfxAudioSource == null || clip == null) return;

        _sfxAudioSource.Stop();
        _sfxAudioSource.clip = clip;
        _sfxAudioSource.Play();

        Debug.Log($"사운드 재생: {clip.name}");
    }

    IEnumerator PlayEndSoundsSequence()
    {
        // 첫 번째 사운드 재생
        if (_endSound1 != null)
        {
            PlaySound(_endSound1);
            Debug.Log($"End 사운드 1 재생: {_endSound1.name}");

            // 첫 번째 사운드 끝날 때까지 대기
            yield return new WaitForSeconds(_endSound1.length);
        }

        // 두 번째 사운드 재생
        if (_endSound2 != null)
        {
            PlaySound(_endSound2);
            Debug.Log($"End 사운드 2 재생: {_endSound2.name}");
        }
    }

    IEnumerator DelayedTransition(float delay, System.Action onComplete)
    {
        if (delay > 0)
        {
            Debug.Log($"전환 대기: {delay}초");
            yield return new WaitForSeconds(delay);
        }

        onComplete?.Invoke();
        DeactivateAllFadeObjects();
        _animator.SetBool("Fade", false);

        // 새 스텝 시작 시 활동 타이머 리셋
        _lastActivityTime = Time.time;

        Debug.Log("패널 전환 완료!");
    }
}
