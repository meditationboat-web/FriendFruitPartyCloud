using System.Collections.Generic;
using UnityEngine;
using Mediapipe.Unity.Sample.FaceLandmarkDetection;
using Mediapipe.Tasks.Vision.FaceLandmarker;
using mptcc = Mediapipe.Tasks.Components.Containers;

public class DualFaceToGorillaManager : MonoBehaviour
{
  public static DualFaceToGorillaManager Instance { get; private set; }

  // 记录吃进嘴里的物品信息
  public struct PendingItem
  {
    public ItemType Type;
    public bool IsTutorial;

    public PendingItem(ItemType type, bool isTutorial)
    {
      Type = type;
      IsTutorial = isTutorial;
    }
  }

  [Header("Mediapipe 人脸推理组件")]
  public FaceLandmarkerRunner faceLandmarkerRunner;

  [Header("玩家 1（左）")]
  public PlayerController player1;
  public GorillaFaceAnim gorillaAnim1;

  [Header("玩家 2（右）")]
  public PlayerController player2;
  public GorillaFaceAnim gorillaAnim2;

  [Header("体感参数调节")]
  public float jawOpenThreshold = 0.45f;
  public float moveSensitivity = 1.8f;

  [Header("动画器状态名称")]
  public string idleStateName = "gorilla_idle";

  [Header("==== 键盘调试 ====")]
  public bool enableKeyboardDebug = false;
  public float keyboardMoveSpeed = 5f;

  private const float CameraDepth = 10f;
  private Camera _cam;
  private bool _isFaceControlActive = false;

  public bool player1IsMouthOpen { get; private set; }
  public bool player2IsMouthOpen { get; private set; }

  private bool _p1WasMouthOpen, _p2WasMouthOpen;

  // 使用新的 PendingItem 结构体
  private readonly List<PendingItem> _p1PendingItems = new List<PendingItem>();
  private readonly List<PendingItem> _p2PendingItems = new List<PendingItem>();

  private float _minX, _maxX, _minY, _maxY;

  void Awake()
  {
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
    _cam = Camera.main;

    _minX = ViewportToWorldX(0.05f);
    _maxX = ViewportToWorldX(0.95f);
    _minY = ViewportToWorldY(0.12f);
    _maxY = ViewportToWorldY(0.9f);
  }

  // GameManager owns activation; Start must not overwrite its selection.

  public void SetFaceControlActive(bool active)
  {
    _isFaceControlActive = active;
    if (player1 != null) player1.enabled = !active;
    if (player2 != null) player2.enabled = !active;

    if (!active)
    {
      player1IsMouthOpen = false;
      player2IsMouthOpen = false;
      _p1WasMouthOpen = false;
      _p2WasMouthOpen = false;
      _p1PendingItems.Clear();
      _p2PendingItems.Clear();
    }
  }

  void Update()
  {
    if (!_isFaceControlActive) return;

    if (enableKeyboardDebug)
    {
      HandleKeyboardControl();
      return;
    }

    player1IsMouthOpen = false;
    player2IsMouthOpen = false;

    if (faceLandmarkerRunner == null || !faceLandmarkerRunner.LatestFaceResult.HasValue || _cam == null) return;

    FaceLandmarkerResult result = faceLandmarkerRunner.LatestFaceResult.Value;
    var faceList = result.faceLandmarks;
    if (faceList == null) return;

    if (faceList.Count >= 1 && player1 != null)
    {
      ProcessSingleFace(0, player1, gorillaAnim1, result, _p1PendingItems, out bool p1MouthOpen);
      player1IsMouthOpen = p1MouthOpen;
    }
    else _p1WasMouthOpen = false;

    if (faceList.Count >= 2 && player2 != null)
    {
      ProcessSingleFace(1, player2, gorillaAnim2, result, _p2PendingItems, out bool p2MouthOpen);
      player2IsMouthOpen = p2MouthOpen;
    }
    else _p2WasMouthOpen = false;
  }

