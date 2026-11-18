using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIFocusGroup : MonoBehaviour
{
  // 支持 Button 和 Toggle
  public List<Selectable> selectables = new List<Selectable>();

  [Header("选中时是否放大")]
  public bool scaleOnSelect = true;
  public float selectedScale = 1.1f;

  private int _currentIndex = 0;
  private bool _isActive = false;

  public void EnableFocus()
  {
    _isActive = true;
    _currentIndex = 0;
    UpdateHighlight();
  }

  public void DisableFocus()
  {
    _isActive = false;
    if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    for (int i = 0; i < selectables.Count; i++)
    {
      if (selectables[i] != null) selectables[i].transform.localScale = Vector3.one;
    }
  }

  public void MoveLeft()
  {
    if (!_isActive || selectables.Count == 0) return;
    _currentIndex = (_currentIndex - 1 + selectables.Count) % selectables.Count;
    UpdateHighlight();
  }

  public void MoveRight()
  {
    if (!_isActive || selectables.Count == 0) return;
    _currentIndex = (_currentIndex + 1) % selectables.Count;
    UpdateHighlight();
  }

  public void ClickCurrent()
  {
    if (!_isActive || selectables.Count == 0) return;

    var current = selectables[_currentIndex];
    if (current is Button btn) btn.onClick.Invoke();
    else if (current is Toggle toggle) toggle.isOn = !toggle.isOn;
  }

  private void UpdateHighlight()
  {
    if (!_isActive || selectables.Count == 0) return;

    if (EventSystem.current != null)
      EventSystem.current.SetSelectedGameObject(selectables[_currentIndex].gameObject);

    for (int i = 0; i < selectables.Count; i++)
    {
      if (selectables[i] == null) continue;
      selectables[i].transform.localScale = (i == _currentIndex && scaleOnSelect) ? Vector3.one * selectedScale : Vector3.one;
    }
  }
}
