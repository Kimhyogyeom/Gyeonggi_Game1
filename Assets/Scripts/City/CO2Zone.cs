using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CO2 존을 관리하는 스크립트
/// - 벌이 진입하면 활성화되어 CO2 감소 시작
/// - 연기 파티클 크기 제어
/// - UI CO2 게이지 및 구름 이미지 3개 관리
/// </summary>
namespace SeongWon
{

public class CO2Zone : MonoBehaviour
{
    [Header("CO2 Zone Settings")]
    [Tooltip("이 존의 초기 CO2량 (0이 되면 존 해제)")]
    [SerializeField] private float initialCO2Amount = 100f;
    
    [Tooltip("사용자가 뛸 때 CO2가 감소하는 속도")]
    [SerializeField] private float co2DecreaseRate = 20f;
    
    [Header("Smoke Particle Settings")]
    [Tooltip("연기 파티클 시스템")]
    [SerializeField] private ParticleSystem smokeParticle;
    
    [Tooltip("초기 파티클 크기")]
    [SerializeField] private float initialParticleSize = 5f;
    
    [Tooltip("최종 파티클 크기 (CO2 = 0일 때)")]
    [SerializeField] private float finalParticleSize = 0f;
    
    [Header("UI Settings")]
    [Tooltip("CO2 게이지 UI")]
    [SerializeField] private Image co2GaugeUI;
    
    [Tooltip("CO2 구름 이미지 3개 (좌우로 움직임)")]
    [SerializeField] private GameObject[] cloudImages = new GameObject[3];
    
    [Tooltip("구름 이미지 좌우 이동 범위")]
    [SerializeField] private float cloudMoveRange = 50f;
    
    [Tooltip("구름 이미지 좌우 이동 속도")]
    [SerializeField] private float cloudMoveSpeed = 1f;
    
    private float currentCO2Amount;
    private bool isActive = false;
    
    // 구름 이미지 초기 위치 저장
    private Vector3[] cloudInitialPositions = new Vector3[3];
    private float cloudMoveTime = 0f;
    
    // [최적화] 캐싱된 상태
    private bool[] cloudActiveStates = new bool[3];
    private float lastCO2Percent = -1f;
    
    // 현재 CO2량 (0~1로 정규화)
    public float NormalizedCO2 => initialCO2Amount > 0 ? currentCO2Amount / initialCO2Amount : 0f;
    public bool IsActive => isActive;
    
    private void Start()
    {
        currentCO2Amount = initialCO2Amount;
        
        // 구름 이미지 초기 위치 저장
        for (int i = 0; i < cloudImages.Length; i++)
        {
            if (cloudImages[i] != null)
            {
                cloudInitialPositions[i] = cloudImages[i].transform.localPosition;
                cloudImages[i].SetActive(false); // 존이 활성화되기 전에는 숨김
            }
        }
        
        // [버그 수정] 파티클 초기화 - 게임 시작 시 항상 재생 상태로
        if (smokeParticle != null)
        {
            var main = smokeParticle.main;
            main.startSize = initialParticleSize;
            
            if (!smokeParticle.isPlaying)
                smokeParticle.Play();
        }
        
        UpdateVisuals();
    }
    
    private void Update()
    {
        // [최적화] 활성화 상태일 때만 업데이트
        if (isActive)
        {
            UpdateCloudMovement();
        }
    }
    
    /// <summary>
    /// CO2를 감소시킵니다
    /// </summary>
    /// <param name="speedFactor">사용자의 점프 강도 (0~1)</param>
    /// <returns>CO2가 0이 되었는지 여부</returns>
    public bool DecreaseCO2(float speedFactor)
    {
        if (!isActive) return true;
        
        currentCO2Amount -= speedFactor * co2DecreaseRate * Time.deltaTime;
        currentCO2Amount = Mathf.Max(0f, currentCO2Amount);
        
        UpdateVisuals();
        
        if (currentCO2Amount <= 0f)
        {
            isActive = false;
            return true; // CO2가 모두 제거됨
        }
        
        return false;
    }
    
