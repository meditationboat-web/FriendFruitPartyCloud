using UnityEngine;
using UnityEngine.UI;

public class HowToPlayPanel : MonoBehaviour
{
  [Header("面板根物体")]
  public GameObject panelRoot;

  [Header("三个子页面（请给这三个物体分别挂载 UIFocusGroup 和 GestureUIController）")]
  public GameObject page1;
  public GameObject page2;
  public GameObject page3;

  [Header("翻页按钮")]
  public Button btnPage1ToPage2;
  public Button btnPage2ToPage1;
  public Button btnPage2ToPage3;
  public Button btnPage3ToPage2;

  [Header("每页退出按钮")]
  public Button btnExit1;
  public Button btnExit2;
  public Button btnExit3;

  [Header("关闭后要显示的总开始界面")]
  public GameObject mainStartPanel;

  private int _currentPage = 1;

  void Awake()
  {
    if (panelRoot != null) panelRoot.SetActive(false);
    BindButtons();
  }

  void BindButtons()
  {
    Bind(btnPage1ToPage2, () => ShowPage(2));
    Bind(btnPage2ToPage1, () => ShowPage(1));
    Bind(btnPage2ToPage3, () => ShowPage(3));
    Bind(btnPage3ToPage2, () => ShowPage(2));
    Bind(btnExit1, Close);
    Bind(btnExit2, Close);
    Bind(btnExit3, Close);
  }

  static void Bind(Button btn, UnityEngine.Events.UnityAction action)
  {
    if (btn == null) return;
    btn.onClick.RemoveAllListeners();
    btn.onClick.AddListener(action);
  }

  public void Open()
  {
    if (mainStartPanel != null) mainStartPanel.SetActive(false);
    if (panelRoot != null) panelRoot.SetActive(true);
    ShowPage(1);
  }

  public void Close()
  {
    if (panelRoot != null) panelRoot.SetActive(false);
    if (mainStartPanel != null) mainStartPanel.SetActive(true);
  }

  void ShowPage(int page)
  {
    _currentPage = Mathf.Clamp(page, 1, 3);

    // 通过 SetActive 切换页面。
    // 关闭的页面会自动触发 GestureUIController.OnDisable 注销事件，
    // 开启的页面会自动触发 GestureUIController.OnEnable 注册事件并接管焦点。
    if (page1 != null) page1.SetActive(_currentPage == 1);
    if (page2 != null) page2.SetActive(_currentPage == 2);
    if (page3 != null) page3.SetActive(_currentPage == 3);
  }
}
