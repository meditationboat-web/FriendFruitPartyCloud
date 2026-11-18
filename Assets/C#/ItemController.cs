using UnityEngine;

public enum ItemType
{
  Banana = 0, Apple = 1, Melon = 2, BananaPeel = 3, Bomb = 4
}

public class ItemController : MonoBehaviour
{
  public static int ActiveCount = 0;
  public static readonly int[] Scores = { 10, 5, 5, 0, -5 };

  [Header("与玩家的碰撞判定半径")]
  public float hitRadius = 0.5f;
  [Header("自转速度范围（度/秒，取绝对值）")]
  public float spinSpeedMin = 60f;
  public float spinSpeedMax = 220f;

  private ItemType _type;
  private int _moveMode;
  private Vector2 _moveDir;
  private float _speed;
  private int _owner;
  public int Owner => _owner;

  /// <summary>是否为新手教学水果：不计分、不消耗任务、不移动</summary>
  public bool IsTutorial { get; private set; }

  private float _minViewX, _maxViewX;
  private float _baseX;
  private float _sineAmp, _sineFreq, _sinePhase;
  private float _age;
  private float _spinSpeed;

  private Camera _cam;
  private bool _done;
  private bool _initialized;
  private bool _hasBeenInside;

  void Awake()
  {
    _cam = Camera.main;
    ActiveCount++;
  }

  void OnDestroy() => ActiveCount--;

  public void Init(ItemType t, int mode, Vector2 dir, int playerIndex)
  {
    IsTutorial = false;
    _initialized = true;
    _type = t;
    _moveMode = mode;
    _moveDir = dir.normalized;
    _owner = playerIndex;
    _speed = Random.Range(2.0f, 3.0f);

    SetOwnerBounds();

    _baseX = transform.position.x;
    _sineAmp = Random.Range(0.5f, 1.2f);
    _sineFreq = Random.Range(1.5f, 3.0f);
    _sinePhase = Random.Range(0f, Mathf.PI * 2f);
    _spinSpeed = Random.Range(spinSpeedMin, spinSpeedMax);
    if (Random.value < 0.5f) _spinSpeed = -_spinSpeed;

    transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
    _age = 0f; _done = false; _hasBeenInside = false;
  }

  /// <summary>新手教学水果：固定位置、不移动</summary>
  public void InitTutorial(ItemType t, int playerIndex)
  {
    IsTutorial = true;
    _initialized = true;
    _type = t;
    _moveMode = -1;
    _moveDir = Vector2.down;
    _owner = playerIndex;
    _speed = 0f;

    SetOwnerBounds();

    _baseX = transform.position.x;
    _sineAmp = 0f; _sineFreq = 0f; _sinePhase = 0f;
    _spinSpeed = 0f;

    transform.rotation = Quaternion.identity;
    _age = 0f; _done = false; _hasBeenInside = false;
  }

  void SetOwnerBounds()
  {
    if (_owner == 0) { _minViewX = 0.05f; _maxViewX = 0.45f; }
    else { _minViewX = 0.55f; _maxViewX = 0.95f; }
  }

  public void ForceRemove()
  {
    if (_done) return;
    _done = true;
    Destroy(gameObject);
  }

  void Update()
  {
    if (!_initialized || _done) return;

    if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
    {
      ForceRemove();
      return;
    }

    float dt = Time.deltaTime;
    _age += dt;

    transform.Rotate(0f, 0f, _spinSpeed * dt, Space.Self);
    MoveItem(dt);
    CheckHit();
    CheckOutOfBounds();
  }

  void MoveItem(float dt)
  {
    Vector3 pos = transform.position;
    switch (_moveMode)
    {
      case 0: pos += (Vector3)(Vector2.down * _speed * dt); break;
      case 1:
      case 2: pos += (Vector3)(_moveDir * _speed * dt); break;
      case 3:
        pos.y -= _speed * dt;
        pos.x = _baseX + Mathf.Sin(_age * _sineFreq + _sinePhase) * _sineAmp;
        break;
    }
    transform.position = pos;
  }

  void CheckHit()
  {
    if (_done) return;

    foreach (PlayerController p in PlayerController.All)
    {
      float dist = Vector2.Distance(p.transform.position, transform.position);
      if (dist > p.hitRadius + hitRadius) continue;

      // 统一判定：必须张嘴才能吃掉
      if (CanBeEatenByMouth(p))
      {
        // 【修改】把 IsTutorial 标记一起传给 Manager
        DualFaceToGorillaManager.Instance.AddItemToMouth(p.playerIndex, _type, IsTutorial);
        ForceRemove();
        return;
      }

      // 没张嘴，水果直接穿透，什么都没有发生
    }
  }

  bool CanBeEatenByMouth(PlayerController p)
  {
    if (DualFaceToGorillaManager.Instance == null) return false;

    return (p.playerIndex == 0)
        ? DualFaceToGorillaManager.Instance.player1IsMouthOpen
        : DualFaceToGorillaManager.Instance.player2IsMouthOpen;
  }

  void CheckOutOfBounds()
  {
    if (_done || _cam == null) return;

    Vector3 vp = _cam.WorldToViewportPoint(transform.position);
    bool insideX = vp.x >= _minViewX && vp.x <= _maxViewX;
    bool insideY = vp.y >= -0.25f && vp.y <= 1.35f;

    if (insideX && insideY)
    {
      _hasBeenInside = true;
      return;
    }
    if (!_hasBeenInside) return;

    _done = true;
    Destroy(gameObject);
  }
}
