using UnityEngine;
using UnityEngine.Splines;   // SplineAnimate 사용
using UnityEngine.Events;    // 게임 종료 이벤트용 (선택)
using UnityEngine.UI;
using System.Collections;
namespace SeongWon
{

#pragma warning disable 0414

public class BeeOnSplineWithPollen : MonoBehaviour
{
    [Header("Spline 이동 관련설정")]
    [SerializeField] private SplineAnimate splineAnimate;

    [Header("이동 시간(초단위 설정)")]
    [Tooltip("머리 움직임이 전혀없을 때(Idle), 스플라인 끝까지 가는 데 걸리는 시간")]
    [SerializeField] private float idleTravelTimeSeconds = 100f;

    [Tooltip("머리 움직임이 적을 때(Base), 스플라인 끝까지 가는 데 걸리는 시간")]
    [SerializeField] private float slowTravelTimeSeconds = 60f;

    [Tooltip("머리 움직임이 최대입력일 때(Fast) 스플라인 끝까지 가는 시간")]
    [SerializeField] private float fastTravelTimeSeconds = 20f;

    [Tooltip("피버 상태에서의 이동시간")]
    [SerializeField] private float feverTravelTimeSeconds = 10f;

    [Header("피버 관련 설정")]
    [Tooltip("피버 게이지가 차기 시작하기 위해 필요한 최소 속도(threshold)")]
    [SerializeField] private float feverThreshold = 0.005f;

    [Tooltip("머리를 많이 흔들면( speedFactor >= feverThreshold ) 피버 게이지가 채워지는 속도")]
    [SerializeField] private float feverChargePerSecond = 0.8f;

    [Tooltip("머리를 덜 흔들면 피버 게이지가 감소하는 속도 (사용하지 않음)")]
    [SerializeField] private float feverDecayPerSecond = 0.1f;

    [Tooltip("피버 모드가 유지되는 시간 (초)")]
    [SerializeField] private float feverDuration = 10f;

    [Tooltip("피버 상태에서의 파티클 배율")]
    [SerializeField] private float feverParticleMultiplier = 3f;

    [Header("파티클 설정")]
    [SerializeField] private ParticleSystem pollenParticle;

    [Tooltip("정지/느린 상태에서의 파티클 방출량 (Rate over Time)")]
    [SerializeField] private float idleRateOverTime = 5f;

    [Tooltip("최대 속도에서의 파티클 방출량 (Rate over Time)")]
    [SerializeField] private float moveRateOverTime = 0f;

    [Tooltip("일반 상태에서의 파티클 방출량 (Rate over Distance)")]
    [SerializeField] private float normalRateOverDistance = 2f;

    [Tooltip("최대 속도에서의 파티클 방출량 (Rate over Distance)")]
    [SerializeField] private float maxRateOverDistance = 6f;

    [Header("Fog 제어 설정")]
    [SerializeField] private bool controlFog = true;
    [SerializeField] private float startFogDensity = 0.17f;
    [SerializeField] private float endFogDensity = 0f;
    [SerializeField] private bool autoEnableFog = true;
    [SerializeField, Range(0f, 1f)]
    private float fogClearProgress = 0.66f;

    [Header("Skybox 제어 설정")]
    [SerializeField] private bool controlSkybox = true;
    [SerializeField] private Material skyboxMaterial;
    [SerializeField] private string colorPropertyName = "_Tint"; // 대부분의 스카이박스는 _Tint 또는 _SkyColor
    [SerializeField] private Color startSkyColor = new Color(0.3f, 0.3f, 0.3f, 1f); // 어두운/탁한 색
    [SerializeField] private Color endSkyColor = new Color(0.8f, 0.8f, 0.8f, 1f);   // 연회색 (맑아진 느낌)

