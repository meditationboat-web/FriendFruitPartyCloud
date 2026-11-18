using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
  public static ItemSpawner Instance;

  private const int PlayerCount = 2;
  private const int MaxItemsOnScreen = 12;

  // 摄像机到游戏平面的深度距离（如果水果出屏或变形，调整这个值，通常与UI的Plane Distance一致）
  private const float CameraDepth = 10f;

  [Header("模式一：物品预制体 0香蕉 1苹果 2哈密瓜 3香蕉皮 4炸弹")]
  public GameObject[] itemPrefabs = new GameObject[5];

  [Header("模式一生成权重：香蕉 / 苹果 / 哈密瓜 / 香蕉皮 / 炸弹")]
  public float[] weights = { 30f, 20f, 20f, 20f, 15f };

  private float[] _originalWeights;
  private readonly float[] _timers = new float[PlayerCount];
  private readonly float[] _pauseTimers = new float[PlayerCount];
  private Camera _cam;

  void Awake()
  {
    Instance = this;
    _cam = Camera.main;
    _originalWeights = (float[])weights.Clone();
  }

  public void ResetTimers()
  {
    ClearAll();
    for (int p = 0; p < PlayerCount; p++)
    {
      _timers[p] = Random.Range(0.3f, 0.6f);
      _pauseTimers[p] = 0f;
    }
  }

  public void ClearAll()
  {
    ItemController[] items = FindObjectsOfType<ItemController>(includeInactive: true);
    foreach (var item in items) if (item != null) item.ForceRemove();
  }

  public void ClearSide(int playerIndex)
  {
    ItemController[] items = FindObjectsOfType<ItemController>(includeInactive: true);
    foreach (var item in items)
    {
      if (item != null && item.Owner == playerIndex) item.ForceRemove();
    }
  }

  public void PauseSpawn(int playerIndex, float duration)
  {
    if (playerIndex < 0 || playerIndex >= PlayerCount) return;
    _pauseTimers[playerIndex] = Mathf.Max(_pauseTimers[playerIndex], duration);
    ClearSide(playerIndex);
  }

  void Update()
  {
    if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
    if (GameManager.Instance.IsTutorialPhase) return;

    for (int p = 0; p < PlayerCount; p++)
    {
      if (_pauseTimers[p] > 0f)
      {
        _pauseTimers[p] -= Time.deltaTime;
        continue;
      }

      _timers[p] -= Time.deltaTime;
      if (_timers[p] <= 0f)
      {
        TrySpawn(p);
        _timers[p] = GetSpawnInterval();
      }
    }
  }

  float GetSpawnInterval()
  {
    float remain = GameManager.Instance.TimeLeft;
    if (remain <= 10f) return Random.Range(0.4f, 0.7f);
    if (remain > 40f) return Random.Range(1.0f, 1.5f);
    return Random.Range(0.7f, 1.0f);
  }

  float[] GetCurrentWeights()
  {
    if (GameManager.Instance == null || GameManager.Instance.currentMode != GameMode.TaskMission)
      return weights;

    var p1 = GameManager.Instance.player1;
    var p2 = GameManager.Instance.player2;
    if (p1 == null || p2 == null) return _originalWeights;

    float[] w = (float[])_originalWeights.Clone();

    bool bananaDone = p1.RemainBanana <= 0 && p2.RemainBanana <= 0;
    bool appleDone = p1.RemainApple <= 0 && p2.RemainApple <= 0;
    bool melonDone = p1.RemainMelon <= 0 && p2.RemainMelon <= 0;

    if (bananaDone) ReduceFruitWeight(w, 0);
    if (appleDone) ReduceFruitWeight(w, 1);
    if (melonDone) ReduceFruitWeight(w, 2);

    for (int i = 0; i < 3; i++) if (w[i] < 0f) w[i] = 0f;
    return w;
  }

  static void ReduceFruitWeight(float[] w, int reduceIndex)
  {
    w[reduceIndex] -= 5f;
    for (int i = 0; i < 3; i++)
      if (i != reduceIndex) w[i] += 2.5f;
  }

  ItemType PickType()
  {
    float[] useWeights = GetCurrentWeights();
    float total = 0f;
    foreach (var w in useWeights) total += w;

    if (total <= 0f) return ItemType.Bomb;

    float rnd = Random.Range(0f, total);
    float acc = 0f;
    for (int i = 0; i < useWeights.Length; i++)
    {
      acc += useWeights[i];
      if (rnd <= acc) return (ItemType)i;
    }
    return ItemType.Bomb;
  }

  void TrySpawn(int player)
  {
    if (ItemController.ActiveCount >= MaxItemsOnScreen) return;
    if (itemPrefabs == null || itemPrefabs.Length < 5 || _cam == null) return;

    ItemType type = PickType();
    int moveMode = Random.Range(0, 4);

    Vector3 viewPort = GetSpawnViewport(moveMode, player, out Vector2 dir);

    // 【修复核心】：必须传入一个正数的 Z 值，否则在透视相机下所有水果都会挤压在屏幕中缝
    viewPort.z = CameraDepth;

    Vector3 worldPos = _cam.ViewportToWorldPoint(viewPort);
    worldPos.z = 0f; // 游戏内 2D 平面保持为 0

    GameObject go = Instantiate(itemPrefabs[(int)type], worldPos, Quaternion.identity);
    ItemController ic = go.GetComponent<ItemController>() ?? go.GetComponentInChildren<ItemController>(true);

    if (ic == null)
    {
      Destroy(go);
      return;
    }

    ic.Init(type, moveMode, dir, player);
  }

  Vector3 GetSpawnViewport(int moveMode, int player, out Vector2 dir)
  {
    float minX = player == 0 ? 0.08f : 0.58f;
    float maxX = player == 0 ? 0.42f : 0.92f;
    dir = Vector2.down;

    switch (moveMode)
    {
      case 0: return new Vector3(Random.Range(minX, maxX), 1.15f, 0f);
      case 1:
        if (player == 0) { dir = Vector2.right; return new Vector3(-0.15f, Random.Range(0.2f, 0.85f), 0f); }
        else { dir = Vector2.left; return new Vector3(1.15f, Random.Range(0.2f, 0.85f), 0f); }
      case 2:
        float dx = Random.Range(-0.8f, 0.8f);
        dir = new Vector2(dx, -1f).normalized;
        return new Vector3(Random.Range(minX, maxX), 1.15f, 0f);
      case 3:
      default:
        return new Vector3(Random.Range(minX + 0.05f, maxX - 0.05f), 1.15f, 0f);
    }
  }
}
