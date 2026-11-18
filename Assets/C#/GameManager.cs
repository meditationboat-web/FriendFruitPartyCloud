using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using Mediapipe.Unity.Sample.FaceLandmarkDetection;

public enum GameState { Start, Countdown, Playing, GameOver }
public enum GameMode { FruitCatch, TaskMission }

[System.Serializable]
public struct MissionData
{
  public int bananaCount;
  public int appleCount;
  public int melonCount;
  public int Total => bananaCount + appleCount + melonCount;
}

public class GameManager : MonoBehaviour
{
  public static GameManager Instance;

  [Header("当前模式")]
  public GameMode currentMode = GameMode.FruitCatch;

  [Header("玩家")]
  public PlayerController player1;
  public PlayerController player2;

  [Header("UI 面板")]
  public GameObject mainStartPanel;
  public GameObject gamePanel;
  public GameObject gameOverPanelFruitCatch;
  public GameObject gameOverPanelTaskMission;

  [HideInInspector] public GameObject fruitCatchSubPanel;
  [HideInInspector] public GameObject taskMissionSubPanel;

  [Header("通用 UI 文本")]
  public Text countdownText;
  public Text timerText;

  // ================= 模式一专属 UI =================
  [Header("模式一 UI")]
  public Text score1Text;
  public Text score2Text;
  public Text resultScore1FruitCatch;
  public Text resultScore2FruitCatch;
  public Image resultImageFruitCatch;
  public Sprite p1WinSpriteFruitCatch;
  public Sprite p2WinSpriteFruitCatch;
  public Sprite drawSpriteFruitCatch;
  public Image p1ComboImage;
  public Text p1ComboText;
  public Image p2ComboImage;
  public Text p2ComboText;

  // ================= 模式二专属 UI =================
  [Header("模式二 UI")]
  public TextMeshProUGUI p1BananaText, p1AppleText, p1MelonText;
  public TextMeshProUGUI p2BananaText, p2AppleText, p2MelonText;
  public Animator p1BananaAnim, p1AppleAnim, p1MelonAnim, p2BananaAnim, p2AppleAnim, p2MelonAnim;
  public Image resultImageTaskMission;
  public Sprite p1WinSpriteTaskMission, p2WinSpriteTaskMission, drawSpriteTaskMission;

  [Header("水果Sprite素材")]
  public Sprite bananaSprite;
  public Sprite appleSprite;
  public Sprite melonSprite;

  [Header("对局配置")]
  public float gameDuration = 60f;
  public int missionMinTotal = 25;
  public int missionMaxTotal = 35;

  [Header("==== 弹窗与面板 ====")]
  public Button btnOpenSetting;
  public GameSettingPopup settingPopup;
  public Button btnOpenHowToPlay;
  public HowToPlayPanel howToPlayPanel;
  public WelcomePopup welcomePopup;

  // ================= 体感控制管理器 =================
  [Header("==== 体感控制 ====")]
  public PoseGestureToUI poseGestureController;
  public DualFaceToGorillaManager faceGestureController;

  [Tooltip("整个 Pose System 父物体")]
  public GameObject poseRunnerObject;

  [Tooltip("整个 Face System 父物体")]
  public GameObject faceRunnerObject;

  [Header("==== 新手教学 ====")]
  public GameObject[] tutorialItemPrefabs = new GameObject[3];
  public Transform[] p1TutorialPoints = new Transform[3];
  public Transform[] p2TutorialPoints = new Transform[3];
  public Text tutorialHintText;
  public float tutorialMaxDuration = 25f;

  [HideInInspector] public MissionData sharedMission;
  public bool IsTutorialPhase { get; private set; }

  private int _p1LastBanana, _p1LastApple, _p1LastMelon;
  private int _p2LastBanana, _p2LastApple, _p2LastMelon;
  private readonly List<ItemController> _tutorialItems = new List<ItemController>();

  public GameState State { get; private set; }
  public float TimeLeft { get; private set; }
  private Coroutine gameRoutine;
  private Coroutine _modeSwitchCoroutine;