    [Header("Skybox Ground Color (추가)")]
    [SerializeField] private string groundColorPropertyName = "_GroundColor";
    [SerializeField] private Color startGroundColor = new Color(0.38f, 0.31f, 0.16f, 1f); // #615028 (탁한 올리브/회갈색)
    [SerializeField] private Color endGroundColor = new Color(0.23f, 0.59f, 0.62f, 1f);   // #3A969F (청록색)

    [Header("CO2 구름 제어")]
    [SerializeField] private GameObject[] globalCloudImages = new GameObject[5];
    [SerializeField] private float cloudMoveRange = 50f;
    [SerializeField] private float cloudMoveSpeed = 1f;

    private Vector3[] cloudInitialPositions = new Vector3[5];
    private float cloudMoveTime = 0f;
    private Image[] cloudImagesCache = new Image[5]; // 알파값 제어를 위한 이미지 캐시

    [Header("미디어파이프 연동 설정")]
    [Tooltip("MediaPipe PoseHeadAndGestureController에서 머리/손 값을 가져올지 여부")]
    [SerializeField] private bool useMediaPipeControl = true;

    // MediaPipe 컨트롤러 참조
    private PoseHeadAndGestureController poseController;

    // MediaPipe에서 온 0~1 값 (머리흔드는속도 등)
    private float motionValue;

    private bool isFollowingPath = false;

    [Header("CO2 Zone Settings")]
    [Tooltip("현재 벌이 머물고 있는 CO2 존")]
    private CO2Zone currentCO2Zone = null;
    private bool isInCO2Zone = false;
    
    [Tooltip("CO2 존 안내 문구 UI")]
    [SerializeField] private GameObject co2ZoneTextUI;
    
    [Tooltip("CO2 존 배경 이미지")]
    [SerializeField] private GameObject co2ZoneBackgroundImage;

    [Header("피버 이펙트 파티클")]
    [SerializeField] private ParticleSystem speedLineParticle;

    [Header("효과 관련")]
    [SerializeField] private GameObject Growthcollider;
    [SerializeField] private ParticleSystem victoryParticle;
    
    [Header("효과음 설정")]
    [Tooltip("비행 효과음 AudioSource")]
    [SerializeField] private AudioSource flightSoundEffect;
    
    [Tooltip("최소 속도일 때 pitch (느린 소리)")]
    [SerializeField] private float minPitch = 1.0f;
    
    [Tooltip("최대 속도일 때 pitch (빠른 소리)")]
    [SerializeField] private float maxPitch = 2.0f;
    
    [Tooltip("피버 상태일 때 pitch 배율")]
    [SerializeField] private float feverPitchMultiplier = 1.3f;

    [Header("게이지들")]
    [SerializeField] private Image carbonGauge;     // 시작 시 1.0 → 도착하면 0.0
    [SerializeField] private Image feverGauge;      // 0.0 ~ 1.0 (피버 게이지로 사용)
    [SerializeField] private Image destination;     // 시작 시 0.0 → 도착하면 1.0

    // 피버 관련 변수
    private bool isFeverActive = false;
    private float feverTimer = 0f;
    private float feverValue = 0f;
    private bool isFeverPaused = false;  // CO2 존에서 피버 일시정지 여부

    // 수동 진행도(0~1)
    private float manualProgress = 0f;

    // 벌 아이콘을 위한 UI
    [SerializeField] private GameObject beeObj;

    // 벌 UI 출발점 Y좌표
    private float beeOriginYPos = 217;
    // 벌 UI 도착점 Y좌표
    private float beeDestinationYPos = -226;
    
    // [최적화] 캐싱된 UI 상태
    private bool carbonGaugeVisible = false;
    private bool destinationVisible = true;
    private bool feverGaugeVisible = true;

