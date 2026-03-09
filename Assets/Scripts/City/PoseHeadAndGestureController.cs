using System.Collections.Generic;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine;
namespace SeongWon
{


public class PoseHeadAndGestureController : MonoBehaviour
{
    public static PoseHeadAndGestureController instance;

    // 게임 상태
    public enum GameState
    {
        WaitingForUser,         // 사용자 대기 / 탐색
        WaitingForStartGesture, // 사용자 감지됨, 손 흔들기 제스처를 대기
        Running,                // 게임 진행중(머리를 흔들어 벌을 조종)
        GameEnd                 // 게임 종료됨
    }

    private enum MOVEDIR
    {
        None,
        Left,
        Right
    }

    [Header("State")]
    [SerializeField] private GameState currentState = GameState.WaitingForUser;
    public GameState CurrentState => currentState;

    // ─────────────────────────────
    // 시작제스처(양손 머리 위로 올리기) 관련
    // ─────────────────────────────
    [Header("Start Gesture (Hands Above Head)")]
    [Tooltip("양손을 얼마나 오래 머리 위에 유지해야 게임이 시작되는지 (초)")]
    public float handsUpDuration;

    [Tooltip("손목이 코보다 이 값만큼 더 위에 있어야 '손을 머리 위로 올렸다'고 인정")]
    public float handAboveHeadOffset = 0.05f;

    [Tooltip("제스처를 완성해야 하는 최대 허용 시간(초). 초과하면 처음 부터 다시 시작")]
    public float gestureTimeout = 10f;

    private float handsUpTimer = 0f;
    public float GestureProgress => Mathf.Clamp01(handsUpTimer / Mathf.Max(handsUpDuration, 0.1f));

    // ─────────────────────────────
    // 점프 감지로 피버 게이지 제어 관련
    // ─────────────────────────────
    [Header("Jump Detection Settings")]
    [Tooltip("전신 기준 위치를 잡기 위한 캘리브레이션 시간(초)")]
    public float bodyCalibrationTime = 1.5f;

    [Tooltip("점프로 인정하는 최소 상승 높이 (정규화된 좌표) - 통통 튀는 작은 움직임도 감지하도록 작게 설정")]
    public float minJumpHeight = 0.00007f;

    [Tooltip("이 높이만큼 점프하면 최대 속도로 인정(1.0) 처리 - 너무 높게 뛰지 않아도 됨")]
    public float maxJumpHeight = 0.003f;

    [Tooltip("속도값을 부드럽게 보간 (0~1, 높을수록 반응 빨라짐)")]
    [Range(0f, 1f)]
    public float speedSmoothing = 0.2f;

    // 디버깅용
    [Header("Debug")]
    [Range(0f, 1f)] public float bodyBaselineY;
    public bool bodyCalibrated = false;
    public float bodyRawDelta;
    public float bodyAbsDelta;
    [Range(0f, 1f)] public float jumpNormalized;
    [Range(0f, 1f)] public float speedFactor;  // 최종 출력값 (0~1)

    private float bodyCalibTimer = 0f;
    private readonly List<float> bodySamples = new List<float>();

    // 마지막으로 사람을 본 시간 (사용자 사라짐 감지용)
    private float lastSeenTime = 0f;

    [Tooltip("이 시간 동안 사람을 못 보면 WaitingForUser 로 복귀")]
    public float lostTimeout = 10f;

    [Header("Hand Height Condition")]
    [Tooltip("손목이 어깨보다 이 값만큼 더 위에 있어야 '손을 들었다'고 인정")]
    public float handAboveShoulderOffset = 0.03f;

    [Header("Game End Cooldown")]
    [Tooltip("게임 종료 후 이 시간 동안은 자동으로 재시작되지 않음 (초)")]
    public float gameEndCooldown = 5f;

    private PoseLandmarkerResult latestResult;
    private bool hasNewResult = false;

    private float prevBodyY;
    private bool hasPrevBodyY = false;

