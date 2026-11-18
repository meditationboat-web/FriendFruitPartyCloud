using UnityEngine;
using TMPro;

/// <summary>向上漂浮渐消失的加分飘字，生成后不再跟随父物体</summary>
public class FloatingScoreText : MonoBehaviour
{
    [Header("飘字向上移动速度")]
    public float moveUpSpeed = 120f;
    [Header("总存活时长")]
    public float lifeTime = 1.2f;
    [Header("开始透明的时间点")]
    public float fadeStartTime = 0.5f;

    private TextMeshProUGUI _tmp;
    private float _timer;
    private Color _originColor;

    void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();
        if (_tmp != null)
        {
            _originColor = _tmp.color;
        }
    }

    public void SetText(string content)
    {
        if (_tmp != null)
        {
            _tmp.text = content;
        }
        _timer = 0f;
    }

    void Update()
    {
        _timer += Time.deltaTime;

        //向上位移（UI rectTransform，单位像素）
        RectTransform rt = transform as RectTransform;
        rt.anchoredPosition += Vector2.up * moveUpSpeed * Time.deltaTime;

        //渐隐
        if (_timer >= fadeStartTime && _tmp != null)
        {
            float fadeProgress = (_timer - fadeStartTime) / (lifeTime - fadeStartTime);
            float alpha = Mathf.Lerp(_originColor.a, 0f, fadeProgress);
            Color c = _tmp.color;
            c.a = alpha;
            _tmp.color = c;
        }

        if (_timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}