    private void Awake()
    {
        if (splineAnimate == null)
            splineAnimate = GetComponent<SplineAnimate>();

        if (pollenParticle == null)
            pollenParticle = GetComponentInChildren<ParticleSystem>();

        if (pollenParticle != null)
        {
            var main = pollenParticle.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
        }

        // MediaPipe 컨트롤러 찾기
        poseController = PoseHeadAndGestureController.instance;

        if (speedLineParticle != null)
            speedLineParticle.Stop();

        if (Growthcollider != null)
            Growthcollider.gameObject.SetActive(false);

        if (splineAnimate != null)
        {
            splineAnimate.Pause();
            manualProgress = 0f;
            splineAnimate.NormalizedTime = 0f;
        }
    }

    private void Start()
    {
        if (splineAnimate == null)
        {
            Debug.LogWarning("[BeeOnSplineWithPollen] SplineAnimate가 할당되지 않았습니다.");
            return;
        }

        splineAnimate.Pause();

        // Fog 초기 설정
        if (controlFog)
        {
            if (autoEnableFog)
                RenderSettings.fog = true;

            RenderSettings.fogDensity = startFogDensity;
        }

        // 게이지 초기화 - CO2 게이지는 처음에 숨김
        if (carbonGauge != null)
        {
            carbonGauge.fillAmount = 1.0f;
            // carbonGauge의 GameObject를 비활성화 (또는 parent GameObject)
            if (carbonGauge.transform.parent != null)
                carbonGauge.transform.parent.gameObject.SetActive(false);
            else
                carbonGauge.gameObject.SetActive(false);
        }

        if (destination != null)
            destination.fillAmount = 0.0f;

        if (feverGauge != null)
            feverGauge.fillAmount = 0.0f;

        if (beeObj != null)
        {
            Vector3 Pos = beeObj.transform.localPosition;
            Pos.y = beeOriginYPos;
            beeObj.transform.localPosition = Pos;
        }
        
        // CO2 존 안내 문구 초기에 숨김
        if (co2ZoneTextUI != null)
            co2ZoneTextUI.SetActive(false);
        
        // CO2 존 배경 이미지 초기에 숨김
        if (co2ZoneBackgroundImage != null)
            co2ZoneBackgroundImage.SetActive(false);

        // 구름 이미지 초기 위치 및 이미지 컴포넌트 저장
        for (int i = 0; i < globalCloudImages.Length; i++)
        {
            if (globalCloudImages[i] != null)
            {
                cloudInitialPositions[i] = globalCloudImages[i].transform.localPosition;
                cloudImagesCache[i] = globalCloudImages[i].GetComponent<Image>();
            }
        }
    }

