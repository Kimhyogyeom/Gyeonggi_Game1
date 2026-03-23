// Copyright (c) 2023 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System.Collections;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using Mediapipe;
using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using Experimental = Mediapipe.Unity.Experimental;
using Tasks = Mediapipe.Tasks;

namespace SeongWon
{
  public class PoseLandmarkerRunner : VisionTaskApiRunner<PoseLandmarker>
  {
    public static event System.Action<PoseLandmarkerResult> OnPoseResultEvent;
    [SerializeField] private PoseLandmarkerResultAnnotationController _poseLandmarkerResultAnnotationController;

    [SerializeField, Range(0, 1)] private float _horizontalCropRatio = 0.6f;
    private Experimental.TextureFramePool _textureFramePool;
    private RenderTexture _croppedRT;

    public readonly PoseLandmarkDetectionConfig config = new PoseLandmarkDetectionConfig();

    public override void Stop()
    {
      base.Stop();
      _textureFramePool?.Dispose();
      _textureFramePool = null;

      if (_croppedRT != null)
      {
        _croppedRT.Release();
        _croppedRT = null;
      }
    }

    protected override IEnumerator Run()
    {
      Debug.Log($"Delegate = {config.Delegate}");
      Debug.Log($"Image Read Mode = {config.ImageReadMode}");
      Debug.Log($"Model = {config.ModelName}");
      Debug.Log($"Running Mode = {config.RunningMode}");
      Debug.Log($"NumPoses = {config.NumPoses}");
      Debug.Log($"MinPoseDetectionConfidence = {config.MinPoseDetectionConfidence}");
      Debug.Log($"MinPosePresenceConfidence = {config.MinPosePresenceConfidence}");
      Debug.Log($"MinTrackingConfidence = {config.MinTrackingConfidence}");
      Debug.Log($"OutputSegmentationMasks = {config.OutputSegmentationMasks}");

      yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

      var options = config.GetPoseLandmarkerOptions(config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM ? OnPoseLandmarkDetectionOutput : null);
      taskApi = PoseLandmarker.CreateFromOptions(options, GpuManager.GpuResources);
      var imageSource = ImageSourceProvider.ImageSource;

      yield return imageSource.Play();

      if (!imageSource.isPrepared)
      {
        Mediapipe.Logger.LogError(TAG, "Failed to start ImageSource, exiting...");
        yield break;
      }

      // Use RGBA32 as the input format.
      // TODO: When using GpuBuffer, MediaPipe assumes that the input format is BGRA, so maybe the following code needs to be fixed.
      /* [기본 코드: 롤백용]
      _textureFramePool = new Experimental.TextureFramePool(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32, 10);
      screen.Initialize(imageSource);
      _poseLandmarkerResultAnnotationController.InitScreen(imageSource.textureWidth, imageSource.textureHeight);
      */

      // [크롭 코드 시작]
      int croppedWidth = Mathf.RoundToInt(imageSource.textureWidth * _horizontalCropRatio);
      int croppedHeight = imageSource.textureHeight; // 위아래는 자르지 않음
      
      _textureFramePool = new Experimental.TextureFramePool(croppedWidth, croppedHeight, TextureFormat.RGBA32, 10);
      
      screen.Initialize(imageSource);
      // 화면 시각화 크롭 적용
      if (screen != null)
      {
          float xOffset = (1.0f - _horizontalCropRatio) / 2.0f;
          screen.uvRect = new UnityEngine.Rect(xOffset, 0, _horizontalCropRatio, 1.0f);
      }

      SetupAnnotationController(_poseLandmarkerResultAnnotationController, imageSource);
      _poseLandmarkerResultAnnotationController.InitScreen(croppedWidth, croppedHeight);

      _croppedRT = new RenderTexture(croppedWidth, croppedHeight, 0, GraphicsFormatUtility.GetGraphicsFormat(TextureFormat.RGBA32, true));
      _croppedRT.Create();
      // [크롭 코드 끝]

      var transformationOptions = imageSource.GetTransformationOptions();
      var flipHorizontally = transformationOptions.flipHorizontally;
      var flipVertically = transformationOptions.flipVertically;

      // Always setting rotationDegrees to 0 to avoid the issue that the detection becomes unstable when the input image is rotated.
      // https://github.com/homuler/MediaPipeUnityPlugin/issues/1196
      var imageProcessingOptions = new Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: 0);

      AsyncGPUReadbackRequest req = default;
      var waitUntilReqDone = new WaitUntil(() => req.done);
      var waitForEndOfFrame = new WaitForEndOfFrame();
      var result = PoseLandmarkerResult.Alloc(options.numPoses, options.outputSegmentationMasks);