    // 게임 종료 시간 추적 (쿨다운용)
    // -1000으로 초기화하여 시작 시 쿨다운 없이 게임 가능하도록 함
    private float gameEndTime = -1000f;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Update()
    {
        // GameEnd 상태에서는 입력 처리하지 않음 (게임 종료 대기)
        if (currentState == GameState.GameEnd)
        {
            speedFactor = 0f;
            return;
        }

        if (currentState == GameState.WaitingForUser || currentState == GameState.WaitingForStartGesture)
        {
            if (Time.frameCount % 120 == 0) 
            {
                Debug.Log($"[PoseController] CurrentState: {currentState}, LastSeen: {Time.time - lastSeenTime:F1}s ago");
            }
        }

        // 1) 사용자가 사라졌는지 체크 (타임아웃)
        // Running 상태(게임 진행 중)에서는 사람이 안 보여도 게임 계속 진행
        if (Time.time - lastSeenTime > lostTimeout)
        {
            // WaitingForStartGesture 상태 또는 Running 상태에서 타임아웃 적용
            // [롤백용 주석처리: Running 상태 타임아웃 제외 로직]
            /*
            if (currentState == GameState.WaitingForStartGesture)
            {
                // ... 기존 로직 ...
            }
            */
            if (currentState == GameState.WaitingForStartGesture || currentState == GameState.Running)
            {
                Debug.Log($"[PoseController] {currentState} 상태에서 사용자 사라짐 (10초 초과) → WaitingForUser로 복귀");
                currentState = GameState.WaitingForUser;
                ResetGesture();
                ResetBodyCalibration();
                speedFactor = 0f;

                // UI Bar 리셋
                if (LoadingBarUI.instance != null)
                    LoadingBarUI.instance.ResetGauge();

                // 화면도 타이틀로 초기화
                if (PanelManager.instance != null)
                    PanelManager.instance.ReturnToHome();

                // 꽃 상태도 리셋
                if (FlowerManager.instance != null)
                    FlowerManager.instance.ResetFlowers();
            }
        }

        // 2) Mediapipe 결과가 있으면 처리
        if (hasNewResult)
        {
            hasNewResult = false;
            ProcessPoseResult(latestResult);
        }
    }

    // ─────────────────────────────
    // MediaPipeUnityPlugin에서 호출되는 콜백
    // ─────────────────────────────
    public void OnPoseResult(PoseLandmarkerResult result)
    {
        // GameEnd 상태에서는 결과를 무시
        if (currentState == GameState.GameEnd)
            return;

        latestResult = result;
        hasNewResult = true;
    }

    // ─────────────────────────────
    // 실제 포즈 처리 로직 (항상 메인 스레드에서만 호출)
    // ─────────────────────────────
    private void ProcessPoseResult(PoseLandmarkerResult result)
    {
        // GameEnd 상태에서는 포즈 처리하지 않음
        if (currentState == GameState.GameEnd)
            return;

        if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
            return;

        lastSeenTime = Time.time;

        // 여러 사람이 감지되면 가운데에 가장 가까운 사람 선택
        int selectedPersonIndex = SelectCenterPlayer(result);
        var landmarks = result.poseLandmarks[selectedPersonIndex].landmarks;

        const int noseIdx = 0;
        const int lWristIdx = 15;
        const int rWristIdx = 16;

        if (landmarks.Count <= rWristIdx)
            return;

        var nose = landmarks[noseIdx];
        var lWrist = landmarks[lWristIdx];
        var rWrist = landmarks[rWristIdx];

        float noseY = nose.y;           // 코의 Y 좌표 (전신 기준점)
        float leftWristY = lWrist.y;
        float rightWristY = rWrist.y;

        switch (currentState)
        {
            case GameState.WaitingForUser:
                HandleWaitingForUser();
                break;

            case GameState.WaitingForStartGesture:
                HandleGestureState(noseY, leftWristY, rightWristY);
                break;

            case GameState.Running:
                HandleRunningState(noseY);  // 코의 Y 좌표로 점프 감지
                break;

            case GameState.GameEnd:
                // 아무 것도 안 함
                break;
        }
    }