    private void Update()
    {
        if (splineAnimate == null)
            return;



        // poseController가 null이면 다시 찾기 시도
        if (useMediaPipeControl && poseController == null)
            poseController = PoseHeadAndGestureController.instance;
        
        if (useMediaPipeControl && poseController != null)
        {
            var state = poseController.CurrentState;

            if (state == PoseHeadAndGestureController.GameState.Running)
            {
                float speedFactor = poseController.GetSpeedFactor(); // 0~1
                
                // [수정] 0.3f 최소 제한을 제거하여 idleSpeed까지 완전히 떨어질 수 있도록 함
                float effectiveSpeed = speedFactor;
                SetMotionValue(effectiveSpeed);

                if (!isFollowingPath)
                    StartFollowing();
            }
            else
            {
                SetMotionValue(0f);

                if (isFollowingPath)
                    StopFollowing();
            }
        }


        float t = Mathf.Clamp01(motionValue);   // 0~1, 머리 흔드는 속도

        // CO2 존에 있을 때는 피버 업데이트 안 함
        /* [롤백용 주석처리: CO2 존 제거 게임]
        if (!isInCO2Zone)
        {
            UpdateFeverState(t);
        }
        */
        UpdateFeverState(t);

        if (isFollowingPath)
        {
            // CO2 존 처리
            /* [롤백용 주석처리: CO2 존 제거 게임]
            if (isInCO2Zone && currentCO2Zone != null)
            {
                // CO2 존 내부: 이동하지 않고 CO2만 감소 (피버 속도 보너스 없음)
                bool co2Cleared = currentCO2Zone.DecreaseCO2(t);
                
                // carbonGauge 업데이트 (CO2Zone과 동기화)
                if (carbonGauge != null)
                {
                    carbonGauge.fillAmount = currentCO2Zone.NormalizedCO2;
                }
                
                if (co2Cleared)
                {
                    // CO2가 모두 제거됨 -> 존 해제
                    ExitCO2Zone();
                }
                
                // 스플라인 위치 유지 (진행도 업데이트 안 함)
                splineAnimate.NormalizedTime = manualProgress;
            }
            else
            */
            {
                // 1) 프레임당 진행할 "초당 진행률" 계산
                float progressPerSecond;

                /* [기존 로직 주석처리: 입력값에 따른 즉각적인 속도 변화]
                if (isFeverActive)
                {
                    // 피버 상태에서는 피버 속도로 빠르게 이동
                    float feverTime = Mathf.Max(feverTravelTimeSeconds, 0.01f);
                    progressPerSecond = 1f / feverTime;
                }
                else
                {
                    // 일반상태: t(0~1)에 따라서 Idle ~ Base ~ Fast 사이로 보간
                    float idleTime = Mathf.Max(idleTravelTimeSeconds, 0.01f);
                    float slowTime = Mathf.Max(slowTravelTimeSeconds, 0.01f);
                    float fastTime = Mathf.Max(fastTravelTimeSeconds, 0.01f);

                    float idleSpeed = 1f / idleTime;
                    float baseSpeed = 1f / slowTime;
                    float fastSpeed = 1f / fastTime;

                    // 3단계 선형 보간 (t=0: Idle, t=0.5: Base, t=1: Fast)
                    if (t < 0.5f)
                    {
                        progressPerSecond = Mathf.Lerp(idleSpeed, baseSpeed, t * 2f);
                    }
                    else
                    {
                        progressPerSecond = Mathf.Lerp(baseSpeed, fastSpeed, (t - 0.5f) * 2f);
                    }
                }
                */

                // [신규 로직: 게이지(feverValue)에 따른 속도 결정]
                float minTravelTime = Mathf.Max(idleTravelTimeSeconds, 0.01f);
                float maxTravelTime = Mathf.Max(feverTravelTimeSeconds, 0.01f);
                
                float minSpeed = 1f / minTravelTime;
                float maxSpeed = 1f / maxTravelTime;

                // 게이지(0~1)에 따라 최저속도에서 최대속도까지 선형 보간
                progressPerSecond = Mathf.Lerp(minSpeed, maxSpeed, feverValue);

            // 2) 진행도 갱신
            manualProgress += progressPerSecond * Time.deltaTime;
            manualProgress = Mathf.Clamp01(manualProgress);

            // Spline에 반영
            splineAnimate.NormalizedTime = manualProgress;

            // 3) 도착 처리
            if (manualProgress >= 1f)
            {
                StopFollowing();

                if (controlFog)
                    RenderSettings.fogDensity = endFogDensity;

                // carbonGauge는 CO2 존에서만 사용 (진행도와 무관)

                if (destination != null)
                    destination.fillAmount = 1f;

                if (beeObj != null)
                {
                    Vector3 Pos = beeObj.transform.localPosition;
                    Pos.y = beeDestinationYPos;
                    beeObj.transform.localPosition = Pos;
                }

                StartCoroutine(OnBeeReachedEnd());
            }
            }
        }
        else
        {
            // 멈춘 상태에서도 현재 위치 유지
            splineAnimate.NormalizedTime = manualProgress;
        }

        // 현재 진행도(0~1)
        float progress = manualProgress;

        // 게이지 업데이트 - carbonGauge는 CO2 존에서만 사용하므로 여기서 업데이트 안 함
        if (destination != null)
            destination.fillAmount = progress;

        if (beeObj != null)
        {
            Vector3 Pos = beeObj.transform.localPosition;
            Pos.y = (-443 * progress) + beeOriginYPos;
            beeObj.transform.localPosition = Pos;
        }

        // Fog 업데이트
        if (controlFog)
        {
            // 진행도에 따라 Fog 밀도 조절 (목적지에 가까울수록 0에 수렴)
            float fogDensity = Mathf.Lerp(startFogDensity, endFogDensity, progress);
            RenderSettings.fogDensity = fogDensity;
        }

        // Skybox 업데이트
        if (controlSkybox)
        {
            Material targetMat = skyboxMaterial;
            
            // 할당된 머티리얼이 없으면 현재 씬의 스카이박스 사용
            if (targetMat == null)
                targetMat = RenderSettings.skybox;

            if (targetMat != null)
            {
                Color currentSkyColor = Color.Lerp(startSkyColor, endSkyColor, progress);
                
                // 순차적으로 흔한 속성 이름들을 시도
                string actualPropertyName = colorPropertyName;
                bool propertyFound = targetMat.HasProperty(actualPropertyName);

                if (!propertyFound && actualPropertyName == "_Tint")
                {
                    if (targetMat.HasProperty("_SkyTint"))
                    {
                        actualPropertyName = "_SkyTint";
                        propertyFound = true;
                    }
                    else if (targetMat.HasProperty("_Color"))
                    {
                        actualPropertyName = "_Color";
                        propertyFound = true;
                    }
                }

                if (propertyFound)
                {
                    targetMat.SetColor(actualPropertyName, currentSkyColor);
                    
                    // Ground Color 추가 연출
                    if (targetMat.HasProperty(groundColorPropertyName))
                    {
                        Color currentGroundColor = Color.Lerp(startGroundColor, endGroundColor, progress);
                        targetMat.SetColor(groundColorPropertyName, currentGroundColor);
                    }

                    // GI 업데이트를 호출하여 변경사항이 화면에 즉시 반영되도록 함
                    DynamicGI.UpdateEnvironment();
                }
            }
        }

        // CO2 구름 업데이트
        UpdateGlobalClouds(progress);

        // ─────────────────────────────
        // 파티클(꽃가루/스피드라인) 및 시각효과 업데이트
        // ─────────────────────────────
        
        // [신규 로직: 게이지 기반 시각 효과]
        bool isFeverVisual = (feverValue >= 0.8f);

        if (pollenParticle != null)
        {
            var emission = pollenParticle.emission;
            // 게이지(feverValue)에 따라 파티클 양 조절
            float rateTime = Mathf.Lerp(idleRateOverTime, moveRateOverTime, feverValue);
            float rateDist = Mathf.Lerp(normalRateOverDistance, maxRateOverDistance, feverValue);

            if (isFeverVisual)
            {
                rateTime *= feverParticleMultiplier;
                rateDist *= feverParticleMultiplier;
            }

            emission.rateOverTime = rateTime;
            emission.rateOverDistance = rateDist;

            var main = pollenParticle.main;
            main.startSizeMultiplier = isFeverVisual ? feverParticleMultiplier : 1f;
        }

        if (speedLineParticle != null)
        {
            if (isFeverVisual && !speedLineParticle.isPlaying)
                speedLineParticle.Play();
            else if (!isFeverVisual && speedLineParticle.isPlaying)
                speedLineParticle.Stop();
        }

        if (Growthcollider != null)
        {
            // [수정] 꽃은 속도(feverValue)와 상관없이 벌이 날고 있을 때(isFollowingPath) 항상 피어야 함
            Growthcollider.gameObject.SetActive(isFollowingPath);
        }
        
        // 효과음 업데이트 (게이지 값에 비례)
        UpdateSoundEffect(feverValue, isFeverVisual);
    }