      // NOTE: we can share the GL context of the render thread with MediaPipe (for now, only on Android)
      var canUseGpuImage = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && GpuManager.GpuResources != null;
      using var glContext = canUseGpuImage ? GpuManager.GetGlContext() : null;

      while (true)
      {
        //if (Time.frameCount % 90 == 0) Debug.Log("[PoseRunner] Loop Running..."); // 루프 동작 확인용 로그

        if (isPaused)
        {
          yield return new WaitWhile(() => isPaused);
        }

        if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
        {
          yield return new WaitForEndOfFrame();
          continue;
        }

        // [크롭 블릿 처리]
        // 원본 텍스처에서 중앙 비율만큼 떼어내서 _croppedRT에 그리기
        float xOffsetBlit = (1.0f - _horizontalCropRatio) / 2.0f;
        Vector2 scale = new Vector2(_horizontalCropRatio, 1.0f);
        Vector2 offset = new Vector2(xOffsetBlit, 0f);
        Graphics.Blit(imageSource.GetCurrentTexture(), _croppedRT, scale, offset);

        // Build the input Image
        Image image;
        switch (config.ImageReadMode)
        {
          case ImageReadMode.GPU:
            if (!canUseGpuImage)
            {
              throw new System.Exception("ImageReadMode.GPU is not supported");
            }
            // [기본 코드: 롤백용] textureFrame.ReadTextureOnGPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
            textureFrame.ReadTextureOnGPU(_croppedRT, flipHorizontally, flipVertically);
            image = textureFrame.BuildGPUImage(glContext);
            // TODO: Currently we wait here for one frame to make sure the texture is fully copied to the TextureFrame before sending it to MediaPipe.
            // This usually works but is not guaranteed. Find a proper way to do this. See: https://github.com/homuler/MediaPipeUnityPlugin/pull/1311
            yield return waitForEndOfFrame;
            break;
          case ImageReadMode.CPU:
            yield return waitForEndOfFrame;
            // [기본 코드: 롤백용] textureFrame.ReadTextureOnCPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
            textureFrame.ReadTextureOnCPU(_croppedRT, flipHorizontally, flipVertically);
            image = textureFrame.BuildCPUImage();
            textureFrame.Release();
            break;
          case ImageReadMode.CPUAsync:
          default:
            // [기본 코드: 롤백용] req = textureFrame.ReadTextureAsync(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
            req = textureFrame.ReadTextureAsync(_croppedRT, flipHorizontally, flipVertically);
            yield return waitUntilReqDone;

            if (req.hasError)
            {
              Debug.LogWarning($"Failed to read texture from the image source");
              continue;
            }
            image = textureFrame.BuildCPUImage();
            textureFrame.Release();
            break;
        }

        switch (taskApi.runningMode)
        {
          case Tasks.Vision.Core.RunningMode.IMAGE:
            if (taskApi.TryDetect(image, imageProcessingOptions, ref result))
            {
              _poseLandmarkerResultAnnotationController.DrawNow(result);
            }
            else
            {
              _poseLandmarkerResultAnnotationController.DrawNow(default);
            }
            DisposeAllMasks(result);
            break;
          case Tasks.Vision.Core.RunningMode.VIDEO:
            if (taskApi.TryDetectForVideo(image, GetCurrentTimestampMillisec(), imageProcessingOptions, ref result))
            {
              _poseLandmarkerResultAnnotationController.DrawNow(result);
            }
            else
            {
              _poseLandmarkerResultAnnotationController.DrawNow(default);
            }
            DisposeAllMasks(result);
            break;
          case Tasks.Vision.Core.RunningMode.LIVE_STREAM:
            taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
            break;
        }
      }
    }

    private void OnPoseLandmarkDetectionOutput(PoseLandmarkerResult result, Image image, long timestamp)
    {
      // 모든 구독자에게 결과 전달 (Ingame 컨트롤러 등)
      OnPoseResultEvent?.Invoke(result);

      if (result.poseLandmarks != null && result.poseLandmarks.Count > 0)
      {
          // City 게임 컨트롤러로 결과 전달
          if (PoseHeadAndGestureController.instance != null)
          {
              PoseHeadAndGestureController.instance.OnPoseResult(result);
          }
      }

      _poseLandmarkerResultAnnotationController.DrawLater(result);
      DisposeAllMasks(result);
    }

    private void DisposeAllMasks(PoseLandmarkerResult result)
    {
      if (result.segmentationMasks != null)
      {
        foreach (var mask in result.segmentationMasks)
        {
          mask.Dispose();
        }
      }
    }
  }
}
