using UnityEngine;
namespace SeongWon
{


public class FlowerManager : MonoBehaviour
{
    public static FlowerManager instance;
    ObjectSizeManager[] managers;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        managers = GetComponentsInChildren<ObjectSizeManager>();
    }

    public void ResetFlowers() 
    {
        for (int i = 0; i < managers.Length; i++)
        {
            managers[i].ResetSize();
        }
    }
}
}