  private void HandleKeyboardControl()
  {
    if (player1 == null || player2 == null) return;

    Vector3 p1Move = Vector3.zero;
    if (Input.GetKey(KeyCode.W)) p1Move.y += 1f;
    if (Input.GetKey(KeyCode.S)) p1Move.y -= 1f;
    if (Input.GetKey(KeyCode.A)) p1Move.x -= 1f;
    if (Input.GetKey(KeyCode.D)) p1Move.x += 1f;

    if (p1Move != Vector3.zero)
    {
      player1.transform.position += p1Move.normalized * keyboardMoveSpeed * Time.deltaTime;
      ClampPosition(player1);
    }

    bool p1JawOpen = Input.GetKey(KeyCode.LeftShift);
    HandleMouthState(0, p1JawOpen, gorillaAnim1, _p1PendingItems, player1);
    player1IsMouthOpen = p1JawOpen;

    Vector3 p2Move = Vector3.zero;
    if (Input.GetKey(KeyCode.UpArrow)) p2Move.y += 1f;
    if (Input.GetKey(KeyCode.DownArrow)) p2Move.y -= 1f;
    if (Input.GetKey(KeyCode.LeftArrow)) p2Move.x -= 1f;
    if (Input.GetKey(KeyCode.RightArrow)) p2Move.x += 1f;

    if (p2Move != Vector3.zero)
    {
      player2.transform.position += p2Move.normalized * keyboardMoveSpeed * Time.deltaTime;
      ClampPosition(player2);
    }

    bool p2JawOpen = Input.GetKey(KeyCode.RightControl);
    HandleMouthState(1, p2JawOpen, gorillaAnim2, _p2PendingItems, player2);
    player2IsMouthOpen = p2JawOpen;
  }

  private void ClampPosition(PlayerController player)
  {
    Vector3 pos = player.transform.position;
    pos.x = Mathf.Clamp(pos.x, _minX, _maxX);
    pos.y = Mathf.Clamp(pos.y, _minY, _maxY);
    player.transform.position = pos;
  }

  private void HandleMouthState(int faceIndex, bool isOpenNow, GorillaFaceAnim faceAnim, List<PendingItem> pendingItems, PlayerController player)
  {
    bool wasOpen = (faceIndex == 0) ? _p1WasMouthOpen : _p2WasMouthOpen;

    if (isOpenNow)
    {
      if (!wasOpen) faceAnim?.PlayAnim("OpenMouth");
    }
    else
    {
      if (wasOpen)
      {
        if (pendingItems.Count > 0) ConsumePendingItems(player, faceAnim, pendingItems);
        else faceAnim?.PlayState(idleStateName); // 没吃到东西切回待机
      }
    }

    if (faceIndex == 0) _p1WasMouthOpen = isOpenNow;
    else _p2WasMouthOpen = isOpenNow;
  }

  private void ProcessSingleFace(int faceIndex, PlayerController player, GorillaFaceAnim faceAnim, FaceLandmarkerResult result, List<PendingItem> pendingItems, out bool isMouthOpen)
  {
    isMouthOpen = false;
    mptcc.NormalizedLandmarks oneFace = result.faceLandmarks[faceIndex];
    var nose = oneFace.landmarks[1];

    float minX, maxX;
    float minY = ViewportToWorldY(0.12f);
    float maxY = ViewportToWorldY(1f);

    if (faceIndex == 0) { minX = ViewportToWorldX(0.05f); maxX = ViewportToWorldX(0.45f); }
    else { minX = ViewportToWorldX(0.55f); maxX = ViewportToWorldX(0.95f); }

    float targetX = Mathf.Lerp(minX, maxX, nose.x);
    float targetY = Mathf.Lerp(maxY, minY, nose.y);

    Vector3 pos = player.transform.position;
    pos.x = Mathf.Lerp(pos.x, targetX, Time.deltaTime * moveSensitivity);
    pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * moveSensitivity);
    player.transform.position = pos;

