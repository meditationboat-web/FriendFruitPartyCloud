using System.Collections;
using System.Collections.Generic;
using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using Mediapipe.Unity.Sample.FaceLandmarkDetection;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 设置弹窗：移动灵敏度 / 张嘴阈值 / 识别人脸数 / 摄像头（下拉框）。
/// 打开时隐藏总开始界面并显示弹窗；任意退出路径都会隐藏弹窗并恢复总开始界面。
/// </summary>
public class GameSettingPopup : MonoBehaviour
{
  [Header("弹窗根物体")]
  public GameObject popupRoot;

  [Header("关闭后要恢复显示的总开始界面")]
  public GameObject mainStartPanel;

  [Header("输入框")]
  public InputField inputMoveSensitivity;
  public InputField inputJawOpenThreshold;
  public InputField inputNumFaces;

  [Header("摄像头下拉框")]
  public Dropdown cameraDropdown;

  [Header("按钮")]
  public Button btnClose;

  [Header("业务引用")]
  public DualFaceToGorillaManager dualFaceMgr;
  public FaceLandmarkerRunner faceRunner;

  [Header("默认值（首次运行使用）")]
  public int defaultNumFaces = 2;
  public int defaultCameraIndex = 1;

  // 摄像头索引缓存（static：跨弹窗开关保留）
  private static bool _hasCachedCameraIndex;
  private static int _cachedCameraIndex;

  private Coroutine _refreshCameraRoutine;

  // ================= Unity 生命周期 =================

  void Awake()
  {
    if (popupRoot != null) popupRoot.SetActive(false);

    if (btnClose != null)
    {
      btnClose.onClick.RemoveAllListeners();
      btnClose.onClick.AddListener(OnClickCloseSetting);
    }

    if (cameraDropdown != null)
    {
      cameraDropdown.ClearOptions();
      cameraDropdown.onValueChanged.RemoveAllListeners();
      cameraDropdown.onValueChanged.AddListener(OnCameraDropdownChanged);
    }
  }

  void Start()
  {
    StartCoroutine(ApplyInitialCameraIndexWhenReady());
  }

  IEnumerator ApplyInitialCameraIndexWhenReady()
  {
    float t = 0f;
    while (ImageSourceProvider.ImageSource == null && t < 5f)
    {
      t += Time.unscaledDeltaTime;
      yield return null;
    }
    if (ImageSourceProvider.ImageSource == null) yield break;

    int index = _hasCachedCameraIndex ? _cachedCameraIndex : defaultCameraIndex;
    ApplyCameraIndexToSource(index);
  }

  // ================= 打开 / 保存 / 关闭 =================

  public void OpenPopup()
  {
    if (dualFaceMgr == null || faceRunner == null)
    {
      Debug.LogError("GameSettingPopup 引用缺失，检查 Inspector 赋值");
      return;
    }

    inputMoveSensitivity.text = dualFaceMgr.moveSensitivity.ToString("F2");
    inputJawOpenThreshold.text = dualFaceMgr.jawOpenThreshold.ToString("F2");
    inputNumFaces.text = faceRunner.config.NumFaces.ToString();

    RefreshCameraDropdown();

    if (mainStartPanel != null) mainStartPanel.SetActive(false);
    if (popupRoot != null) popupRoot.SetActive(true);
  }

  public void OnClickSaveSetting()
  {
    ApplyMoveSensitivity();
    ApplyJawOpenThreshold();
    ApplyNumFaces();
    ApplySelectedCamera();
    ClosePopup();
  }

  public void OnClickCancelSetting() => ClosePopup();
  public void OnClickCloseSetting() => ClosePopup();

  // ================= 各项应用 =================

  void ApplyMoveSensitivity()
  {
    if (!TryParseFloat(inputMoveSensitivity.text, out float value))
    {
      Debug.LogWarning($"[设置] 移动灵敏度解析失败：\"{inputMoveSensitivity.text}\"（检查全角数字），保留原值");
      inputMoveSensitivity.text = dualFaceMgr.moveSensitivity.ToString("F2");
      return;
    }
    dualFaceMgr.moveSensitivity = Mathf.Clamp(value, 0.1f, 10f);
    inputMoveSensitivity.text = dualFaceMgr.moveSensitivity.ToString("F2");
  }

  void ApplyJawOpenThreshold()
  {
    if (!TryParseFloat(inputJawOpenThreshold.text, out float value))
    {
      Debug.LogWarning($"[设置] 张嘴阈值解析失败：\"{inputJawOpenThreshold.text}\"（检查全角数字），保留原值");
      inputJawOpenThreshold.text = dualFaceMgr.jawOpenThreshold.ToString("F2");
      return;
    }
    dualFaceMgr.jawOpenThreshold = Mathf.Clamp(value, 0.01f, 1f);
    inputJawOpenThreshold.text = dualFaceMgr.jawOpenThreshold.ToString("F2");
  }

