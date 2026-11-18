using System;
using UnityEngine;

public class GestureInputManager : MonoBehaviour
{
  public static GestureInputManager Instance;

  // UI 监听的事件
  public event Action OnSwipeLeft;
  public event Action OnSwipeRight;
  public event Action OnConfirm;

  [Header("键盘调试 (A=左手, D=右手, Space=双手)")]
  public bool enableKeyboardDebug = true;

  [Header("全局防抖冷却时间（秒）")]
  public float cooldown = 1.0f;
  private float _nextGestureTime = 0f;

  void Awake() { Instance = this; }

  void Update()
  {
    if (!enableKeyboardDebug || Time.time < _nextGestureTime) return;

    if (Input.GetKeyDown(KeyCode.A)) TriggerSwipeLeft();
    if (Input.GetKeyDown(KeyCode.D)) TriggerSwipeRight();
    if (Input.GetKeyDown(KeyCode.Space)) TriggerConfirm();
  }

  public void TriggerSwipeLeft()
  {
    if (Time.time < _nextGestureTime) return;
    _nextGestureTime = Time.time + cooldown;
    OnSwipeLeft?.Invoke();
  }

  public void TriggerSwipeRight()
  {
    if (Time.time < _nextGestureTime) return;
    _nextGestureTime = Time.time + cooldown;
    OnSwipeRight?.Invoke();
  }

  public void TriggerConfirm()
  {
    if (Time.time < _nextGestureTime) return;
    _nextGestureTime = Time.time + cooldown;
    OnConfirm?.Invoke();
  }
}
