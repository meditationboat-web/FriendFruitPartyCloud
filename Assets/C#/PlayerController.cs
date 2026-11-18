using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerController : MonoBehaviour
{
  public static readonly List<PlayerController> All = new List<PlayerController>();

  [Header("玩家编号: 0=玩家一(左), 1=玩家二(右)")]
  public int playerIndex = 0;
  [Header("抓取判定半径(世界单位)")]
  public float dragRadius = 1.8f;
  [Header("得分 / 受击判定半径(世界单位)")]
  public float hitRadius = 1.0f;
  [Header("模式一：受击无敌时间（秒），也用作模式一炸弹闪烁时长")]
  public float invincibleDuration = 1.0f;
  [Header("连击超时时间(秒)，超过该时间未吃到同类水果重置连击")]
  public float comboExpireTime = 4f;

  [Header("本玩家专属连击UI，拖入场景UI组件")]
  public Image comboImage;
  public TextMeshProUGUI comboText;
  [Tooltip("连击UI的根物体的Animator（存放弹出动画）")]
  public Animator comboAnimator;

  [Header("【飘字】加分浮动TMP预制体(必须是Canvas下TextMeshPro‑UGUI)")]
  public GameObject floatingScoreTMPPrefab;
  [Header("【飘字】相对于玩家右上角的UI偏移(像素)")]
  public Vector2 floatingTextOffset = new Vector2(60, 40);

  public int Score { get; private set; }

  //========任务模式剩余计数========
  public int RemainBanana { get; private set; }
  public int RemainApple { get; private set; }
  public int RemainMelon { get; private set; }

  //====连击系统 仅模式一正常水果生效====
  public ItemType? LastFruitType { get; private set; }
  public int ComboCount { get; private set; }
  private float _comboLastPickTime; //上一次吃到加分水果的时间

  private Camera cam;
  private int activePointer = int.MinValue;
  private Vector3 grabOffset;
  static readonly Dictionary<int, PlayerController> pointerOwner = new Dictionary<int, PlayerController>();

  private float minX, maxX, minY, maxY;
  private float invincibleTimer = 0f;
  private float stunTimer = 0f;

  public bool IsStunned => stunTimer > 0f;
  public bool IsInvincible => invincibleTimer > 0f;

  private SpriteRenderer[] spriteRenderers;

  void Awake()
  {
    cam = Camera.main;
    All.Add(this);
    spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
  }

  void OnDestroy()
  {
    All.Remove(this);
  }

  void Start()
  {
    if (playerIndex == 0)
    {
      minX = ViewportToWorldX(-0.05f);
      maxX = ViewportToWorldX(0.5f);
    }
    else
    {
      minX = ViewportToWorldX(0.51f);
      maxX = ViewportToWorldX(1.1f);
    }
    minY = ViewportToWorldY(0.12f);
    maxY = ViewportToWorldY(1f);

    HideCombo();
    _comboLastPickTime = -999f;
  }

  float ViewportToWorldX(float vx)
      => cam.ViewportToWorldPoint(new Vector3(vx, 0.5f, 0f)).x;

  float ViewportToWorldY(float vy)
      => cam.ViewportToWorldPoint(new Vector3(0.5f, vy, 0f)).y;

  // ---------- 数据接口 ----------
  public void ResetScore()
  {
    Score = 0;
  }

  public void AddScore(int v)
  {
    int old = Score;
    Score += v;
    int delta = Score - old;

    //【关键】任务模式不生成分数飘字
    if (delta != 0 && GameManager.Instance.currentMode == GameMode.FruitCatch)
    {
      ShowFloatingScore(delta);
    }
  }

  /// <summary>任务模式初始化剩余收集数量</summary>
  public void InitMissionCount(MissionData data)
  {
    RemainBanana = data.bananaCount;
    RemainApple = data.appleCount;
    RemainMelon = data.melonCount;
  }

  /// <summary>任务模式：吃到水果消耗任务计数，调用后刷新UI</summary>
  public void ConsumeMissionItem(ItemType t)
  {
    if (GameManager.Instance == null || GameManager.Instance.currentMode != GameMode.TaskMission)
      return;

    switch (t)
    {
      case ItemType.Banana:
        if (RemainBanana > 0) RemainBanana--;
        break;
      case ItemType.Apple:
        if (RemainApple > 0) RemainApple--;
        break;
      case ItemType.Melon:
        if (RemainMelon > 0) RemainMelon--;
        break;
    }
    GameManager.Instance.RefreshMissionUI();
  }

  /// <summary>判断该玩家任务是否全部完成</summary>
  public bool IsMissionComplete()
  {
    return RemainBanana <= 0 && RemainApple <= 0 && RemainMelon <= 0;
  }

  void ShowFloatingScore(int deltaValue)
  {
    if (floatingScoreTMPPrefab == null || cam == null) return;

    Vector2 playerWorldPos = transform.position;
    Vector2 screenPoint = cam.WorldToScreenPoint(playerWorldPos);

    Canvas canvas = Object.FindObjectOfType<Canvas>();
    if (canvas == null) return;

    GameObject go = Object.Instantiate(floatingScoreTMPPrefab, canvas.transform);
    FloatingScoreText fs = go.GetComponent<FloatingScoreText>();
    if (fs == null)
    {
      Object.Destroy(go);
      return;
    }

    RectTransform rt = go.GetComponent<RectTransform>();
    RectTransform canvasRt = canvas.GetComponent<RectTransform>();
    Vector2 anchoredPos;

    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        canvasRt,
        screenPoint,
        canvas.worldCamera,
        out anchoredPos);

    anchoredPos += floatingTextOffset;
    rt.anchoredPosition = anchoredPos;

    string showStr = deltaValue > 0 ? $"+{deltaValue}" : $"{deltaValue}";
    fs.SetText(showStr);
  }

  public void ResetCombo()
  {
    LastFruitType = null;
    ComboCount = 0;
    _comboLastPickTime = -999f;
    HideCombo();
  }

  public void HideCombo()
  {
    if (comboImage != null) comboImage.gameObject.SetActive(false);
    if (comboText != null) comboText.gameObject.SetActive(false);
  }

  public void ShowCombo(ItemType fruitType, int comboCnt)
  {
    if (comboImage == null || comboText == null) return;

    Sprite targetSprite = null;
    if (GameManager.Instance != null)
    {
      switch (fruitType)
      {
        case ItemType.Banana: targetSprite = GameManager.Instance.bananaSprite; break;
        case ItemType.Apple: targetSprite = GameManager.Instance.appleSprite; break;
        case ItemType.Melon: targetSprite = GameManager.Instance.melonSprite; break;
      }
    }
    if (targetSprite == null) return;

    comboImage.sprite = targetSprite;
    comboImage.gameObject.SetActive(true);
    comboText.text = $"×{comboCnt}";
    comboText.gameObject.SetActive(true);

    if (comboAnimator != null)
    {
      comboAnimator.SetTrigger("PlayPop");
    }
  }

  public void BreakCombo()
  {
    ResetCombo();
  }

  public int CalculateComboScore(ItemType currentFruit, int baseScore)
  {
    // 仅模式一处理连击
    if (GameManager.Instance.currentMode == GameMode.FruitCatch)
    {
      // 吃到水果瞬间也再校验一次超时（双重保险）
      float now = Time.time;
      if (LastFruitType.HasValue)
      {
        float deltaTime = now - _comboLastPickTime;
        if (deltaTime > comboExpireTime)
        {
          ResetCombo();
        }
      }

      if (LastFruitType.HasValue && LastFruitType.Value == currentFruit)
      {
        ComboCount += 1;
      }
      else
      {
        LastFruitType = currentFruit;
        ComboCount = 1;
      }
      _comboLastPickTime = now;
    }

    int add = baseScore + (ComboCount - 1) * 2;

    if (GameManager.Instance.currentMode == GameMode.FruitCatch)
    {
      ShowCombo(currentFruit, ComboCount);
    }
    return add;
  }

  /// <summary>模式一：吃到香蕉皮，原地眩晕，打断连击</summary>
  public void Stun(float duration)
  {
    stunTimer = Mathf.Max(stunTimer, duration);
    ReleasePointer();
    GorillaFaceAnim faceAnim = GetComponentInChildren<GorillaFaceAnim>();
    if (faceAnim != null) faceAnim.PlayAnim("Stun");
    BreakCombo();
  }

  /// <summary>模式一吃到炸弹，仅触发玩家闪烁，不扣血、不产生无敌判定；打断连击</summary>
  public void TriggerBombFlash()
  {
    invincibleTimer = invincibleDuration;
    BreakCombo();
  }

  void Update()
  {
    //无敌闪烁
    if (invincibleTimer > 0f)
    {
      invincibleTimer -= Time.deltaTime;
      float alpha = Mathf.PingPong(Time.time * 12f, 1f) > 0.5f ? 0.3f : 1f;
      SetAlpha(alpha);
      if (invincibleTimer <= 0f)
      {
        SetAlpha(1f);
      }
    }

    if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
    {
      ReleasePointer();
      return;
    }

    //【修复】每帧检测连击超时，4秒没吃到水果自动清除连击
    if (GameManager.Instance.currentMode == GameMode.FruitCatch && LastFruitType.HasValue)
    {
      float delta = Time.time - _comboLastPickTime;
      if (delta > comboExpireTime)
      {
        ResetCombo();
      }
    }

    //眩晕期间禁止移动
    if (stunTimer > 0f)
    {
      stunTimer -= Time.deltaTime;
      ReleasePointer();
      return;
    }

    //触摸输入
    for (int i = 0; i < Input.touchCount; i++)
    {
      Touch t = Input.GetTouch(i);
      HandlePointer(t.fingerId, t.position, t.phase);
    }
    //鼠标输入
    if (Input.touchCount == 0)
    {
      if (Input.GetMouseButtonDown(0))
        HandlePointer(-1, Input.mousePosition, TouchPhase.Began);
      else if (Input.GetMouseButton(0))
        HandlePointer(-1, Input.mousePosition, TouchPhase.Moved);
      else if (Input.GetMouseButtonUp(0))
        HandlePointer(-1, Input.mousePosition, TouchPhase.Ended);
    }
  }

  void SetAlpha(float a)
  {
    if (spriteRenderers == null) return;
    for (int i = 0; i < spriteRenderers.Length; i++)
    {
      if (spriteRenderers[i] == null) continue;
      Color c = spriteRenderers[i].color;
      c.a = a;
      spriteRenderers[i].color = c;
    }
  }

  void HandlePointer(int id, Vector2 screenPos, TouchPhase phase)
  {
    Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
    world.z = 0f;

    switch (phase)
    {
      case TouchPhase.Began:
        if (activePointer == int.MinValue && !pointerOwner.ContainsKey(id))
        {
          if (Vector2.Distance(world, transform.position) <= dragRadius)
          {
            activePointer = id;
            pointerOwner[id] = this;
            grabOffset = transform.position - world;
            grabOffset.z = 0f;
          }
        }
        break;
      case TouchPhase.Moved:
      case TouchPhase.Stationary:
        if (activePointer == id)
        {
          Vector3 target = world + grabOffset;
          target.x = Mathf.Clamp(target.x, minX, maxX);
          target.y = Mathf.Clamp(target.y, minY, maxY);
          target.z = 0f;
          transform.position = target;
        }
        break;
      case TouchPhase.Ended:
      case TouchPhase.Canceled:
        if (activePointer == id)
        {
          pointerOwner.Remove(id);
          activePointer = int.MinValue;
        }
        break;
    }
  }

  void ReleasePointer()
  {
    if (activePointer != int.MinValue)
    {
      pointerOwner.Remove(activePointer);
      activePointer = int.MinValue;
    }
  }
}
