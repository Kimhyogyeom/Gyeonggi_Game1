using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
namespace SeongWon
{


public class EndPanel : MonoBehaviour
{
    public static EndPanel instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    private void OnEnable()
    {
        StartCoroutine(CoReturnToTitle());
    }

    IEnumerator CoReturnToTitle() 
    {
        yield return new WaitForSecondsRealtime(5.0f);

        PanelManager.instance.ReturnToHome();

        if (PoseHeadAndGestureController.instance != null)
        {
            PoseHeadAndGestureController.instance.ResetToWaitingForUser();
        }
    }

}
}
