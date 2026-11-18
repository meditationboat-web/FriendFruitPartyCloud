using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine;

public class PoseGestureToUI : MonoBehaviour
{
  public PoseLandmarkerRunner poseRunner;
  public bool isMirrored = true;
  [Range(0.01f, 0.2f)] public float raiseThreshold = 0.05f;

  private bool _isLeftHandUp = false;
  private bool _isRightHandUp = false;
  private float _lastTriggerTime = 0f;

  private const int LeftShoulderIdx = 11;
  private const int RightShoulderIdx = 12;
  private const int LeftWristIdx = 15;
  private const int RightWristIdx = 16;

  // ★ 新增：被禁用时强行清理状态，防止状态卡死
  void OnDisable()
  {
    ResetStates();
  }

  void Update()
  {
    if (poseRunner == null || !poseRunner.LatestPoseResult.HasValue)
    {
      ResetStates();
      return;
    }

    var poses = poseRunner.LatestPoseResult.Value.poseLandmarks;
    if (poses == null || poses.Count == 0)
    {
      ResetStates();
      return;
    }

    var landmarks = poses[0].landmarks;
    if (landmarks == null || landmarks.Count <= RightWristIdx)
    {
      ResetStates();
      return;
    }

    bool leftUp = landmarks[LeftWristIdx].y < (landmarks[LeftShoulderIdx].y - raiseThreshold);
    bool rightUp = landmarks[RightWristIdx].y < (landmarks[RightShoulderIdx].y - raiseThreshold);

    if (isMirrored)
    {
      bool temp = leftUp;
      leftUp = rightUp;
      rightUp = temp;
    }

    if (Time.time - _lastTriggerTime < 1.0f) return;

    if (leftUp && rightUp && (!_isLeftHandUp || !_isRightHandUp)) TriggerConfirm();
    else if (leftUp && !rightUp && !_isLeftHandUp) TriggerSwipeLeft();
    else if (!leftUp && rightUp && !_isRightHandUp) TriggerSwipeRight();

    _isLeftHandUp = leftUp;
    _isRightHandUp = rightUp;
  }

  private void ResetStates()
  {
    _isLeftHandUp = false;
    _isRightHandUp = false;
  }

  private void TriggerSwipeLeft() { _lastTriggerTime = Time.time; GestureInputManager.Instance?.TriggerSwipeLeft(); }
  private void TriggerSwipeRight() { _lastTriggerTime = Time.time; GestureInputManager.Instance?.TriggerSwipeRight(); }
  private void TriggerConfirm() { _lastTriggerTime = Time.time; GestureInputManager.Instance?.TriggerConfirm(); }
}
