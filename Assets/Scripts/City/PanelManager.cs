using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SeongWon
{


public class PanelManager : MonoBehaviour
{
    public static PanelManager instance;

    [SerializeField] GameObject[] panels;
    int currentPanel;

    public bool isShowLoadingPopUp;
    public int CurrentPanel
    {
        get { return currentPanel; }
        set
        {
            if (value < 0)
            {
                return;
            }
            else if (value > panels.Length)
            {
                currentPanel = 0;
            }
            else
            {
                currentPanel = value;
            }

            LoadPanel();
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else 
        {
            Destroy(gameObject);
        }

        CurrentPanel = 0;
    }
    public void IncreasePanels()
    {
        CurrentPanel++;
    }

    public void DecreasePanels()
    {
        if (CurrentPanel == 5)
        {
            CurrentPanel = 2;
            return;
        }

        CurrentPanel--;
    }

    public void ShowLoadingPopUp()
    {
        panels[panels.Length - 1].SetActive(true);
        isShowLoadingPopUp = true;
    }

    public void HideLoadingPopUp()
    {
        panels[panels.Length - 1].SetActive(false);
        isShowLoadingPopUp = false;
    }

    protected virtual void LoadPanel()
    {
        for (int i = 0; i < panels.Length; ++i)
        {
            if (i == CurrentPanel)
                panels[i].SetActive(true);
            else if (i != panels.Length)
                panels[i].SetActive(false);
        }
    }

    public void ReturnToHome()
    {
        CurrentPanel = 0;
        Debug.Log("Return To Home");
        
        // [버그 수정] 타이틀로 돌아갈 때 모든 CO2 존 파티클 다시 시작
        CO2Zone[] allZones = Object.FindObjectsByType<CO2Zone>(FindObjectsSortMode.None);
        foreach (var zone in allZones)
        {
            zone.ResetZoneToInitial();
        }

        // 벌 상태 리셋 (게이지 및 진행도 초기화)
        BeeOnSplineWithPollen bee = Object.FindFirstObjectByType<BeeOnSplineWithPollen>();
        if (bee != null)
        {
            bee.ResetBeeState();
        }
    }

    public void ReloadPanel()
    {
        panels[currentPanel].gameObject.SetActive(false);
        panels[currentPanel].gameObject.SetActive(true);
    }
}
}
