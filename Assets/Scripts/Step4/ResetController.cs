using UnityEngine;

public class ResetController : MonoBehaviour
{
    [SerializeField] private HandPanelController _handPanelController;
    [SerializeField] private HandWaveController _handWaveController;
    [SerializeField] private HandWaveController2 _handWaveController2;
    [SerializeField] private HandSwingController _handSwingController;

    [SerializeField] private GameObject _currentPanel;
    [SerializeField] private GameObject _nextPanel;

    /// <summary>
    /// All Reset
    /// </summary>
    public void ResetAllControllers()
    {
        _handPanelController.ResetProgress();
        _handWaveController.ResetWaveController();
        _handWaveController2.ResetWaveController();
        _handSwingController.ResetSwingController();

        _currentPanel.SetActive(false);
        _nextPanel.SetActive(true);
    }
}