  private bool IsRunning => State == GameState.Countdown || State == GameState.Playing;

  void Awake()
  {
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
    Application.targetFrameRate = 60;
    gameObject.AddComponent<LiveGameCameraBackground>().game = this;
    // Prevent both runners from opening the shared camera during scene startup.
    if (poseRunnerObject != null) poseRunnerObject.SetActive(false);
    if (faceRunnerObject != null) faceRunnerObject.SetActive(false);
  }

  void Start()
  {
    State = GameState.Start;

    if (settingPopup != null && settingPopup.mainStartPanel == null) settingPopup.mainStartPanel = mainStartPanel;
    if (welcomePopup != null && welcomePopup.mainStartPanel == null) welcomePopup.mainStartPanel = mainStartPanel;
    if (howToPlayPanel != null && howToPlayPanel.mainStartPanel == null) howToPlayPanel.mainStartPanel = mainStartPanel;

    HideAllMainPanels();
    if (countdownText != null) countdownText.gameObject.SetActive(false);
    if (resultImageFruitCatch != null) resultImageFruitCatch.gameObject.SetActive(false);
    if (resultImageTaskMission != null) resultImageTaskMission.gameObject.SetActive(false);
    HideAllComboUI();
    SetMissionTextActive(false);

    BindButton(btnOpenSetting, OnClickOpenSettingPopup);
    BindButton(btnOpenHowToPlay, OnClickOpenHowToPlay);

    if (welcomePopup != null && welcomePopup.ShouldShow())
      welcomePopup.Show();
    else
      ShowMainStartPage();

    StartCoroutine(DelayedInit());
  }

  IEnumerator DelayedInit()
  {
    yield return null;
    SetControlMode(isUI: true);
  }

  void BindButton(Button btn, UnityEngine.Events.UnityAction action)
  {
    if (btn == null) return;
    btn.onClick.RemoveAllListeners();
    btn.onClick.AddListener(action);
  }

  // ================= 体感模式切换（核心控制显隐） =================
  void SetControlMode(bool isUI)
  {
    if (_modeSwitchCoroutine != null) StopCoroutine(_modeSwitchCoroutine);
    _modeSwitchCoroutine = StartCoroutine(SwitchModeRoutine(isUI));
  }

  IEnumerator SwitchModeRoutine(bool isUI)
  {
    // Stop inference before releasing the shared camera, then enable one owner.
    if (poseGestureController != null) poseGestureController.enabled = false;
    if (faceGestureController != null) faceGestureController.SetFaceControlActive(false);
    if (poseRunnerObject != null) poseRunnerObject.SetActive(false);
    if (faceRunnerObject != null) faceRunnerObject.SetActive(false);

    yield return new WaitForSecondsRealtime(0.4f);

    if (isUI)
    {
      if (poseRunnerObject != null) poseRunnerObject.SetActive(true);
      if (poseGestureController != null) poseGestureController.enabled = true;
    }
    else
    {
      if (faceRunnerObject != null) faceRunnerObject.SetActive(true);
      if (faceGestureController != null) faceGestureController.SetFaceControlActive(true);
    }
    _modeSwitchCoroutine = null;
  }

  // ================= 首页入口 =================
  public void OnClickEnterFruitCatchPage() { if (IsRunning) return; currentMode = GameMode.FruitCatch; OnClickStartGameFromModePage(); }
  public void OnClickEnterTaskMissionPage() { if (IsRunning) return; currentMode = GameMode.TaskMission; GenerateRandomSharedMission(); OnClickStartGameFromModePage(); }
  public void OnClickOpenHowToPlay() { if (!IsRunning && howToPlayPanel != null) howToPlayPanel.Open(); }
  public void OnClickOpenSettingPopup() { if (!IsRunning && settingPopup != null) settingPopup.OpenPopup(); }