    public void ActivateZone()
    {
        isActive = true;
        currentCO2Amount = initialCO2Amount;
        cloudMoveTime = 0f;
        
        // [버그 수정] 캐시 초기화
        lastCO2Percent = -1f;
        for (int i = 0; i < cloudActiveStates.Length; i++)
        {
            cloudActiveStates[i] = false;
        }
        
        // 구름 이미지 모두 활성화
        for (int i = 0; i < cloudImages.Length; i++)
        {
            if (cloudImages[i] != null)
            {
                cloudImages[i].SetActive(true);
                cloudActiveStates[i] = true;
            }
        }
        
        // [버그 수정] 파티클 명시적으로 재시작
        if (smokeParticle != null)
        {
            var main = smokeParticle.main;
            main.startSize = initialParticleSize;
            
            if (!smokeParticle.isPlaying)
                smokeParticle.Play();
        }
        
        UpdateVisuals();
    }
    
    public void ResetZone()
    {
        currentCO2Amount = initialCO2Amount;
        isActive = false;
        cloudMoveTime = 0f;
        
        // [버그 수정] 캐시 초기화
        lastCO2Percent = -1f;
        for (int i = 0; i < cloudActiveStates.Length; i++)
        {
            cloudActiveStates[i] = false;
        }
        
        // 구름 이미지 모두 비활성화
        for (int i = 0; i < cloudImages.Length; i++)
        {
            if (cloudImages[i] != null)
            {
                cloudImages[i].SetActive(false);
                cloudImages[i].transform.localPosition = cloudInitialPositions[i];
            }
        }
        
        UpdateVisuals();
    }
    
    /// <summary>
    /// 타이틀 화면으로 돌아갈 때 존을 완전히 초기화 (파티클 재시작 포함)
    /// </summary>
    public void ResetZoneToInitial()
    {
        ResetZone();
        
        // 파티클 명시적으로 재시작
        if (smokeParticle != null)
        {
            var main = smokeParticle.main;
            main.startSize = initialParticleSize;
            
            if (!smokeParticle.isPlaying)
                smokeParticle.Play();
        }
    }
    
    private void UpdateVisuals()
    {
        // 1. CO2 게이지 업데이트
        if (co2GaugeUI != null)
        {
            co2GaugeUI.fillAmount = NormalizedCO2;
        }
        
        // 2. 연기 파티클 크기 업데이트
        if (smokeParticle != null)
        {
            var main = smokeParticle.main;
            float targetSize = Mathf.Lerp(finalParticleSize, initialParticleSize, NormalizedCO2);
            main.startSize = targetSize;
            
            // CO2가 0이면 파티클 정지
            if (currentCO2Amount <= 0f && smokeParticle.isPlaying)
            {
                smokeParticle.Stop();
            }
            else if (currentCO2Amount > 0f && !smokeParticle.isPlaying && isActive)
            {
                smokeParticle.Play();
            }
        }
        
        // 3. 구름 이미지 단계별 표시/숨김
        UpdateCloudVisibility();
    }
    
    private void UpdateCloudVisibility()
    {
        float co2Percent = NormalizedCO2;
        
        // [최적화] CO2 퍼센트가 0.05 이상 변경되었을 때만 업데이트
        // 단, isActive가 false가 되면 무조건 업데이트 (구름 숨기기 위해)
        if (isActive && Mathf.Abs(co2Percent - lastCO2Percent) < 0.05f)
            return;
        
        lastCO2Percent = co2Percent;
        
        for (int i = 0; i < cloudImages.Length; i++)
        {
            if (cloudImages[i] != null)
            {
                // 3단계로 구분: 66% 이상, 33~66%, 0~33%
                float threshold = (2 - i) / 3f; // i=0: 0.66, i=1: 0.33, i=2: 0
                
                bool shouldBeActive = isActive && co2Percent > threshold;
                
                // [최적화] 상태 변경이 필요할 때만 SetActive 호출
                if (cloudActiveStates[i] != shouldBeActive)
                {
                    cloudImages[i].SetActive(shouldBeActive);
                    cloudActiveStates[i] = shouldBeActive;
                }
            }
        }
    }
    
    private void UpdateCloudMovement()
    {
        cloudMoveTime += Time.deltaTime * cloudMoveSpeed;
        
        // [최적화] 캐싱된 활성 상태 사용
        for (int i = 0; i < cloudImages.Length; i++)
        {
            if (cloudImages[i] != null && cloudActiveStates[i])
            {
                // Sin 함수로 좌우 이동
                float offset = Mathf.Sin(cloudMoveTime + i * 1.5f) * cloudMoveRange;
                Vector3 newPos = cloudInitialPositions[i];
                newPos.x += offset;
                cloudImages[i].transform.localPosition = newPos;
            }
        }
    }
}
}
