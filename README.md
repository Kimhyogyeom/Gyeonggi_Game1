# 파주 유아기후환경 인터랙티브 게임

손동작 인식 기반 재생에너지 교육용 Unity 게임

## 프로젝트 개요

유아를 대상으로 한 기후환경 교육 인터랙티브 게임으로, MediaPipe를 활용한 손동작 인식 기술을 통해 재생에너지(태양광, 풍력, 수력)에 대해 학습할 수 있습니다.

## 주요 기능

### 손동작 인식
- **MediaPipe Hand Landmarker** 기반 실시간 손동작 추적
- 웹캠을 통한 비접촉식 인터랙션
- 최적화된 웹캠 설정 (640x480, 60FPS)으로 빠른 손 움직임 안정적 인식

### 게임 단계
1. **Step 0**: 손 인식 시작 패널
2. **Step 1**: 손 흔들기 (태양광 에너지)
   - 손을 위아래로 흔들어 태양광 패널 활성화
   - 12회 웨이브 완료 시 다음 단계 진행
3. **Step 2**: 손 흔들기 (풍력 에너지)
   - 손동작으로 풍력 발전기 작동
4. **Step 3**: 손 스윙 (수력 에너지)
   - 손 스윙 동작으로 수력 발전 체험
5. **Step 4**: 완료 및 리셋

### UI/UX
- 실시간 진행도 슬라이더
- 부드러운 페이드 인/아웃 애니메이션
- 3D 지구본 및 환경 오브젝트
- 파티클 효과를 통한 시각적 피드백

## 기술 스택

- **Unity Engine**: 게임 개발 플랫폼
- **MediaPipe Unity Plugin**: 손 인식 및 추적
- **C#**: 게임 로직 구현
- **WebCamTexture**: 실시간 웹캠 입력

## 주요 스크립트

### 핵심 컨트롤러
- `WebcamController.cs`: 웹캠 초기화 및 설정 (640x480, 60FPS)
- `HandWaveController.cs`: Step1 손 흔들기 감지 및 진행도 관리
- `HandWaveController2.cs`: Step2 손 흔들기 감지
- `HandSwingController.cs`: Step3 손 스윙 감지
- `ResetController.cs`: Step4 게임 리셋

### UI 컨트롤러
- `HandPanelController.cs`: 손 인식 시작 패널
- `FadeAnimatorController.cs`: 페이드 애니메이션
- `SubtleWiggleUI.cs`: UI 미세 흔들림 효과
- `AutoRotateY.cs`: Y축 자동 회전
- `BounceY.cs`: Y축 바운스 애니메이션

### 기타
- `ForceFullScreen.cs`: 전체화면 강제 설정

## 최적화 이력

### 웹캠 설정 최적화 (2025-12-23)
- 해상도: 1280x720 → 640x480
- FPS: 30 → 60
- 유아의 빠른 손 흔들기에도 안정적인 추적 가능

## 개발 환경

- Unity 2022.x 이상
- MediaPipe Unity Plugin
- Windows 10/11
- 웹캠 (권장: Logitech C920 HD Pro)

## 설치 및 실행

1. Unity Hub에서 프로젝트 열기
2. MediaPipe Unity Plugin 설치 확인
3. `Assets/Scenes/MyMotionGame.unity` 씬 열기
4. 웹캠 연결 확인
5. Play 버튼으로 실행

## 라이선스

이 프로젝트는 경기도 파주시 유아 기후환경 교육을 위해 개발되었습니다.

## 사용된 에셋

- Planet Earth Free
- TextMesh Pro
- Cartoon FX Remaster (JMO Assets)
- MediaPipe Unity Plugin
