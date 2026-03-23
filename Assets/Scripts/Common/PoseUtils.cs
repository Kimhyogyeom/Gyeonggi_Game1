using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;

public static class PoseUtils
{
    /// <summary>
    /// 여러 사람 중 화면 중앙(0.5, 0.5)에 가장 가까운 사람의 인덱스를 반환
    /// </summary>
    public static int SelectCenterPlayer(PoseLandmarkerResult result)
    {
        var poseLandmarks = result.poseLandmarks;

        if (poseLandmarks.Count == 1)
            return 0;

        int bestIndex = 0;
        float minDistance = float.MaxValue;

        for (int i = 0; i < poseLandmarks.Count; i++)
        {
            var landmarks = poseLandmarks[i].landmarks;
            if (landmarks.Count <= 12)
                continue;

            // 상체 중심점 (코, 양 어깨 평균)
            float centerX = (landmarks[0].x + landmarks[11].x + landmarks[12].x) / 3f;
            float centerY = (landmarks[0].y + landmarks[11].y + landmarks[12].y) / 3f;

            float dist = (centerX - 0.5f) * (centerX - 0.5f) + (centerY - 0.5f) * (centerY - 0.5f);

            if (dist < minDistance)
            {
                minDistance = dist;
                bestIndex = i;
            }
        }

        return bestIndex;
    }
}
