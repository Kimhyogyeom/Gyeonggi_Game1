using UnityEngine;
using UnityEngine.UI;
namespace SeongWon
{


public class LoadingBarUI : MonoBehaviour
{
    public static LoadingBarUI instance;

    [Header("UI References")]
    [SerializeField] private Image progressBar;
    [SerializeField] private GameObject container;

    private void Awake()
    {
        instance = this;
    }

    private void OnEnable()
    {
        ResetGauge();
    }

    public void IncreaseGauge() 
    {
        float progress = PoseHeadAndGestureController.instance.GestureProgress;
        progressBar.fillAmount = progress;
    }

    public void ResetGauge() 
    {
        progressBar.fillAmount = 0;
    }
}
}
