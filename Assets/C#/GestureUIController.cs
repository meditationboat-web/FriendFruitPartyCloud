using UnityEngine;

/// <summary>
/// 通用体感UI控制器：挂在任何需要体感控制的面板根物体上。
/// 它会自动监听手势事件，并控制同物体上的 UIFocusGroup。
/// </summary>
[RequireComponent(typeof(UIFocusGroup))]
public class GestureUIController : MonoBehaviour
{
  private UIFocusGroup _focusGroup;

  [Header("是否限制特定的游戏状态才响应？")]
  public bool restrictByState = false;
  [Tooltip("允许响应的游戏状态")]
  public GameState allowedState = GameState.Start;

  void Awake()
  {
    _focusGroup = GetComponent<UIFocusGroup>();
  }

  void OnEnable()
  {
    // 面板激活时，注册手势事件并开启焦点
    if (GestureInputManager.Instance != null)
    {
      GestureInputManager.Instance.OnSwipeLeft += HandleLeft;
      GestureInputManager.Instance.OnSwipeRight += HandleRight;
      GestureInputManager.Instance.OnConfirm += HandleConfirm;
    }

    if (_focusGroup != null) _focusGroup.EnableFocus();
  }

  void OnDisable()
  {
    // 面板关闭时，注销事件并关闭焦点
    if (GestureInputManager.Instance != null)
    {
      GestureInputManager.Instance.OnSwipeLeft -= HandleLeft;
      GestureInputManager.Instance.OnSwipeRight -= HandleRight;
      GestureInputManager.Instance.OnConfirm -= HandleConfirm;
    }

    if (_focusGroup != null) _focusGroup.DisableFocus();
  }

  private bool CanControl()
  {
    if (!gameObject.activeInHierarchy) return false;

    if (restrictByState && GameManager.Instance != null)
    {
      return GameManager.Instance.State == allowedState;
    }
    return true;
  }

  private void HandleLeft() { if (CanControl()) _focusGroup.MoveLeft(); }
  private void HandleRight() { if (CanControl()) _focusGroup.MoveRight(); }
  private void HandleConfirm() { if (CanControl()) _focusGroup.ClickCurrent(); }
}