    /// <summary>
    /// 피버 게이지 및 상태 업데이트
    /// t: 0~1, 머리 흔들림에서 온 speedFactor
    /// </summary>
    private void UpdateFeverState(float t)
    {
        // CO2 존에서 피버 일시정지되었으면 업데이트 안 함
        if (isFeverPaused)
            return;

        /* [기존 피버 로직 주석처리: 가득 차면 일정 시간 유지되는 방식]
        if (isFeverActive)
        {
            feverTimer -= Time.deltaTime;
            float normalized = Mathf.Clamp01(feverTimer / feverDuration);
            feverValue = normalized;
            ...
        }
        */

        // [신규 로직: 속도 게이지 방식]
        // 머리를 흔들면(점프하면) 게이지 상승, 멈추면 하강
        if (t >= feverThreshold)
        {
            // 사용자가 입력(t)을 줄수록 더 빨리 차오름
            feverValue += feverChargePerSecond * t * Time.deltaTime;
        }
        else
        {
            // 입력이 없으면 서서히 감소
            feverValue -= feverDecayPerSecond * Time.deltaTime;
        }

        feverValue = Mathf.Clamp01(feverValue);

        if (feverGauge != null)
            feverGauge.fillAmount = feverValue;

        // isFeverActive는 시각효과나 사운드에서 참조할 수 있으므로 상태 업데이트만 해줌
        isFeverActive = (feverValue >= 0.8f);
    }