  public void OnClickStartGameFromModePage()
  {
    if (IsRunning) return;
    if (player1 == null || player2 == null) { Debug.LogError("玩家未赋值"); return; }
    if (gameRoutine != null) StopCoroutine(gameRoutine);
    gameRoutine = StartCoroutine(GameRoutine());
  }

  public void OnRestartButton()
  {
    if (currentMode == GameMode.TaskMission) GenerateRandomSharedMission();
    OnClickStartGameFromModePage();
  }

  public void OnBackToMainStartPage()
  {
    if (IsRunning) return;
    if (gameRoutine != null) { StopCoroutine(gameRoutine); gameRoutine = null; }

    State = GameState.Start;
    if (ItemSpawner.Instance != null) ItemSpawner.Instance.ClearAll();
    ForceClearTutorialItems();

    ShowMainStartPage();
    SetControlMode(isUI: true); // 【恢复 UI 肢体控制】

    if (countdownText != null) countdownText.gameObject.SetActive(false);
    if (resultImageFruitCatch != null) resultImageFruitCatch.gameObject.SetActive(false);
    if (resultImageTaskMission != null) resultImageTaskMission.gameObject.SetActive(false);
    HideAllComboUI();
    SetMissionTextActive(false);
  }

  // ================= 对局主流程 =================
  IEnumerator GameRoutine()
  {
    // 【进入游戏】：隐藏 Pose System，打开 Face System
    SetControlMode(isUI: false);

    if (mainStartPanel != null) mainStartPanel.SetActive(false);
    gameOverPanelFruitCatch.SetActive(false);
    gameOverPanelTaskMission.SetActive(false);
    gamePanel.SetActive(true);

    ApplyModeHUDVisibility();
    if (resultImageFruitCatch != null) resultImageFruitCatch.gameObject.SetActive(false);
    if (resultImageTaskMission != null) resultImageTaskMission.gameObject.SetActive(false);
    HideAllComboUI();
    ResetPlayersForNewRound();

    if (currentMode == GameMode.TaskMission) InitMissionForBothPlayers();

    TimeLeft = gameDuration;
    UpdateHUD();

    yield return StartCoroutine(TutorialPhase());

    State = GameState.Countdown;
    countdownText.gameObject.SetActive(true);
    for (int i = 3; i >= 1; i--) { countdownText.text = i.ToString(); yield return new WaitForSeconds(1f); }
    countdownText.text = "GO!";
    yield return new WaitForSeconds(0.6f);
    countdownText.gameObject.SetActive(false);

    if (ItemSpawner.Instance != null) ItemSpawner.Instance.ResetTimers();
    State = GameState.Playing;

    if (AudioManager.Instance != null) AudioManager.Instance.PlayBGM();

    while (true)
    {
      if (currentMode == GameMode.TaskMission)
      {
        if (player1.IsMissionComplete() || player2.IsMissionComplete()) { EndGame(); yield break; }
      }
      else
      {
        TimeLeft -= Time.deltaTime;
        if (TimeLeft <= 0) { EndGame(); yield break; }
      }
      UpdateHUD();
      yield return null;
    }
  }

  IEnumerator TutorialPhase()
  {
    IsTutorialPhase = true;
    _tutorialItems.Clear();
    SpawnTutorialItems();

    Text hintText = tutorialHintText != null ? tutorialHintText : countdownText;
    if (hintText != null) { hintText.text = "吃掉水果开始游戏！"; hintText.gameObject.SetActive(true); }

    State = GameState.Playing;

    float elapsed = 0f;
    while (elapsed < tutorialMaxDuration)
    {
      _tutorialItems.RemoveAll(ic => ic == null);
      if (_tutorialItems.Count == 0) break;
      elapsed += Time.deltaTime;
      yield return null;
    }

    ForceClearTutorialItems();
    if (hintText != null) hintText.gameObject.SetActive(false);
    IsTutorialPhase = false;
  }

  void ForceClearTutorialItems()
  {
    for (int i = 0; i < _tutorialItems.Count; i++) if (_tutorialItems[i] != null) _tutorialItems[i].ForceRemove();
    _tutorialItems.Clear();
  }

