using UnityEngine;

public class AutoRotateY : MonoBehaviour
{
    public float rotationSpeed = 10f;

    private void Update()
    {
        // 월드 좌표계 기준 Y축 회전
        transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
    }
}