    /// <summary>
    /// 효과음 pitch를 피버 상태에 따라 조절
    /// 평상시: 1.0, 피버: 2.0
    /// </summary>
    private void UpdateSoundEffect(float speedFactor, bool isFever)
    {
        if (flightSoundEffect == null) return;
        
        // 이동 중일 때만 효과음 재생
        if (isFollowingPath)
        {
            // 효과음이 재생 중이 아니면 시작
            if (!flightSoundEffect.isPlaying)
            {
                flightSoundEffect.Play();
            }
            
            // 게이지(0~1)에 따라 pitch 조절
            // 0일 때 minPitch, 1일 때 maxPitch (isFever가 true면 feverPitchMultiplier 적용)
            float basePitch = Mathf.Lerp(minPitch, maxPitch, speedFactor);
            flightSoundEffect.pitch = isFever ? basePitch * feverPitchMultiplier : basePitch;
        }
        else
        {
            // 멈춰있을 때는 효과음 정지
            if (flightSoundEffect.isPlaying)
            {
                flightSoundEffect.Stop();
            }
        }
    }

    /// <summary>
    /// 외부 또는 MediaPipe에서 들어오는 0~1값 세팅
    /// </summary>
    public void SetMotionValue(float value)
    {
        motionValue = Mathf.Clamp01(value);
    }

    public void StartFollowing()
    {
        if (splineAnimate == null)
            return;

        // 게이지들 초기화
        if (carbonGauge != null)
            carbonGauge.fillAmount = 1.0f;

        if (feverGauge != null)
            feverGauge.fillAmount = 0.0f;

        if (destination != null)
            destination.fillAmount = 0.0f;

        if (beeObj != null)
        {
            Vector3 Pos = beeObj.transform.localPosition;
            Pos.y = beeOriginYPos;
            beeObj.transform.localPosition = Pos;
        }

        feverValue = 0f;
        isFeverActive = false;
        feverTimer = 0f;

        // CO2 존 상태 초기화
        isInCO2Zone = false;
        currentCO2Zone = null;

        // 처음부터 다시 시작
        manualProgress = 0f;
        splineAnimate.NormalizedTime = 0f;

        isFollowingPath = true;

        if (Growthcollider != null)
            Growthcollider.gameObject.SetActive(true);

        if (victoryParticle != null)
            victoryParticle.Stop();

        splineAnimate.Pause();
    }