  void SpawnTutorialItems()
  {
    GameObject[] prefabs = tutorialItemPrefabs;
    if (prefabs == null || prefabs.Length < 3 || prefabs[0] == null)
      prefabs = ItemSpawner.Instance != null ? ItemSpawner.Instance.itemPrefabs : null;
    if (prefabs == null || prefabs.Length < 3) return;

    SpawnTutorialForPlayer(0, p1TutorialPoints, prefabs);
    SpawnTutorialForPlayer(1, p2TutorialPoints, prefabs);
  }

  void SpawnTutorialForPlayer(int owner, Transform[] points, GameObject[] prefabs)
  {
    if (points == null) return;
    for (int i = 0; i < points.Length && i < 3; i++)
    {
      if (points[i] == null) continue;
      GameObject go = Instantiate(prefabs[i], points[i].position, Quaternion.identity);
      ItemController ic = go.GetComponent<ItemController>();
      if (ic == null) ic = go.GetComponentInChildren<ItemController>(true);
      if (ic == null) { Destroy(go); continue; }
      ic.InitTutorial((ItemType)i, owner);
      ic.transform.position = points[i].position;
      _tutorialItems.Add(ic);
    }
  }

  void ResetPlayersForNewRound() { player1.ResetScore(); player2.ResetScore(); player1.ResetCombo(); player2.ResetCombo(); }

  void InitMissionForBothPlayers()
  {
    player1.InitMissionCount(sharedMission); player2.InitMissionCount(sharedMission);
    _p1LastBanana = player1.RemainBanana; _p1LastApple = player1.RemainApple; _p1LastMelon = player1.RemainMelon;
    _p2LastBanana = player2.RemainBanana; _p2LastApple = player2.RemainApple; _p2LastMelon = player2.RemainMelon;
    ResetAllMissionAnimators(); RefreshMissionUI();
  }

  void GenerateRandomSharedMission()
  {
    MissionData md;
    do { md.bananaCount = Random.Range(5, 16); md.appleCount = Random.Range(5, 16); md.melonCount = Random.Range(5, 16); }
    while (md.Total < missionMinTotal || md.Total > missionMaxTotal);
    sharedMission = md;
  }

  void ApplyModeHUDVisibility()
  {
    bool isMode1 = currentMode == GameMode.FruitCatch;
    if (score1Text != null && score1Text.transform.parent != null) score1Text.transform.parent.gameObject.SetActive(isMode1);
    if (score2Text != null && score2Text.transform.parent != null) score2Text.transform.parent.gameObject.SetActive(isMode1);
    if (timerText != null) timerText.gameObject.SetActive(isMode1);
    SetMissionTextActive(!isMode1);
  }

  public void UpdateHUD()
  {
    if (player1 == null || player2 == null || currentMode != GameMode.FruitCatch) return;
    if (timerText != null) timerText.text = Mathf.CeilToInt(TimeLeft).ToString();
    if (score1Text != null) score1Text.text = " " + player1.Score;
    if (score2Text != null) score2Text.text = " " + player2.Score;
  }

  public void HideAllComboUI()
  {
    if (p1ComboImage != null) p1ComboImage.gameObject.SetActive(false);
    if (p1ComboText != null) p1ComboText.gameObject.SetActive(false);
    if (p2ComboImage != null) p2ComboImage.gameObject.SetActive(false);
    if (p2ComboText != null) p2ComboText.gameObject.SetActive(false);
  }

  void SetMissionTextActive(bool active)
  {
    SetParentActive(p1BananaText, active); SetParentActive(p1AppleText, active); SetParentActive(p1MelonText, active);
    SetParentActive(p2BananaText, active); SetParentActive(p2AppleText, active); SetParentActive(p2MelonText, active);
  }

  static void SetParentActive(Component c, bool active) { if (c != null && c.transform.parent != null) c.transform.parent.gameObject.SetActive(active); }

