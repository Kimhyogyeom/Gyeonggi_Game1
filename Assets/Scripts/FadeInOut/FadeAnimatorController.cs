using Unity.VisualScripting;
using UnityEngine;

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

    public void AnimatorFadeInPlay()
    {
        _animator.SetBool("Fade", true);
    }

    public void AnimatorFadeOutPlay()
    {
        switch (_currentState)
        {
            case State.Step0:
                {
                    _currentState = State.Step1;
                    _handPanelController.TransitionToPanel2();
                    break;
                }
            case State.Step1:
                {
                    _currentState = State.Step2;
                    _handWaveController.OnEventStartCoroutine();
                    break;
                }
            case State.Step2:
                {
                    _currentState = State.Step3;
                    _handWaveController2.OnEventStartCoroutine();
                    break;
                }
            case State.Step3:
                {
                    _currentState = State.Step4;
                    _handSwingController.OnEventStartCoroutine();
                    break;
                }
            case State.Step4:
                {
                    _currentState = State.Step0;
                    _resetController.ResetAllControllers();
                    break;
                }

            default:
                {
                    break;
                }
        }
        _animator.SetBool("Fade", false);
    }
}