    public void StopFollowing()
    {
        if (splineAnimate == null)
            return;

        isFollowingPath = false;

        if (Growthcollider != null)
            Growthcollider.gameObject.SetActive(false);

        splineAnimate.Pause();
    }

    /// <summary>
    /// 게임을 초기 상태로 되돌립니다. (홈 화면으로 돌아갈 때 호출)
    /// </summary>
    public void ResetBeeState()
    {
        manualProgress = 0f;
        if (splineAnimate != null)
        {
            splineAnimate.NormalizedTime = 0f;
            splineAnimate.Pause();
        }

        isFollowingPath = false;
        isFeverActive = false;
        feverValue = 0f;
        feverTimer = 0f;

        if (destination != null)
            destination.fillAmount = 0f;

        if (feverGauge != null)
            feverGauge.fillAmount = 0f;

        if (carbonGauge != null)
        {
            carbonGauge.fillAmount = 1.0f;
            if (carbonGauge.transform.parent != null)
                carbonGauge.transform.parent.gameObject.SetActive(false);
            else
                carbonGauge.gameObject.SetActive(false);
        }

        if (beeObj != null)
        {
            Vector3 Pos = beeObj.transform.localPosition;
            Pos.y = beeOriginYPos;
            beeObj.transform.localPosition = Pos;
        }

        if (controlFog)
            RenderSettings.fogDensity = startFogDensity;

        if (isFeverActive) // 이미 false로 설정했지만 파티클 정지 확인
        {
            if (speedLineParticle != null && speedLineParticle.isPlaying)
                speedLineParticle.Stop();
        }

        if (victoryParticle != null)
            victoryParticle.Stop();
    }

    private IEnumerator OnBeeReachedEnd()
    {
        isFeverActive = false;

        if (victoryParticle != null)
            victoryParticle.Play();

        PoseHeadAndGestureController.instance.SetGameEndState();

        yield return new WaitForSecondsRealtime(5.0f);

        PanelManager.instance.IncreasePanels();
        FlowerManager.instance.ResetFlowers();
    }

    // CO2 존 콜리전 감지
    private void OnTriggerEnter(Collider other)
    {
        /* [롤백용 주석처리: CO2 존 제거 게임]
        CO2Zone zone = other.GetComponent<CO2Zone>();
        if (zone != null && !isInCO2Zone)
        {
            EnterCO2Zone(zone);
        }
        */
    }

    private void EnterCO2Zone(CO2Zone zone)
    {
        isInCO2Zone = true;
        currentCO2Zone = zone;
        currentCO2Zone.ActivateZone();
        
        // CO2 게이지 UI 표시
        if (carbonGauge != null && !carbonGaugeVisible)
        {
            if (carbonGauge.transform.parent != null)
                carbonGauge.transform.parent.gameObject.SetActive(true);
            else
                carbonGauge.gameObject.SetActive(true);
            carbonGauge.fillAmount = 1.0f;
            carbonGaugeVisible = true;
        }
        
        // 목적지 UI 숨김
        if (destination != null && destinationVisible)
        {
            if (destination.transform.parent != null)
                destination.transform.parent.gameObject.SetActive(false);
            else
                destination.gameObject.SetActive(false);
            destinationVisible = false;
        }
        
        // 피버 게이지 UI 숨김
        if (feverGauge != null && feverGaugeVisible)
        {
            if (feverGauge.transform.parent != null)
                feverGauge.transform.parent.gameObject.SetActive(false);
            else
                feverGauge.gameObject.SetActive(false);
            feverGaugeVisible = false;
        }
        
        // 피버가 활성화 중이면 일시정지
        if (isFeverActive)
        {
            isFeverPaused = true;
            
            // 피버 파티클 정지
            if (speedLineParticle != null && speedLineParticle.isPlaying)
                speedLineParticle.Stop();
        }
        
        // CO2 존 안내 문구 표시
        if (co2ZoneTextUI != null)
            co2ZoneTextUI.SetActive(true);
        
        // CO2 존 배경 이미지 표시
        if (co2ZoneBackgroundImage != null)
            co2ZoneBackgroundImage.SetActive(true);
        
        Debug.Log($"[BeeOnSpline] CO2 존 진입! CO2 제거 시작");
    }