  public void RefreshMissionUI()
  {
    if (currentMode != GameMode.TaskMission) return;
    int p1B = player1.RemainBanana, p1A = player1.RemainApple, p1M = player1.RemainMelon;
    if (p1BananaText) p1BananaText.text = $"×{p1B}"; if (p1AppleText) p1AppleText.text = $"×{p1A}"; if (p1MelonText) p1MelonText.text = $"×{p1M}";
    CheckAndTriggerAnim(p1BananaAnim, ref _p1LastBanana, p1B); CheckAndTriggerAnim(p1AppleAnim, ref _p1LastApple, p1A); CheckAndTriggerAnim(p1MelonAnim, ref _p1LastMelon, p1M);
    int p2B = player2.RemainBanana, p2A = player2.RemainApple, p2M = player2.RemainMelon;
    if (p2BananaText) p2BananaText.text = $"×{p2B}"; if (p2AppleText) p2AppleText.text = $"×{p2A}"; if (p2MelonText) p2MelonText.text = $"×{p2M}";
    CheckAndTriggerAnim(p2BananaAnim, ref _p2LastBanana, p2B); CheckAndTriggerAnim(p2AppleAnim, ref _p2LastApple, p2A); CheckAndTriggerAnim(p2MelonAnim, ref _p2LastMelon, p2M);
  }

  void CheckAndTriggerAnim(Animator anim, ref int oldVal, int newVal)
  {
    if (anim == null || oldVal == newVal) return;
    if (oldVal > 0) { if (newVal > 0) anim.SetTrigger("OnTextChanged"); else if (newVal == 0) anim.SetTrigger("OnItemComplete"); }
    oldVal = newVal;
  }

  void ResetAllMissionAnimators()
  {
    ResetOneAnimator(p1BananaAnim); ResetOneAnimator(p1AppleAnim); ResetOneAnimator(p1MelonAnim);
    ResetOneAnimator(p2BananaAnim); ResetOneAnimator(p2AppleAnim); ResetOneAnimator(p2MelonAnim);
  }

  static void ResetOneAnimator(Animator anim) { if (anim != null) anim.Play("IdleNormal", 0, 0); }

  void EndGame()
  {
    State = GameState.GameOver;

    // 【游戏结束】：恢复 Pose System，关闭 Face System
    SetControlMode(isUI: true);

    if (AudioManager.Instance != null) { AudioManager.Instance.StopBGM(); AudioManager.Instance.PlayResult(); }
    if (ItemSpawner.Instance != null) ItemSpawner.Instance.ClearAll();
    gamePanel.SetActive(false);
    HideAllComboUI();
    SetMissionTextActive(false);

    if (currentMode == GameMode.FruitCatch) EndFruitCatch();
    else EndTaskMission();
  }

  void EndFruitCatch()
  {
    gameOverPanelFruitCatch.SetActive(true);
    int s1 = player1.Score, s2 = player2.Score;
    if (resultScore1FruitCatch != null) resultScore1FruitCatch.text = s1.ToString();
    if (resultScore2FruitCatch != null) resultScore2FruitCatch.text = s2.ToString();
    if (resultImageFruitCatch != null)
    {
      resultImageFruitCatch.gameObject.SetActive(true);
      Sprite target = s1 > s2 ? p1WinSpriteFruitCatch : s2 > s1 ? p2WinSpriteFruitCatch : drawSpriteFruitCatch;
      if (target != null) resultImageFruitCatch.sprite = target;
    }
  }

  void EndTaskMission()
  {
    gameOverPanelTaskMission.SetActive(true);
    bool p1Done = player1.IsMissionComplete(), p2Done = player2.IsMissionComplete();
    if (resultImageTaskMission != null)
    {
      resultImageTaskMission.gameObject.SetActive(true);
      Sprite target = p1Done && !p2Done ? p1WinSpriteTaskMission : p2Done && !p1Done ? p2WinSpriteTaskMission : drawSpriteTaskMission;
      if (target != null) resultImageTaskMission.sprite = target;
    }
  }