    /// <summary>
    /// 여러 사람 중 화면 가운데에 가장 가까운 사람을 선택
    /// </summary>
    private int SelectCenterPlayer(PoseLandmarkerResult result)
    {
        var poseLandmarks = result.poseLandmarks;
        
        if (poseLandmarks.Count == 1)
            return 0;

        int bestIndex = 0;
        float minDistanceToCenter = float.MaxValue;

        for (int i = 0; i < poseLandmarks.Count; i++)
        {
            var landmarks = poseLandmarks[i].landmarks;
            if (landmarks.Count == 0)
                continue;

            // 상체 중심점 계산 (코, 왼쪽 어깨, 오른쪽 어깨의 평균)
            const int noseIdx = 0;
            const int leftShoulderIdx = 11;
            const int rightShoulderIdx = 12;

            if (landmarks.Count <= rightShoulderIdx)
                continue;

            float centerX = (landmarks[noseIdx].x + landmarks[leftShoulderIdx].x + landmarks[rightShoulderIdx].x) / 3f;
            float centerY = (landmarks[noseIdx].y + landmarks[leftShoulderIdx].y + landmarks[rightShoulderIdx].y) / 3f;

            // 화면 중앙(0.5, 0.5)과의 거리 계산
            float distanceToCenter = Mathf.Sqrt(
                Mathf.Pow(centerX - 0.5f, 2) + 
                Mathf.Pow(centerY - 0.5f, 2)
            );

            if (distanceToCenter < minDistanceToCenter)
            {
                minDistanceToCenter = distanceToCenter;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    // ─────────────────────────────
    // 상태별 처리
    // ─────────────────────────────
    private void HandleWaitingForUser()
    {
        // 게임 종료 후 쿨다운 시간이 지나지 않았으면 전환하지 않음
        if (Time.time - gameEndTime < gameEndCooldown)
        {
            return; // 쿨다운 중에는 제스처 대기 상태로 전환하지 않음
        }

        currentState = GameState.WaitingForStartGesture;
        ResetGesture();
        ResetBodyCalibration();
        speedFactor = 0f;
        lastSeenTime = Time.time; // 상태 전환 시 타임아웃 타이머 초기화
        Debug.Log("[PoseController] 사용자 감지 → 양손 머리 위로 올리기 제스처 대기 상태로 전환");
    }

    /// <summary>
    /// 양손을 모두 머리 위로 올리면 게임 시작
    /// </summary>
    private void HandleGestureState(float noseY, float leftWristY, float rightWristY)
    {
        // 양손이 모두 코보다 handAboveHeadOffset만큼 위에 있는지 확인
        // MediaPipe 좌표: Y값이 작을수록 위쪽
        bool leftHandUp = leftWristY < noseY - handAboveHeadOffset;
        bool rightHandUp = rightWristY < noseY - handAboveHeadOffset;

        if (leftHandUp || rightHandUp)
        {
            // 양손 중 하나라도 머리 위에 있으면 타이머 증가
            handsUpTimer += Time.deltaTime;
            LoadingBarUI.instance.IncreaseGauge();
            
            if (handsUpTimer >= handsUpDuration)
            {
                // 충분히 오래 유지했으면 게임 시작
                currentState = GameState.Running;
                ResetBodyCalibration();
                lastSeenTime = Time.time; // 게임 시작 시 타임아웃 타이머 초기화
                Debug.Log($"[PoseController] 제스처 완료 ({handsUpTimer:F2}초) -> 게임 시작!");
                
                if (PanelManager.instance != null)
                {
                    PanelManager.instance.IncreasePanels();
                }
                return;
            }
        }
        else
        {
            // 손을 내리면 타이머 리셋
            if (handsUpTimer > 0)
            {
                handsUpTimer = 0f;
                if (LoadingBarUI.instance != null)
                    LoadingBarUI.instance.ResetGauge();
            }
        }
    }

    private void HandleRunningState(float bodyY)
    {
        if (!bodyCalibrated)
        {
            CalibrateBody(bodyY);
        }
        else
        {
            UpdateJumpSpeed(bodyY);
        }
    }

    private void ResetGesture()
    {
        handsUpTimer = 0f;
    }

    // ─────────────────────────────
    // 전신 기준 위치 캘리브레이션
    // ─────────────────────────────
    private void CalibrateBody(float bodyY)
    {
        bodyCalibTimer += Time.deltaTime;
        bodySamples.Add(bodyY);

        if (bodyCalibTimer >= bodyCalibrationTime)
        {
            float sum = 0f;
            for (int i = 0; i < bodySamples.Count; i++)
                sum += bodySamples[i];

            bodyBaselineY = sum / bodySamples.Count;
            bodyCalibrated = true;
            Debug.Log($"[PoseController] 전신 캘리브레이션 완료. baselineY={bodyBaselineY}");
        }
    }

    private void ResetBodyCalibration()
    {
        bodyCalibrated = false;
        bodyCalibTimer = 0f;
        bodySamples.Clear();
        bodyBaselineY = 0f;
        bodyRawDelta = 0f;
        bodyAbsDelta = 0f;
        jumpNormalized = 0f;
        speedFactor = 0f;

        hasPrevBodyY = false;
        prevBodyY = 0f;
    }

    /// <summary>
    /// 전신 Y 위치의 프레임 간 변화량을 기반으로 0~1 속도값 계산
    /// 상하 움직임 모두 인정 (방방 뛰기)
    /// </summary>
    private void UpdateJumpSpeed(float bodyY)
    {
        if (!hasPrevBodyY)
        {
            prevBodyY = bodyY;
            hasPrevBodyY = true;

            bodyRawDelta = 0f;
            bodyAbsDelta = 0f;
            jumpNormalized = 0f;
            speedFactor = 0f;
            return;
        }

        float dy = bodyY - prevBodyY;
        prevBodyY = bodyY;

        bodyRawDelta = dy;

        // 상하 움직임 모두 인정 (절대값 사용)
        bodyAbsDelta = Mathf.Abs(dy);

        // 최소 움직임보다 작으면 무시 (노이즈 필터링)
        if (bodyAbsDelta < minJumpHeight)
        {
            bodyAbsDelta = 0f;
        }

        // minJumpHeight ~ maxJumpHeight 범위로 정규화
        jumpNormalized = Mathf.InverseLerp(minJumpHeight, maxJumpHeight, bodyAbsDelta);
        jumpNormalized = Mathf.Clamp01(jumpNormalized);

        // 움직임이 있으면 빠르게 올라가고, 없으면 천천히 감소
        if (jumpNormalized > speedFactor)
        {
            // 올라갈 때는 빠르게
            speedFactor = Mathf.Lerp(speedFactor, jumpNormalized, 0.5f);
        }
        else
        {
            // 내려갈 때는 느리게 (값이 유지됨)
            speedFactor = Mathf.Lerp(speedFactor, jumpNormalized, 0.05f);
        }
    }

    // 외부에서 속도값 읽기 위한 메서드
    public float GetSpeedFactor()
    {
        return speedFactor;
    }

    // ─────────────────────────────
    // 게임 종료 처리 (게임 끝남 / 결과판 표시시)
    // ─────────────────────────────

    /// <summary>
    /// 게임이 끝났을 때(벌이 목적지 도착, ResultPanel 표시 시작 등) 호출
    /// </summary>
    public void SetGameEndState()
    {
        currentState = GameState.GameEnd;
        ResetGesture();
        ResetBodyCalibration();
        speedFactor = 0f;
        gameEndTime = Time.time; // 게임 종료 시간 기록
        Debug.Log("[PoseController] GameEnd 상태로 전환 (입력 처리 중지)");
    }

    /// <summary>
    /// 타이틀로 돌아가서 다시 새 게임을 시작할 준비가 되었을 때 호출
    /// </summary>
    public void ResetToWaitingForUser()
    {
        currentState = GameState.WaitingForUser;
        ResetGesture();
        ResetBodyCalibration();
        speedFactor = 0f;
        lastSeenTime = Time.time;
        
        // UI Bar 리셋
        if (LoadingBarUI.instance != null)
            LoadingBarUI.instance.ResetGauge();
        
        // gameEndTime은 유지하여 쿨다운 동안 자동 재시작 방지
        lastSeenTime = Time.time;
        Debug.Log("[PoseController] 새 게임 준비 완료 → WaitingForUser (쿨다운 중)");
    }
}
}