    float jawOpenVal = 0f;
    if (result.faceBlendshapes != null && result.faceBlendshapes.Count > faceIndex)
    {
      var blendCategories = result.faceBlendshapes[faceIndex].categories;
      foreach (var cat in blendCategories)
      {
        if (cat.categoryName == "jawOpen") { jawOpenVal = cat.score; break; }
      }
    }

    bool isJawOpenNow = jawOpenVal > jawOpenThreshold;
    HandleMouthState(faceIndex, isJawOpenNow, faceAnim, pendingItems, player);
    isMouthOpen = isJawOpenNow;
  }

  // 【修改】接收 IsTutorial 标记
  public void AddItemToMouth(int playerIndex, ItemType type, bool isTutorial)
  {
    PendingItem item = new PendingItem(type, isTutorial);
    if (playerIndex == 0) _p1PendingItems.Add(item);
    else _p2PendingItems.Add(item);
  }

  private void ConsumePendingItems(PlayerController player, GorillaFaceAnim faceAnim, List<PendingItem> pendingItems)
  {
    if (pendingItems.Count == 0) return;

    bool playHitBomb = false;
    bool playStun = false;
    bool playEatBanana = false;
    bool playEatFruit = false;

    foreach (var item in pendingItems)
    {
      ItemType type = item.Type;

      // 【核心】如果是教学水果，只播放对应动画和音效，不加分不扣任务
      if (item.IsTutorial)
      {
        if (AudioManager.Instance != null)
        {
          switch (type)
          {
            case ItemType.Apple: AudioManager.Instance.PlayApple(); break;
            case ItemType.Banana: AudioManager.Instance.PlayBanana(); break;
            case ItemType.Melon: AudioManager.Instance.PlayMelon(); break;
          }
        }
        if (type == ItemType.Banana) playEatBanana = true;
        else playEatFruit = true;
        continue; // 跳过下面的计分逻辑
      }

      // --- 正常的物品结算逻辑 ---
      switch (type)
      {
        case ItemType.Bomb:
          player.AddScore(-5);
          player.TriggerBombFlash();
          if (AudioManager.Instance != null) AudioManager.Instance.PlayBomb();

          ItemController[] allItems = Object.FindObjectsOfType<ItemController>(includeInactive: true);
          foreach (ItemController other in allItems)
          {
            if (other != null && other.Owner == player.playerIndex) other.ForceRemove();
          }
          ItemSpawner.Instance?.PauseSpawn(player.playerIndex, 3f);
          playHitBomb = true;
          break;

        case ItemType.BananaPeel:
          player.Stun(2f);
          if (AudioManager.Instance != null) AudioManager.Instance.PlayBananaPeel();
          playStun = true;
          break;

        default: // 普通水果
          int realAdd = player.CalculateComboScore(type, ItemController.Scores[(int)type]);
          player.AddScore(realAdd);
          player.ConsumeMissionItem(type);

          if (AudioManager.Instance != null)
          {
            switch (type)
            {
              case ItemType.Apple: AudioManager.Instance.PlayApple(); break;
              case ItemType.Banana: AudioManager.Instance.PlayBanana(); break;
              case ItemType.Melon: AudioManager.Instance.PlayMelon(); break;
            }
          }
          if (type == ItemType.Banana) playEatBanana = true;
          else playEatFruit = true;
          break;
      }
    }

    // 动画优先级
    if (playHitBomb) faceAnim?.PlayAnim("HitBomb");
    else if (playStun) faceAnim?.PlayAnim("Stun");
    else if (playEatBanana) faceAnim?.PlayAnim("EatBanana");
    else if (playEatFruit) faceAnim?.PlayAnim("EatFruit");

    pendingItems.Clear();
    GameManager.Instance?.UpdateHUD();
  }

  float ViewportToWorldX(float vx) => _cam.ViewportToWorldPoint(new Vector3(vx, 0.5f, CameraDepth)).x;
  float ViewportToWorldY(float vy) => _cam.ViewportToWorldPoint(new Vector3(0.5f, vy, CameraDepth)).y;
}