  void HideAllMainPanels()
  {
    if (mainStartPanel != null) mainStartPanel.SetActive(false);
    if (gamePanel != null) gamePanel.SetActive(false);
    if (gameOverPanelFruitCatch != null) gameOverPanelFruitCatch.SetActive(false);
    if (gameOverPanelTaskMission != null) gameOverPanelTaskMission.SetActive(false);
  }

  void ShowMainStartPage() { HideAllMainPanels(); if (mainStartPanel != null) mainStartPanel.SetActive(true); }
}

// Camera background is created at runtime so replacing this script needs no scene edits.
public sealed class LiveGameCameraBackground : MonoBehaviour
{
  public GameManager game;
  private Canvas canvas;
  private RawImage image;
  private RawImage original;
  private readonly Dictionary<Renderer, bool> scenery = new Dictionary<Renderer, bool>();
  private Image panelImage;
  private bool panelWasEnabled;
  private bool showing;

  void Start()
  {
    var camera = Camera.main;
    if (camera == null) { Debug.LogError("Live camera background requires MainCamera."); return; }
    var root = new GameObject("LiveGameCameraBackground", typeof(RectTransform), typeof(Canvas));
    root.transform.SetParent(camera.transform, false);
    canvas = root.GetComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceCamera;
    canvas.worldCamera = camera;
    // Put the camera image behind world-space players and fruit, with HUD above both.
    canvas.planeDistance = camera.farClipPlane - 1f;
    canvas.sortingOrder = -32700;
    var view = new GameObject("CameraImage", typeof(RectTransform), typeof(RawImage));
    view.transform.SetParent(root.transform, false);
    image = view.GetComponent<RawImage>();
    image.raycastTarget = false;
    image.color = Color.white;
    foreach (var renderer in FindObjectsOfType<SpriteRenderer>(true))
      if (renderer.gameObject.scene == game.gameObject.scene &&
          (renderer.name == "Square" || renderer.name == "Square (1)"))
        scenery[renderer] = renderer.enabled;
    if (game.gamePanel != null) panelImage = game.gamePanel.GetComponent<Image>();
    if (panelImage != null) panelWasEnabled = panelImage.enabled;
    canvas.gameObject.SetActive(false);
  }

  void LateUpdate()
  {
    if (canvas == null || game == null) return;
    bool active = game.faceRunnerObject != null && game.faceRunnerObject.activeInHierarchy;
    canvas.gameObject.SetActive(active);
    if (active != showing)
    {
      foreach (var entry in scenery) if (entry.Key != null) entry.Key.enabled = active ? false : entry.Value;
      if (panelImage != null) panelImage.enabled = active ? false : panelWasEnabled;
      if (!active && original != null) original.enabled = true;
      showing = active;
    }
    if (!active) return;
    if (original == null)
    {
      var runner = game.faceGestureController != null ? game.faceGestureController.faceLandmarkerRunner : null;
      if (runner != null)
      {
        original = runner.PreviewImage;
      }
    }
    if (original == null) return;
    original.enabled = false;
    image.texture = original.texture;
    image.uvRect = original.uvRect;
    image.rectTransform.localRotation = original.rectTransform.localRotation;
    if (image.texture == null) return;
    var frame = (RectTransform)canvas.transform;
    var rect = image.rectTransform;
    rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
    rect.anchoredPosition = Vector2.zero;
    bool rotated = Mathf.Abs(Mathf.Sin(rect.localEulerAngles.z * Mathf.Deg2Rad)) > .5f;
    float w = image.texture.width, h = image.texture.height;
    float scale = Mathf.Max(frame.rect.width / (rotated ? h : w), frame.rect.height / (rotated ? w : h));
    rect.sizeDelta = new Vector2(w, h) * scale;
  }

  void OnDestroy()
  {
    foreach (var entry in scenery) if (entry.Key != null) entry.Key.enabled = entry.Value;
    if (panelImage != null) panelImage.enabled = panelWasEnabled;
    if (original != null) original.enabled = true;
    if (canvas != null) Destroy(canvas.gameObject);
  }
}
