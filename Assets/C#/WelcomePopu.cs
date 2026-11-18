using UnityEngine;
using UnityEngine.UI;

public class WelcomePopup : MonoBehaviour
{
  [Header("弹窗根物体")]
  public GameObject popupRoot;
  public GameObject mainStartPanel;

  [Header("UI焦点组")]
  public UIFocusGroup focusGroup;

  [Header("不再提示 Toggle")]
  public Toggle toggleDontShow;

  private bool _isShowing = false;

  void Awake()
  {
    if (popupRoot != null) popupRoot.SetActive(false);
  }

  public bool ShouldShow() => PlayerPrefs.GetInt("WelcomeDontShowAgain", 0) == 0;

  public void Show()
  {
    _isShowing = true;
    if (mainStartPanel != null) mainStartPanel.SetActive(false);
    if (popupRoot != null) popupRoot.SetActive(true);

    if (toggleDontShow != null) toggleDontShow.isOn = false;
    if (focusGroup != null) focusGroup.EnableFocus();
  }

  public void Close()
  {
    _isShowing = false;
    if (focusGroup != null) focusGroup.DisableFocus();

    if (toggleDontShow != null && toggleDontShow.isOn)
    {
      PlayerPrefs.SetInt("WelcomeDontShowAgain", 1);
      PlayerPrefs.Save();
    }

    if (popupRoot != null) popupRoot.SetActive(false);
    if (mainStartPanel != null) mainStartPanel.SetActive(true);
  }

  void OnEnable()
  {
    if (GestureInputManager.Instance != null)
    {
      GestureInputManager.Instance.OnSwipeLeft += OnLeft;
      GestureInputManager.Instance.OnSwipeRight += OnRight;
      GestureInputManager.Instance.OnConfirm += OnConfirm;
    }
  }

  void OnDisable()
  {
    if (GestureInputManager.Instance != null)
    {
      GestureInputManager.Instance.OnSwipeLeft -= OnLeft;
      GestureInputManager.Instance.OnSwipeRight -= OnRight;
      GestureInputManager.Instance.OnConfirm -= OnConfirm;
    }
  }

  private void OnLeft() { if (_isShowing) focusGroup.MoveLeft(); }
  private void OnRight() { if (_isShowing) focusGroup.MoveRight(); }
  private void OnConfirm() { if (_isShowing) focusGroup.ClickCurrent(); }
}