    private void ExitCO2Zone()
    {
        if (currentCO2Zone != null)
        {
            Debug.Log($"[BeeOnSpline] CO2 존 해제! 이동 재개");
            // [버그 수정] CO2 존 리셋 호출하여 구름 이미지 제대로 숨김
            currentCO2Zone.ResetZone();
        }
        
        // CO2 게이지 UI 숨김
        if (carbonGauge != null && carbonGaugeVisible)
        {
            if (carbonGauge.transform.parent != null)
                carbonGauge.transform.parent.gameObject.SetActive(false);
            else
                carbonGauge.gameObject.SetActive(false);
            carbonGaugeVisible = false;
        }
        
        // 목적지 UI 다시 표시
        if (destination != null && !destinationVisible)
        {
            if (destination.transform.parent != null)
                destination.transform.parent.gameObject.SetActive(true);
            else
                destination.gameObject.SetActive(true);
            destinationVisible = true;
        }
        
        // 피버 게이지 UI 다시 표시
        if (feverGauge != null && !feverGaugeVisible)
        {
            if (feverGauge.transform.parent != null)
                feverGauge.transform.parent.gameObject.SetActive(true);
            else
                feverGauge.gameObject.SetActive(true);
            feverGaugeVisible = true;
        }
        
        // 피버 일시정지 해제 (기존 피버 타이머 그대로 재개)
        if (isFeverPaused)
        {
            isFeverPaused = false;
            
            // 피버 파티클 재개
            if (isFeverActive && speedLineParticle != null && !speedLineParticle.isPlaying)
                speedLineParticle.Play();
        }
        
        // CO2 존 안내 문구 숨김
        if (co2ZoneTextUI != null)
            co2ZoneTextUI.SetActive(false);
        
        // CO2 존 배경 이미지 숨김
        if (co2ZoneBackgroundImage != null)
            co2ZoneBackgroundImage.SetActive(false);
        
        isInCO2Zone = false;
        currentCO2Zone = null;
    }

    /// <summary>
    /// 진행도(0~1)에 따라 구름을 움직이고 하나씩 제거합니다. (알파 페이딩 포함)
    /// </summary>
    private void UpdateGlobalClouds(float progress)
    {
        cloudMoveTime += Time.deltaTime * cloudMoveSpeed;
        
        // 사용자가 요청한 사라지는 지점 (20%, 60%, 80%)
        // 인덱스 0: 20% 지점에서 완전히 사라짐
        // 인덱스 1: 60% 지점에서 완전히 사라짐
        // 인덱스 2: 80% 지점에서 완전히 사라짐
        float[] fadeThresholds = { 0.1f, 0.3f, 0.5f, 0.7f, 0.8f };

        for (int i = 0; i < globalCloudImages.Length; i++)
        {
            if (globalCloudImages[i] == null)
                continue;

            float threshold = fadeThresholds[i];
            
            // 현재 진행도가 해당 구름의 임계값(threshold)보다 작으면 활성화, 크거나 같으면 비활성화 (순식간에 사라짐)
            bool shouldBeActive = progress < threshold;
            
            if (globalCloudImages[i].activeSelf != shouldBeActive)
                globalCloudImages[i].SetActive(shouldBeActive);

            if (shouldBeActive)
            {
                // 움직임 제어 (가시성 확보된 경우만)
                float offset = Mathf.Sin(cloudMoveTime + i * 1.5f) * cloudMoveRange;
                Vector3 newPos = cloudInitialPositions[i];
                newPos.x += offset;
                globalCloudImages[i].transform.localPosition = newPos;
            }
        }
    }
}
}
