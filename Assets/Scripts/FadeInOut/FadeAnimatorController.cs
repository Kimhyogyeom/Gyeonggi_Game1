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

    [Header("BGM 설정")]
    [SerializeField] private AudioClip _bgmClip;           // BGM 클립
    [SerializeField] private bool _playBgmOnStart = true;  // 시작 시 BGM 재생 여부
    [SerializeField] [Range(0f, 1f)] private float _bgmVolume = 0.5f;  // BGM 볼륨

    private AudioSource _bgmAudioSource;  // 자동 생성됨

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
        _animator.SetBool("Fade", false);

        Debug.Log("패널 전환 완료!");
    }
}
