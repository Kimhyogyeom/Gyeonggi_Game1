<div align="center">

# 🌍 파주 유아기후환경 인터랙티브 게임

### 손동작 인식 기반 재생에너지 교육용 Unity 게임

![Unity](https://img.shields.io/badge/Unity-000000?style=for-the-badge&logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![MediaPipe](https://img.shields.io/badge/MediaPipe-00C4CC?style=for-the-badge&logo=google&logoColor=white)

[![License](https://img.shields.io/badge/License-Gyeonggi_Province-blue?style=flat-square)](https://github.com/Kimhyogyeom/Gyeonggi_Game1)
[![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey?style=flat-square)](https://github.com/Kimhyogyeom/Gyeonggi_Game1)

</div>

---

## 📖 프로젝트 개요

유아를 대상으로 한 **기후환경 교육 인터랙티브 게임**입니다.
MediaPipe를 활용한 **손동작 인식 기술**을 통해 재생에너지(태양광, 풍력, 수력)에 대해 즐겁게 학습할 수 있습니다.

### ✨ 핵심 특징

- 🖐️ **비접촉식 인터랙션** - 손동작만으로 게임 조작
- 🎯 **유아 친화적 UI/UX** - 직관적이고 재미있는 인터페이스
- ⚡ **최적화된 성능** - 60FPS 실시간 손동작 인식
- 🌱 **교육적 콘텐츠** - 재생에너지에 대한 이해 증진

---

## 🎮 주요 기능

### 🖐️ 손동작 인식 시스템

```
MediaPipe Hand Landmarker 기반
├─ 실시간 손 위치 추적
├─ 비접촉식 인터랙션
└─ 웹캠 최적화 (640x480 @ 60FPS)
```

- 유아의 빠른 손 움직임에도 안정적인 추적
- 정확한 제스처 인식 알고리즘
- 낮은 지연시간으로 즉각적인 피드백

### 🎯 게임 단계 구성

<table>
<tr>
<td align="center"><b>Step 0</b></td>
<td>손 인식 시작 패널</td>
</tr>
<tr>
<td align="center"><b>Step 1</b></td>
<td>☀️ 손 흔들기 (태양광 에너지)<br>손을 위아래로 흔들어 태양광 패널 활성화</td>
</tr>
<tr>
<td align="center"><b>Step 2</b></td>
<td>🌀 손 흔들기 (풍력 에너지)<br>손동작으로 풍력 발전기 작동</td>
</tr>
<tr>
<td align="center"><b>Step 3</b></td>
<td>💧 손 스윙 (수력 에너지)<br>손 스윙 동작으로 수력 발전 체험</td>
</tr>
<tr>
<td align="center"><b>Step 4</b></td>
<td>✅ 완료 및 리셋</td>
</tr>
</table>

### 🎨 UI/UX 요소

- ⏳ 실시간 진행도 슬라이더
- 🎬 부드러운 페이드 인/아웃 애니메이션
- 🌍 3D 지구본 및 환경 오브젝트
- ✨ 파티클 효과를 통한 시각적 피드백

---

## 🛠️ 기술 스택

<div align="center">

### Game Engine & Language
![Unity](https://img.shields.io/badge/Unity_2022-000000?style=for-the-badge&logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)

### Computer Vision
![MediaPipe](https://img.shields.io/badge/MediaPipe-00C4CC?style=for-the-badge&logo=google&logoColor=white)

### Input Device
![WebCam](https://img.shields.io/badge/WebCam_API-FF6B6B?style=for-the-badge&logo=camera&logoColor=white)

</div>

---

## 📁 프로젝트 구조

```
Assets/
├─ Scripts/
│  ├─ WebcamController/
│  │  └─ WebcamController.cs          # 웹캠 초기화 (640x480@60FPS)
│  ├─ Step0/
│  │  └─ HandPanelController.cs       # 손 인식 시작 패널
│  ├─ Step1/
│  │  └─ HandWaveController.cs        # 태양광 - 손 흔들기 감지
│  ├─ Step2/
│  │  └─ HandWaveController2.cs       # 풍력 - 손 흔들기 감지
│  ├─ Step3/
│  │  └─ HandSwingController.cs       # 수력 - 손 스윙 감지
│  ├─ Step4/
│  │  └─ ResetController.cs           # 게임 리셋
│  ├─ FadeInOut/
│  │  └─ FadeAnimatorController.cs    # 페이드 애니메이션
│  ├─ SubtleWiggleUI/
│  │  ├─ SubtleWiggleUI.cs           # UI 흔들림 효과
│  │  ├─ AutoRotateY.cs              # Y축 자동 회전
│  │  └─ BounceY.cs                  # Y축 바운스
│  └─ ForceFullScreen/
│     └─ ForceFullScreen.cs           # 전체화면 강제
├─ Scenes/
│  └─ MyMotionGame.unity              # 메인 씬
└─ ...
```

---

## ⚡ 최적화 이력

### 📅 2025-12-23: 웹캠 설정 최적화

| 항목 | Before | After | 개선 효과 |
|------|--------|-------|-----------|
| 해상도 | 1280x720 | 640x480 | 처리 속도 향상 |
| FPS | 30 | 60 | 반응성 2배 향상 |
| 지연시간 | ~33ms | ~16ms | 유아 사용성 개선 |

**결과:** 유아의 빠른 손 흔들기에도 안정적인 추적 가능 ✅

---

## 💻 개발 환경

### 필수 요구사항

- Unity 2022.x 이상
- MediaPipe Unity Plugin
- Windows 10/11
- 웹캠 (권장: Logitech C920 HD Pro)

---

## 🚀 설치 및 실행

```bash
# 1. Unity Hub에서 프로젝트 열기
File → Open Project → Gyeonggi_Game1

# 2. MediaPipe Unity Plugin 설치 확인
Window → Package Manager → MediaPipe Unity Plugin

# 3. 메인 씬 열기
Assets/Scenes/MyMotionGame.unity

# 4. 웹캠 연결 확인 후 실행
Play 버튼 클릭 ▶️
```

---

## 📦 사용된 에셋

- 🌍 **Planet Earth Free** - 3D 지구본
- 📝 **TextMesh Pro** - UI 텍스트
- ✨ **Cartoon FX Remaster** (JMO Assets) - 파티클 효과
- 🖐️ **MediaPipe Unity Plugin** - 손동작 인식

---

## 📜 라이선스

이 프로젝트는 **경기도 파주시 유아 기후환경 교육**을 위해 개발되었습니다.

---

<div align="center">

### 👨‍💻 Developer

**Kimhyogyeom** | [GitHub](https://github.com/Kimhyogyeom) | gyrua77@gmail.com

---

[![Made with Unity](https://img.shields.io/badge/Made%20with-Unity-000000?style=flat-square&logo=unity)](https://unity.com)
[![Powered by MediaPipe](https://img.shields.io/badge/Powered%20by-MediaPipe-00C4CC?style=flat-square&logo=google)](https://mediapipe.dev)

**⭐ Star this repository if you found it helpful!**

</div>