  void ApplyNumFaces()
  {
    if (!TryParseInt(inputNumFaces.text, out int value))
    {
      Debug.LogWarning($"[设置] 人脸数量解析失败：\"{inputNumFaces.text}\"（检查全角数字），保留原值");
      inputNumFaces.text = faceRunner.config.NumFaces.ToString();
      return;
    }

    int clamped = Mathf.Clamp(value, 1, 4);
    bool changed = faceRunner.config.NumFaces != clamped;

    faceRunner.config.NumFaces = clamped;
    inputNumFaces.text = clamped.ToString();
    Debug.Log($"[设置] 人脸数量 = {clamped}（changed={changed}）");

    if (changed)
      Debug.Log("[设置] 人脸数量已保存，重启 Mediapipe 识别后生效");
  }

  void ApplySelectedCamera()
  {
    if (cameraDropdown == null)
    {
      Debug.LogWarning("[设置] cameraDropdown 未赋值，跳过摄像头保存");
      return;
    }

    var imgSource = ImageSourceProvider.ImageSource;
    if (imgSource == null || imgSource.sourceCandidateNames == null || imgSource.sourceCandidateNames.Length == 0)
    {
      Debug.LogWarning("[设置] ImageSource 未就绪，跳过摄像头保存");
      return;
    }

    ApplyCameraIndexToSource(cameraDropdown.value);
  }

  void OnCameraDropdownChanged(int newIndex)
  {
    ApplyCameraIndexToSource(newIndex);
  }

  void ApplyCameraIndexToSource(int value)
  {
    var imgSource = ImageSourceProvider.ImageSource;
    if (imgSource == null) return;

    var names = imgSource.sourceCandidateNames;
    if (names == null || names.Length == 0) return;

    int maxCam = names.Length - 1;
    int finalIndex = Mathf.Clamp(value, 0, maxCam);

    imgSource.SelectSource(finalIndex);                 // 立即预约（下次 Play 生效）
    WebCamSource.DefaultSourceIndex = finalIndex;       // 作为下次 Play 的默认值

    _cachedCameraIndex = finalIndex;
    _hasCachedCameraIndex = true;

    Debug.Log($"[设置] 选中摄像头 [{finalIndex}] {names[finalIndex]}");
  }

  // ================= 摄像头下拉框刷新 =================

  void RefreshCameraDropdown()
  {
    if (_refreshCameraRoutine != null) StopCoroutine(_refreshCameraRoutine);
    _refreshCameraRoutine = StartCoroutine(RefreshCameraDropdownRoutine());
  }

  IEnumerator RefreshCameraDropdownRoutine()
  {
    if (cameraDropdown == null) yield break;

    float t = 0f;
    while (ImageSourceProvider.ImageSource == null && t < 5f)
    {
      t += Time.unscaledDeltaTime;
      yield return null;
    }

    var imgSource = ImageSourceProvider.ImageSource;
    if (imgSource == null)
    {
      SetDropdownPlaceholder("(摄像头未就绪)");
      yield break;
    }

    var names = imgSource.sourceCandidateNames;
    if (names == null || names.Length == 0)
    {
      SetDropdownPlaceholder("(无可用摄像头)");
      yield break;
    }

    cameraDropdown.ClearOptions();
    cameraDropdown.AddOptions(new List<string>(names));
    cameraDropdown.interactable = true;

    int current = GetDisplayCameraIndex();
    if (current < 0 || current >= names.Length) current = 0;

    cameraDropdown.value = current;
    cameraDropdown.RefreshShownValue();
  }

  void SetDropdownPlaceholder(string label)
  {
    cameraDropdown.ClearOptions();
    cameraDropdown.AddOptions(new List<string> { label });
    cameraDropdown.value = 0;
    cameraDropdown.RefreshShownValue();
    cameraDropdown.interactable = false;
  }

  int GetDisplayCameraIndex()
  {
    if (_hasCachedCameraIndex) return _cachedCameraIndex;

    var imgSource = ImageSourceProvider.ImageSource;
    if (imgSource == null) return defaultCameraIndex;

    var names = imgSource.sourceCandidateNames;
    if (names == null || names.Length == 0) return defaultCameraIndex;

    string current = imgSource.sourceName;
    if (string.IsNullOrEmpty(current)) return defaultCameraIndex;

    for (int i = 0; i < names.Length; i++)
    {
      if (names[i] == current) return i;
    }
    return defaultCameraIndex;
  }

  // ================= 输入解析（含全角清洗） =================

  static bool TryParseInt(string raw, out int value)
  {
    value = 0;
    if (string.IsNullOrEmpty(raw)) return false;
    return int.TryParse(CleanNumberString(raw), out value);
  }

  static bool TryParseFloat(string raw, out float value)
  {
    value = 0f;
    if (string.IsNullOrEmpty(raw)) return false;
    return float.TryParse(CleanNumberString(raw), out value);
  }

  static string CleanNumberString(string raw)
  {
    if (string.IsNullOrEmpty(raw)) return raw;

    char[] buf = raw.Trim().ToCharArray();
    for (int i = 0; i < buf.Length; i++)
    {
      char c = buf[i];
      if (c >= '０' && c <= '９') buf[i] = (char)(c - '０' + '0');
      else if (c == '．') buf[i] = '.';
      else if (c == '－' || c == '﹣') buf[i] = '-';
    }
    return new string(buf).Replace(" ", "").Replace("　", "");
  }

  // ================= 关闭 =================

  private void ClosePopup()
  {
    if (popupRoot != null) popupRoot.SetActive(false);
    if (mainStartPanel != null) mainStartPanel.SetActive(true);
  }
}
